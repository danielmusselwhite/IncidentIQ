# IncidentIQ

IncidentIQ is an AI-powered incident analysis platform for engineers. Users submit technical incidents through a React frontend, and the system asynchronously analyses them using historical incidents and operational runbooks to produce likely causes, recommended actions, similar incidents, and supporting evidence.

## Architecture

### Software architecture

IncidentIQ uses a lightweight **Clean Architecture** approach:

```text
React Web
    ↓
ASP.NET Core API
    ↓
Application
    ↓
Domain
    ↑
Infrastructure
```

* **Domain** contains core business models and rules.
* **Application** contains use cases and abstractions.
* **Infrastructure** implements persistence and external integrations.
* **API / Worker** act as application hosts.

### Planned Azure architecture

```text
React
  ↓
APIM
  ↓
ASP.NET Core API — Azure Container Apps
  ├── Cosmos DB
  └── Azure Service Bus
            ↓
       .NET Worker — ACA
            ↓
     Cosmos Vector Search
       + Azure AI
            ↓
      Analysis Result
            ↓
        Cosmos DB
            ↓
        Event Grid
            ↓
    Python Azure Function
```

Supporting services will include **ACR, Managed Identity, Key Vault, App Configuration, Application Insights, Log Analytics, OpenTelemetry and KEDA autoscaling**.

### Local development

IncidentIQ supports two development modes for Cosmos DB:

* **Docker Compose** runs the API alongside the local **Cosmos DB Emulator**. The emulator uses persistent Docker volumes, so local Cosmos data is retained between container restarts.

* **Running the API directly** with `dotnet run` uses the configured **Azure Cosmos DB development account** instead of the emulator. Azure connection settings are kept outside source control using local configuration/user secrets.

**Docker-Compose, running local cosmos**

```text
Docker Compose
    ↓
IncidentIQ.Api
    ↓
Local Cosmos DB Emulator
    ↓
Persistent Docker volume
```

**dotnet run, using Azure Cosmos DB**

```text
dotnet run
    ↓
IncidentIQ.Api
    ↓
Azure Cosmos DB
    ↓
IncidentIQ / Incidents
```

The Docker Compose environment also includes:
- Persistent Cosmos data and certificate volumes.
- HTTPS communication with the Cosmos Emulator.
- Fixed local API ports for predictable frontend configuration.
- Cosmos database/container initialization during development.

This keeps everyday local development fast and self-contained while still allowing the API to be run directly against the real Azure development database when Azure integration needs to be verified.

---

## How IncidentIQ is used

From the frontend, engineers will be able to:

* Submit incidents with service, environment, severity, symptoms and error information.
* Track analysis status from `Queued → Processing → Completed / Failed`.
* Review AI-generated likely causes and recommended actions.
* View supporting runbook references and similar historical incidents.
* Provide feedback on analysis quality.
* Administrators can manage runbooks, retry failed analyses and view operational health.

At a high level:

```text
Submit Incident
      ↓
API stores incident
      ↓
Service Bus queues analysis
      ↓
Worker retrieves relevant knowledge
      ↓
Azure AI performs evidence-backed analysis
      ↓
Result stored in Cosmos DB
      ↓
Frontend displays result
```

---

## IncidentIQ.Api

ASP.NET Core Web API providing the HTTP interface used by the React frontend.

Currently implemented:

```text
POST /api/incidents
GET  /api/incidents
GET  /api/incidents/{id}
GET  /api/health
```

Incident creation uses **FluentValidation**, with **Exception Handling Middleware** returning ASP.NET Core Problem Details responses.

Controllers delegate application behaviour to handlers rather than accessing Cosmos DB directly.

Future responsibilities include:

* Runbook endpoints.
* Submitting asynchronous analysis work to Service Bus.
* Authentication and authorization.
* Exposing analysis results and operational actions.

---

## IncidentIQ.Worker

.NET background worker responsible for asynchronous processing.

Eventually it will:

