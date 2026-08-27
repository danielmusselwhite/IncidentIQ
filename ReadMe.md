# IncidentIQ

IncidentIQ is an AI-powered incident analysis platform for engineers. Users submit technical incidents through a React frontend, and the system processes them asynchronously through Azure Service Bus and a .NET Worker. The planned AI pipeline will use historical incidents and operational runbooks to produce likely causes, recommended actions, similar incidents, and supporting evidence.

The project is being built incrementally as a practical Azure/AI engineering project, with local emulators used for day-to-day development and Bicep used to provision the Azure development environment.

## Documentation

### Project Structure

Each main area has its own responsibility:

- **Infrastructure**

  - [`infra`](infra/) — Bicep infrastructure-as-code, Azure bootstrap resources, environment parameters, and local emulator configuration.

- **Main projects**

  - [`IncidentIQ.Api`](src/IncidentIQ.Api/) — ASP.NET Core Web API.

  - [`IncidentIQ.Worker`](src/IncidentIQ.Worker/) — .NET Worker for asynchronous incident processing.

  - [`IncidentIQ.Web`](src/IncidentIQ.Web/) — React frontend.

- **Class libraries**

  - [`IncidentIQ.Domain`](src/IncidentIQ.Domain/) — Core business models and rules.

  - [`IncidentIQ.Application`](src/IncidentIQ.Application/) — Application use cases, handlers, validation, and abstractions.

  - [`IncidentIQ.Infrastructure`](src/IncidentIQ.Infrastructure/) — Cosmos DB and Azure Service Bus implementations.

- **Tests**

  - [`IncidentIQ.Tests`](tests/IncidentIQ.Tests/) — Unit and integration tests.

### Guides

- [Development Guide](docs/DEVELOPMENT.md) — how to run IncidentIQ fully locally with emulators or against the Azure development environment.

- [Azure Dev Lifecycle](docs/INCIDENTIQ-AZURE-DEV-LIFECYCLE.md) — how to create, delete, recreate and reconfigure the Azure development environment.

## Architecture

### Software architecture

![Clean Architecture Diagram: credit Dor Lugasi-Gal, Microsoft Dev Blogs](docs/images/clean-architecture.png)

IncidentIQ uses a lightweight **Clean Architecture** approach:

```text

React Web

    ↓

ASP.NET Core API / .NET Worker

    ↓

Application

    ↓

Domain

    ↑

Infrastructure

```

- **Domain** contains core business models and rules.

- **Application** contains use cases, handlers, validation, and abstractions.

- **Infrastructure** implements persistence and external integrations.

- **API / Worker** are application hosts: the API handles HTTP requests while the Worker consumes asynchronous work.

### Current asynchronous incident flow

The core asynchronous pipeline is now implemented:

```text

React

  ↓

ASP.NET Core API

  ↓

Cosmos DB — Incident created as Queued

  ↓

Azure Service Bus — AnalyseIncident command

  ↓

.NET Worker

  ↓

AnalyseIncidentHandler

  ↓

Processing

  ↓

Completed

```

Correlation IDs are propagated from the API into the Service Bus command and Worker logging context so the same workflow can be traced across components.

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

       .NET Worker — Azure Container Apps

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

## Local Development

IncidentIQ supports two development modes:

For prerequisites, configuration and step-by-step startup instructions, see: [Development Guide](docs/DEVELOPMENT.md)

### Fully local — Docker Compose

React

  ↓

IncidentIQ.Api

  ├── Cosmos DB Emulator

  └── Service Bus Emulator

            ↓

       IncidentIQ.Worker

            ↓

       Cosmos DB Emulator

This is the normal day-to-day development mode and keeps the application self-contained without requiring Azure resources to remain running.

### Azure-connected — dotnet run

React

  ↓

Local API / Worker

  ├── Azure Cosmos DB

  └── Azure Service Bus

This mode is used when real Azure integration, authentication/RBAC, Service Bus behaviour, Cosmos behaviour, or telemetry needs to be verified.

## How IncidentIQ Is Used

Engineers can currently:

