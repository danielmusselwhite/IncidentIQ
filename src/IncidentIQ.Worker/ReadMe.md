# IncidentIQ.Worker

`IncidentIQ.Worker` is the .NET background-processing host for IncidentIQ.

It currently runs four hosted services:

```text
IncidentOutboxWorker
└── Incidents Change Feed → analyse-incident

AnalyseIncidentWorker
└── analyse-incident → Application analysis workflow

RunbookIndexChangeFeedWorker
└── Runbooks Change Feed → index-runbook

IndexRunbookWorker
└── index-runbook → Application Runbook indexing workflow
```

Keeping relay/consumer responsibilities separate lets Incident analysis and Runbook indexing retry, evolve, and eventually scale independently.

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


The Runbook ingestion path is separate:

```text
Cosmos Runbooks container
          ↓
     Cosmos Change Feed
          ↓
RunbookIndexChangeFeedWorker
          ↓
    IRunbookIndexQueue
          ↓
 Service Bus: index-runbook
          ↓
    IndexRunbookWorker
          ↓
    IndexRunbookHandler
          ↓
      RunbookChunker
          ↓
   IEmbeddingGenerator
    ├── DevelopmentDummyEmbeddingGenerator
    └── AzureEmbeddingGenerator → text-embedding-3-small
          ↓
   IRunbookChunkStore
          ↓
Cosmos RunbookChunks
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

## `RunbookIndexChangeFeedWorker`

Responsible for detecting created/updated Runbooks and publishing asynchronous indexing work. It monitors the `Runbooks` Change Feed, uses an independent processor name/lease checkpoint, builds `IndexRunbookCommand`, and publishes through `IRunbookIndexQueue`. Existing Runbooks can be backfilled when this processor is introduced.

## `IndexRunbookWorker`

Responsible for consuming `index-runbook` commands. It deserializes the command, creates a DI scope per message, resolves scoped `IndexRunbookHandler`, completes only after chunking/embedding/persistence succeeds, and allows processing failures to flow into Service Bus redelivery/DLQ behaviour.

The handler reloads the current Runbook, generates deterministic overlapping chunks, calls `IEmbeddingGenerator`, and replaces the current `RunbookChunks` set.

## AI Implementation Selection

The Worker selects its analyzer from the host environment:

```text
DOTNET_ENVIRONMENT=Development
→ AddDevelopmentAIDependencies()
→ DevelopmentDummyIncidentAnalyzer
→ DevelopmentDummyEmbeddingGenerator

Non-Development
→ AddAzureAIDependencies(...)
→ AzureIncidentAnalyzer
→ AzureEmbeddingGenerator
```

This means local Docker development exercises the complete Incident-analysis and Runbook-ingestion messaging/persistence flows without calling Azure OpenAI.

## Reliability

Both Service Bus consumers settle messages only after their Application workflow succeeds:

```text
Incident analysis failure
→ analyzer/handler rethrows
→ Service Bus redelivery
→ final Incident failure handling + DLQ after retry exhaustion

Runbook indexing failure
→ chunking/embedding/persistence throws
→ Service Bus redelivery
→ DLQ after retry exhaustion

Malformed message
→ immediate DLQ
```

The real Incident analyzer has a small bounded SDK retry policy and request timeout, but Service Bus remains the durable outer retry mechanism. Runbook embedding/indexing failures likewise propagate to `IndexRunbookWorker` so the queue remains responsible for durable redelivery rather than failures being swallowed inside the ingestion pipeline.

See [Design Decisions & Trade-offs](../../docs/DESIGN-DECISIONS.md) for the detailed reasoning.

## AI Telemetry

`AzureIncidentAnalyzer` records structured success/failure logs containing analysis duration, model, deployment, and failure category. It deliberately avoids logging prompts, raw responses, and Incident payload fields.

Full distributed tracing, dependency metrics, dashboards, and KQL remain future work.

## Structure

```text
IncidentIQ.Worker/
├── IncidentOutboxWorker.cs
├── AnalyseIncidentWorker.cs
├── RunbookIndexChangeFeedWorker.cs
├── IndexRunbookWorker.cs
├── Program.cs
├── Dockerfile
├── appsettings.json
└── Properties/
```

`Program.cs` registers shared Application/Infrastructure dependencies, chooses environment-specific analyzer/embedding implementations, registers `AnalyseIncidentHandler` and `IndexRunbookHandler` as scoped, and hosts all four background services.

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

Runbook vector ingestion is now implemented locally. Next work adds Runbook vector retrieval/metadata filtering, followed by historical-Incident retrieval and evidence-backed RAG; later stages add full observability, completion events, operational tooling, and KEDA-based scaling.
