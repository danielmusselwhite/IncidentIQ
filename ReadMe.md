# IncidentIQ

**AI-powered incident analysis built with React, .NET, Azure, and vector search.**

IncidentIQ helps engineers submit technical incidents, process analysis asynchronously, manage operational Runbooks, and search indexed Runbook content semantically. The deployed development environment uses Azure OpenAI for structured analysis and embeddings, with Cosmos DB for persistence and vector retrieval.

> **Current status:** Runbook ingestion and semantic vector search are complete. The next stage adds historical-Incident retrieval and combines both evidence sources into grounded RAG analysis.

## What It Can Do

- Submit, browse, search, and inspect Incidents through the React frontend.
- Track analysis through `Queued → Processing → Completed / Failed`.
- Review persisted AI summaries, likely causes, confidence scores, recommended actions, model metadata, and analysis time.
- Create, view, edit, and delete operational Runbooks.
- Index Runbooks asynchronously and search their content semantically with top-K vector retrieval and optional service filtering.
- Retry failed Incident analysis through the backend retry/requeue flow.

## Core Stack

| Area              | Technology                                                        |
| ----------------- | ----------------------------------------------------------------- |
| **Frontend**      | React, TypeScript, Vite                                           |
| **Backend**       | ASP.NET Core API, .NET Worker, Clean Architecture                 |
| **Data**          | Azure Cosmos DB for NoSQL, Change Feed, vector search             |
| **Messaging**     | Azure Service Bus, transactional outbox, retries and DLQs         |
| **AI**            | Azure OpenAI structured analysis and embeddings                   |
| **Cloud**         | Azure Container Apps, Static Web Apps, ACR, Managed Identity/RBAC |
| **Observability** | OpenTelemetry, Application Insights, Log Analytics                |
| **Delivery**      | Bicep, GitHub Actions, OIDC                                       |

## Architecture at a Glance

IncidentIQ separates synchronous HTTP work from asynchronous background processing. The diagrams below use the same colours and arrow styles throughout.

### Diagram Key

```mermaid
flowchart TB
    subgraph Components["Component colours"]
        direction LR
        UI["Web / UI"]:::web
        HOST["API / Worker"]:::host
        APP["Application"]:::application
        DOMAIN["Domain"]:::domain
        INFRA["Infrastructure"]:::infra
        DATA["Data"]:::data
        MSG["Messaging"]:::messaging
        AI["AI"]:::ai
    end

    subgraph Connections["Connection examples"]
        direction LR
        S1["Caller"] -->|"direct / synchronous"| S2["Receiver"]
        A1["Producer"] ==>|"asynchronous hand-off"| A2["Consumer"]
        W1["Interface"] -. "implementation / wiring" .-> W2["Adapter"]
    end

    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e,stroke-width:2px;
    classDef host fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef application fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef domain fill:#fef3c7,stroke:#d97706,color:#78350f,stroke-width:2px;
    classDef infra fill:#f3e8ff,stroke:#9333ea,color:#581c87,stroke-width:2px;
    classDef data fill:#ecfdf5,stroke:#059669,color:#064e3b,stroke-width:2px;
    classDef messaging fill:#fff7ed,stroke:#d97706,color:#7c2d12,stroke-width:2px;
    classDef ai fill:#ede9fe,stroke:#7c3aed,color:#4c1d95,stroke-width:2px;

    linkStyle 0 stroke:#2563eb,stroke-width:2px;
    linkStyle 1 stroke:#d97706,stroke-width:3px;
    linkStyle 2 stroke:#9333ea,stroke-width:2px;
```

- **Solid arrow** — the caller waits for the operation or result.
- **Thick arrow** — asynchronous delivery through Change Feed or Service Bus.
- **Dotted arrow** — implementation, dependency-injection, deployment, telemetry, or RBAC relationship.

### System Overview

The API and Workers do **not** call Cosmos DB, Service Bus, or Azure OpenAI directly. They invoke Application handlers/use cases, which depend on Application interfaces; Infrastructure supplies the Azure-specific implementations through dependency injection.

The external Azure services are shown as separate boundaries below so the synchronous API path and asynchronous background paths are easy to follow.

