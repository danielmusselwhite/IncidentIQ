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

- [x] Keep editable Runbooks separate from derived vectorised `RunbookChunk` documents.
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
* [x] Implement `AzureIncidentAnalyzer`.
* [x] Generate structured summary, likely causes and recommended actions.
* [x] Validate and map Azure AI responses into `IncidentAnalysisResult`.
* [x] Add persisted analysis read path with `IIncidentAnalysisReader` + Cosmos point read.
* [x] Expose persisted AI analysis through `GET /api/incidents/{id}/analysis`.
* [x] Display AI summary, likely causes/confidence, recommended actions and metadata in React.
* [x] Add deterministic `DevelopmentDummyIncidentAnalyzer` for normal local development.
* [x] Handle AI timeout, throttling and transient failure scenarios.
* [x] Add AI latency and failure telemetry.
* [x] Deploy Stage 10 changes to Azure.
* [x] Verify the full `Queued → Processing → AI analysis → Completed` flow.
* [x] Verify AI failure/retry behaviour in Azure.
* [x] Update documentation for Azure AI, local deterministic analysis, resilience, telemetry, and architecture diagrams.

## Stage 11 — Runbook Ingestion & Vector Search

Stage 11 builds a complete Runbook vector-search subsystem: source Runbooks are indexed asynchronously into derived vector chunks, and the API can retrieve semantically related chunks through Cosmos vector search.

### 11A — Runbook Vector Ingestion

* [x] Define `IndexRunbookCommand` and the indexing workflow.
* [x] Define dedicated `RunbookChunk` application and Cosmos persistence models.
* [x] Configure a vector-enabled `RunbookChunks` container locally and through Bicep.
* [x] Configure the `/embedding` 1536-dimension cosine `quantizedFlat` vector index.
* [x] Add the `text-embedding-3-small` Runbook embedding deployment to Azure AI Bicep.
* [x] Add `IEmbeddingGenerator` with deterministic local and Azure OpenAI implementations.
* [x] Implement deterministic overlapping Runbook chunking.
* [x] Implement replace-based Runbook chunk persistence and stale-chunk cleanup.
* [x] Add the `index-runbook` Service Bus queue locally and through Bicep.
* [x] Publish indexing commands from the `Runbooks` Cosmos Change Feed.
* [x] Consume indexing commands with `IndexRunbookWorker`.
* [x] Generate and persist vectorised Runbook chunks.
* [x] Verify create/update indexing end-to-end locally.
* [x] Remove indexed chunks before deleting a Runbook.
* [x] Add ingestion tests and edge-case coverage.
* [x] Deploy Runbook ingestion changes to Azure.
* [x] Verify real Azure embeddings, re-indexing, and deletion cleanup in Azure.
* [x] Update ingestion documentation and architecture notes.

### 11B — Runbook Vector Retrieval

* [x] Define `IRunbookChunkRetriever` and the `RunbookChunkMatch` result model.
* [x] Generate embeddings for retrieval queries through `IEmbeddingGenerator`.
* [x] Implement Cosmos `VectorDistance` retrieval in `CosmosRunbookChunkRetriever`.
* [x] Return top-K relevant Runbook chunks.
* [x] Add metadata filtering, including service filtering.
* [x] Handle empty/no-result retrieval scenarios.
* [x] Add retrieval and API coverage.
* [x] Measure retrieval latency.
* [x] Measure Cosmos request-unit (RU) usage.
* [x] Expose Runbook vector retrieval through `GET /api/Runbooks/search`.
* [x] Fix Cosmos vector-query projection/deserialization into `CosmosRunbookChunkMatchResult`.
* [x] Configure API Managed Identity/RBAC for Azure OpenAI embedding access.
* [x] Pass Azure OpenAI embedding configuration into the API Container App through Bicep.
* [x] Deploy Stage 11B infrastructure/application changes to Azure.
* [x] Verify Runbook vector retrieval end-to-end locally and in Azure.
* [x] Update retrieval documentation and architecture notes.

## Stage 12 — Historical Incident Retrieval & Grounded RAG

### 12A — Historical Incident Vector Retrieval