* Consume `AnalyseIncident` and `IndexRunbook` commands.
* Generate embeddings.
* Retrieve similar incidents and runbook chunks.
* Build RAG context.
* Call Azure AI.
* Store structured analysis results.
* Handle retries, failures and idempotency.
* Publish completion events.

---

## IncidentIQ.Web

React frontend used by engineers and administrators.

Currently implemented:

* Shared application layout and navigation.
* Incident dashboard and search.
* Submit Incident page.
* Incident Detail page.
* Loading, validation, empty and error states.
* Typed API client for communication with `IncidentIQ.Api`.

Future screens include:

* Similar incidents and supporting evidence.
* Runbook management.
* Analysis feedback.
* Operations / administration.

The frontend communicates only with the API and does not directly access Azure services.

---

## IncidentIQ.Domain

Contains the core business model and rules without dependencies on Azure, Cosmos DB or ASP.NET Core.

Currently includes:

```text
Incident
IncidentStatus
IncidentSeverity
```

Future domain behaviour will include controlled state transitions such as processing, completion and failure.

---

## IncidentIQ.Application

Contains application use cases and abstractions.

Currently implemented:

```text
CreateIncident
GetIncidentById
GetAllIncidents
```

Future use cases will include:

```text
AnalyseIncident
IndexRunbook
```

It defines abstractions such as:

```text
IIncidentRepository
```

without knowing that Cosmos DB provides the implementation.

---

## IncidentIQ.Infrastructure

Contains implementations for external services.

Cosmos persistence currently uses the native **Azure Cosmos DB SDK** and includes:

```text
CosmosOptions
CosmosInitializer
CosmosIncidentRepository
IncidentDocument
```

Later Infrastructure will also contain integrations for:

```text
Service Bus
Azure AI
Event Grid
Identity
Configuration
```

---

## Testing and CI

The solution currently includes:

* Application unit tests for Incident handlers and validation.
* API integration tests using ASP.NET Core `WebApplicationFactory`.
* An in-memory Incident repository for isolated API testing.
* GitHub Actions CI running restore, build and tests on pull requests and pushes to `main`.

---

# Development Progress

## Stage 1 — Project Foundation

* [x] Create solution and repository structure.
* [x] Create ASP.NET Core API.
* [x] Create React frontend.
* [x] Create .NET Worker.
* [x] Add API health check.
* [x] Add Domain, Application and Infrastructure projects.
* [x] Establish Clean Architecture project dependencies.

## Stage 2 — Local Cosmos Infrastructure

* [x] Add Docker support and Visual Studio Docker Compose orchestration.
* [x] Add local Cosmos DB Emulator.
* [x] Configure local Cosmos HTTPS certificate handling.
* [x] Add persistent Cosmos data and certificate volumes.
* [x] Add Cosmos configuration and initialization.
* [x] Create `Incident` domain model and status/severity types.
* [x] Create `IIncidentRepository`.
* [x] Create Cosmos incident persistence model and repository.

## Stage 3 — Incident API

* [x] Create Incident API contracts and response models.
* [x] Create `CreateIncidentRequest` and `CreateIncidentHandler`.
* [x] Add FluentValidation.
* [x] Add centralized exception handling and Problem Details.
* [x] Add `POST /api/incidents`.
* [x] Add `GET /api/incidents`.
* [x] Add `GET /api/incidents/{id}`.
* [x] Add incident API/application tests.
* [ ] Paginate `GET /api/incidents`.

## Stage 4 — Core Incident Frontend

* [x] Build Submit Incident page.
* [x] Build Incident List / Dashboard.
* [x] Build Incident Detail page.
* [x] Connect React to the Incident API.
* [x] Add shared application layout and navigation.
* [x] Add loading, validation and error states.
* [ ] Update Incident List to support paginated API results.

## Stage 5 — First Azure Environment

Move the working Cosmos-backed application from local development into an initial Azure development environment.