```mermaid
flowchart TB
    User["Engineer"]:::external -->|"uses"| Web["React Web"]:::web

    subgraph Sync["Synchronous API path"]
        direction LR
        API["ASP.NET Core API<br/>Controllers"]:::host
        ApiHandlers["Application<br/>handlers / use cases"]:::application
        ApiPorts["Application interfaces<br/>repositories • stores • retrievers • AI"]:::application
        ApiAdapters["Infrastructure adapters"]:::infra

        API -->|"command / query"| ApiHandlers
        ApiHandlers -->|"calls"| ApiPorts
        ApiPorts -. "implemented by" .-> ApiAdapters
    end

    Web -->|"HTTPS"| API

    subgraph Cosmos["Azure Cosmos DB"]
        direction LR
        Incidents["Incidents<br/>+ analysis outbox"]:::data
        Runbooks["Runbooks"]:::data
        Chunks["RunbookChunks<br/>vector index"]:::data
        IncidentFeed["Incidents<br/>Change Feed"]:::data
        RunbookFeed["Runbooks<br/>Change Feed"]:::data

        Incidents ==>|"committed changes"| IncidentFeed
        Runbooks ==>|"committed changes"| RunbookFeed
    end

    AI["Azure OpenAI<br/>analysis + embeddings"]:::ai

    ApiAdapters -->|"Incident reads / writes"| Incidents
    ApiAdapters -->|"Runbook CRUD"| Runbooks
    ApiAdapters -->|"vector retrieval"| Chunks
    ApiAdapters -->|"query embeddings"| AI

    subgraph Relay["Change Feed relays — Worker host"]
        direction LR
        IncidentRelay["IncidentOutboxWorker"]:::host
        IncidentQueuePort["IIncidentAnalysisQueue"]:::application
        RunbookRelay["RunbookIndexChangeFeedWorker"]:::host
        RunbookQueuePort["IRunbookIndexQueue"]:::application

        IncidentRelay -->|"publish command"| IncidentQueuePort
        RunbookRelay -->|"publish command"| RunbookQueuePort
    end

    IncidentFeed ==>|"outbox item"| IncidentRelay
    RunbookFeed ==>|"Runbook change"| RunbookRelay

    subgraph Bus["Azure Service Bus"]
        direction LR
        AnalyseQueue["analyse-incident"]:::messaging
        IndexQueue["index-runbook"]:::messaging
    end

    IncidentQueuePort ==>|"AzureServiceBusIncidentAnalysisQueue"| AnalyseQueue
    RunbookQueuePort ==>|"AzureServiceBusRunbookIndexQueue"| IndexQueue

    subgraph Consumers["Service Bus consumers — Worker host"]
        direction LR
        AnalyseWorker["AnalyseIncidentWorker"]:::host
        AnalyseHandler["AnalyseIncidentHandler"]:::application
        IndexWorker["IndexRunbookWorker"]:::host
        IndexHandler["IndexRunbookHandler"]:::application

        AnalyseWorker -->|"dispatch command"| AnalyseHandler
        IndexWorker -->|"dispatch command"| IndexHandler
    end

    AnalyseQueue ==>|"AnalyseIncidentCommand"| AnalyseWorker
    IndexQueue ==>|"IndexRunbookCommand"| IndexWorker

    AnalyseHandler -->|"repositories / analyzer / store"| WorkerAdapters["Infrastructure adapters"]:::infra
    IndexHandler -->|"repository / embeddings / chunk store"| WorkerAdapters

    WorkerAdapters -->|"Incident state + analysis"| Incidents
    WorkerAdapters -->|"load source Runbook"| Runbooks
    WorkerAdapters -->|"replace vector chunks"| Chunks
    WorkerAdapters -->|"analysis / embeddings"| AI

    classDef external fill:#f8fafc,stroke:#64748b,color:#0f172a,stroke-width:2px;
    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e,stroke-width:2px;
    classDef host fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef application fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef infra fill:#f3e8ff,stroke:#9333ea,color:#581c87,stroke-width:2px;
    classDef data fill:#ecfdf5,stroke:#059669,color:#064e3b,stroke-width:2px;
    classDef messaging fill:#fff7ed,stroke:#d97706,color:#7c2d12,stroke-width:2px;
    classDef ai fill:#ede9fe,stroke:#7c3aed,color:#4c1d95,stroke-width:2px;

    style Sync fill:#fbfdff,stroke:#2563eb,stroke-width:2px;
    style Cosmos fill:#f6fffb,stroke:#059669,stroke-width:2px;
    style Relay fill:#fbfdff,stroke:#2563eb,stroke-width:2px;
    style Bus fill:#fffaf0,stroke:#d97706,stroke-width:2px;
    style Consumers fill:#fbfdff,stroke:#2563eb,stroke-width:2px;

    linkStyle 0,4 stroke:#0284c7,stroke-width:2px;
    linkStyle 1,2,11,12,17,18 stroke:#16a34a,stroke-width:2px;
    linkStyle 3,21,22 stroke:#9333ea,stroke-width:2px;
    linkStyle 5,6,7,8,9,13,14,23,24,25 stroke:#059669,stroke-width:2px;
    linkStyle 5,6,13,14 stroke:#059669,stroke-width:3px;
    linkStyle 10,26 stroke:#7c3aed,stroke-width:2px;
    linkStyle 15,16,19,20 stroke:#d97706,stroke-width:3px;
```

