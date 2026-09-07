# IncidentIQ — Design Decisions & Trade-offs

This document records the main architecture, reliability, messaging, persistence, and AI-integration decisions in IncidentIQ and explains **why** they were chosen.

For runtime setup see [DEVELOPMENT.md](DEVELOPMENT.md). For test coverage and manual verification see [tests/ReadMe.md](../tests/ReadMe.md).

## 1. Asynchronous Incident Processing

Incident analysis runs outside the HTTP request:

```text
API accepts Incident
      ↓
durable asynchronous work
      ↓
Worker processes analysis
```

**Why:** API requests stay responsive and analysis can retry or scale independently.

**Trade-off:** the system is eventually consistent, so an Incident may remain `Queued` or `Processing` before reaching a terminal state.

## 2. Processing State and Attempt Metadata

Current lifecycle:

```text
Queued → Processing → Completed
                  └──→ Failed
```

The Incident tracks `AttemptCount`, `LastAttemptAt`, `ProcessingStartedAt`, `CompletedAt`, `FailedAt`, and `FailureReason`.

**Why:** processing state and operational metadata remain visible in the application rather than existing only in logs.

**Trade-off:** this is not a full historical audit of every attempt.

## 3. Service Bus Retries and DLQ

Message-level processing failures use Service Bus redelivery rather than a custom application retry scheduler.

```text
Processing succeeds
→ complete message

Processing fails before max deliveries
→ leave unsettled / redeliver

Invalid payload
→ immediate DLQ

Final allowed processing failure
→ mark Incident Failed
→ DLQ
```

**Why:** Service Bus already provides durable retry and dead-letter semantics.

**Trade-off:** the outer retry unit is the complete analysis message. Azure AI performs only a small bounded set of SDK-level retries for short-lived transport/service faults before allowing the failure to propagate back to the Worker.

The DLQ preserves failed work for investigation and deliberate backend retry/requeue rather than silently discarding it.

## 4. Basic Idempotency and Duplicate Detection

IncidentIQ assumes messages may be delivered more than once.

An Incident already in `Completed` state is treated as a no-op. An Incident in `Processing` can be attempted again because that may represent a legitimate retry.

Each `AnalyseIncidentCommand` has a stable `CommandId`, which is used as the Service Bus `MessageId`.

```text
same CommandId
      ↓
same MessageId
      ↓
Service Bus duplicate detection
      +
Completed-state idempotency
```

**Why:** the goal is safe repeated delivery, not an unrealistic end-to-end exactly-once guarantee.

**Trade-off:** the current state check is intentionally basic. Cosmos ETags/optimistic concurrency or explicit command-processing records can strengthen protection before high-concurrency Worker scaling.

## 5. Cosmos + Service Bus Dual-Write Problem

The original flow performed two independent writes:

```text
Create Incident in Cosmos
      ↓
Publish Service Bus command
```

If Cosmos succeeded and Service Bus failed, the Incident could remain `Queued` with no durable analysis request. A normal transaction cannot span Cosmos DB and Service Bus.

## 6. Transactional Outbox

Incident creation now writes both documents atomically:

```text
Cosmos TransactionalBatch
├── IncidentDocument
└── IncidentAnalysisOutboxDocument
```

The API no longer publishes directly to Service Bus.

The `Incidents` container uses `/incidentId` so the Incident and its outbox document share one logical partition while retaining different document IDs.

```text
Incident
id = incident-123
incidentId = incident-123

Outbox
id = outbox-command-456
incidentId = incident-123
```

**Why:** either both the business state and durable analysis request are created, or neither is.

**Trade-off:** the `Incidents` container contains multiple document types, so queries use a `documentType` discriminator where appropriate.

## 7. Change Feed Outbox Relay

The durable outbox is relayed asynchronously:

```text
Incidents container
      ↓
Cosmos Change Feed
      ↓
IncidentOutboxWorker
      ↓
IIncidentAnalysisQueue
      ↓
Service Bus
```

`ChangeFeedLeases` is an SDK-managed Cosmos container used to track Change Feed ownership and checkpoint progress.

**Why:** Change Feed avoids custom polling, distributed locking, checkpointing, and multi-Worker coordination.

**Trade-off:** Change Feed is at-least-once. The same outbox record may be observed again, which is why stable command IDs and idempotent processing remain necessary.

