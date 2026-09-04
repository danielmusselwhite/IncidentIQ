# IncidentIQ

IncidentIQ is an AI-powered incident analysis platform for engineers. Users submit technical incidents through a React frontend, and the system processes them asynchronously through Cosmos DB, Azure Service Bus, a .NET Worker, and Azure OpenAI.

The project is being built incrementally as a practical Azure/AI engineering project. The current system includes Incident and Runbook management, a transactional outbox, Service Bus reliability/DLQ handling, structured AI analysis, persisted analysis retrieval, frontend analysis display, bounded AI resilience, and structured AI telemetry. Local development uses deterministic AI output so the complete workflow can run without Azure OpenAI credentials.

## Architecture

### Clean Architecture

IncidentIQ follows a lightweight Clean Architecture approach: business rules sit at the centre, Application defines the use cases and abstractions around them, and the outer adapters connect those use cases to HTTP, background processing, persistence, messaging, and Azure services.

![Clean Architecture Diagram](./docs/images/clean-architecture.png)

### 1. Azure Infrastructure Architecture

The deployed development environment is provisioned with Bicep. The main runtime, data, AI, identity, observability, and delivery components are grouped below so the Azure boundary is easier to read.

```mermaid
flowchart LR
    User["Engineer / Browser"]:::external
    GitHub["GitHub Actions<br/>OIDC"]:::delivery

    subgraph Azure["Azure Development Environment"]
        direction LR

        subgraph Frontend["Frontend"]
            SWA["Static Web Apps<br/>React + Vite"]:::frontend
        end

        subgraph Compute["Compute"]
            API["API Container App<br/>ASP.NET Core"]:::compute
            Worker["Worker Container App<br/>.NET Worker"]:::compute
        end

        subgraph Platform["Data, Messaging & AI"]
            Cosmos["Cosmos DB<br/>Incidents • Runbooks • Analysis"]:::data
            ServiceBus["Service Bus<br/>analyse-incident + DLQ"]:::messaging
            OpenAI["Azure OpenAI<br/>Incident Analysis"]:::ai
        end

        subgraph Observability["Observability"]
            Insights["Application Insights"]:::observe
            Logs["Log Analytics"]:::observe
        end

        subgraph Delivery["Images & Identity"]
            ACR["Container Registry"]:::delivery
            ApiMI["API Managed Identity"]:::identity
            WorkerMI["Worker Managed Identity"]:::identity
        end
    end

    User --> SWA
    SWA -->|HTTPS| API

    API --> Cosmos
    Worker --> Cosmos
    Worker --> ServiceBus
    Worker --> OpenAI

    API -. telemetry .-> Insights
    Worker -. telemetry .-> Insights
    Insights --> Logs

    ACR -. image .-> API
    ACR -. image .-> Worker
    GitHub -. deploy .-> SWA
    GitHub -. deploy .-> API
    GitHub -. deploy .-> Worker
    GitHub -. push .-> ACR

    ApiMI -. RBAC .-> Cosmos
    WorkerMI -. RBAC .-> Cosmos
    WorkerMI -. RBAC .-> ServiceBus
    WorkerMI -. RBAC .-> OpenAI

    classDef external fill:#f8fafc,stroke:#64748b,color:#0f172a,stroke-width:2px;
    classDef frontend fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e,stroke-width:2px;
    classDef compute fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef data fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef messaging fill:#fef3c7,stroke:#d97706,color:#78350f,stroke-width:2px;
    classDef ai fill:#f3e8ff,stroke:#9333ea,color:#581c87,stroke-width:2px;
    classDef observe fill:#ffedd5,stroke:#ea580c,color:#7c2d12,stroke-width:2px;
    classDef identity fill:#fce7f3,stroke:#db2777,color:#831843,stroke-width:2px;
    classDef delivery fill:#f1f5f9,stroke:#475569,color:#0f172a,stroke-width:2px;

    style Azure fill:#ffffff,stroke:#64748b,stroke-width:3px
    style Frontend fill:#f8fafc,stroke:#bae6fd,stroke-width:2px
    style Compute fill:#f8fafc,stroke:#bfdbfe,stroke-width:2px
    style Platform fill:#f8fafc,stroke:#cbd5e1,stroke-width:2px
    style Observability fill:#f8fafc,stroke:#fed7aa,stroke-width:2px
    style Delivery fill:#f8fafc,stroke:#e2e8f0,stroke-width:2px
```

### 2. Internal Application Architecture

At code level, the solution keeps responsibilities separated by project. This diagram intentionally stays above individual classes and shows the important *types* of code each layer contains and the direction of dependencies.

