# IncidentIQ — Design Decisions & Trade-offs

This document records the main reliability and messaging decisions in IncidentIQ and explains **why** they were chosen.

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

Transient processing failures use Service Bus redelivery rather than a custom retry scheduler.

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

**Trade-off:** retry policy is currently message-level. Later AI integration may need more specific handling for throttling, timeouts, invalid input, or permanent model failures.

The DLQ preserves failed work for later investigation and admin retry/requeue rather than silently discarding it.

## 4. Basic Idempotency and Duplicate Detection

IncidentIQ assumes messages may be delivered more than once.

An Incident already in `Completed` state is treated as a no-op. An Incident in `Processing` can be attempted again because that may represent a legitimate retry.

Each `AnalyseIncidentCommand` also has a stable `CommandId`, which is used as the Service Bus `MessageId`.

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

**Trade-off:** the current state check is intentionally basic. Cosmos ETags/optimistic concurrency or stronger side-effect idempotency can be added before high-concurrency Worker scaling.

## 5. Cosmos + Service Bus Dual-Write Problem

The original flow performed two independent writes:

```text
Create Incident in Cosmos
      ↓
Publish Service Bus command
```

If Cosmos succeeded and Service Bus failed, the Incident could remain `Queued` with no durable analysis request.

A normal transaction cannot span Cosmos DB and Service Bus.

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

**Trade-off:** the `Incidents` container now contains multiple document types, so queries must use the `documentType` discriminator where appropriate.

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

**Why:** Change Feed avoids building custom polling, distributed locking, checkpointing, and multi-Worker coordination.

**Trade-off:** Change Feed is at-least-once. The same outbox record may be observed again, which is why stable command IDs and idempotent processing remain necessary.

Outbox documents are not currently updated with `Published = true`; Change Feed lease/checkpoint state tracks relay progress instead. Historical outbox cleanup can later be handled through TTL or another retention policy.

## 8. At-Least-Once by Design

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

This provides **at-least-once delivery with duplicate-safe processing**.

That is intentionally preferred over attempting a complex distributed exactly-once guarantee.

## 9. Remaining Reliability Work

Stage 8 still includes:

```text
Admin retry/requeue backend capability
Reliability and duplicate-message tests
```

A deliberate admin retry should create a **new** command ID so Service Bus duplicate detection does not suppress a legitimate new analysis run.

The retry UI itself belongs later in the Operations/Admin frontend stage.

## Current Accepted Trade-offs

- Basic rather than full idempotency.
- No complete attempt-history audit yet.
- No automatic DLQ reprocessing.
- No automatic outbox cleanup yet.
- Final failure persistence can still be affected by Cosmos availability.
- Stronger optimistic concurrency can be added before significant Worker scaling.

These are deliberate limits: the current design demonstrates realistic cloud reliability patterns without adding complexity before it is needed.
