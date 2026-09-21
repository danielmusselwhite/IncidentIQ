# IncidentIQ Architecture

## Component View

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#ffffff"}} }%%
flowchart TB
    User["Engineer"]:::user --> Entra["Microsoft Entra ID"]:::identity
    Entra --> Web["React Web<br/>MSAL"]:::web
    Web -->|"Bearer token<br/>access_as_user"| API["ASP.NET Core API"]:::host

    subgraph Application
        Incident["Incident handlers"]:::app
        Runbook["Runbook handlers"]:::app
        Retrieval["RAG context builders + retrievers"]:::app
        Assistant["Operational Assistant handler"]:::app
        Ports["Repositories • Stores • Queues • AI interfaces"]:::app
    end

    subgraph Worker["Worker Host"]
        Outbox["IncidentOutboxWorker"]:::host
        Analyse["AnalyseIncidentWorker"]:::host
        RBRelay["RunbookIndexChangeFeedWorker"]:::host
        RBIndex["IndexRunbookWorker"]:::host
        HIRelay["HistoricalIncidentIndexChangeFeedWorker"]:::host
        HIIndex["IndexHistoricalIncidentWorker"]:::host
    end

    subgraph Infrastructure
        CosmosAdapters["Cosmos adapters"]:::infra
        BusAdapters["Service Bus adapters"]:::infra
        AIAdapters["Azure OpenAI adapters"]:::infra
    end

    subgraph Azure
        Cosmos["Cosmos DB<br/>Incidents • Runbooks • vectors • leases"]:::data
        Bus["Service Bus<br/>analysis + indexing queues"]:::msg
        OpenAI["Azure OpenAI<br/>chat + embeddings"]:::ai
    end

    API --> Incident
    API --> Runbook
    API --> Retrieval
    API --> Assistant

    Worker --> Application
    Application -. interfaces .-> Infrastructure

    CosmosAdapters --> Cosmos
    BusAdapters --> Bus
    AIAdapters --> OpenAI

    Cosmos ==>|Change Feed| Worker
    Bus ==>|Commands| Worker

    API -. "Managed Identity" .-> Cosmos
    API -. "Managed Identity" .-> OpenAI
    Worker -. "Managed Identity" .-> Cosmos
    Worker -. "Managed Identity" .-> Bus
    Worker -. "Managed Identity" .-> OpenAI

    classDef user fill:#f8fafc,stroke:#64748b,color:#0f172a,stroke-width:2px;
    classDef identity fill:#fef3c7,stroke:#d97706,color:#78350f,stroke-width:2px;
    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e,stroke-width:2px;
    classDef host fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef app fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef infra fill:#f3e8ff,stroke:#9333ea,color:#581c87,stroke-width:2px;
    classDef data fill:#ecfdf5,stroke:#059669,color:#064e3b,stroke-width:2px;
    classDef msg fill:#fff7ed,stroke:#d97706,color:#7c2d12,stroke-width:2px;
    classDef ai fill:#ede9fe,stroke:#7c3aed,color:#4c1d95,stroke-width:2px;
```

## Boundaries

- **Domain:** business entities and state rules.
- **Application:** use cases, orchestration and provider-independent interfaces.
- **Infrastructure:** Cosmos, Service Bus and Azure OpenAI implementations.
- **API:** HTTP/authentication boundary.
- **Worker:** Change Feed and Service Bus execution boundary.
- **Web:** authenticated engineer UI.

## Identity

```text
User → Entra → React/MSAL → Bearer token → API
API / Worker → Managed Identity → Azure resources
```

- Controller endpoints require authentication and `access_as_user`.
- `/api/health` remains anonymous.
- User tokens are not forwarded to Cosmos, Service Bus or Azure OpenAI.
- Engineer/Administrator role policies are Stage 14C.

## Data

| Container | Partition key | Purpose |
| --- | --- | --- |
| `Incidents` | `/incidentId` | Incident, outbox, analysis/evidence |
| `Runbooks` | `/id` | Editable Runbooks |
| `RunbookChunks` | `/runbookId` | Derived Runbook vectors |
| `HistoricalIncidentVectors` | `/incidentId` | Derived completed-Incident vectors |
| `ChangeFeedLeases` | `/id` | Change Feed checkpoints |

Source records and rebuildable vector indexes remain separate.

See [Runtime Flows](flows/README.md) for request-by-request diagrams.
