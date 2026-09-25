# Design Decisions & Trade-offs

This document records the decisions that materially shape IncidentIQ. Runtime details live in [Development](DEVELOPMENT.md), [Runtime Flows](flows/README.md) and [Observability](OBSERVABILITY.md).

## 1. Clean Architecture Boundaries

- **Domain:** entities and business state.
- **Application:** use cases and interfaces.
- **Infrastructure:** Azure/Cosmos/Service Bus implementations.
- **API/Worker:** transport hosts.

**Why:** application logic stays testable and independent of Azure SDKs.

## 2. Incident Analysis Is Asynchronous

```text
API → durable work → Service Bus → Worker → analysis
```

This keeps HTTP requests short and allows buffering, retry and horizontal scaling. Incident state is eventually consistent: `Queued → Processing → Completed/Failed`.

## 3. Transactional Outbox Solves the Dual Write

Incident creation atomically writes the Incident and `IncidentAnalysisOutboxDocument` in the same Cosmos logical partition. A Change Feed relay publishes the command to Service Bus.

**Why:** Cosmos and Service Bus cannot participate in one transaction; the outbox prevents a saved Incident from losing its analysis request.

**Trade-off:** Change Feed is at-least-once, so duplicate processing must be safe.

## 4. Delivery Is At-Least-Once, Not Exactly-Once

Protection includes:
- stable `CommandId` → Service Bus `MessageId`,
- Service Bus duplicate detection,
- completed-state idempotency,
- bounded delivery attempts and DLQ.

**Trade-off:** concurrent Workers could still duplicate expensive work before one completes. ETags/optimistic concurrency are a future hardening option.

## 5. Analysis and Completed State Persist Atomically

A successful analysis writes the completed Incident and analysis/evidence documents in one Cosmos transactional batch.

**Why:** avoids `Completed` without an analysis, or an analysis without completed state.

## 6. Purpose-Specific Persistence Interfaces

Examples:
- `IIncidentRepository` — source Incident state,
- `IIncidentSubmissionStore` — atomic Incident + outbox write,
- `IIncidentAnalysisStore` / reader — analysis persistence,
- retrievers — semantic search.

**Why:** repositories do not become catch-all persistence/search/messaging interfaces.

## 7. Source Data and Vector Indexes Stay Separate

```text
Runbooks → RunbookChunks
Incidents → HistoricalIncidentVectors
```

Derived vectors can be rebuilt when chunking, models or indexes change without mutating source records.

## 8. Indexing Uses Change Feed + Service Bus

Runbook and historical-Incident indexing are asynchronous so CRUD/completion paths do not wait for embeddings and indexing receives durable retry/DLQ behaviour.

## 9. AI Capabilities Are Provider-Independent

Application owns `IIncidentAnalyzer`, `IEmbeddingGenerator` and `IOperationalAssistant`; Infrastructure supplies deterministic Development and Azure implementations.

## 10. Structured Output + Semantic Validation

Azure OpenAI schemas validate response shape. Application code validates request-specific meaning, especially `HI-*` / `RB-*` references.

**Why:** a static schema cannot know which evidence was retrieved for one request.

## 11. Grounding Uses Two Evidence Types

- Historical Incidents: previous observations.
- Runbooks: operational guidance.

One query/Incident embedding is reused for both retrieval paths where appropriate. Similarity is a ranking signal, not calibrated AI confidence.

## 12. Assistant Conversation Is Stateless on the Backend

React keeps recent conversation state and resends it with each request. The current question drives retrieval; history supplies language continuity only. Evidence remains answer-scoped.

## 13. AI Retry and Logging Stay Bounded

- Azure SDK: small transient retry policy.
- Adapter: overall timeout + failure classification.
- Service Bus: durable outer retry for asynchronous work.
- Logs record metadata, not raw prompts/evidence/model responses.

## 14. User Identity and Workload Identity Are Separate

```text
User → Entra → React/MSAL → API
API / Worker → Managed Identity → Azure resources
```

- API validates Entra JWTs, requires `access_as_user`, and enforces Engineer/Administrator roles.
- Managed Identities access Cosmos, Service Bus, Azure OpenAI and ACR.
- User access tokens are not forwarded to Azure dependencies.
- Entra app registrations remain tenant bootstrap configuration rather than Bicep-managed application infrastructure.

## 15. Observability Crosses the Outbox Boundary Explicitly

The API request's W3C `traceparent`/`tracestate` is persisted in the analysis command/outbox. The Worker restores that parent context before relaying to Service Bus and creates semantic spans around retrieval, AI generation and persistence.

**Why:** the Cosmos outbox is an asynchronous persistence boundary; correlation IDs alone do not create a continuous OpenTelemetry trace.

Custom metrics track queue wait, processing duration, AI duration, terminal failures and administrator retries. Detailed diagnostics remain in Application Insights rather than being recreated in the product UI.

## 16. Worker Scaling Uses Queue Depth, but Not Scale-to-Zero

The Worker Container App uses KEDA on the `analyse-incident` Service Bus queue:

```text
min replicas: 1
max replicas: 3
target:       2 queued messages / replica
```

**Why minimum 1:** the same host also runs Cosmos Change Feed processors that create Service Bus work. Scaling to zero would leave nobody observing Cosmos, so no queue message could wake the Worker.

**Trade-off:** scaling the Container App scales all hosted services together. Splitting relay and consumer hosts would enable true scale-to-zero but is intentionally outside portfolio scope.

## 17. Bootstrap Infrastructure Is Separate

`rg-incidentiq-bootstrap` holds GitHub deployment identity/OIDC. `rg-incidentiq-dev` is disposable.

**Why:** the dev environment can be destroyed to reduce cost without recreating GitHub federation each time.

## Current Accepted Trade-offs

- Basic rather than concurrency-safe idempotency.
- No complete attempt-history audit.
- No automatic DLQ replay.
- No outbox retention cleanup yet.
- Source and derived vector stores are eventually consistent.
- Assistant conversations are browser-only.
- Operations UI shows application state; deep telemetry remains in Azure Monitor/Application Insights.
- Change Feed relays and queue consumers share one Worker host, preventing scale-to-zero.