* [x] Define the searchable historical Incident representation.
* [x] Define dedicated historical Incident vector persistence model and store abstraction.
* [x] Configure the `HistoricalIncidentVectors` Cosmos container locally and through Bicep.
* [x] Configure the `/embedding` 1536-dimension cosine vector index.
* [x] Reuse `IEmbeddingGenerator` to generate embeddings for completed historical Incidents.
* [x] Implement `IndexHistoricalIncidentHandler`.
* [x] Add the `index-historical-incident` Service Bus queue locally and through Bicep.
* [x] Publish historical Incident indexing commands from the `Incidents` Cosmos Change Feed.
* [x] Consume indexing commands with `IndexHistoricalIncidentWorker`.
* [x] Verify completed Incident indexing end-to-end locally.
* [x] Define similar-Incident retrieval abstraction and result model.
* [x] Implement Cosmos `VectorDistance` retrieval for historical Incidents.
* [x] Add service/environment metadata filtering, top-K retrieval and relevance threshold behaviour.
* [x] Add historical Incident retrieval tests and telemetry.
* [x] Verify historical Incident retrieval locally.
* [x] Deploy historical Incident indexing/retrieval changes to Azure.
* [x] Verify historical Incident indexing and retrieval end-to-end in Azure.
* [ ] Update historical Incident retrieval documentation and architecture notes.

### 12B — Combined RAG Context & Grounded Incident Analysis

* [x] Keep historical Incident evidence and Runbook evidence separate in the retrieval model.
* [x] Define the combined RAG context supplied to Incident analysis.
* [x] Build retrieval input from the Incident title, description, symptoms and relevant metadata.
* [x] Retrieve similar historical Incidents.
* [x] Retrieve relevant Runbook chunks.
* [x] Apply relevance gating before retrieved evidence is supplied to the AI.
* [x] Build combined RAG context from historical Incidents and Runbook chunks.
* [x] Generate evidence-backed Incident analysis.
* [x] Include historical Incident and Runbook references in the structured analysis result.
* [x] Validate returned references/citations against the evidence actually retrieved.
* [x] Persist the grounded analysis and the evidence used to generate it.
* [x] Display the persisted analysis, similar-Incident evidence and Runbook evidence in the frontend.
* [x] Add RAG orchestration and evidence-validation tests.
* [x] Verify grounded Incident analysis end-to-end locally.
* [x] Verify grounded Incident analysis end-to-end in Azure.
* [ ] Update RAG documentation and architecture diagrams.

### 12C — Live Similar Incident Discovery

> Deferred — possible future enhancement.
>
> Persisted grounded analyses already expose the historical Incidents used
> during analysis. Live similarity could later provide a current view as new
> Incidents are indexed, but it is not required for the core grounded RAG
> workflow or the initial Operational Assistant.

* [ ] Add an endpoint for retrieving current similar Incidents for an existing Incident.
* [ ] Reuse the Incident's persisted embedding rather than generating a new embedding on every request.
* [ ] Exclude the current Incident from its own similarity results.
* [ ] Display current similar Incidents separately from the evidence used by the original analysis.
* [ ] Add caching design notes for Azure Cache for Redis.
* [ ] Add endpoint/retrieval tests.
* [ ] Verify live similar-Incident discovery in Azure.
### 12D — Interactive Grounded RAG Assistant

Build a dedicated Operational Assistant experience that answers natural-language
engineering questions using historical Incidents and Runbooks as grounded evidence.

The initial Assistant is stateless on the backend. Conversation state is kept in
the React application for the current browser session and is not persisted until
authenticated user identity is available.

#### 12D.1 — Assistant Contracts & Grounding

* [x] Define operational question, conversation-turn and answer contracts.
* [x] Add validation for Assistant questions and optional metadata filters.
* [x] Build retrieval input from the current user question.
* [x] Generate one embedding per user question.
* [x] Retrieve relevant historical Incidents.
* [x] Retrieve relevant Runbook chunks.
* [x] Support optional service/environment retrieval filters.
* [x] Build a combined operational grounding context.
* [x] Keep evidence identifiers request-scoped to each Assistant answer.
* [x] Keep retrieval and AI generation provider-independent in the Application layer.

#### 12D.2 — Grounded Assistant Generation