Outbox documents are not currently updated with a `Published` flag; Change Feed lease/checkpoint state tracks relay progress. Historical outbox cleanup can later be handled through TTL or another retention policy.

## 8. Atomic Completed State + Analysis Persistence

A successful AI analysis changes two pieces of durable state:

```text
Incident → Completed
+
IncidentAnalysisDocument
```

`IIncidentAnalysisStore` is implemented by `CosmosIncidentAnalysisStore`, which persists both in one Cosmos transactional batch inside the Incident partition.

**Why:** the system should not expose `Completed` while the corresponding analysis document failed to persist, or persist an analysis while the Incident still appears incomplete.

**Trade-off:** this relies on keeping analysis documents in the same logical Incident partition.

## 9. Separate Analysis Read Path

Analysis retrieval is deliberately separate from `IIncidentRepository`:

```text
IIncidentRepository
→ Incident state

IIncidentAnalysisReader
→ persisted AI analysis
```

`CosmosIncidentAnalysisReader` performs a point read of the deterministic analysis document ID for the Incident partition.

**Why:** Incident persistence and AI-analysis persistence have different responsibilities and evolve independently.

## 10. Provider-Independent AI Boundary

`IIncidentAnalyzer` is defined in Application. Infrastructure provides the implementation:

```text
Development
→ DevelopmentDummyIncidentAnalyzer

Deployed / non-Development
→ AzureIncidentAnalyzer
```

`IncidentAnalysisResult`, `LikelyCause`, and `RecommendedAction` remain Application models rather than Azure SDK/domain models.

**Why:** Application owns the analysis use case without depending on Azure OpenAI types, and local development can exercise the full asynchronous pipeline without Azure credentials.

## 11. Structured AI Output

`AzureIncidentAnalyzer` requires a structured response schema, deserializes the model output into Infrastructure response types, performs semantic validation, and maps the result into `IncidentAnalysisResult`.

**Why:** downstream persistence/API/frontend code receives a predictable shape rather than arbitrary prose.

**Trade-off:** schema-valid output can still be semantically poor, so later stages add retrieval evidence and formal AI evaluation.

## 12. AI Resilience Boundaries

IncidentIQ deliberately avoids stacking multiple large retry layers.

```text
Azure AI SDK
→ small bounded retry policy
→ individual network timeout

AzureIncidentAnalyzer
→ overall request timeout
→ classify failure
→ rethrow

AnalyseIncidentWorker / Service Bus
→ durable message redelivery
→ DLQ after retry exhaustion
```

The analyzer classifies failures as timeout, throttled, service failure, client failure, or invalid response. Genuine caller/Worker cancellation remains `OperationCanceledException` and is not turned into an AI failure.

**Why:** SDK retries handle very short-lived service/network faults, while Service Bus remains the durable outer retry mechanism for the complete workflow.

**Trade-off:** a future circuit breaker or richer resilience pipeline may still be useful, but adding another retry layer now could multiply AI calls unnecessarily.

## 13. AI Telemetry Without Payload Logging

The Azure analyzer records structured success/failure logs including:

```text
DurationMs
FailureCategory
DeploymentName
ModelName
```

It deliberately does not log Incident descriptions, symptoms, prompts, or raw model responses.

**Why:** Stage 10 needs enough telemetry to diagnose latency/failure behaviour without unnecessarily recording potentially sensitive operational payloads.

Full dependency metrics, distributed tracing, dashboards, and KQL remain Stage 15 work.

## 14. At-Least-Once by Design

The reliability model combines:

```text
Transactional Outbox
        +
Change Feed checkpoints
        +
Stable CommandId / MessageId
        +
Service Bus duplicate detection
        +
Application state-based idempotency
```

This provides **at-least-once delivery with duplicate-safe processing** and is intentionally preferred over a complex distributed exactly-once guarantee.

## Current Accepted Trade-offs

- Basic rather than full concurrency-safe idempotency.
- No complete attempt-history audit yet.
- No automatic DLQ reprocessing; requeue is deliberate through the backend capability.
- No automatic outbox cleanup yet.
- Final failure persistence can still be affected by Cosmos availability.
- AI quality is currently based on the submitted Incident only; evidence-backed RAG begins in Stages 11–12.
- AI telemetry is intentionally lightweight until the full observability stage.
- Stronger optimistic concurrency can be added before significant Worker scaling.

These are deliberate limits: the current design demonstrates realistic cloud reliability patterns without adding complexity before it is needed.