```mermaid
flowchart LR
    Web["IncidentIQ.Web<br/>Pages • Components • API Clients"]:::web

    subgraph Hosts["Application Hosts"]
        API["IncidentIQ.Api<br/>Controllers • DTOs • Middleware"]:::host
        Worker["IncidentIQ.Worker<br/>Hosted Services • Message Consumers"]:::host
    end

    subgraph Application["IncidentIQ.Application"]
        AppTypes["Commands & Queries<br/>Handlers<br/>Validators<br/>Interfaces / Abstractions"]:::application
    end

    subgraph Domain["IncidentIQ.Domain"]
        DomainTypes["Entities / Aggregates<br/>Enums & Value Objects<br/>Business Rules"]:::domain
    end

    subgraph Infrastructure["IncidentIQ.Infrastructure"]
        InfraTypes["Repositories & Stores<br/>Messaging Adapters<br/>Azure AI / SDK Clients"]:::infra
    end

    Web -->|HTTP| API
    API --> AppTypes
    Worker --> AppTypes
    AppTypes --> DomainTypes

    InfraTypes -->|implements abstractions| AppTypes
    InfraTypes --> DomainTypes

    API -. composition root .-> InfraTypes
    Worker -. composition root .-> InfraTypes

    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e,stroke-width:2px;
    classDef host fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef application fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef domain fill:#fef3c7,stroke:#d97706,color:#78350f,stroke-width:2px;
    classDef infra fill:#f3e8ff,stroke:#9333ea,color:#581c87,stroke-width:2px;

    style Hosts fill:#f8fafc,stroke:#93c5fd,stroke-width:2px
    style Application fill:#f0fdf4,stroke:#86efac,stroke-width:2px
    style Domain fill:#fffbeb,stroke:#fbbf24,stroke-width:2px
    style Infrastructure fill:#faf5ff,stroke:#d8b4fe,stroke-width:2px
```

### 3. Example Message Flow — Submit an Incident

A submitted Incident is persisted before it is queued. The API writes the Incident and outbox record atomically, then the asynchronous pipeline moves the command through Change Feed and Service Bus to the analysis Worker.

```mermaid
flowchart TD
    Web["React Web<br/>Submit Incident"]:::web

    subgraph Request["Synchronous Request"]
        API["API<br/>POST /api/incidents"]:::host
        Command["Application<br/>CreateIncidentCommand"]:::application
        CreateHandler["Create Incident Handler"]:::application
        SubmissionStore["Incident Submission Store"]:::infra
        InitialWrite["Cosmos Transactional Batch<br/>Incident + Outbox"]:::data
        Created["201 Created<br/>Incident is Queued"]:::result
    end

    subgraph Async["Asynchronous Processing"]
        ChangeFeed["Cosmos Change Feed"]:::data
        OutboxWorker["Outbox Worker"]:::host
        Queue["Service Bus<br/>AnalyseIncidentCommand"]:::messaging
        AnalyseWorker["Analysis Worker"]:::host
        AnalyseHandler["Analyse Incident Handler"]:::application
        Analyzer["Incident Analyzer"]:::application
        AI["Azure OpenAI<br/>or local dummy analyzer"]:::ai
        FinalWrite["Cosmos Transactional Batch<br/>Completed Incident + Analysis"]:::data
    end

    Result["Frontend polls status<br/>then requests persisted analysis"]:::result

    Web --> API
    API --> Command
    Command --> CreateHandler
    CreateHandler --> SubmissionStore
    SubmissionStore --> InitialWrite
    InitialWrite --> Created
    Created --> Web

    InitialWrite --> ChangeFeed
    ChangeFeed --> OutboxWorker
    OutboxWorker --> Queue
    Queue --> AnalyseWorker
    AnalyseWorker --> AnalyseHandler
    AnalyseHandler --> Analyzer
    Analyzer --> AI
    AI --> AnalyseHandler
    AnalyseHandler --> FinalWrite
    FinalWrite --> Result
    Result --> Web

    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e,stroke-width:2px;
    classDef host fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef application fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef infra fill:#f3e8ff,stroke:#9333ea,color:#581c87,stroke-width:2px;
    classDef data fill:#ecfdf5,stroke:#059669,color:#064e3b,stroke-width:2px;
    classDef messaging fill:#fef3c7,stroke:#d97706,color:#78350f,stroke-width:2px;
    classDef ai fill:#f3e8ff,stroke:#7c3aed,color:#4c1d95,stroke-width:2px;
    classDef result fill:#f8fafc,stroke:#64748b,color:#0f172a,stroke-width:2px;

    style Request fill:#f8fafc,stroke:#94a3b8,stroke-width:2px
    style Async fill:#f8fafc,stroke:#94a3b8,stroke-width:2px
```

## Current Functionality

Engineers can currently:

- Submit, browse, search, and inspect Incidents.
- Track `Queued → Processing → Completed / Failed` without manually refreshing the page.
- View persisted AI-generated summaries, likely causes/confidence scores, recommended actions, model metadata, and analysis time.
- Create, view, edit, and delete operational Runbooks.
- Retry failed analysis through the backend retry/requeue capability.