The key architectural boundary is now visible in both directions: **HTTP work reaches Azure services only through Application interfaces and Infrastructure adapters**, while background work starts from Cosmos Change Feed or Service Bus and then enters the same Application layer.


### Clean Architecture

IncidentIQ uses a lightweight Clean Architecture approach. Domain and Application code stay independent of Azure SDKs and persistence technologies; Infrastructure provides the concrete adapters, and the API/Worker hosts wire them together with dependency injection.

<p align="center">
  <img src="./docs/images/clean-architecture.png" alt="IncidentIQ Clean Architecture diagram" width="480" />
</p>

```mermaid
flowchart LR
    Host["API / Worker host"]:::host -->|"command / query"| Handler["Application handler"]:::application
    Handler -->|"business rules"| Domain["Domain model"]:::domain
    Handler -->|"calls interface"| Port["Application interface"]:::application
    Infra["Infrastructure adapter"]:::infra -. "implements" .-> Port
    Host -. "DI wires adapter" .-> Infra
    Infra -->|"SDK / protocol"| External["Cosmos / Service Bus / Azure OpenAI"]:::external

    classDef external fill:#f8fafc,stroke:#64748b,color:#0f172a,stroke-width:2px;
    classDef host fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef application fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef domain fill:#fef3c7,stroke:#d97706,color:#78350f,stroke-width:2px;
    classDef infra fill:#f3e8ff,stroke:#9333ea,color:#581c87,stroke-width:2px;

    linkStyle 0,2 stroke:#16a34a,stroke-width:2px;
    linkStyle 1 stroke:#d97706,stroke-width:2px;
    linkStyle 3,4,5 stroke:#9333ea,stroke-width:2px;
```

For example, an Application handler can depend on `IRunbookChunkRetriever` without knowing that the deployed implementation is `CosmosRunbookChunkRetriever`.

| Abstraction            | Responsibility                            | Examples                                                                   |
| ---------------------- | ----------------------------------------- | -------------------------------------------------------------------------- |
| **Repository**         | Persistence around a source/domain entity | `IIncidentRepository`, `IRunbookRepository`                                |
| **Store**              | Purpose-specific write boundary           | `IIncidentSubmissionStore`, `IIncidentAnalysisStore`, `IRunbookChunkStore` |
| **Reader / Retriever** | Purpose-specific read or search           | `IIncidentAnalysisReader`, `IRunbookChunkRetriever`                        |
| **Queue**              | Messaging boundary                        | `IIncidentAnalysisQueue`, `IRunbookIndexQueue`                             |
| **AI abstraction**     | Provider-independent AI capability        | `IIncidentAnalyzer`, `IEmbeddingGenerator`                                 |

## Key Workflows

These diagrams show the runtime flow only. The component READMEs contain the implementation-level class and configuration details.

### 1. Submit and Analyse an Incident

Incident submission is synchronous only until the Incident and its analysis outbox entry are committed to Cosmos. The HTTP request can then return; dispatch and AI analysis continue independently.