* [x] Create base Bicep structure and environment parameter files.
* [x] Configure GitHub → Azure authentication using OIDC.
* [x] Add Bicep validation/deployment workflow.
* [x] Create Cosmos DB Bicep module.
* [x] Create Log Analytics and Application Insights Bicep modules.
* [x] Deploy the first Azure development environment using Bicep.
* [x] Configure the API to use Azure Cosmos DB.
* [x] Add initial API telemetry to Application Insights.
* [x] Introduce Managed Identity and Cosmos RBAC where practical.
* [x] Verify Incident CRUD against Azure Cosmos DB.
* [x] Keep the Cosmos Emulator configuration for local development.

## Stage 6 — Runbook Management

- [x] Create `Runbook` domain model.
- [x] Create `IRunbookRepository`.
- [x] Create dedicated `Runbooks` Cosmos container and persistence model.
- [x] Add Runbook CRUD API.
- [x] Build Runbook management frontend.
- [x] Add Runbook tests.
- [x] Keep editable Runbooks separate from future vectorised `RunbookChunk` documents.
- [x] Update Cosmos Bicep configuration for the `Runbooks` container.

## Stage 7 — Service Bus & Asynchronous Processing

Provision the Azure messaging infrastructure before integrating it into the application.

* [ ] Create Service Bus Bicep module.
* [ ] Define queues/topics and DLQ configuration in Bicep.
* [ ] Deploy Service Bus to the Azure development environment.
* [ ] Configure Managed Identity/RBAC for Service Bus access where practical.
* [ ] Add Service Bus application integration.
* [ ] Define `AnalyseIncident` command.
* [ ] Connect Incident submission to Service Bus.
* [ ] Implement Worker message consumption.
* [ ] Implement `Queued → Processing → Completed / Failed`.
* [ ] Propagate correlation IDs between API and Worker.
* [ ] Add frontend processing-status polling.

## Stage 8 — Reliability & Messaging

* [ ] Add transient retry handling.
* [ ] Add dead-letter handling.
* [ ] Make Worker processing idempotent.
* [ ] Add processing attempt/failure metadata.
* [ ] Handle Cosmos + Service Bus dual-write consistency.
* [ ] Add admin retry/requeue functionality.
* [ ] Add reliability and duplicate-message tests.

## Stage 9 — Application Deployment

Deploy the complete working application to Azure.

* [ ] Create ACR Bicep module.
* [ ] Create Container Apps Environment Bicep module.
* [ ] Create API Container App Bicep module.
* [ ] Create Worker Container App Bicep module.
* [ ] Create frontend hosting infrastructure.
* [ ] Deploy ACR, Container Apps and frontend hosting through Bicep.
* [ ] Add API/Worker container build and publish workflow.
* [ ] Build and push API/Worker images to ACR.
* [ ] Deploy API and Worker containers.
* [ ] Deploy the React frontend.
* [ ] Configure frontend → API connectivity.
* [ ] Verify the complete asynchronous workflow in Azure.

## Stage 10 — Azure AI

Provision Azure AI infrastructure before integrating AI functionality.

* [ ] Create Azure AI resource/deployment Bicep modules.
* [ ] Deploy Azure AI resources to the development environment.
* [ ] Configure Managed Identity/RBAC where supported.
* [ ] Add Azure AI application integration.
* [ ] Generate structured incident analysis.
* [ ] Validate structured AI responses.
* [ ] Handle AI timeout, throttling and failure scenarios.
* [ ] Add AI request latency/failure telemetry.
* [ ] Display likely causes and recommended actions.

## Stage 11 — Runbook Ingestion & Vector Search

* [ ] Define `IndexRunbook` workflow.
* [ ] Define dedicated Runbook chunk/vector persistence.
* [ ] Configure Cosmos vector policies and indexes through Bicep.
* [ ] Chunk Runbook content.
* [ ] Generate embeddings.
* [ ] Store Runbook chunks, embeddings and retrieval metadata.
* [ ] Implement Runbook vector retrieval.
* [ ] Add metadata filtering.
* [ ] Measure retrieval latency and RU usage.

## Stage 12 — Historical Incident Retrieval & RAG

