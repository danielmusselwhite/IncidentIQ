# IncidentIQ.Infrastructure

`IncidentIQ.Infrastructure` contains the concrete implementations that connect IncidentIQ's Application abstractions to external systems.

It owns provider-specific details for Cosmos DB, Service Bus and Azure OpenAI, plus deterministic Development implementations used to exercise AI/vector workflows locally.

## Responsibilities

Current responsibilities include:

- Cosmos DB client configuration and local initialization.
- Incident persistence.
- Atomic Incident + analysis-outbox persistence.
- Atomic completed Incident + structured-analysis persistence.
- Persisted analysis point reads.
- Runbook source persistence.
- Derived Runbook chunk/vector persistence.
- Cosmos vector retrieval over Runbook chunks.
- Mapping Cosmos vector-query projections into Application `RunbookChunkMatch` results.
- Retrieval latency and Cosmos Request Unit (RU) telemetry.
- Azure Service Bus client configuration plus `AnalyseIncidentCommand` and `IndexRunbookCommand` publishing.
- Azure OpenAI structured Incident analysis.
- Azure OpenAI Runbook/query embedding generation.
- Deterministic Development Incident analysis and embedding generation.
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
│       ├── CosmosRunbookChunkRetriever.cs
│       ├── CosmosRunbookChunkMatchResult.cs
│       └── Documents/
└── DependencyInjection.cs
```

The exact folder placement of small persistence projection types is an implementation detail; the architectural point is that Cosmos-specific result mapping remains inside Infrastructure.

## Cosmos DB

IncidentIQ uses the native Azure Cosmos DB SDK.

```text
IncidentIQ Database
├── Incidents          /incidentId
├── Runbooks           /id
├── RunbookChunks      /runbookId   (vector-enabled)
└── ChangeFeedLeases   /id
```

### Incident Persistence

The `Incidents` container stores multiple document types sharing the Incident partition:

```text
IncidentDocument
IncidentAnalysisOutboxDocument
IncidentAnalysisDocument
```

This supports two important transactional batches:

```text
submission/retry
→ Incident + Outbox

successful analysis
→ Completed Incident + Analysis
```

`CosmosIncidentAnalysisReader` reads a persisted analysis using its deterministic ID (`analysis-{incidentId}`) and the raw Incident ID as partition key, giving an efficient point read.

### Runbook Vector Index

`CosmosRunbookChunkStore` persists the derived Runbook search index. Chunk IDs are deterministic, all chunks for one Runbook share `/runbookId`, and replacement removes stale chunks when a Runbook becomes shorter or is re-indexed.

`RunbookChunks` is configured with:

```text
vector path        /embedding
vector type        float32
vector dimensions  1536
distance function  cosine
vector index       quantizedFlat
```

The source `Runbooks` container remains the editable system of record. `RunbookChunks` is rebuildable, derived search data.

### Runbook Vector Retrieval

`CosmosRunbookChunkRetriever` implements `IRunbookChunkRetriever` and translates a provider-independent query vector into a Cosmos vector query:

```text
IReadOnlyList<float> queryEmbedding
        ↓
Cosmos VectorDistance(c.embedding, @queryEmbedding)
        ↓
optional service metadata filter
        ↓
ORDER BY vector distance
        ↓
TOP @topK
        ↓
CosmosRunbookChunkMatchResult
        ↓
RunbookChunkMatch
```

The explicit projection/result mapping matters. Cosmos query rows are first materialised into `CosmosRunbookChunkMatchResult`, with JSON property mappings aligned to the query aliases, and are then converted to the Application model. This prevents a valid result row from silently producing empty/default `RunbookId`, `ChunkIndex`, `Title`, `Service`, `Content` or `Distance` values.

The retriever measures query latency and accumulates Cosmos Request Units (RUs). The returned `Distance` is cosine distance: lower is more similar; it is not a probability or confidence score.

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

`AzureServiceBusIncidentAnalysisQueue` implements `IIncidentAnalysisQueue` and publishes `AnalyseIncidentCommand` to `analyse-incident`.

`AzureServiceBusRunbookIndexQueue` implements `IRunbookIndexQueue` and publishes `IndexRunbookCommand` to `index-runbook`.

The API does not publish directly to Service Bus. Incident commands are relayed from the transactional outbox, while Runbook indexing commands are published by the Worker-side Runbooks Change Feed relay.

## Azure AI

### Incident Analysis

```text
IIncidentAnalyzer
├── DevelopmentDummyIncidentAnalyzer
└── AzureIncidentAnalyzer
    ↓
Azure OpenAI ChatClient
    ↓
incident-analysis / gpt-5-mini
```

`AzureIncidentAnalyzer` builds structured messages, requests the strict response schema, validates the returned JSON, maps provider-specific data into `IncidentAnalysisResult`, classifies failures and records safe structured telemetry.

The real analyzer has a bounded SDK retry policy and request timeout. Service Bus remains the durable outer retry mechanism for the asynchronous analysis workflow.

### Embeddings

```text
IEmbeddingGenerator
├── DevelopmentDummyEmbeddingGenerator
└── AzureEmbeddingGenerator
    ↓
Azure OpenAI EmbeddingClient
    ↓
runbook-embedding / text-embedding-3-small
```

`AzureEmbeddingGenerator` requests the configured 1536 dimensions and validates the returned vector length before returning a provider-independent float vector.

The same abstraction now serves two Stage 11 paths:

```text
Worker → embed Runbook chunks during asynchronous indexing
API    → embed search text before synchronous vector retrieval
```

In Development, the deterministic implementation is used by both sides so stored chunk vectors and query vectors are generated in the same vector space without Azure OpenAI cost.

## Authentication

```text
Docker Compose Development
→ emulator credentials / connection strings
→ deterministic Development AI + embeddings
```

```text
Azure / non-Development
→ DefaultAzureCredential
→ workload Managed Identity
→ Cosmos / Service Bus / Azure OpenAI RBAC as required by each host
```

By the end of Stage 11:

- The **Worker Managed Identity** accesses Cosmos, Service Bus and Azure OpenAI for Incident analysis and Runbook indexing.
- The **API Managed Identity** accesses Cosmos and Azure OpenAI so `GET /api/runbooks/search` can generate query embeddings and execute vector retrieval.

## Design Approach

- Application defines abstractions; Infrastructure implements them.
- Azure/Cosmos SDK types remain outside Domain/Application where practical.
- External-service configuration is bound through options classes.
- Long-lived SDK clients are registered and reused through dependency injection.
- Source Runbook persistence is separate from derived vector-index storage/retrieval.
- Provider-specific vector projection/deserialization is contained at the Infrastructure boundary.
- Resilience logic classifies and propagates failures rather than hiding them from the appropriate HTTP/Worker retry boundary.
