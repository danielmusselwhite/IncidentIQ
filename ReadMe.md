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

Docker Compose currently runs the API alongside the **Cosmos DB Emulator**.

The local environment includes:

* Persistent Cosmos data and certificate volumes.
* HTTPS communication with the Cosmos Emulator.
* Fixed local API ports for predictable frontend configuration.
* Cosmos database/container initialization during development.

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

## Stage 4 — Core Incident Frontend

* [x] Build Submit Incident page.
* [x] Build Incident List / Dashboard.
* [x] Build Incident Detail page.
* [x] Connect React to the Incident API.
* [x] Add shared application layout and navigation.
* [x] Add loading, validation and error states.

## Stage 5 — First Azure Environment

Move the working Cosmos-backed application from local-only development to an initial Azure development environment.

* [ ] Create base Bicep structure and environment parameter files.
* [ ] Configure GitHub → Azure deployment using OIDC.
* [ ] Add Bicep validation/deployment workflow.
* [ ] Create Cosmos DB Bicep module.
* [ ] Create Log Analytics / Application Insights Bicep modules.
* [ ] Deploy the first Azure development environment using Bicep.
* [ ] Configure the API to use Azure Cosmos DB.
* [ ] Verify Incident CRUD against Azure Cosmos DB.
* [ ] Keep the local Cosmos Emulator configuration for local development.

## Stage 6 — Runbook Management

* [ ] Create Runbook domain model.
* [ ] Create Runbook repository and Cosmos persistence.
* [ ] Add Runbook CRUD API.
* [ ] Build Runbook management frontend.
* [ ] Add Runbook tests.
* [ ] Update Cosmos Bicep configuration if additional containers/indexing are required.

## Stage 7 — Service Bus & Asynchronous Processing

Create the Azure infrastructure **before** integrating it into the application.

* [ ] Create Service Bus Bicep module.
* [ ] Define queues/topics and DLQ configuration in Bicep.
* [ ] Deploy Service Bus to the Azure development environment.
* [ ] Add Service Bus application integration.
* [ ] Define `AnalyseIncident` command.
* [ ] Connect Incident submission to Service Bus.
* [ ] Implement Worker message consumption.
* [ ] Implement `Queued → Processing → Completed / Failed`.
* [ ] Add frontend processing-status polling.

## Stage 8 — Reliability & Messaging

* [ ] Add transient retry handling.
* [ ] Add dead-letter handling.
* [ ] Make Worker processing idempotent.
* [ ] Add processing attempt/failure metadata.
* [ ] Handle Cosmos + Service Bus dual-write consistency.
* [ ] Add admin retry/requeue functionality.
* [ ] Add reliability and duplicate-message tests.

## Stage 9 — Container Deployment

* [ ] Create ACR Bicep module.
* [ ] Create Container Apps Environment Bicep module.
* [ ] Create API Container App Bicep module.
* [ ] Create Worker Container App Bicep module.
* [ ] Deploy ACR and Container Apps using Bicep.
* [ ] Build and push API/Worker images to ACR.
* [ ] Deploy API and Worker containers.
* [ ] Verify the asynchronous workflow in Azure.

## Stage 10 — Azure AI

Create Azure AI infrastructure before adding AI functionality.

* [ ] Create Azure AI resource/deployment Bicep modules.
* [ ] Deploy Azure AI resources to the development environment.
* [ ] Add Azure AI application integration.
* [ ] Generate structured incident analysis.
* [ ] Validate structured AI responses.
* [ ] Handle AI timeout/throttling/failure scenarios.
* [ ] Display likely causes and recommended actions.

## Stage 11 — Runbook Ingestion & Vector Search

* [ ] Define `IndexRunbook` workflow.
* [ ] Chunk Runbook content.
* [ ] Generate embeddings.
* [ ] Configure Cosmos vector indexes through Bicep where applicable.
* [ ] Store Runbook chunks and embeddings.
* [ ] Implement Runbook vector retrieval.
* [ ] Add metadata filtering.
* [ ] Measure retrieval latency and RU usage.

## Stage 12 — Historical Incident Retrieval & RAG

* [ ] Generate searchable historical Incident representations.
* [ ] Add historical Incident embeddings.
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
* [ ] Add engineer analysis feedback functionality.

## Stage 14 — Security, Configuration & API Gateway

Provision Azure resources through Bicep before application integration.

* [ ] Create Key Vault Bicep module.
* [ ] Create App Configuration Bicep module.
* [ ] Create Managed Identities and RBAC assignments through Bicep.
* [ ] Create APIM Bicep module.
* [ ] Deploy resources through Bicep.
* [ ] Configure API and Worker Managed Identity.
* [ ] Remove connection-string authentication where Managed Identity can be used.
* [ ] Move application configuration into App Configuration.
* [ ] Store remaining secrets in Key Vault.
* [ ] Route public API traffic through APIM.
* [ ] Add Entra authentication.
* [ ] Add Engineer / Administrator authorization.

## Stage 15 — Scaling & Observability

* [ ] Configure Worker KEDA scaling through Container Apps Bicep.
* [ ] Add OpenTelemetry instrumentation.
* [ ] Configure Application Insights through Bicep.
* [ ] Propagate distributed trace/correlation information.
* [ ] Add analysis duration, queue wait and failure telemetry.
* [ ] Create useful KQL queries and dashboards.
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

## Stage 18 — CI/CD & Portfolio Polish

* [ ] **Paginate the get all incidents endpoint.**
* [ ] Expand unit and integration test coverage.
* [ ] Add CI/CD pipeline.
* [ ] Add container build/publish pipeline.
* [ ] Add Bicep validation/deployment pipeline.
* [ ] Support repeatable development-environment deployment from IaC.
* [ ] Seed realistic demo data.
* [ ] Finalise frontend styling and UX.
* [ ] Complete architecture documentation and ADRs.
* [ ] Add architecture diagrams.
* [ ] Create polished README.
* [ ] Create portfolio demo/video.
* [ ] Perform final end-to-end testing.

## Stage 19 — Optional AI-200 Experiments

Each Azure experiment should also define its infrastructure through Bicep before deployment.

* [ ] PostgreSQL + pgvector retriever.
* [ ] Azure Managed Redis experiment.
* [ ] AKS Worker deployment.
* [ ] App Service API container deployment.
* [ ] Cosmos Change Feed Runbook indexing experiment.
* [ ] Document findings and architectural trade-offs.
