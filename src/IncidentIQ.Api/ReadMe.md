# IncidentIQ.Api

ASP.NET Core HTTP/authentication host. Business orchestration lives in Application; Azure-specific implementations live in Infrastructure.

## Responsibilities

- Microsoft Entra JWT validation and delegated-scope enforcement.
- Engineer/Administrator authorization policies.
- Incident, Runbook, Assistant, current-user and Operations endpoints.
- Request/response mapping, Problem Details, CORS, Swagger/OpenAPI and health checks.
- OpenTelemetry export to Application Insights.

## Authentication & Authorization

```text
React/MSAL
→ Authorization: Bearer <token>
→ Microsoft.Identity.Web
→ require access_as_user
→ role policy
→ controller
```

- no/invalid token → `401`,
- missing required scope/role → `403`,
- `Engineer` or `Administrator` → normal product APIs,
- `Administrator` → Operations and retry,
- `/api/health` → anonymous.

Integration tests replace Entra with deterministic test authentication while preserving authorization behaviour.

## Main Endpoints

```text
GET  /api/me

POST /api/incidents
GET  /api/incidents
GET  /api/incidents/{id}
GET  /api/incidents/{id}/analysis
POST /api/incidents/{id}/retry               # Administrator

POST   /api/runbooks
GET    /api/runbooks
GET    /api/runbooks/search
GET    /api/runbooks/{id}
PUT    /api/runbooks/{id}
DELETE /api/runbooks/{id}

POST /api/assistant/questions

GET /api/operations/summary                  # Administrator
GET /api/operations/failed-incidents         # Administrator

GET /api/health                              # anonymous
```

## Observability

The API exports telemetry with role name `IncidentIQ.Api`. Incident create/retry uses the current trace ID as the application correlation ID and persists W3C trace context into the asynchronous analysis command/outbox.

See [Observability & Scaling](../../docs/OBSERVABILITY.md).