* [x] Add an `IOperationalAssistant` abstraction.
* [x] Implement the Azure OpenAI Operational Assistant.
* [x] Supply retrieved historical Incident and Runbook evidence to the model.
* [x] Treat retrieved evidence and conversation content as untrusted data rather than instructions.
* [x] Generate natural-language answers constrained to supplied operational evidence.
* [x] Return supporting `HI-*` and `RB-*` evidence references with answer content.
* [x] Validate returned evidence references against the evidence actually supplied.
* [x] Handle questions where no relevant evidence is retrieved.
* [x] Add deterministic Development Assistant behaviour for local development.
* [x] Add timeout, throttling and Azure AI failure handling.
* [x] Reuse shared request-scoped evidence reference generation across grounded AI features.
* [x] Keep conversation history separate from grounding evidence.
* [x] Supply previous conversation turns to the model for follow-up-question continuity.

#### 12D.3 — Stateless Conversational API

* [x] Add an Assistant API endpoint.
* [x] Accept the current question and optional recent conversation history.
* [x] Keep conversation history ephemeral and supplied by the client on each request.
* [x] Use prior conversation turns for conversational continuity without persisting them.
* [x] Keep semantic retrieval based on the current operational question rather than the full conversation history.
* [x] Return the grounded answer together with the historical Incident and Runbook evidence retrieved for that answer.
* [x] Return request-scoped evidence identifiers that correspond to the evidence returned by the API.
* [x] Add API validation and error handling.
* [x] Add Assistant API tests.

#### 12D.4 — Operational Assistant Frontend

* [x] Add an `Assistant` item to the main navigation.
* [x] Add a dedicated `/assistant` page.
* [x] Build a conversational user/Assistant message interface.
* [x] Keep the current conversation in React state only.
* [x] Send recent conversation history with each Assistant request.
* [x] Add optional Service and Environment filters.
* [x] Add a desktop evidence/source panel alongside the conversation.
* [x] Display clickable `HI-*` and `RB-*` references alongside the answer sections they support.
* [x] Allow evidence references to preview/highlight their corresponding source.
* [x] Link historical Incident evidence to Incident details.
* [x] Link Runbook evidence to Runbook details.
* [x] Keep evidence scoped to the Assistant response that retrieved it.
* [x] Add responsive behaviour for smaller screens.
* [x] Add useful empty, loading and error states.
* [x] Add a clear-conversation action.
* [x] Do not persist conversation history before authenticated user identity exists.

#### 12D.5 — Verification

* [x] Add grounding-context orchestration tests.
* [x] Add evidence/citation-validation tests.
* [x] Add Assistant handler tests.
* [x] Add Assistant API mapping tests.
* [ ] Verify multi-turn conversational behaviour end-to-end locally.
* [ ] Verify optional Service and Environment filters through the complete Assistant flow.
* [ ] Verify behaviour when no relevant evidence is retrieved.
* [ ] Verify the Operational Assistant end-to-end in Azure.
* [ ] Verify historical Incident and Runbook citations in real Azure responses.
* [ ] Verify follow-up questions against the real Azure OpenAI implementation.

### 12E — RAG & Architecture Documentation

Consolidate the architecture documentation now that the core asynchronous,
vector-search and grounded-AI flows are implemented.

#### 12E.1 — Important Application Flows

* [x] Create a dedicated documentation folder for important end-to-end flows, for example `docs/flows/`.
* [x] Document Incident submission and asynchronous analysis in a detailed sequence/flow diagram.
  * [x] `User → POST /api/incidents → IncidentsController → CreateIncidentCommand → CreateIncidentHandler`.
  * [x] Show Cosmos persistence and transactional outbox creation.
  * [x] Show Incidents Change Feed relay → Service Bus `analyse-incident`.
  * [x] Show `AnalyseIncidentWorker → AnalyseIncidentHandler`.
  * [x] Show RAG retrieval → Azure OpenAI → completed analysis persistence.
* [x] Document Runbook indexing flow.
  * [x] Runbook create/update → Cosmos Change Feed.
  * [x] `IndexRunbookCommand` → Service Bus.
  * [x] `IndexRunbookWorker`.
  * [x] chunking → embeddings → `RunbookChunks`.
* [x] Document historical Incident indexing flow.
  * [x] completed Incident → Cosmos Change Feed.
  * [x] `IndexHistoricalIncidentCommand`.
  * [x] Service Bus → indexing Worker.
  * [x] embeddings → `HistoricalIncidentVectors`.
* [x] Document grounded Incident-analysis flow.
  * [x] Incident retrieval input.
  * [x] single embedding generation.
  * [x] parallel historical Incident and Runbook retrieval.
  * [x] combined grounding context.
  * [x] Azure OpenAI structured analysis.
  * [x] evidence-reference validation.
  * [x] persisted evidence snapshots.
