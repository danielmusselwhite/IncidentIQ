# IncidentIQ.Application

`IncidentIQ.Application` contains IncidentIQ's use cases, orchestration logic, provider-independent models and external-service abstractions.

It sits between the application hosts (`IncidentIQ.Api` and `IncidentIQ.Worker`) and the Domain model. Application defines **what the system needs to do**; Infrastructure supplies **how external systems do it**.

## Dependency Direction

```text
API / Worker
    ↓
Application command/query + handler/use case
    ↓
Domain rules + Application models
    ↓
Application abstraction
    ↓  implemented by
Infrastructure
    ↓
Cosmos / Service Bus / Azure OpenAI
```

The dependency arrow in source code points inward: Infrastructure references Application in order to implement its interfaces. Application does not reference Infrastructure.

## Why Repository, Store, Reader/Retriever and Queue Are Separate

The interfaces are named for the responsibility the Application layer needs, rather than for the Azure service that happens to implement them:

| Kind | Purpose | Examples |
|---|---|---|
| Repository | Load/save a source domain entity | `IIncidentRepository`, `IRunbookRepository` |
| Store | Perform a purpose-specific write/persistence operation | `IIncidentSubmissionStore`, `IIncidentAnalysisStore`, `IRunbookChunkStore` |
| Reader/Retriever | Purpose-specific read or search path | `IIncidentAnalysisReader`, `IRunbookChunkRetriever` |
| Queue | Publish a command without exposing the broker | `IIncidentAnalysisQueue`, `IRunbookIndexQueue` |
| AI abstraction | Ask for provider-independent AI/vector behaviour | `IIncidentAnalyzer`, `IEmbeddingGenerator` |

This prevents `IIncidentRepository` or `IRunbookRepository` becoming catch-all interfaces containing unrelated transactional, vector-search and messaging behaviour.

## Incident Use Cases

Current Incident use cases include:

```text
CreateIncident
GetIncidentById
GetAllIncidents
GetIncidentAnalysisById
AnalyseIncident
RetryAnalyseIncident
```

### Create Incident

```text
CreateIncidentHandler
      ↓
FluentValidation
      ↓
Incident.Create()
      ↓
create AnalyseIncidentCommand
      ↓
IIncidentSubmissionStore
```

`IIncidentSubmissionStore` represents one durable submission operation. Its Cosmos implementation atomically persists the Incident and analysis-outbox document.

### Analyse Incident

`AnalyseIncidentHandler` is invoked by `AnalyseIncidentWorker`:

```text
AnalyseIncidentCommand
      ↓
IIncidentRepository.GetByIdAsync
      ↓
StartProcessingAttempt
      ↓
persist Processing state
      ↓
IIncidentAnalyzer
      ↓
IncidentAnalysisResult
      ↓
MarkCompleted
      ↓
IIncidentAnalysisStore
      ↓
persist Completed Incident + analysis atomically
```

If processing ultimately exhausts retries, final-failure handling persists the terminal `Failed` state. Completed incidents are treated as a no-op to provide the current basic state-based idempotency boundary.

### Read Persisted Analysis

```text
GetIncidentAnalysisByIdHandler
      ↓
IIncidentAnalysisReader
      ↓
IncidentAnalysisResult?
```

The handler throws the Application-level not-found exception when no persisted analysis exists; the API translates that into Problem Details.

### AI Contracts

Provider-independent structured analysis models live in Application:

```text
IncidentAnalysisInput
IncidentAnalysisResult
LikelyCause
RecommendedAction
IIncidentAnalyzer
```

Azure SDK types remain outside Application.

## Runbook Use Cases

Stage 11 expands Runbooks from source CRUD into a separate derived vector index.

```text
Source Runbook use cases
├── CreateRunbook
├── GetRunbookById
├── GetAllRunbooks
├── UpdateRunbook
└── DeleteRunbook

Derived search/index concerns
├── IndexRunbookCommand
├── IndexRunbookHandler
├── RunbookChunker
├── RunbookChunk
├── RunbookChunkMatch
├── IRunbookChunkStore
├── IRunbookChunkRetriever
└── IEmbeddingGenerator
```

Runbook CRUD handlers use `IRunbookRepository` and remain independent of Cosmos DB implementation details. `DeleteRunbookHandler` also clears the derived vector index through `IRunbookChunkStore` before deleting the source Runbook.

### Index Runbook

`IndexRunbookHandler` is invoked by `IndexRunbookWorker` and orchestrates the provider-independent ingestion use case:

```text
IndexRunbookCommand
      ↓
IRunbookRepository.GetByIdAsync
      ↓
RunbookChunker
      ↓
IEmbeddingGenerator
      ↓
RunbookChunk[]
      ↓
IRunbookChunkStore.ReplaceForRunbookAsync
```

