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
- **API:** HTTP, authentication/authorization and DTO boundary.
- **Worker:** Change Feed relays and Service Bus consumers.
- **Web:** authenticated engineer/admin UI.

## Identity & Authorization

```text
User → Entra → React/MSAL → Bearer token → API
API / Worker → Managed Identity → Azure resources
```

- All controller endpoints require an authenticated token with `access_as_user`.
- Normal Incident, Runbook and Assistant functionality requires `Engineer` or `Administrator`.
- Operations and Incident retry require `Administrator`.
- `/api/me` exposes the authenticated user's roles to the UI; backend policies remain the security boundary.
- `/api/health` remains anonymous.
- User tokens are never forwarded to Cosmos, Service Bus or Azure OpenAI.

## Asynchronous Analysis

```text
POST /api/incidents
→ Incident + outbox in one Cosmos transactional batch
→ Cosmos Change Feed
→ Service Bus analyse-incident
→ AnalyseIncidentWorker
→ retrieve historical Incidents + Runbook chunks
→ Azure OpenAI
→ completed Incident + analysis/evidence
```

The W3C trace context is persisted with the outbox command so API and Worker activity can participate in the same distributed trace.

## Data

| Container | Partition key | Purpose |
| --- | --- | --- |
| `Incidents` | `/incidentId` | Incident, outbox, analysis/evidence |
| `Runbooks` | `/id` | Editable Runbooks |
| `RunbookChunks` | `/runbookId` | Derived Runbook vectors |
| `HistoricalIncidentVectors` | `/incidentId` | Derived completed-Incident vectors |
| `ChangeFeedLeases` | `/id` | Change Feed checkpoints |

Source records and rebuildable vector indexes remain separate.

## Observability & Scaling

- API and Worker emit OpenTelemetry to one Application Insights resource with role names `IncidentIQ.Api` and `IncidentIQ.Worker`.
- Custom spans cover outbox relay, analysis, retrieval, AI generation and persistence.
- Custom metrics cover queue wait, processing duration, AI duration, terminal failures and administrator retries.
- The Worker Container App uses KEDA against the `analyse-incident` Service Bus queue.
- Scaling is configured for **1–3 replicas**, with a target of **2 queued analysis messages per replica** and a 15-second polling interval.
- Minimum replicas remains 1 because the same Worker host also owns Cosmos Change Feed relays.

See [Observability & Scaling](OBSERVABILITY.md) and [Runtime Flows](flows/README.md).