* [x] Document Operational Assistant flow.
  * [x] React Assistant → `POST /api/assistant/questions`.
  * [x] current question + ephemeral conversation history.
  * [x] embedding generated from the current question.
  * [x] historical Incident and Runbook retrieval.
  * [x] `OperationalQuestionContext`.
  * [x] `AzureOperationalAssistant`.
  * [x] structured grounded response.
  * [x] `HI-*` / `RB-*` validation.
  * [x] answer-specific evidence returned to React.

#### 12E.2 — Architecture Diagrams

* [x] Add a detailed component-level architecture diagram showing the important commands, handlers, repositories, Workers and external Azure services.
* [x] Add a simplified high-level colour-coded architecture diagram suitable for the README/portfolio.
* [x] Clearly distinguish synchronous HTTP flows from asynchronous Service Bus / Change Feed flows.
* [x] Clearly distinguish source documents from derived vector documents.
* [x] Clearly distinguish application orchestration from Infrastructure implementations.
* [x] Show where Azure OpenAI, Cosmos vector search and Service Bus participate in each flow.

#### 12E.3 — LLM & Grounded RAG Documentation

* [x] Document the complete LLM/RAG flow used by Incident analysis.
* [x] Document the complete LLM/RAG flow used by the Operational Assistant.
* [x] Explain why historical Incident evidence and Runbook evidence remain separate.
* [x] Explain request-scoped evidence identifiers such as `HI-1` and `RB-1`.
* [x] Explain why evidence references are validated after model generation.
* [x] Explain why previous Assistant conversation turns provide context but are not treated as grounding evidence.
* [x] Explain why semantic retrieval for the Assistant is based primarily on the current question.
* [x] Explain how prompt-injection risk is reduced by treating retrieved content and previous conversation messages as untrusted data.
* [x] Document behaviour when no relevant grounding evidence is available.

#### 12E.4 — Structured Azure OpenAI Output

* [x] Document `AzureIncidentAnalysisSchema` and its role in enforcing the expected structured Incident-analysis response.
* [x] Document `AzureOperationalAssistantSchema` and its role in enforcing structured Assistant answers.
* [x] Explain the difference between JSON-schema validation and application-level semantic validation.
* [x] Explain why valid `HI-*` / `RB-*` identifiers are validated in application code rather than encoded as a static JSON-schema enum.
* [x] Document the mapping from Azure OpenAI response DTOs into provider-independent Application models.
* [x] Document Azure AI timeout, throttling, transient-failure and invalid-response handling.

#### 12E.5 — Local Development & Live Azure Debugging

* [x] Document the normal deterministic local-development configuration.
  * [x] Development dummy Incident analyser.
  * [x] Development dummy Operational Assistant.
  * [x] Development embedding generator.
  * [x] local Cosmos / Service Bus infrastructure.
* [x] Add a guide for selectively debugging local code against live Azure resources when required.
* [x] Explain how to run the local API or Worker against Azure Cosmos DB.
* [x] Explain how to use real Azure OpenAI embeddings/Chat models from local development.
* [x] Explain how to connect local processing to Azure Service Bus when deliberately testing messaging behaviour.
* [x] Document required Azure authentication using `DefaultAzureCredential` / developer Azure credentials.
* [x] Document the risks of accidentally modifying shared Azure development data while debugging locally.
* [x] Recommend using the normal dummy/emulator configuration by default and live Azure resources only for targeted integration debugging.
* [x] Add example configuration overrides without committing secrets or credentials.
* [x] Add troubleshooting guidance for Managed Identity/RBAC, Service Bus permissions, Cosmos Change Feed leases and Azure OpenAI access.

#### 12E.6 — Final Stage 12 Documentation Review

* [x] Update `docs/DESIGN-DECISIONS.md` with the final Stage 12 architectural decisions.
* [x] Update the root README high-level architecture where necessary.
* [x] Ensure detailed diagrams remain in `docs/` rather than overwhelming the root README.
* [x] Ensure terminology is consistent across code and documentation:
  * [x] Incident.
  * [x] Runbook.
  * [x] grounding evidence.
  * [x] historical Incident retrieval.
  * [x] Operational Assistant.
  * [x] request-scoped evidence references.
