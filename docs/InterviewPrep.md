# IncidentIQ Interview Prep

## Architecture

- **Domain:** entities and business state.
- **Application:** use cases, orchestration and interfaces.
- **Infrastructure:** Cosmos, Service Bus and Azure OpenAI adapters.
- **API:** HTTP, authentication, DTOs and Problem Details.
- **Worker:** Change Feed relays and Service Bus consumers.
- **Web:** authenticated React engineer experience.

## Incident Submission

```text
POST /api/incidents
→ IncidentsController
→ CreateIncidentHandler
→ IIncidentSubmissionStore
→ Cosmos transactional batch:
   Incident + outbox
→ Change Feed
→ IncidentOutboxWorker
→ Service Bus
→ AnalyseIncidentWorker
→ AnalyseIncidentHandler
```

**Why an outbox?** Cosmos and Service Bus cannot share one transaction. Persisting the Incident and durable intent atomically prevents a saved Incident from losing its analysis request.

## Reliability

- Service Bus provides durable buffering, redelivery and DLQ.
- `AutoCompleteMessages = false` so messages complete only after successful processing.
- `CommandId` becomes stable `MessageId` for duplicate detection.
- Completed-state checks provide basic idempotency.
- This is **at-least-once with duplicate handling**, not exactly-once.
- ETags/optimistic concurrency could strengthen horizontal-scale idempotency.

### Message lock vs counters

- **Message lock:** temporarily gives one receiver ownership.
- **DeliveryCount:** Service Bus delivery attempts.
- **AttemptCount:** IncidentIQ processing metadata persisted in Cosmos.

## Cosmos Partitioning

`Incidents` uses `/incidentId` so related Incident, outbox and analysis documents can share a logical partition and participate in transactional batches.

## Runbook Indexing

```text
Runbook
→ Change Feed
→ index-runbook
→ IndexRunbookWorker
→ chunk
→ embed
→ RunbookChunks
```

`Runbooks` is source data; `RunbookChunks` is rebuildable retrieval data.

## Historical Incident Indexing

```text
Completed Incident
→ Change Feed
→ index-historical-incident
→ embed title/description/symptoms
→ HistoricalIncidentVectors
```

Previous AI analysis is not embedded; retrieval should represent the original Incident.

## Vector Retrieval

- Index and query embeddings must use the same vector space.
- Cosmos `VectorDistance` ranks matches.
- Distance is **not** AI confidence.
- Metadata filters narrow by service/environment.
- Infrastructure projection DTOs isolate Cosmos serialization details.

## Grounded RAG

```text
Incident/question
→ one embedding
→ historical Incident retrieval + Runbook retrieval
→ combined context
→ Azure OpenAI
→ structured response
→ validate HI-* / RB-* references
```

- `HI-*`: what happened previously.
- `RB-*`: operational guidance.
- JSON schema validates shape; Application validates request-specific references.

## Operational Assistant

- Current question drives retrieval.
- Recent conversation history supplies continuity only.
- Backend remains stateless.
- Evidence IDs are answer-scoped.

## Authentication

```text
User → Entra → React/MSAL → Bearer token → API
API / Worker → Managed Identity → Azure
```

- API validates Entra JWTs with `Microsoft.Identity.Web`.
- Controller endpoints require `access_as_user`.
- `401` = not authenticated.
- `403` = authenticated but not authorized.
- User tokens are not forwarded to Azure resources.
- Stage 14C adds Engineer/Admin roles.

## Good Trade-offs to Explain

- Eventual consistency in exchange for resilient async processing.
- Managed Identity instead of stored production credentials.
- Deterministic local AI for fast/offline development.
- Controlled evaluation rather than claiming model quality from anecdotes.
- Small portfolio scope: production-hardening ideas are documented but intentionally deferred.