The command carries Runbook/revision identity rather than copying the full Runbook payload. The handler reloads the current source Runbook, builds deterministic overlapping chunks, generates one embedding per chunk and replaces the existing derived chunk set.

`RunbookChunk` is an Application indexing/search model rather than a Domain entity because chunk boundaries and vectors are derived retrieval concerns, not source business state.

### Retrieve Relevant Runbook Chunks

Stage 11B introduces a provider-independent vector-retrieval boundary:

```text
search text
   ↓
IEmbeddingGenerator
   ↓
query vector
   ↓
IRunbookChunkRetriever.RetrieveAsync(
    queryEmbedding,
    service?,
    topK)
   ↓
IReadOnlyList<RunbookChunkMatch>
```

`IRunbookChunkRetriever` expresses the Application requirement—retrieve the most relevant Runbook chunks—without exposing Cosmos SQL, `VectorDistance`, request-charge APIs or Cosmos SDK result types.

`RunbookChunkMatch` carries the retrieval data needed by callers, including Runbook identity, chunk identity/content, metadata and vector distance. Provider-specific projection objects such as `CosmosRunbookChunkMatchResult` stay in Infrastructure.

The current retriever supports top-K retrieval and optional service filtering. `Distance` is a ranking value where lower cosine distance means closer vector similarity; it is not an AI confidence percentage.

Stage 12 will reuse these Stage 11 retrieval primitives when building the combined Incident + Runbook RAG context. Stage 11 itself deliberately stops at retrieving relevant Runbook evidence.

## Important Abstractions and Implementations

```text
IIncidentRepository
└── CosmosIncidentRepository

IIncidentSubmissionStore
└── CosmosIncidentSubmissionStore

IIncidentAnalyzer
├── DevelopmentDummyIncidentAnalyzer
└── AzureIncidentAnalyzer

IIncidentAnalysisStore
└── CosmosIncidentAnalysisStore

IIncidentAnalysisReader
└── CosmosIncidentAnalysisReader

IRunbookRepository
└── CosmosRunbookRepository

IRunbookChunkStore
└── CosmosRunbookChunkStore

IRunbookChunkRetriever
└── CosmosRunbookChunkRetriever

IEmbeddingGenerator
├── DevelopmentDummyEmbeddingGenerator
└── AzureEmbeddingGenerator

IRunbookIndexQueue
└── AzureServiceBusRunbookIndexQueue

IIncidentAnalysisQueue
└── AzureServiceBusIncidentAnalysisQueue
```

`IIncidentAnalysisQueue` is used by the Incident outbox relay to publish the persisted `AnalyseIncidentCommand`. `IRunbookIndexQueue` is used by the Runbooks Change Feed relay to publish `IndexRunbookCommand`.

## Host / Dependency-Injection Boundary

Application abstractions are resolved differently depending on the host and environment:

```text
Worker
├── IIncidentAnalyzer
│   ├── DevelopmentDummyIncidentAnalyzer
│   └── AzureIncidentAnalyzer
└── IEmbeddingGenerator (Runbook ingestion)
    ├── DevelopmentDummyEmbeddingGenerator
    └── AzureEmbeddingGenerator

API
├── IEmbeddingGenerator (search-query embedding)
│   ├── DevelopmentDummyEmbeddingGenerator
│   └── AzureEmbeddingGenerator
└── IRunbookChunkRetriever
    └── CosmosRunbookChunkRetriever
```

Service Bus hosted services create a DI scope per message and resolve scoped handlers inside that message scope. The API resolves the search dependencies for the HTTP request path.

## Validation

Commands are validated with FluentValidation before side effects occur.

```text
Invalid command
      ↓
ValidationException
      ↓
no persistence
```

The API converts validation failures into Problem Details responses.

## Structure

```text
IncidentIQ.Application/
├── Common/
│   └── Abstractions/
├── Analyse/
├── Incidents/
│   ├── Create/
│   ├── GetById/
│   ├── GetAll/
│   └── ...analysis/retry use cases
├── Runbooks/
│   ├── Create/
│   ├── GetById/
│   ├── GetAll/
│   ├── Update/
│   ├── Delete/
│   └── Index/
└── DependencyInjection.cs
```

Provider-specific Cosmos/Azure classes are intentionally absent from this project.

## Testing

Application behaviour is tested in:

```text
tests/IncidentIQ.Application.Tests
```

Mocks/fakes are used for Application abstractions so orchestration, indexing and retrieval-facing behaviour can be tested without Azure resources.

See [tests/ReadMe.md](../../tests/ReadMe.md) for the wider testing strategy.
