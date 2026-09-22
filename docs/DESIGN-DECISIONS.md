# Design Decisions & Trade-offs

This document records the decisions that materially shape IncidentIQ. Runtime details live in [Development](DEVELOPMENT.md) and [Runtime Flows](flows/README.md).

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

- Keeps HTTP requests fast.
- Allows independent retry, buffering and scaling.
- Incident state is eventually consistent: `Queued → Processing → Completed/Failed`.

## 3. Transactional Outbox Solves the Dual Write

Incident creation atomically writes:

```text
Incidents partition
├── Incident
└── IncidentAnalysisOutboxDocument
```

A Change Feed relay publishes the outbox command to Service Bus.

**Why:** avoids an Incident being saved while its analysis command is lost.

**Trade-off:** Change Feed is at-least-once, so duplicates must be safe.

## 4. At-Least-Once Delivery, Not Exactly-Once

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
- `IIncidentRepository` — source Incident state.
- `IIncidentSubmissionStore` — atomic Incident + outbox write.
- `IIncidentAnalysisStore` / `Reader` — analysis write/read.
- retrievers — semantic search.

**Why:** repositories do not become catch-all persistence/search/messaging interfaces.

## 7. Source Data and Vector Indexes Stay Separate

```text
Runbooks → RunbookChunks
Incidents → HistoricalIncidentVectors
```

Derived vectors can be rebuilt when chunking/models/indexes change without changing source records.

## 8. Indexing Uses Change Feed + Service Bus

Runbook and historical-Incident indexing are asynchronous.

**Why:** CRUD/completion paths do not wait for embeddings; indexing gets durable retry/DLQ behavior.

## 9. AI Capabilities Are Provider-Independent

Application owns:
- `IIncidentAnalyzer`
- `IEmbeddingGenerator`
- `IOperationalAssistant`

Infrastructure supplies deterministic Development and Azure implementations.

## 10. Structured Output + Semantic Validation

Azure OpenAI schemas validate response shape. Application code validates request-specific meaning, especially `HI-*` / `RB-*` references.

**Why:** a static schema cannot know which evidence was retrieved for one request.

## 11. Grounding Uses Two Evidence Types

- Historical Incidents: previous observations.
- Runbooks: operational guidance.

One query/Incident embedding is reused for both retrieval paths where appropriate.

**Trade-off:** retrieval ranking is a similarity signal, not calibrated AI confidence.

## 12. Assistant Conversation Is Stateless on the Backend

React keeps recent conversation state and resends it with each request.

- Current question drives retrieval.
- History provides language continuity only.
- Evidence remains answer-scoped.

Persisted conversations are deferred until they provide enough product value to justify ownership/retention rules.

## 13. AI Retry and Telemetry Stay Bounded

- Azure SDK: small transient retry policy.
- Adapter: overall timeout + failure classification.
- Service Bus: durable outer retry for asynchronous work.
- Logs record metadata, not raw prompts/evidence/model responses.

## 14. User Identity and Workload Identity Are Separate

```text
User → Entra → React/MSAL → API
API / Worker → Managed Identity → Azure resources
```

- API validates Entra JWTs and requires `access_as_user`.
- Managed Identities access Cosmos, Service Bus, Azure OpenAI and ACR.
- User access tokens are not forwarded to Azure dependencies.

Entra app registrations are stable tenant bootstrap configuration; normal Azure infrastructure remains in Bicep.

## 15. Bootstrap Infrastructure Is Separate

`rg-incidentiq-bootstrap` holds GitHub deployment identity/OIDC. `rg-incidentiq-dev` is disposable.

**Why:** the dev environment can be destroyed to reduce cost without recreating GitHub federation each time.

## Current Accepted Trade-offs

- Basic rather than concurrency-safe idempotency.
- No complete attempt-history audit.
- No automatic DLQ replay.
- No outbox retention cleanup yet.
- Source and derived vector stores are eventually consistent.
- Assistant conversations are browser-only.
- Full distributed tracing/KEDA are Stage 15.
