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

Build a concise, repeatable evaluation framework demonstrating that IncidentIQ's
retrieval and grounded AI behaviour can be measured rather than assessed only
through ad-hoc testing.

### 13A — Controlled Evaluation Dataset

* [x] Create an `IncidentIQ.Evaluation` tooling project.
* [x] Define version-controlled evaluation scenario contracts.
* [x] Create realistic synthetic historical Incidents and Runbooks with stable IDs.
* [x] Create evaluation scenarios with known expected evidence.
* [x] Cover:
  * [x] clear positive retrieval cases.
  * [x] ambiguous/multi-source cases.
  * [x] irrelevant semantic distractors.
  * [x] no-evidence cases.
  * [x] Service filtering.
  * [x] Environment filtering.
* [x] Validate expected evidence against the controlled corpus.
* [x] Document dataset purpose and limitations.

### 13B — Retrieval Evaluation

* [x] Evaluate historical-Incident retrieval.
* [x] Evaluate Runbook retrieval.
* [x] Measure Precision@K and Recall@K.
* [x] Record ranking and cosine-distance information.
* [x] Evaluate no-evidence behaviour separately.
* [x] Verify metadata filtering behaviour.
* [x] Produce a machine-readable evaluation report.

### 13C — Citation & Grounding Evaluation

* [x] Generate grounded responses through the real Azure AI implementations.
* [x] Measure citation validity for generated answers.
* [x] Verify every returned `HI-*` / `RB-*` reference exists in supplied evidence.
* [x] Verify no-evidence answers do not invent citations.
* [x] Record citation evaluation results in the machine-readable report.
* [x] Document semantic claim-level citation coverage as requiring human judgement rather than treating it as a deterministic metric.

### 13D — Generated Analysis Quality

* [x] Define a lightweight human-review rubric covering:
  * [x] summary quality.
  * [x] likely-cause relevance.
  * [x] recommended-action relevance.
  * [x] appropriate uncertainty.
  * [x] grounding in supplied evidence.
* [x] Preserve the actual generated AI response for review.
* [x] Generate a Markdown human-review report for evaluation runs.
* [x] Keep subjective generated-answer quality separate from deterministic retrieval and citation metrics.
* [x] Review representative evidence-backed, ambiguous and no-evidence scenarios.
* [x] Record the no-evidence generic-guidance behaviour as a known grounding limitation.
* [x] Document the limitations of automated evaluation for subjective response quality.

### 13E — Evaluation Documentation

* [x] Record retrieval and citation-validity baselines.
* [x] Document dataset size and methodology.
* [x] Document evaluation limitations and known imperfect retrieval behaviour.
* [x] Document generated evaluation outputs:
  * [x] machine-readable JSON report.
  * [x] human-review Markdown report.
* [x] Add dedicated AI evaluation documentation.
* [x] Record representative human-review findings.

---

## Stage 14 — Security & Configuration

Add production-style user authentication and authorization while preserving the
existing Managed Identity model used for service-to-service Azure access.

### 14A — Protect the API with Microsoft Entra

* [x] Create/configure a Microsoft Entra application registration for the IncidentIQ API.
* [x] Expose a delegated API scope such as `access_as_user`.
* [x] Add Microsoft Entra JWT bearer authentication to the ASP.NET Core API.
* [x] Validate issuer, audience and access tokens through the Microsoft identity platform integration.
* [x] Add authentication middleware before authorization middleware.
* [x] Protect application API controllers with authenticated-user authorization.
* [x] Keep `/api/health` anonymous for platform/container health checks.
* [x] Add API tests verifying:
  * [x] unauthenticated requests return `401 Unauthorized`.
  * [x] authenticated requests can reach protected endpoints.
* [x] Verify protected API behaviour locally.

### 14B — Authenticate the React Application