```mermaid
flowchart TB
    subgraph Submit["1 · Synchronous HTTP request"]
        direction LR
        Web["React UI"]:::web
        Controller["IncidentsController"]:::host
        Command["CreateIncidentCommand"]:::application
        Create["CreateIncidentHandler"]:::application
        Submission["IIncidentSubmissionStore"]:::application
        Response["201 Created<br/>Incident = Queued"]:::external

        Web -->|"POST /api/incidents"| Controller
        Controller -->|"map request"| Command
        Command -->|"HandleAsync"| Create
        Create -->|"Incident + AnalyseIncidentCommand"| Submission
    end

    subgraph Cosmos["Azure Cosmos DB"]
        direction LR
        Incidents["Incidents container<br/>Incident + analysis outbox<br/>written atomically"]:::data
        IncidentFeed["Incidents Change Feed"]:::data
        Incidents ==>|"committed outbox observed"| IncidentFeed
    end

    Submission -->|"CosmosIncidentSubmissionStore<br/>TransactionalBatch"| Incidents
    Incidents -->|"commit succeeds"| Response
    Response -->|"HTTP response"| Web

    subgraph Dispatch["2 · Asynchronous outbox relay"]
        direction LR
        Relay["IncidentOutboxWorker"]:::host
        QueuePort["IIncidentAnalysisQueue"]:::application
        Relay -->|"publish AnalyseIncidentCommand"| QueuePort
    end

    IncidentFeed ==>|"outbox document"| Relay

    subgraph ServiceBus["Azure Service Bus"]
        AnalyseQueue["analyse-incident queue"]:::messaging
    end

    QueuePort ==>|"AzureServiceBusIncidentAnalysisQueue"| AnalyseQueue

    subgraph Process["3 · Asynchronous analysis"]
        direction LR
        Worker["AnalyseIncidentWorker"]:::host
        Handler["AnalyseIncidentHandler"]:::application

        subgraph Orchestration["Handler orchestration"]
            direction LR
            Repo["IIncidentRepository<br/>load + update Processing"]:::application
            Analyzer["IIncidentAnalyzer<br/>generate analysis"]:::application
            Store["IIncidentAnalysisStore<br/>persist Completed + Analysis"]:::application
            Repo -->|"then"| Analyzer -->|"then"| Store
        end

        Worker -->|"dispatch command"| Handler
        Handler -->|"orchestrates"| Repo
    end

    AnalyseQueue ==>|"AnalyseIncidentCommand"| Worker

    Repo -->|"CosmosIncidentRepository"| Incidents
    Analyzer -->|"AzureIncidentAnalyzer"| AI["Azure OpenAI<br/>structured analysis"]:::ai
    Store -->|"CosmosIncidentAnalysisStore<br/>transactional batch"| Incidents

    classDef external fill:#f8fafc,stroke:#64748b,color:#0f172a,stroke-width:2px;
    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e,stroke-width:2px;
    classDef host fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef application fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef data fill:#ecfdf5,stroke:#059669,color:#064e3b,stroke-width:2px;
    classDef messaging fill:#fff7ed,stroke:#d97706,color:#7c2d12,stroke-width:2px;
    classDef ai fill:#ede9fe,stroke:#7c3aed,color:#4c1d95,stroke-width:2px;

    style Submit fill:#fbfdff,stroke:#2563eb,stroke-width:2px;
    style Cosmos fill:#f6fffb,stroke:#059669,stroke-width:2px;
    style Dispatch fill:#fffaf0,stroke:#d97706,stroke-width:2px;
    style ServiceBus fill:#fffaf0,stroke:#d97706,stroke-width:2px;
    style Process fill:#f7fff9,stroke:#16a34a,stroke-width:2px;
    style Orchestration fill:#f7fff9,stroke:#16a34a,stroke-width:1px;

    linkStyle 0,7 stroke:#0284c7,stroke-width:2px;
    linkStyle 1,2,3,8,11,12,13,14 stroke:#16a34a,stroke-width:2px;
    linkStyle 4,5,6,16,18 stroke:#059669,stroke-width:2px;
    linkStyle 9,10,15 stroke:#d97706,stroke-width:3px;
    linkStyle 17 stroke:#7c3aed,stroke-width:2px;
```

The transactional outbox is what makes this reliable: the Incident and `AnalyseIncidentCommand` outbox record succeed or fail together. `IncidentOutboxWorker` later relays that durable request to Service Bus; it does **not** perform the analysis itself.

