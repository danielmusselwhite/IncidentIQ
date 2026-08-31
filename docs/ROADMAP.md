# IncidentIQ Development Roadmap

This file tracks implementation progress and future stages. The root README stays intentionally high-level; detailed design rationale lives in `docs/DESIGN-DECISIONS.md`.

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

* [x] Connect Incident submission to the asynchronous analysis pipeline.
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
* [x] Handle Cosmos + Service Bus dual-write consistency.

  * [x] Done via outbox pattern to ensure eventual consistency between Cosmos DB and Service Bus.

* [x] Add admin retry/requeue functionality.

  * [x] Only the backend logic and API endpoint

  * [ ] Will later in stage 16 include the front-end side on  the admin operations page

* [x] Add reliability and duplicate-message tests.

## Stage 9 — Azure Deployment

Deploy the complete working application to Azure.

### 9A — RBAC
* [x] Align Azure RBAC with the transactional outbox architecture.

### 9B — Azure Container Registry
* [x] Create ACR Bicep module.
* [x] Configure managed identity image pull access.

### 9C — Container Apps Environment
* [x] Create Container Apps Environment Bicep module.
* [x] Connect it to the existing Log Analytics workspace.

### 9D — API Container App
* [x] Create API Container App Bicep module.
* [x] Configure ingress, managed identity and Cosmos settings.

### 9E — Worker Container App
* [x] Create Worker Container App Bicep module.
* [x] Configure managed identity, Cosmos and Service Bus settings.
* [x] Ensure `MaxDeliveryCount` matches the Service Bus queue configuration.

### 9F — Frontend Hosting
* [x] Create frontend hosting infrastructure.
* [x] Configure frontend → API connectivity.

### 9G — CI/CD & Deployment
* [x] Add API/Worker container build and publish workflow.
* [x] Build and push images to ACR.
* [x] Deploy API, Worker and React frontend.

### 9H — Azure Verification
* [x] Verify the complete asynchronous workflow in Azure.
* [x] Verify retry and failure behaviour.

## Stage 10 — Azure AI

Integrate real Azure AI analysis into the deployed IncidentIQ workflow.

* [x] Define structured incident analysis contracts.
* [x] Add `IIncidentAnalyzer` abstraction.
* [x] Add structured analysis persistence model.
* [x] Persist completed Incident + analysis atomically in Cosmos.
* [x] Update analysis handler and tests for the new AI flow.
* [x] Create Azure AI resource/deployment Bicep.
* [x] Configure Worker Managed Identity/RBAC for Azure AI.
* [x] Pass Azure AI configuration into the Worker Container App.
* [ ] Implement `AzureIncidentAnalyzer`.
* [ ] Generate structured summary, likely causes and recommended actions.
* [ ] Validate and map Azure AI responses into `IncidentAnalysisResult`.
* [ ] Handle AI timeout, throttling and transient failure scenarios.
* [ ] Add AI latency and failure telemetry.
* [ ] Expose persisted analysis through the API.
* [ ] Display AI-generated analysis in the React frontend.
* [ ] Deploy Stage 10 changes to Azure.
* [ ] Verify the full `Queued → Processing → AI analysis → Completed` flow.
* [ ] Verify AI failure/retry behaviour in Azure.

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

- [x] Add architecture and create-incident message-flow diagrams. 
- [ ] See about integrating with repo eg github so it can analyse for potentially breaking changes. (Eg if payments fail it may notice that a commit changed the payment service just before these related incidents started rolling in)

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
