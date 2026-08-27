# IncidentIQ.Worker

`IncidentIQ.Worker` is the .NET background-processing host for IncidentIQ.

It currently runs two separate hosted services:

```text
IncidentOutboxWorker
└── Cosmos Change Feed → Service Bus

AnalyseIncidentWorker
└── Service Bus → Application analysis workflow
```

Keeping these responsibilities separate allows the outbox relay and incident analysis pipeline to evolve independently.

## Current Flow

```text
Cosmos Incidents container
├── IncidentDocument
└── IncidentAnalysisOutboxDocument
          ↓
     Cosmos Change Feed
          ↓
   IncidentOutboxWorker
          ↓
   IIncidentAnalysisQueue
          ↓
 Service Bus: analyse-incident
          ↓
   AnalyseIncidentWorker
          ↓
   AnalyseIncidentHandler
          ↓
 CosmosIncidentRepository
          ↓
Queued → Processing → Completed / Failed
```

## `IncidentOutboxWorker`

Responsible for relaying durable outbox entries to Service Bus.

It:

- Monitors the Cosmos `Incidents` container through the Change Feed Processor.
- Uses the `ChangeFeedLeases` container for ownership and checkpoints.
- Ignores normal Incident changes.
- Reads `AnalyseIncidentOutbox` documents.
- Converts each outbox document back into `AnalyseIncidentCommand`.
- Publishes through `IIncidentAnalysisQueue`.

The Change Feed is at-least-once, so duplicate relay is possible. Stable command IDs, Service Bus duplicate detection, and analysis idempotency provide complementary protection.

## `AnalyseIncidentWorker`

Responsible for consuming analysis commands from Service Bus.

It:

- Consumes the `analyse-incident` queue.
- Deserializes `AnalyseIncidentCommand`.
- Adds correlation, incident, and command IDs to logging scope.
- Invokes `AnalyseIncidentHandler`.
- Completes messages only after successful processing.
- Allows transient failures to be redelivered.
- Dead-letters permanently invalid messages immediately.
- Marks the Incident `Failed` and dead-letters the command after retry exhaustion.

## Reliability

Current processing behaviour is:

```text
Success
→ complete message

Transient failure
→ Service Bus redelivery

Invalid message
→ immediate DLQ

Retries exhausted
→ Incident Failed
→ DLQ

Already Completed
→ no-op
```

See [Design Decisions & Trade-offs](../../docs/DESIGN-DECISIONS.md) for the reasoning behind retries, DLQ handling, idempotency, and the outbox pattern.

## Structure

```text
IncidentIQ.Worker/
├── IncidentOutboxWorker.cs
├── AnalyseIncidentWorker.cs
├── Program.cs
├── Dockerfile
├── appsettings.json
└── Properties/
```

`Program.cs` registers Application and Infrastructure dependencies plus both hosted Worker services.

## Local Development

Run as part of Docker Compose:

```powershell
docker compose up --build
```

or directly against configured Azure resources:

```powershell
dotnet run --project src\IncidentIQ.Worker
```

See the [Development Guide](../../docs/DEVELOPMENT.md) for configuration.

## Planned Work

Later stages will replace the temporary analysis step with Azure AI/RAG processing and add richer telemetry, completion events, operational tooling, and KEDA-based scaling.