Reliability and AI features currently include:

- Cosmos transactional outbox for Incident submission.
- Cosmos Change Feed relay to Service Bus.
- Stable command/message IDs and Service Bus duplicate detection.
- Basic state-based idempotency.
- Bounded Service Bus redelivery and DLQ handling.
- Atomic persistence of `Completed` Incident state + structured analysis.
- Separate analysis read path using `IIncidentAnalysisReader`.
- Deterministic local analyzer in `Development`.
- Azure OpenAI structured output outside `Development`.
- Bounded Azure AI SDK retries plus an overall request timeout.
- Failure classification for timeout, throttling, service/client failures, and invalid model responses.
- Structured AI success/failure logs with duration, model, deployment, and failure category.

## Projects

| Project                                                       | Responsibility                                                                     |
| ------------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| [`IncidentIQ.Web`](src/IncidentIQ.Web/)                       | React frontend for Incidents, Runbooks, and persisted AI analysis                  |
| [`IncidentIQ.Api`](src/IncidentIQ.Api/)                       | ASP.NET Core HTTP API and Problem Details boundary                                 |
| [`IncidentIQ.Worker`](src/IncidentIQ.Worker/)                 | Change Feed outbox relay and Service Bus analysis processing                       |
| [`IncidentIQ.Domain`](src/IncidentIQ.Domain/)                 | Core business models and lifecycle rules                                           |
| [`IncidentIQ.Application`](src/IncidentIQ.Application/)       | Use cases, handlers, validation, and external-service abstractions                 |
| [`IncidentIQ.Infrastructure`](src/IncidentIQ.Infrastructure/) | Cosmos DB, Service Bus, Azure OpenAI, local AI, and Azure SDK implementations      |
| [`infra`](infra/)                                             | Bicep, identities/RBAC, deployment configuration, and local emulator configuration |
| [`tests`](tests/)                                             | Application, API, Worker, and reliability tests                                    |

## Documentation

| Document                                                      | Purpose                                                                               |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| [Development Guide](docs/DEVELOPMENT.md)                      | Run IncidentIQ locally with emulators/dummy AI or locally against Azure               |
| [Design Decisions & Trade-offs](docs/DESIGN-DECISIONS.md)     | Messaging, reliability, outbox, idempotency, AI resilience, and persistence decisions |
| [Azure Dev Lifecycle](docs/INCIDENTIQ-AZURE-DEV-LIFECYCLE.md) | Create, tear down, recreate, and reconfigure the Azure dev environment                |
| [Infrastructure](infra/ReadMe.md)                             | Bicep structure, Azure resources, identities, RBAC, and resource ownership            |
| [Testing](tests/ReadMe.md)                                    | Automated test boundaries and manual end-to-end verification                          |
| [Roadmap](docs/ROADMAP.md)                                    | Completed stages and planned work                                                     |
| [Troubleshooting](docs/TROUBLESHOOTING.md)                    | Common local Docker, Cosmos, Service Bus, DI, and AI configuration issues             |

Each main project also has its own README for implementation-specific responsibilities.

## Quick Start

### Docker Compose — normal local development

IncidentIQ has Visual Studio Container/Compose support. Select the appropriate Docker Compose debug target and start debugging, or run from the repository root:

```powershell
docker compose up --build
```

The local Worker runs with `DOTNET_ENVIRONMENT=Development`, so `IIncidentAnalyzer` resolves to `DevelopmentDummyIncidentAnalyzer`. The full asynchronous workflow therefore works locally without Azure OpenAI credentials.

Typical local endpoints:

- API Swagger: `https://localhost:7156/swagger`
- Web: `http://localhost:5173`
- Cosmos DB Emulator/Data Explorer: `http://localhost:1234/`

Local `.env` values are used for emulator credentials. See the [Development Guide](docs/DEVELOPMENT.md) for the exact setup.

### Azure-connected development

Use the Azure-connected mode when you specifically want to verify real Cosmos DB, Service Bus, Azure OpenAI, Managed Identity/RBAC, or Application Insights behaviour. Configuration and login instructions are in the [Development Guide](docs/DEVELOPMENT.md) and [Azure Dev Lifecycle](docs/INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).

## Testing

Run the backend test suite from the repository root:

```powershell
dotnet test
```

See [tests/ReadMe.md](tests/ReadMe.md) for the testing strategy and reliability checks.

## Troubleshooting

See the [Troubleshooting Guide](docs/TROUBLESHOOTING.md) for common development issues and resolutions.

## Roadmap

Stage 10 delivers the first complete Azure AI incident-analysis flow. The next stage adds Runbook chunking, embeddings, Cosmos vector search, and retrieval for RAG.

See the full [Development Roadmap](docs/ROADMAP.md).