The frontend can poll the Incident while it is `Queued` or `Processing`, then retrieve the persisted analysis once processing completes.


### 2. Index a Runbook

Runbook CRUD and vector indexing are deliberately separate. The editable Runbook is saved synchronously first; Cosmos Change Feed then starts the asynchronous indexing pipeline.

```mermaid
flowchart TB
    subgraph Save["1 · Synchronous Runbook save"]
        direction LR
        Web["React Runbook UI"]:::web
        Controller["RunbooksController"]:::host
        Handler["Create / Update Runbook Handler"]:::application
        Repo["IRunbookRepository"]:::application
        Response["HTTP success<br/>saved Runbook"]:::external

        Web -->|"POST / PUT"| Controller
        Controller -->|"command"| Handler
        Handler -->|"save Runbook"| Repo
    end

    subgraph Cosmos["Azure Cosmos DB"]
        direction LR
        Runbooks["Runbooks container<br/>editable source of truth"]:::data
        RunbookFeed["Runbooks Change Feed"]:::data
        Chunks["RunbookChunks container<br/>derived vector index"]:::data
        Runbooks ==>|"committed change observed"| RunbookFeed
    end

    Repo -->|"CosmosRunbookRepository"| Runbooks
    Runbooks -->|"commit succeeds"| Response
    Response -->|"HTTP response"| Web

    subgraph Dispatch["2 · Asynchronous indexing dispatch"]
        direction LR
        Relay["RunbookIndexChangeFeedWorker"]:::host
        QueuePort["IRunbookIndexQueue"]:::application
        Relay -->|"publish IndexRunbookCommand"| QueuePort
    end

    RunbookFeed ==>|"Runbook change"| Relay

    subgraph ServiceBus["Azure Service Bus"]
        IndexQueue["index-runbook queue"]:::messaging
    end

    QueuePort ==>|"AzureServiceBusRunbookIndexQueue"| IndexQueue

    subgraph Indexing["3 · Asynchronous indexing"]
        direction LR
        Worker["IndexRunbookWorker"]:::host
        IndexHandler["IndexRunbookHandler"]:::application

        subgraph Orchestration["Handler orchestration"]
            direction LR
            SourceRepo["IRunbookRepository<br/>load latest source"]:::application
            Chunker["RunbookChunker"]:::application
            Embedder["IEmbeddingGenerator"]:::application
            Store["IRunbookChunkStore"]:::application
            SourceRepo -->|"then"| Chunker -->|"then"| Embedder -->|"then"| Store
        end

        Worker -->|"dispatch command"| IndexHandler
        IndexHandler -->|"orchestrates"| SourceRepo
    end

    IndexQueue ==>|"IndexRunbookCommand"| Worker

    SourceRepo -->|"CosmosRunbookRepository"| Runbooks
    Embedder -->|"AzureEmbeddingGenerator"| AI["Azure OpenAI<br/>text-embedding-3-small"]:::ai
    Store -->|"CosmosRunbookChunkStore<br/>replace derived chunks"| Chunks

    classDef external fill:#f8fafc,stroke:#64748b,color:#0f172a,stroke-width:2px;
    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e,stroke-width:2px;
    classDef host fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef application fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef data fill:#ecfdf5,stroke:#059669,color:#064e3b,stroke-width:2px;
    classDef messaging fill:#fff7ed,stroke:#d97706,color:#7c2d12,stroke-width:2px;
    classDef ai fill:#ede9fe,stroke:#7c3aed,color:#4c1d95,stroke-width:2px;

    style Save fill:#fbfdff,stroke:#2563eb,stroke-width:2px;
    style Cosmos fill:#f6fffb,stroke:#059669,stroke-width:2px;
    style Dispatch fill:#fffaf0,stroke:#d97706,stroke-width:2px;
    style ServiceBus fill:#fffaf0,stroke:#d97706,stroke-width:2px;
    style Indexing fill:#f7fff9,stroke:#16a34a,stroke-width:2px;
    style Orchestration fill:#f7fff9,stroke:#16a34a,stroke-width:1px;

    linkStyle 0,6 stroke:#0284c7,stroke-width:2px;
    linkStyle 1,2,7,10,11,12,13,14 stroke:#16a34a,stroke-width:2px;
    linkStyle 3,4,5,8,16,18 stroke:#059669,stroke-width:2px;
    linkStyle 3,8 stroke:#059669,stroke-width:3px;
    linkStyle 9,15 stroke:#d97706,stroke-width:3px;
    linkStyle 17 stroke:#7c3aed,stroke-width:2px;
```