- Submit incidents with service, environment, severity, symptoms and error information.

- View incident dashboards and incident details.

- Track incident state through the asynchronous processing flow.

- Create, view, edit and delete operational runbooks.

The completed AI workflow will additionally allow engineers to:

- Review AI-generated likely causes and recommended actions.

- View supporting runbook references and similar historical incidents.

- Provide feedback on analysis quality.

- Retry failed analyses and view operational health through administrator tooling.

At a high level, the completed workflow is planned to be:

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

## Projects

IncidentIQ is split into a small set of application hosts and supporting class libraries.

Project

Responsibility

IncidentIQ.Web

React frontend for incidents, runbooks, and future analysis/operations views.

IncidentIQ.Api

ASP.NET Core HTTP API used by the frontend.

IncidentIQ.Worker

Background host that consumes asynchronous Service Bus commands.

IncidentIQ.Domain

Core business models, state, and business rules.

IncidentIQ.Application

Application use cases, handlers, validation, and abstractions.

IncidentIQ.Infrastructure

Cosmos DB, Service Bus, and other external-service implementations.

tests

Unit and API integration tests.

Each project contains its own README with more detail about its internal structure and responsibilities.

## Testing and CI

IncidentIQ currently uses:

Application unit tests.

ASP.NET Core API integration tests using WebApplicationFactory.

In-memory repositories and messaging fakes for isolated tests.

Local Docker integration testing using the Cosmos DB and Service Bus emulators.

GitHub Actions for restore, build, and test validation.

See tests/README.md for more information.

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

* [x] Create Service Bus Bicep module.

* [x] Define queues/topics and DLQ configuration in Bicep.

* [x] Deploy Service Bus to the Azure development environment.

* [x] Configure Managed Identity/RBAC for Service Bus access where practical.

* [x] Add Service Bus application integration.

* [x] Define `AnalyseIncident` command.

* [x] Connect Incident submission to Service Bus.

* [x] Implement Worker message consumption.

* [x] Implement `Queued → Processing → Completed / Failed` (failed is moved to stage 8).

  * [x] `Queued → Processing → Completed`

* [x] Propagate correlation IDs between API and Worker.

* [x] Add frontend processing-status polling.

## Stage 8 — Reliability & Messaging

* [x] Add transient retry handling.

* [x] Add dead-letter handling.

* [x] Final `Failed` handling after retries are exhausted.

* [x] Make Worker processing idempotent (done via basic state-based idempotency).

* [x] Add processing attempt/failure metadata.

* [ ] Handle Cosmos + Service Bus dual-write consistency.

* [ ] Add admin retry/requeue functionality.

* [ ] Add reliability and duplicate-message tests.
  

## Stage 9 — Application Deployment

Deploy the complete working application to Azure.

* [ ] Create ACR Bicep module.

* [ ] Create Container Apps Environment Bicep module.

* [ ] Create API Container App Bicep module.

* [ ] Create Worker Container App Bicep module.
  * [ ] When deploying worker to ACA make sure application setting matches the bicep queue setting for `MaxDeliveryCount`.

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

* [ ] Measure retrieval relevance / Recall\@K.

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

## Stage 19 - Optional other potential improvements

- [ ] Add Polly maybe?
- [ ] Atm we just have basic state-based idempotency by disallowing work on incidents that are already marked as completed. Could strengthen this by implementing more robust idempotency mechanisms, such as request tokens, distributed locks, or optimistic concurrecy/ eTags.
- [ ] Add a flow chart to show how everything goes together eg the API, Worker but also internally eg Command, Handler, etc. etc. 

## Stage 20 — Optional AI-200 Experiments

Keep experiments isolated from the primary architecture and document the trade-offs discovered.

* [ ] PostgreSQL + pgvector retriever.

* [ ] Azure Managed Redis experiment.

* [ ] AKS Worker deployment.

* [ ] App Service API container deployment.

* [ ] Cosmos Change Feed Runbook indexing experiment.

* [ ] Define experiment infrastructure through Bicep.

* [ ] Compare each experiment against the primary architecture.

* [ ] Document findings and architectural trade-offs.
