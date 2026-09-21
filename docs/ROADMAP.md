# IncidentIQ Roadmap

Completed stages are intentionally summarized; active/future work stays detailed.

## Stage 1 — Foundation
- [x] Solution structure, API, React Web, Worker, Domain/Application/Infrastructure and health endpoint.

## Stage 2 — Local Cosmos
- [x] Docker Compose, Cosmos emulator, persistence configuration and Incident repository.

## Stage 3 — Incident API
- [x] Incident create/read APIs, validation, Problem Details and tests.
- [ ] Paginate `GET /api/incidents`.

## Stage 4 — Incident Frontend
- [x] Submit, dashboard, detail/status polling, layout and error/loading states.
- [ ] Update dashboard for paginated API results.

## Stage 5 — Azure Foundation
- [x] Bicep, GitHub OIDC, Cosmos, monitoring, Managed Identity/RBAC and first Azure verification.

## Stage 6 — Runbooks
- [x] Runbook domain/persistence, CRUD API/UI and tests.

## Stage 7 — Async Processing
- [x] Service Bus, `AnalyseIncidentCommand`, Worker consumption, correlation IDs and status polling.

## Stage 8 — Reliability
- [x] Retries, DLQ, failure metadata, basic idempotency, transactional outbox and backend retry/requeue.
- [ ] Admin retry UI moved to Stage 16.

## Stage 9 — Azure Deployment
- [x] ACR, Container Apps, Static Web Apps, RBAC and CI/CD deployment.

## Stage 10 — Azure AI
- [x] Structured Incident analysis, Azure OpenAI integration, resilience, telemetry and persisted analysis UI.

## Stage 11 — Runbook Vector Search
- [x] Async chunk indexing, embeddings, Cosmos vector storage/retrieval, filters, latency/RU measurement and Azure verification.

## Stage 12 — Historical Retrieval & Grounded RAG
- [x] Historical Incident indexing/retrieval.
- [x] Combined historical-Incident + Runbook grounding.
- [x] Evidence validation and persisted evidence snapshots.
- [x] Stateless Operational Assistant with answer-scoped evidence.
- [x] Architecture/RAG documentation.
- [ ] Optional final real-Azure Assistant verification cases.
- [ ] Live similar-Incident endpoint deferred.

## Stage 13 — AI Evaluation
- [x] Controlled synthetic dataset.
- [x] Precision@K / Recall@K retrieval evaluation.
- [x] Citation-validity and no-evidence checks.
- [x] Human-review rubric and documented baseline.

## Stage 14 — Security & Configuration

### 14A — Protect the API
- [x] `IncidentIQ API` Entra registration.
- [x] `access_as_user` delegated scope.
- [x] `Microsoft.Identity.Web` JWT bearer authentication.
- [x] Authentication before authorization middleware.
- [x] Protected controller endpoints.
- [x] Anonymous `/api/health`.
- [x] Authentication integration tests.
- [x] Real local Entra token verified.

### 14B — Authenticate React
- [x] `IncidentIQ Web` SPA registration.
- [x] Local + Azure Static Web Apps redirect URIs.
- [x] Delegated API permission.
- [x] MSAL integration and authentication gate.
- [x] Shared token-aware API client.
- [x] Silent token acquisition.
- [x] Login/logout and real user profile.
- [x] Local React → Entra → API flow verified.

### 14C — Engineer / Administrator Authorization
- [ ] Define Entra app roles: `Engineer`, `Administrator`.
- [ ] Configure role claims.
- [ ] Add Engineer and Administrator authorization policies.
- [ ] Let Administrators satisfy normal Engineer access.
- [ ] Protect Incident, Runbook and Assistant endpoints with Engineer access.
- [ ] Protect retry/requeue with Administrator access.
- [ ] Add authorization tests for allowed/forbidden cases.

### 14D — Azure Deployment & Configuration Review
- [ ] Pass API Entra configuration to Container Apps.
- [ ] Pass SPA Entra configuration into Vite deployment build.
- [ ] Verify hosted sign-in and authenticated API calls.
- [ ] Verify Engineer/Admin behavior in Azure.
- [ ] Review Managed Identity/RBAC for least privilege.
- [ ] Keep tenant/client/scope IDs as configuration, not secrets.
- [ ] Document final security model.

## Stage 15 — Observability & Scaling
- [ ] Add/complete OpenTelemetry instrumentation.
- [ ] Trace API → Service Bus → Worker → retrieval → AI → Cosmos.
- [ ] Add useful KQL/metrics for failures, queue wait, processing and AI latency.
- [ ] Add Service Bus queue-depth KEDA scaling.
- [ ] Verify controlled scale-out.

## Stage 16 — Minimal Operations
- [ ] Build failed-analysis Operations page.
- [ ] Show failure reason, attempts, timestamps and correlation ID.
- [ ] Connect Administrator-only retry/requeue.
- [ ] Link failures back to Incident details/Application Insights.

## Stage 17 — Portfolio Hardening
- [ ] Full deployed end-to-end verification.
- [ ] Final demo dataset and UX cleanup.
- [ ] Final evaluation baseline.
- [ ] Final README/architecture/screenshots/demo video.
- [ ] Repository cleanup and tagged portfolio release.

# Future Extensions

- [ ] API Management / Azure App Configuration.
- [ ] Key Vault if a genuine runtime secret is introduced.
- [ ] Event Grid + integration Function.
- [ ] Engineer feedback on AI analysis.
- [ ] Advanced operations/DLQ tooling.
- [ ] Persisted Assistant conversations.
- [ ] Advanced retrieval/reranking/Redis experiments.
- [ ] GitHub change intelligence.
- [ ] Agentic operations with explicit approval.
- [ ] Stronger concurrency/idempotency.
- [ ] Automated semantic evaluation beyond retrieval/citation validity.