`IndexRunbookCommand` carries the Runbook identity rather than the full document. `IndexRunbookHandler` reloads the current Runbook, chunks it, generates embeddings, then replaces that Runbook's derived vector-search chunks.

`RunbookChunks` are search data rather than the editable source of truth. Re-indexing replaces stale chunks, and deleting a Runbook also removes its derived chunks.


### 3. Search Runbooks Semantically

Semantic search is different from indexing: it is a **fully synchronous** request/response path. The API still does not query Cosmos or Azure OpenAI directly; it works through Application abstractions whose implementations live in Infrastructure.

```mermaid
flowchart TB
    subgraph Request["Synchronous search request"]
        direction LR
        Web["React / API client"]:::web
        API["Runbooks search endpoint"]:::host
        Embedder["IEmbeddingGenerator"]:::application
        Vector["Query embedding<br/>float[1536]"]:::application
        Retriever["IRunbookChunkRetriever"]:::application
        Results["RunbookChunkMatch[]<br/>top-K + distance"]:::external

        Web -->|"GET /api/runbooks/search<br/>query + service? + topK"| API
        API -->|"GenerateAsync(query)"| Embedder
        Vector -->|"RetrieveAsync(vector, service?, topK)"| Retriever
        Results -->|"200 OK"| Web
    end

    AI["Azure OpenAI<br/>text-embedding-3-small"]:::ai

    subgraph Cosmos["Azure Cosmos DB"]
        Chunks["RunbookChunks<br/>VectorDistance + optional service filter"]:::data
    end

    Embedder -->|"AzureEmbeddingGenerator"| AI
    AI -->|"1536-d vector"| Vector
    Retriever -->|"CosmosRunbookChunkRetriever"| Chunks
    Chunks -->|"ranked rows + cosine distance"| Results

    classDef external fill:#f8fafc,stroke:#64748b,color:#0f172a,stroke-width:2px;
    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e,stroke-width:2px;
    classDef host fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef application fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef data fill:#ecfdf5,stroke:#059669,color:#064e3b,stroke-width:2px;
    classDef ai fill:#ede9fe,stroke:#7c3aed,color:#4c1d95,stroke-width:2px;

    style Request fill:#fbfdff,stroke:#2563eb,stroke-width:2px;
    style Cosmos fill:#f6fffb,stroke:#059669,stroke-width:2px;

    linkStyle 0 stroke:#0284c7,stroke-width:2px;
    linkStyle 1,2,3 stroke:#16a34a,stroke-width:2px;
    linkStyle 4,5 stroke:#7c3aed,stroke-width:2px;
    linkStyle 6,7 stroke:#059669,stroke-width:2px;
```

There is **no Change Feed or Service Bus hop** in search. The caller waits for both embedding generation and Cosmos vector retrieval before receiving the response. Smaller cosine distance means a stronger match; retrieval also records latency and Cosmos Request Unit (RU) consumption.


## Engineering Highlights

- **Clean Architecture + dependency inversion** — Application owns use cases and interfaces; Infrastructure owns Azure-specific implementations.
- **Durable asynchronous processing** — Cosmos transactional outbox, Change Feed relays, Service Bus commands, duplicate detection, bounded retries, and DLQs.
- **Structured AI analysis** — persisted summaries, likely causes, confidence scores, recommended actions, and model metadata.
- **Runbook ingestion pipeline** — deterministic overlapping chunking, embeddings, replace-based re-indexing, and stale-data cleanup.
- **Vector search** — Cosmos `VectorDistance`, 1536-dimension `float32` cosine vectors, top-K retrieval, service filtering, latency, and RU telemetry.
- **Cloud-native security** — Managed Identity/RBAC for workloads and GitHub OIDC for deployments.
- **Observability** — OpenTelemetry, Application Insights, Log Analytics, correlation IDs, and operational telemetry.
- **Local-first development** — Cosmos/Service Bus emulators and deterministic AI implementations allow the main workflows to run without Azure OpenAI credentials.

## Projects

