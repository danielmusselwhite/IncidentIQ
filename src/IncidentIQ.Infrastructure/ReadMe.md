# IncidentIQ.Infrastructure

`IncidentIQ.Infrastructure` contains the concrete implementations used by the Application layer to communicate with external systems.

It is responsible for persistence, messaging, Azure/OpenAI SDK integration, deterministic local AI, and technical concerns that should not live inside Domain or Application.

## Responsibilities

Current responsibilities include:

- Cosmos DB client configuration and local initialization.
- Incident persistence.
- Atomic Incident + analysis-outbox persistence.
- Atomic completed Incident + structured-analysis persistence.
- Persisted analysis point reads.
- Runbook source persistence and vectorised Runbook chunk persistence.
- Azure Service Bus client configuration plus `AnalyseIncidentCommand` and `IndexRunbookCommand` publishing.
- Azure OpenAI structured incident analysis and Runbook embedding generation.
- Deterministic development incident analysis and embedding generation.
- Azure AI timeout/retry/failure classification and structured telemetry.
- Azure authentication through `DefaultAzureCredential` where configured.
- Infrastructure dependency-injection registration.

## High-Level Structure

```text
IncidentIQ.Infrastructure/
├── AzureAI/
│   ├── AzureIncidentAnalyzer.cs
│   ├── DevelopmentDummyIncidentAnalyzer.cs
│   └── Embedding/
│       ├── AzureEmbeddingOptions.cs
│       ├── AzureEmbeddingGenerator.cs
│       └── DevelopmentDummyEmbeddingGenerator.cs
├── Messaging/
│   ├── AzureServiceBusIncidentAnalysisQueue.cs
│   ├── AzureServiceBusRunbookIndexQueue.cs
│   └── ServiceBusOptions.cs
├── Persistence/
│   └── Cosmos/
│       ├── CosmosOptions.cs
│       ├── CosmosInitializer.cs
│       ├── CosmosIncidentRepository.cs
│       ├── CosmosIncidentSubmissionStore.cs
│       ├── CosmosIncidentAnalysisStore.cs
│       ├── CosmosIncidentAnalysisReader.cs
│       ├── CosmosRunbookRepository.cs
│       ├── CosmosRunbookChunkStore.cs
│       └── Documents/
└── DependencyInjection.cs
```

## Cosmos DB

IncidentIQ uses the native Azure Cosmos DB SDK.

```text
IncidentIQ Database
├── Incidents          /incidentId
├── Runbooks           /id
├── RunbookChunks      /runbookId
└── ChangeFeedLeases   /id
```

The `Incidents` container stores multiple document types sharing the Incident partition:

```text
IncidentDocument
IncidentAnalysisOutboxDocument
IncidentAnalysisDocument
```

This supports two transactional batches:

```text
submission/retry
→ Incident + Outbox

successful analysis
→ Completed Incident + Analysis
```

`CosmosIncidentAnalysisReader` reads a persisted analysis using its deterministic ID (`analysis-{incidentId}`) and the raw Incident ID as partition key, giving an efficient point read.

`CosmosRunbookChunkStore` persists the derived Runbook search index. Chunk IDs are deterministic, all chunks for one Runbook share `/runbookId`, and replacement removes stale chunks when a Runbook becomes shorter or is re-indexed. `RunbookChunks` is created with a 1536-dimension `/embedding` cosine vector policy and `quantizedFlat` vector index.

## Transactional Outbox

Incident creation and deliberate retry operations persist both Incident state and an `AnalyseIncident` outbox request through `CosmosIncidentSubmissionStore`.

```text
API/Application
→ persist Incident + Outbox atomically
→ Cosmos Change Feed
→ IncidentOutboxWorker
→ IIncidentAnalysisQueue
→ Service Bus
```

This avoids the Cosmos + Service Bus dual-write failure mode.

## Service Bus

`AzureServiceBusIncidentAnalysisQueue` implements `IIncidentAnalysisQueue` and publishes `AnalyseIncidentCommand` to `analyse-incident`. `AzureServiceBusRunbookIndexQueue` implements `IRunbookIndexQueue` and publishes `IndexRunbookCommand` to `index-runbook`.

The API does not publish directly to Service Bus. Incident commands are relayed from the transactional outbox, while Runbook indexing commands are published by the Worker-side Runbooks Change Feed relay.

## Azure AI

### Development

```text
IIncidentAnalyzer
└── DevelopmentDummyIncidentAnalyzer
```

`AddDevelopmentAIDependencies()` is used when the Worker environment is `Development`. It returns deterministic structured analysis so the complete local workflow can be tested without Azure credentials or model cost.

### Azure / non-Development

```text
IIncidentAnalyzer
└── AzureIncidentAnalyzer
    ↓
Azure OpenAI ChatClient
```

`AzureIncidentAnalyzer`:

- Builds system/user chat messages from `IncidentAnalysisInput`.
- Requires the strict structured response schema.
- Deserializes and semantically validates the returned JSON.
- Maps Infrastructure response types to `IncidentAnalysisResult`.
- Uses an overall request timeout.
- Classifies throttling, service/client failures, timeout, and invalid model responses.
- Preserves caller cancellation semantics.
- Logs structured duration/success/failure metadata without logging the Incident payload or raw model response.

The Azure OpenAI client is long-lived and uses a bounded SDK retry policy plus an individual network timeout. Service Bus remains the durable outer retry mechanism.

Current defaults are:

```text
MaxRetries = 2
NetworkTimeoutSeconds = 60
RequestTimeoutSeconds = 90
```

### Runbook Embeddings

```text
IEmbeddingGenerator
├── DevelopmentDummyEmbeddingGenerator
└── AzureEmbeddingGenerator
    ↓
Azure OpenAI EmbeddingClient
    ↓
runbook-embedding / text-embedding-3-small
```

`AzureEmbeddingGenerator` requests the configured 1536 dimensions and validates the returned vector length before passing the provider-independent float vector back to Application. The deterministic Development implementation produces repeatable local vectors so the ingestion pipeline can run without Azure OpenAI.

## Authentication

Infrastructure supports two common modes:

```text
Docker Compose Development
→ emulator credentials / connection strings
→ DevelopmentDummyIncidentAnalyzer
→ DevelopmentDummyEmbeddingGenerator
```

```text
Azure / non-Development
→ DefaultAzureCredential
→ Managed Identity or developer Azure identity
→ Azure OpenAI / Cosmos / Service Bus RBAC
```

## Design Approach

- Application defines abstractions; Infrastructure implements them.
- Azure SDK types remain outside Domain/Application where practical.
- External-service configuration is bound through options classes.
- Long-lived SDK clients are registered and reused through dependency injection.
- Incident persistence and analysis persistence/read responsibilities remain separate.
- Resilience logic classifies and propagates failures rather than swallowing them, preserving Worker/Service Bus retry semantics.
