# IncidentIQ Component Architecture

This page is the detailed counterpart to the simplified diagram in the root README. It shows the main Application handlers, asynchronous Workers, Infrastructure adapters and Azure services without documenting every class.

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#ffffff"}} }%%
flowchart TB
    Web["React Web<br/>Incidents • Runbooks • Assistant"]:::web

    subgraph Api["ASP.NET Core API"]
        IncidentController["IncidentsController"]:::host
        RunbookController["RunbooksController"]:::host
        AssistantController["AssistantController"]:::host
    end

    subgraph Application["Application"]
        Create["CreateIncidentHandler"]:::app
        Analyse["AnalyseIncidentHandler"]:::app
        RunbookHandlers["Runbook CRUD / search"]:::app
        IndexRunbook["IndexRunbookHandler"]:::app
        IndexHistory["IndexHistoricalIncidentHandler"]:::app
        Ask["AskOperationalQuestionHandler"]:::app

        GroundIncident["IncidentAnalysisContextBuilder"]:::app
        GroundAssistant["OperationalQuestionContextBuilder"]:::app

        IncidentPorts["Incident repositories / stores"]:::port
        RunbookPorts["Runbook repository / chunk store"]:::port
        Retrieval["Historical + Runbook retrievers"]:::port
        AIContracts["IEmbeddingGenerator<br/>IIncidentAnalyzer<br/>IOperationalAssistant"]:::port
        Queues["Queue abstractions"]:::port
    end

    subgraph Workers["Worker host"]
        OutboxWorker["IncidentOutboxWorker"]:::host
        AnalyseWorker["AnalyseIncidentWorker"]:::host
        RunbookRelay["RunbookIndexChangeFeedWorker"]:::host
        RunbookWorker["IndexRunbookWorker"]:::host
        HistoryRelay["HistoricalIncidentIndexChangeFeedWorker"]:::host
        HistoryWorker["IndexHistoricalIncidentWorker"]:::host
    end

    subgraph Infrastructure["Infrastructure"]
        CosmosAdapters["Cosmos repositories / stores / retrievers"]:::infra
        ServiceBusAdapters["Service Bus queue adapters"]:::infra
        AzureAIAdapters["AzureEmbeddingGenerator<br/>AzureIncidentAnalyzer<br/>AzureOperationalAssistant"]:::infra
    end

    subgraph Azure["Azure services"]
        Incidents["Cosmos: Incidents"]:::data
        Runbooks["Cosmos: Runbooks"]:::data
        Chunks["Cosmos: RunbookChunks"]:::derived
        History["Cosmos: HistoricalIncidentVectors"]:::derived
        Leases["Cosmos: ChangeFeedLeases"]:::data

        AnalyseQueue["Service Bus: analyse-incident"]:::msg
        RunbookQueue["Service Bus: index-runbook"]:::msg
        HistoryQueue["Service Bus: index-historical-incident"]:::msg

        OpenAI["Azure OpenAI<br/>chat + embeddings"]:::ai
    end

    Web --> IncidentController
    Web --> RunbookController
    Web --> AssistantController

    IncidentController --> Create
    RunbookController --> RunbookHandlers
    AssistantController --> Ask

    Create --> IncidentPorts
    Ask --> GroundAssistant
    GroundAssistant --> Retrieval
    GroundAssistant --> AIContracts

    Analyse --> GroundIncident
    GroundIncident --> Retrieval
    GroundIncident --> AIContracts
    Analyse --> IncidentPorts

    IndexRunbook --> RunbookPorts
    IndexRunbook --> AIContracts
    IndexHistory --> IncidentPorts
    IndexHistory --> AIContracts

    OutboxWorker --> Queues
    AnalyseWorker --> Analyse
    RunbookRelay --> Queues
    RunbookWorker --> IndexRunbook
    HistoryRelay --> Queues
    HistoryWorker --> IndexHistory

    IncidentPorts -.-> CosmosAdapters
    RunbookPorts -.-> CosmosAdapters
    Retrieval -.-> CosmosAdapters
    Queues -.-> ServiceBusAdapters
    AIContracts -.-> AzureAIAdapters

    CosmosAdapters --> Incidents
    CosmosAdapters --> Runbooks
    CosmosAdapters --> Chunks
    CosmosAdapters --> History

    ServiceBusAdapters --> AnalyseQueue
    ServiceBusAdapters --> RunbookQueue
    ServiceBusAdapters --> HistoryQueue

    AzureAIAdapters --> OpenAI

    Incidents ==>|"Change Feed"| OutboxWorker
    Incidents ==>|"completed Incident feed"| HistoryRelay
    Runbooks ==>|"Change Feed"| RunbookRelay
    Leases -. "checkpoints" .-> OutboxWorker
    Leases -. "checkpoints" .-> RunbookRelay
    Leases -. "checkpoints" .-> HistoryRelay

    AnalyseQueue ==>|"AnalyseIncidentCommand"| AnalyseWorker
    RunbookQueue ==>|"IndexRunbookCommand"| RunbookWorker
    HistoryQueue ==>|"IndexHistoricalIncidentCommand"| HistoryWorker

    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e,stroke-width:2px;
    classDef host fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef app fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef port fill:#f0fdf4,stroke:#22c55e,color:#14532d,stroke-width:1.5px;
    classDef infra fill:#f3e8ff,stroke:#9333ea,color:#581c87,stroke-width:2px;
    classDef data fill:#ecfdf5,stroke:#059669,color:#064e3b,stroke-width:2px;
    classDef derived fill:#f0fdfa,stroke:#0f766e,color:#134e4a,stroke-width:2px;
    classDef msg fill:#fff7ed,stroke:#d97706,color:#7c2d12,stroke-width:2px;
    classDef ai fill:#ede9fe,stroke:#7c3aed,color:#4c1d95,stroke-width:2px;
```

## How to read it

- **API and Worker are hosts.** They receive HTTP, Change Feed or Service Bus work and enter Application.
- **Application owns orchestration.** Commands, handlers, context builders and interfaces describe the use cases.
- **Infrastructure owns provider details.** Cosmos queries, Service Bus SDK calls and Azure OpenAI SDK calls stay outside Application.
- **`Incidents` and `Runbooks` are source data.** `HistoricalIncidentVectors` and `RunbookChunks` are derived retrieval indexes.
- **Synchronous paths** include Runbook search and the Operational Assistant.
- **Asynchronous paths** include Incident analysis, Runbook indexing and historical Incident indexing.

For step-by-step runtime diagrams, see [`docs/flows/`](flows/README.md).