* [x] Review all Stage 12 diagrams and documentation against the implemented system.

## Stage 13 — AI Evaluation

Build a repeatable evaluation framework for IncidentIQ's retrieval and grounded AI behaviour.

### 13A — Controlled Evaluation Dataset

* [x] Create an `IncidentIQ.Evaluation` tooling project.
* [x] Define version-controlled evaluation scenario contracts.
* [x] Create realistic synthetic historical Incidents with stable IDs.
* [x] Create realistic synthetic Runbooks with stable IDs.
* [x] Create evaluation scenarios with known expected evidence.
* [x] Include:
  * [x] clear positive retrieval cases.
  * [x] ambiguous cases with multiple relevant sources.
  * [x] irrelevant semantic distractors.
  * [x] no-evidence cases.
  * [x] Service filtering cases.
  * [x] Environment filtering cases.
* [x] Validate that expected evidence IDs exist in the synthetic corpus.
* [x] Keep the dataset deterministic and safe to run repeatedly.
* [x] Document the purpose and limitations of the evaluation dataset.

### 13B — Retrieval Evaluation

* [ ] Build an evaluation runner for historical-Incident retrieval.
* [ ] Build an evaluation runner for Runbook retrieval.
* [ ] Measure Recall@K against expected relevant evidence.
* [ ] Record returned rank and similarity/distance values.
* [ ] Verify metadata filtering behaviour.
* [ ] Add evaluation cases where irrelevant but semantically similar evidence exists.
* [ ] Produce a machine-readable retrieval evaluation report.

### 13C — Citation & Grounding Evaluation

* [ ] Measure citation validity for generated answers.
* [ ] Verify every returned `HI-*` / `RB-*` reference exists in supplied evidence.
* [ ] Measure citation coverage for material claims where practical.
* [ ] Verify no-evidence answers do not invent citations.
* [ ] Record citation evaluation results.

### 13D — Generated Analysis Quality

* [ ] Define a small scoring rubric for generated analysis quality.
* [ ] Evaluate summary quality.
* [ ] Evaluate likely-cause relevance.
* [ ] Evaluate recommended-action relevance.
* [ ] Evaluate appropriate uncertainty when evidence is weak.
* [ ] Record evaluation results separately from deterministic retrieval metrics.
* [ ] Document which quality measures require human judgement.

### 13E — Evaluation Reporting

* [ ] Aggregate retrieval and generation metrics.
* [ ] Record Recall@K and citation-validity results.
* [ ] Add a concise evaluation report under `docs/`.
* [ ] Document dataset size, limitations and known failure cases.
* [ ] Add representative evaluation results to the README without overstating model quality.

### 13F — Engineer Feedback

* [ ] Add useful / not useful feedback for Incident analysis.
* [ ] Add optional engineer feedback comments.
* [ ] Persist feedback against the analysed Incident.
* [ ] Expose feedback through the API.
* [ ] Add frontend feedback controls.
* [ ] Keep feedback separate from automated evaluation metrics.

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

* [ ] Add authenticated Assistant conversation ownership.
* [ ] Persist Assistant conversations per authenticated user.
* [ ] Add conversation history, resume and deletion functionality.

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

* [ ] Add retry/requeue administration to actually call the DLQ retry method we added in stage 8.
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

* [x] Add architecture diagrams.
* [x] Create polished README with infrastructure, internal architecture, and message-flow Mermaid diagrams.

* [ ] Create portfolio demo/video.
* [ ] Perform final end-to-end testing.

## Stage 19 - Optional other potential improvements

- [ ] Revisit a circuit breaker/named resilience pipeline in Stage 15 if telemetry shows it adds value; avoid adding another retry layer by default.
- [ ] Atm we just have basic state-based idempotency by disallowing work on incidents that are already marked as completed. Could strengthen this by implementing more robust idempotency mechanisms, such as request tokens, distributed locks, or optimistic concurrecy/ eTags.

- [x] Add architecture **and** create-incident message-flow diagrams. 
- [ ] See about integrating with repo eg github so it can analyse for potentially breaking changes. (Eg if payments fail it may notice that a commit changed the payment service just before these related incidents started rolling in)
- [ ] **Add redis cache on the similar incidents for faster retrieval and reduced load on the primary datastore.**
- [ ] Maybe add some sort of Agentic automation for handling repetitive incident management tasks. Unsure how well this will fit in though.