* [x] Create/configure a separate Microsoft Entra SPA application registration.
* [x] Configure local and Azure Static Web Apps redirect URIs.
* [x] Grant the SPA delegated access to the IncidentIQ API scope.
* [x] Add MSAL authentication to the React application.
* [x] Add a shared authentication configuration.
* [x] Add a shared authenticated API client.
* [x] Acquire API access tokens silently where possible.
* [x] Send bearer access tokens with IncidentIQ API requests.
* [x] Show a sign-in experience for unauthenticated users.
* [x] Replace the hard-coded Development User profile with authenticated user information.
* [x] Add sign-out functionality.
* [x] Verify authentication locally through the complete React → API flow.

### 14C — Engineer / Administrator Authorization

* [x] Define Microsoft Entra application roles:
  * [x] `Engineer`.
  * [x] `Administrator`.
* [x] Configure role claims in API access tokens.
* [x] Define ASP.NET Core authorization policies:
  * [x] Engineer access (default)
  * [x] Administrator-only access (atm just for retry endpoint)
* [x] Allow Administrators to satisfy normal Engineer-level application access.
* [x] Protect normal Incident functionality with Engineer-level access.
* [x] Protect normal Runbook functionality with Engineer-level access.
* [x] Protect Operational Assistant functionality with Engineer-level access.
* [x] Protect `POST /api/incidents/{id}/retry` with Administrator authorization.
* [x] Add authorization tests covering Engineer and Administrator boundaries.
* [x] Verify expected `403 Forbidden` behaviour for authenticated users without the required role.

### 14D — Azure Deployment & Configuration Review

* [x] Pass API Entra configuration to the API Container App.
* [x] Pass SPA Entra configuration into the Vite production build.
* [x] Deploy the authentication and authorization changes to Azure.
* [x] Verify Azure sign-in through the hosted React application.
* [x] Verify authenticated React → API calls in Azure.
* [x] Verify Engineer functionality in Azure.
* [x] Verify Administrator-only functionality in Azure.
* [x] Verify unauthenticated API access is rejected.

#### Secrets & Configuration Review

* [x] Review application configuration and classify values as secrets or non-secret configuration.
* [x] Keep Microsoft Entra tenant IDs, client IDs and scope identifiers as normal configuration.
* [x] Confirm browser-delivered SPA configuration contains no client secret.
* [x] Continue using Managed Identity for production access to:
  * [x] Cosmos DB.
  * [x] Service Bus.
  * [x] Azure OpenAI.
  * [x] Azure Container Registry.
* [x] Keep local/emulator credentials outside source control through user-secrets or local configuration.
* [x] Review existing Managed Identity and Azure RBAC assignments for least privilege.
* [x] Document the final authentication, authorization and workload-identity model.

---

## Stage 15 — Operations & Administration

Build a useful Administrator experience for inspecting and recovering failed analysis work.

### 15A — Role-Aware Operations UI

* [x] Add `GET /api/me` to expose the authenticated user's roles.
* [x] Make roles available to the React application.
* [x] Show Administrator-only navigation/actions where appropriate.
* [x] Keep API authorization as the real security boundary.
* [x] Complete the `/operations` page with loading, error and empty states.

### 15B — Failed Incident Operations

* [x] Display failed Incidents on the Operations page.
* [x] Add a dedicated operational API query if the existing Incident endpoints are insufficient.
* [x] Show:
  * [x] Incident/title.
  * [x] service/environment.
  * [x] failure reason.
  * [x] attempt count.
  * [x] timestamps.
  * [x] correlation ID.
* [x] Link failures back to the Incident detail page.

### 15C — Retry & Recovery

* [x] Connect the UI to `POST /api/incidents/{id}/retry`.
* [x] Restrict retry controls to Administrators.
* [x] Add confirmation and success/error feedback.
* [x] Refresh state after retry.
* [x] Verify the full:
  * [x] Failed → Queued → Processing → Completed/Failed flow.
* [x] Verify Engineers cannot retry through direct API calls.

### 15D — Verification

