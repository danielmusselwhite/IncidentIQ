# IncidentIQ.Worker

`IncidentIQ.Worker` is the .NET background-processing host for IncidentIQ.

It runs two separate hosted services:

```text
IncidentOutboxWorker
└── Cosmos Change Feed → Service Bus

AnalyseIncidentWorker
└── Service Bus → Application analysis workflow
```

Keeping these responsibilities separate allows durable work relay and the expensive analysis pipeline to evolve/scale independently.

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
     IIncidentAnalyzer
      ├── DevelopmentDummyIncidentAnalyzer
      └── AzureIncidentAnalyzer → Azure OpenAI
          ↓
 IncidentAnalysisResult
          ↓
CosmosIncidentAnalysisStore
          ↓
Completed Incident + analysis atomically
```

## `IncidentOutboxWorker`

Responsible for relaying durable outbox entries to Service Bus.

It:

- Monitors the Cosmos `Incidents` container through the Change Feed Processor.
- Uses `ChangeFeedLeases` for ownership/checkpoints.
- Ignores normal Incident/analysis changes.
- Reads `AnalyseIncidentOutbox` documents.
- Converts each outbox document back into `AnalyseIncidentCommand`.
- Publishes through `IIncidentAnalysisQueue`.

The Change Feed is at-least-once, so duplicate relay is possible. Stable command IDs, Service Bus duplicate detection, and analysis idempotency provide complementary protection.

## `AnalyseIncidentWorker`

Responsible for consuming analysis commands from Service Bus.

It:

- Consumes the `analyse-incident` queue.
- Deserializes `AnalyseIncidentCommand`.
- Adds correlation, Incident, and command IDs to logging scope.
- Creates a new DI scope per message and resolves scoped `AnalyseIncidentHandler` inside that scope.
- Invokes `AnalyseIncidentHandler`, which calls `IIncidentAnalyzer` and atomically persists the completed Incident + structured analysis.
- Completes messages only after successful processing.
- Allows failures to propagate for Service Bus redelivery.
- Dead-letters permanently invalid messages immediately.
- Marks the Incident `Failed` and dead-letters after retry exhaustion.

## AI Implementation Selection

The Worker selects its analyzer from the host environment:

```text
DOTNET_ENVIRONMENT=Development
→ AddDevelopmentAIDependencies()
→ DevelopmentDummyIncidentAnalyzer

Non-Development
→ AddAzureAIDependencies(...)
→ AzureIncidentAnalyzer
```

This means local Docker development exercises the complete queue/persistence/API/frontend flow without calling Azure OpenAI.

## Reliability

```text
Success
→ complete message

Transient / classified AI failure
→ analyzer rethrows
→ Worker does not complete
→ Service Bus redelivery

Invalid message
→ immediate DLQ

Retries exhausted
→ Incident Failed
→ DLQ

Already Completed
→ no-op
```

The real Azure analyzer has a small bounded SDK retry policy and request timeout, but Service Bus remains the durable outer retry mechanism. This avoids stacking SDK + Polly + Service Bus retries and multiplying expensive AI calls.

See [Design Decisions & Trade-offs](../../docs/DESIGN-DECISIONS.md) for the detailed reasoning.

## AI Telemetry

`AzureIncidentAnalyzer` records structured success/failure logs containing analysis duration, model, deployment, and failure category. It deliberately avoids logging prompts, raw responses, and Incident payload fields.

Full distributed tracing, dependency metrics, dashboards, and KQL remain Stage 15 work.

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

`Program.cs` registers shared Application/Infrastructure dependencies, chooses the environment-specific AI implementation, registers `AnalyseIncidentHandler` as scoped, and hosts both Worker services.

## Local Development

Run as part of Docker Compose:

```powershell
docker compose up --build
```

The Compose Worker must set `DOTNET_ENVIRONMENT=Development` to use the deterministic analyzer.

To run directly:

```powershell
dotnet run --project src\IncidentIQ.Worker
```

See the [Development Guide](../../docs/DEVELOPMENT.md) for local emulator and real-Azure options.

## Planned Work

Later work adds Runbook/historical-Incident retrieval and RAG, full observability, completion events, operational tooling, and KEDA-based scaling.
