# IncidentIQ Interview Prep

## Architecture

- **Domain:** entities and business state.
- **Application:** use cases, orchestration and interfaces.
- **Infrastructure:** Cosmos, Service Bus and Azure OpenAI adapters.
- **API:** HTTP, authentication/authorization, DTOs and Problem Details.
- **Worker:** Change Feed relays and Service Bus consumers.
- **Web:** authenticated React engineer/admin experience.

## Incident Submission

```text
POST /api/incidents
→ CreateIncidentHandler
→ IIncidentSubmissionStore
→ Cosmos transactional batch: Incident + outbox
→ Change Feed
→ IncidentOutboxWorker
→ Service Bus
→ AnalyseIncidentWorker
→ AnalyseIncidentHandler
```

**Why an outbox?** Cosmos and Service Bus cannot share one transaction. Persisting the Incident and durable intent atomically prevents a saved Incident from losing its analysis request.

## Reliability

- Service Bus provides durable buffering, redelivery and DLQ.
- `AutoCompleteMessages = false` completes only after successful processing.
- `CommandId` becomes stable `MessageId` for duplicate detection.
- Completed-state checks provide basic idempotency.
- This is **at-least-once with duplicate handling**, not exactly-once.
- ETags/optimistic concurrency could strengthen horizontal-scale idempotency.

**Message lock vs counters**
- message lock: temporary receiver ownership,
- `DeliveryCount`: Service Bus delivery attempts,
- `AttemptCount`: IncidentIQ processing metadata in Cosmos.

## Cosmos Partitioning

`Incidents` uses `/incidentId` so related Incident, outbox and analysis documents share a logical partition and can participate in transactional batches.

## Indexing & Retrieval

```text
Runbook → Change Feed → index-runbook → chunk/embed → RunbookChunks
Completed Incident → Change Feed → index-historical-incident → embed → HistoricalIncidentVectors
```

Source documents stay separate from rebuildable vectors. Cosmos `VectorDistance` ranks matches; it is not AI confidence. Metadata filters narrow retrieval by service/environment.

## Grounded RAG

```text
Incident/question
→ one embedding
→ historical Incident + Runbook retrieval
→ grounding context
→ Azure OpenAI
→ structured response
→ validate HI-* / RB-* references
```

- `HI-*`: previous operational observations.
- `RB-*`: operational guidance.
- JSON schema validates shape; Application validates request-specific references.

## Operational Assistant

- Current question drives retrieval.
- Recent browser-held history supplies continuity only.
- Backend remains stateless.
- Evidence IDs are answer-scoped.

## Authentication & Authorization

```text
User → Entra → React/MSAL → Bearer token → API
API / Worker → Managed Identity → Azure
```

- API validates Entra JWTs with `Microsoft.Identity.Web` and requires `access_as_user`.
- `Engineer` or `Administrator` can use normal product features.
- Operations and retry require `Administrator`.
- `401` means unauthenticated; `403` means authenticated but unauthorized.
- User tokens are not forwarded to Azure resources.

## Observability

The API request's W3C trace context is persisted through the Cosmos outbox so the asynchronous API → Change Feed → Service Bus → Worker workflow can remain one distributed trace.

Custom spans cover relay, analysis, retrieval, AI generation and persistence. Custom metrics cover queue wait, processing time, AI time, terminal failures and admin retries. API/Worker use distinct Application Insights role names.

## Scaling

The Worker Container App uses KEDA on `analyse-incident` queue depth:

```text
1–3 replicas
2 queued messages / target replica
15-second polling
```

Minimum replicas is 1 because the same host owns Cosmos Change Feed processors. Splitting relay and consumer hosts would enable scale-to-zero, but adds complexity without enough portfolio value.

## Good Trade-offs to Explain

- Eventual consistency for resilient asynchronous processing.
- Managed Identity instead of stored production credentials.
- At-least-once delivery rather than pretending exactly-once guarantees.
- Explicit trace propagation through the transactional outbox.
- Deterministic local AI for fast/offline development.
- Controlled evaluation rather than anecdotal model-quality claims.
- Application Insights for deep diagnostics rather than rebuilding Azure Monitor inside the product.