* [ ] Test Administrator and Engineer behaviour.
* [ ] Add API tests for new operational endpoints.
* [ ] Verify the Operations workflow locally and in Azure.

---

## Stage 16 — Observability & Scaling

Add production-style tracing, metrics and scaling, then expose a small amount of useful operational data in `/operations`.

### 16A — OpenTelemetry & Distributed Tracing

* [ ] Complete OpenTelemetry instrumentation for API and Worker.
* [ ] Export telemetry to Application Insights.
* [ ] Trace the full workflow through:
  * [ ] API.
  * [ ] Cosmos/outbox.
  * [ ] Service Bus.
  * [ ] Worker.
  * [ ] retrieval.
  * [ ] Azure AI.
  * [ ] persistence.
* [ ] Ensure correlation IDs can be used to locate workflows.

### 16B — Operational Metrics & KQL

* [ ] Record:
  * [ ] queue wait duration.
  * [ ] processing duration.
  * [ ] AI latency.
  * [ ] failures.
  * [ ] retries.
* [ ] Add useful KQL queries for:
  * [ ] failures.
  * [ ] average/P95 processing time.
  * [ ] queue wait.
  * [ ] AI latency/failures.
  * [ ] correlation ID lookup.

### 16C — Operations Dashboard

* [ ] Add a small operational summary to `/operations`.
* [ ] Consider:
  * [ ] failed/queued/processing counts.
  * [ ] recent successes/failures.
  * [ ] processing duration.
  * [ ] AI latency.
  * [ ] DLQ/queue health.
* [ ] Keep detailed diagnostics in Application Insights rather than recreating Azure Monitor.

### 16D — Worker Scaling

* [ ] Configure Container Apps/KEDA scaling from Service Bus queue depth.
* [ ] Define sensible min/max replicas.
* [ ] Verify safe multi-replica processing.
* [ ] Verify scale-out and scale-in using a controlled workload.

### 16E — Verification

* [ ] Trace successful, failed and retried analyses end-to-end.
* [ ] Verify metrics and KQL queries against real telemetry.
* [ ] Verify Operations-page metrics.
* [ ] Verify Worker scaling in Azure.
* [ ] Update observability documentation.

---

## Stage 17 — Final Portfolio Hardening

Turn the implemented system into a finished portfolio piece.

### 17A — End-to-End Verification

* [ ] Run the complete deployed workflow:
  * [ ] authenticate.
  * [ ] submit Incident.
  * [ ] enqueue analysis.
  * [ ] retrieve grounding evidence.
  * [ ] generate grounded analysis.
  * [ ] persist result.
  * [ ] display result.
* [ ] Verify failure and retry behaviour.
* [ ] Verify authorization boundaries.
* [ ] Verify distributed tracing.
* [ ] Verify Worker scaling if implemented.
* [ ] Run the AI evaluation suite and record the final baseline.

### 17B — Demo Data & UX

* [ ] Seed a small realistic demonstration dataset.
* [ ] Perform final frontend styling and UX cleanup.
* [ ] Ensure loading, empty, processing, failure and completed states are polished.
* [ ] Ensure authentication/sign-out states are polished.
* [ ] Ensure the Operational Assistant is demo-ready.

### 17C — Portfolio Documentation

* [ ] Update architecture diagrams to match the final implementation.
* [ ] Update the root README.
* [ ] Add the concise AI evaluation summary deferred from Stage 13.
* [ ] Document:
  * [ ] system architecture.
  * [ ] asynchronous processing.
  * [ ] RAG/grounding architecture.
  * [ ] reliability/outbox design.
  * [ ] authentication and authorization.
  * [ ] Managed Identity and Azure RBAC.
  * [ ] observability/scaling.
  * [ ] AI evaluation.
* [ ] Include representative screenshots.
* [ ] Create a short architecture/demo video.
* [ ] Perform final repository cleanup.

### 17D — Final Release

* [ ] Deploy the final portfolio version.
* [ ] Perform final smoke testing.
* [ ] Tag a portfolio/demo release.
* [ ] Mark IncidentIQ feature-complete.