The summaries below are intentionally high-level. Each project name links to its own README for implementation details.

| Project                                                                | High-level responsibility                                                                                                |
| ---------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| [`IncidentIQ.Web`](src/IncidentIQ.Web/README.md)                       | **Engineer UI** — React pages and API clients for Incidents, analysis, and Runbooks.                                     |
| [`IncidentIQ.Api`](src/IncidentIQ.Api/ReadMe.md)                       | **HTTP host** — controllers, HTTP contracts, validation/error presentation, synchronous search, and DI composition root. |
| [`IncidentIQ.Worker`](src/IncidentIQ.Worker/ReadMe.md)                 | **Async host** — Change Feed relays and Service Bus consumers for Incident analysis and Runbook indexing.                |
| [`IncidentIQ.Domain`](src/IncidentIQ.Domain/ReadMe.md)                 | **Business core** — domain entities, lifecycle/state rules, and invariants with no Azure or persistence dependency.      |
| [`IncidentIQ.Application`](src/IncidentIQ.Application/ReadMe.md)       | **Use-case layer** — commands/queries, handlers, validation, application models, and provider-independent interfaces.    |
| [`IncidentIQ.Infrastructure`](src/IncidentIQ.Infrastructure/ReadMe.md) | **External adapters** — Cosmos DB, Service Bus, Azure OpenAI, and deterministic local implementations.                   |
| [`infra`](infra/ReadMe.md)                                             | **Infrastructure as Code** — Bicep, Azure resources, Managed Identity/RBAC, monitoring, and deployment configuration.    |
| [`tests`](tests/ReadMe.md)                                             | **Verification** — Application/API/Worker tests plus reliability, indexing, and vector-retrieval coverage.               |

## Documentation

| Document                                                      | Purpose                                                                                |
| ------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| [Development Guide](docs/DEVELOPMENT.md)                      | Local development and Azure-connected verification                                     |
| [Design Decisions & Trade-offs](docs/DESIGN-DECISIONS.md)     | Architecture rationale for messaging, persistence, AI, ingestion, and vector retrieval |
| [Azure Dev Lifecycle](docs/INCIDENTIQ-AZURE-DEV-LIFECYCLE.md) | Create, tear down, recreate, configure, and verify the Azure dev environment           |
| [Infrastructure](infra/ReadMe.md)                             | Bicep structure, Azure resources, identities, RBAC, and ownership                      |
| [Testing](tests/ReadMe.md)                                    | Automated test boundaries and end-to-end verification                                  |
| [Roadmap](docs/ROADMAP.md)                                    | Completed stages and planned work                                                      |
| [Troubleshooting](docs/TROUBLESHOOTING.md)                    | Common Docker, Cosmos, Service Bus, AI, ingestion, and vector-search issues            |

## Quick Start

### Docker Compose — normal local development

IncidentIQ supports Visual Studio Docker Compose debugging. From the repository root:

```powershell
docker compose up --build
```

In `Development`, deterministic local AI implementations are used, so Incident analysis, Runbook ingestion, and semantic search can be exercised without Azure OpenAI credentials.

Typical local endpoints:

```text
Web:                  http://localhost:5173
API Swagger:          https://localhost:7156/swagger
Cosmos Data Explorer: http://localhost:1234
```

See the [Development Guide](docs/DEVELOPMENT.md) for configuration and verification steps.

### Azure-connected development

Use Azure-connected execution to verify real Cosmos DB, Service Bus, Azure OpenAI, Managed Identity/RBAC, Application Insights, or deployed vector-search behaviour.

See the [Development Guide](docs/DEVELOPMENT.md) and [Azure Dev Lifecycle](docs/INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).

## Testing

Run the backend test suite from the repository root:

```powershell
dotnet test .\IncidentIQ.slnx
```

See [`tests/ReadMe.md`](tests/ReadMe.md) for the testing strategy.

## Roadmap

**Stage 11 — Runbook ingestion and vector search — is complete.**

Stage 12 adds:

1. **Historical Incident Vector Retrieval** — searchable historical Incident embeddings and similar-Incident retrieval.
2. **Grounded RAG Analysis** — combine retrieved Incident and Runbook evidence and feed it into the AI analysis pipeline.

See the full [Development Roadmap](docs/ROADMAP.md).