* [ ] Define searchable historical Incident representation/vector persistence.
* [ ] Generate historical Incident embeddings.
* [ ] Implement similar-Incident retrieval.
* [ ] Keep historical Incident and Runbook evidence separate.
* [ ] Build combined RAG context.
* [ ] Generate evidence-backed analysis.
* [ ] Validate citations against retrieved evidence.
* [ ] Display similar Incidents and supporting evidence.

## Stage 13 — AI Evaluation

* [ ] Create realistic synthetic/demo data.
* [ ] Create controlled AI evaluation dataset.
* [ ] Measure retrieval relevance / Recall@K.
* [ ] Measure citation validity.
* [ ] Evaluate likely-cause and recommendation quality.
* [ ] Record AI/retrieval evaluation metrics.
* [ ] Add engineer analysis feedback functionality.

## Stage 14 — Security, Configuration & API Gateway

Complete the application's production-style security and configuration model.

* [ ] Create Key Vault Bicep module.
* [ ] Create App Configuration Bicep module.
* [ ] Create APIM Bicep module.
* [ ] Complete Managed Identity and least-privilege RBAC assignments through Bicep.
* [ ] Deploy resources through Bicep.
* [ ] Remove remaining connection-string authentication where Managed Identity can be used.
* [ ] Move application configuration into App Configuration.
* [ ] Store remaining secrets in Key Vault.
* [ ] Route public API traffic through APIM.
* [ ] Add Entra authentication.
* [ ] Add Engineer / Administrator authorization.
* [ ] Apply authorization to administrative and operational functionality.

## Stage 15 — Scaling & Observability

Expand the telemetry introduced in earlier stages into full distributed observability.

* [ ] Configure Worker KEDA scaling through Container Apps Bicep.
* [ ] Add OpenTelemetry instrumentation.
* [ ] Propagate distributed trace/correlation information end-to-end.
* [ ] Add analysis duration, queue wait and failure telemetry.
* [ ] Add Service Bus, Cosmos and AI dependency telemetry.
* [ ] Create useful KQL queries and dashboards.
* [ ] Monitor queue depth, Worker scaling and DLQ activity.
* [ ] Verify end-to-end distributed tracing.

## Stage 16 — Operations & Administration

* [ ] Build Operations frontend.
* [ ] Display queue depth and processing metrics.
* [ ] Display Worker scaling information.
* [ ] Display failed analyses and DLQ items.
* [ ] Add retry/requeue administration.
* [ ] Add operational diagnostics.

## Stage 17 — Event-Driven Integrations

Provision Event Grid and Functions before integrating them.

* [ ] Create Event Grid Bicep module.
* [ ] Create Azure Function hosting/resources Bicep module.
* [ ] Deploy Event Grid and Function infrastructure using Bicep.
* [ ] Create supporting Python Azure Function.
* [ ] Publish `AnalysisCompleted` / `AnalysisFailed` events.
* [ ] Consume completion events for audit/notification processing.
* [ ] Add Managed Identity and telemetry to the Function.

## Stage 18 — Hardening & Portfolio Polish

* [ ] Expand unit and integration test coverage.
* [ ] Harden CI/CD and deployment workflows.
* [ ] Add deployment verification/smoke tests.
* [ ] Ensure repeatable development-environment deployment from IaC.
* [ ] Seed realistic demo data.
* [ ] Finalise frontend styling and UX.
* [ ] Review error handling and edge cases.
* [ ] Complete architecture documentation and ADRs.
* [ ] Add architecture diagrams.
* [ ] Create polished README.
* [ ] Create portfolio demo/video.
* [ ] Perform final end-to-end testing.

## Stage 19 — Optional AI-200 Experiments

Keep experiments isolated from the primary architecture and document the trade-offs discovered.

* [ ] PostgreSQL + pgvector retriever.
* [ ] Azure Managed Redis experiment.
* [ ] AKS Worker deployment.
* [ ] App Service API container deployment.
* [ ] Cosmos Change Feed Runbook indexing experiment.
* [ ] Define experiment infrastructure through Bicep.
* [ ] Compare each experiment against the primary architecture.
* [ ] Document findings and architectural trade-offs.