---

# Extensions / Future Work

The following ideas are intentionally excluded from the core portfolio build.
They may be revisited if IncidentIQ is developed beyond its portfolio scope.

## Extension A — API Management & Centralised Configuration

* [ ] Add Azure API Management.
* [ ] Route public API traffic through APIM.
* [ ] Add APIM policies such as rate limiting where useful.
* [ ] Add Azure App Configuration.
* [ ] Centralise application configuration where this provides genuine operational value.

These are valuable production technologies, but the portfolio already demonstrates
Azure infrastructure, authentication, Managed Identity, RBAC and configuration.
They are not required to demonstrate the core IncidentIQ architecture.

## Extension B — External Secrets / Key Vault

* [ ] Add Azure Key Vault when the application has a genuine runtime secret requiring secure storage.
* [ ] Grant application Managed Identities least-privilege Key Vault access.
* [ ] Reference secrets from Container Apps without embedding secret values in application configuration.

Do not introduce Key Vault solely to store non-secret values such as tenant IDs,
client IDs, resource endpoints or scope names.

## Extension C — Event-Driven Integrations

* [ ] Create Event Grid infrastructure.
* [ ] Publish `AnalysisCompleted` / `AnalysisFailed` integration events.
* [ ] Add a Python Azure Function consumer.
* [ ] Use completion events for notifications, auditing or external integrations.

The core asynchronous architecture is already demonstrated through Service Bus and
the transactional outbox. Event Grid should be added only when a genuine integration
requires fan-out rather than purely to demonstrate another Azure service.

## Extension D — Engineer Feedback

* [ ] Add useful / not useful feedback for Incident analysis.
* [ ] Add optional engineer comments.
* [ ] Persist feedback against analysed Incidents.
* [ ] Add frontend feedback controls.
* [ ] Use feedback as a future evaluation/product-improvement signal.

## Extension E — Advanced Operations

* [ ] Custom operational dashboard.
* [ ] Queue-depth visualisation.
* [ ] Worker replica/scaling visualisation.
* [ ] DLQ browser.
* [ ] Advanced replay/reprocessing tooling.

Prefer Application Insights and Azure Monitor for these concerns unless a dedicated
IncidentIQ operations experience becomes a product requirement.

## Extension F — Assistant Conversations

* [ ] Persist Assistant conversations per authenticated user.
* [ ] Add conversation history.
* [ ] Resume previous conversations.
* [ ] Delete conversations.
* [ ] Add conversation ownership rules.

## Extension G — Advanced Retrieval

* [ ] Investigate Redis caching for frequently retrieved similar Incidents.
* [ ] Evaluate retrieval thresholds/reranking.
* [ ] Evaluate production Cosmos vector-engine parity against the controlled evaluation corpus.
* [ ] Expand the evaluation dataset using additional realistic scenarios.

## Extension H — Change Intelligence

* [ ] Integrate with source-control providers such as GitHub.
* [ ] Retrieve recent deployments/commits for affected services.
* [ ] Correlate operational failures with recent changes.
* [ ] Include relevant changes as another grounded evidence source.

## Extension I — Agentic Operations

* [ ] Investigate agentic workflows for repetitive Incident-management tasks.
* [ ] Require explicit approval before performing operational actions.
* [ ] Add appropriate audit and authorization boundaries before allowing automated remediation.

## Extension J — Additional Reliability Hardening

* [ ] Evaluate stronger idempotency using request tokens / optimistic concurrency if required.
* [ ] Revisit circuit breakers if production telemetry demonstrates a need.
* [ ] Add additional deployment/smoke-test automation if IncidentIQ becomes continuously operated rather than primarily demonstrated.

## Extension K - Improved Evaluation

* [ ] Add some form of automated evaluation for the AI analysis, as atm we only automatically evaluate the retrieval and ranking of similar Incidents. With the AI analysis done manually.