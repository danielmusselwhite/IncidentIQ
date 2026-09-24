# IncidentIQ Development Roadmap

This roadmap is intentionally concise. Detailed architectural rationale lives in [Design Decisions](DESIGN-DECISIONS.md), runtime flows in [flows/](flows/README.md), and observability verification in [Observability](OBSERVABILITY.md).

## Completed Foundation

| Stage | Outcome | Status |
| --- | --- | --- |
| 1 | Solution, React, API, Worker and Clean Architecture structure | ✅ |
| 2 | Local Cosmos infrastructure and Incident persistence | ✅ |
| 3 | Incident API, validation and Problem Details | ✅ |
| 4 | Incident frontend and API integration | ✅ |
| 5 | First Azure dev environment, Bicep, OIDC and initial telemetry | ✅ |
| 6 | Runbook CRUD and management UI | ✅ |
| 7 | Service Bus asynchronous Incident analysis | ✅ |
| 8 | Retry/DLQ/idempotency and transactional outbox reliability | ✅ |
| 9 | ACR, Container Apps, Static Web Apps and CI/CD deployment | ✅ |
| 10 | Azure OpenAI structured Incident analysis | ✅ |
| 11 | Runbook chunking, embeddings and Cosmos vector retrieval | ✅ |
| 12 | Historical Incident retrieval, grounded RAG and Operational Assistant | ✅ |
| 13 | Repeatable retrieval/citation evaluation framework | ✅ |

Deferred from early stages: pagination for `GET /api/incidents` and the Incident list UI.

## Stage 14 — Security & Configuration ✅

- [x] Microsoft Entra-protected API with delegated `access_as_user` scope.
- [x] React/MSAL login, token acquisition and sign-out.
- [x] `Engineer` and `Administrator` application roles.
- [x] Engineer-level policies for normal Incident/Runbook/Assistant use.
- [x] Administrator-only retry.
- [x] Managed Identity retained for Cosmos, Service Bus, Azure OpenAI and ACR.
- [x] Authentication/authorization integration tests.
- [x] Azure deployment and role verification.

## Stage 15 — Operations & Administration ✅

- [x] `GET /api/me` exposes current-user roles to React.
- [x] Role-aware navigation and Administrator-only Operations route.
- [x] `GET /api/operations/failed-incidents` for failed analysis work.
- [x] Failed Incident table with service/environment, severity, attempts, timestamps and failure reason.
- [x] Retry UI connected to `POST /api/incidents/{id}/retry`.
- [x] Confirmation, success/error feedback and refresh after retry.
- [x] API remains the security boundary; direct Engineer retry is forbidden.

## Stage 16 — Observability & Scaling

### 16A — Distributed Tracing

- [x] OpenTelemetry configured for API and Worker.
- [x] Distinct Application Insights role names: `IncidentIQ.Api` and `IncidentIQ.Worker`.
- [x] W3C trace context persisted through the Cosmos outbox.
- [x] Custom spans for outbox relay, analysis, retrieval, AI generation and persistence.
- [ ] Verify successful, failed and retried traces end-to-end in Azure.

### 16B — Operational Metrics & KQL

- [x] Queue-wait histogram.
- [x] Processing-duration histogram.
- [x] AI-duration histogram.
- [x] Terminal-failure counter.
- [x] Administrator-retry counter.
- [x] Document KQL for correlation lookup, latency and counters.
- [ ] Verify all custom metrics in Application Insights.

### 16C — Operations Summary

- [x] Administrator summary endpoint.
- [x] `/operations` counts for Total, Queued, Processing, Completed and Failed.
- [x] Keep detailed telemetry in Application Insights rather than duplicating Azure Monitor in React.

### 16D — Worker Scaling

- [x] KEDA Service Bus queue-depth rule on `analyse-incident`.
- [x] Configure 1–3 replicas, target 2 queued messages per replica, 15-second polling.
- [x] Keep minimum 1 because the host also runs Cosmos Change Feed relays.
- [x] Grant queue-scoped Service Bus Data Owner access needed by the scaler.
- [ ] Verify scale-out under controlled backlog.
- [ ] Verify scale-in back to one replica.
- [ ] Confirm multi-replica processing preserves expected idempotency/reliability behaviour.

### 16E — Azure Verification

- [ ] Deploy the Stage 16 branch.
- [ ] Verify one successful analysis trace.
- [ ] Verify one failed analysis trace and terminal-failure metric.
- [ ] Retry a failed Incident and verify trace + retry metric.
- [ ] Verify queue-wait, processing and AI-duration metrics.
- [ ] Verify Operations summary counts.
- [ ] Verify KEDA scale-out/scale-in.
- [ ] Record any final fixes and mark Stage 16 complete.

## Stage 17 — Final Portfolio Hardening

### End-to-End Verification

- [ ] Run the complete deployed workflow from authentication through grounded analysis persistence/display.
- [ ] Verify failure/retry and authorization boundaries.
- [ ] Re-run the AI evaluation suite and record the final baseline.

### Demo & UX

- [ ] Seed a small realistic demonstration dataset.
- [ ] Final frontend styling and state/error polish.
- [ ] Ensure Operational Assistant and Administrator flows are demo-ready.

### Portfolio Documentation

- [ ] Final architecture/documentation pass after Stage 16 verification.
- [ ] Add representative screenshots.
- [ ] Create a short architecture/demo video.
- [ ] Perform repository cleanup.

### Release

- [ ] Deploy final portfolio version.
- [ ] Smoke test.
- [ ] Tag a portfolio/demo release.
- [ ] Mark IncidentIQ feature-complete.

## Future Work

Intentionally outside the core portfolio scope:

- API Management / Azure App Configuration when they provide real operational value.
- Key Vault when a genuine runtime secret exists.
- Event Grid completion integrations / notification Functions.
- Engineer feedback on AI analysis.
- Advanced DLQ replay and attempt-history tooling.
- Persistent Assistant conversations.
- Retrieval reranking/threshold experiments and a larger evaluation corpus.
- GitHub/deployment change intelligence as another grounding source.
- Agentic operational workflows with explicit approval/audit boundaries.
- Stronger concurrency-safe idempotency with ETags/optimistic concurrency.
- Split relay/consumer Worker hosts if true scale-to-zero becomes valuable.
