# IncidentIQ.Api

ASP.NET Core HTTP/authentication host. Business orchestration lives in Application; Azure-specific implementations live in Infrastructure.

## Responsibilities

- Microsoft Entra JWT validation.
- `access_as_user` authorization.
- Incident, Runbook and Assistant endpoints.
- Request/response mapping, validation and Problem Details.
- CORS, Swagger/OpenAPI, health checks and telemetry.

## Authentication

```text
React/MSAL
→ Authorization: Bearer <token>
→ Microsoft.Identity.Web
→ authenticated ClaimsPrincipal
→ require access_as_user
→ controller
```

```csharp
app.MapControllers()
    .RequireAuthorization()
    .RequireScope("access_as_user");
```

Behavior:
- no/invalid token → `401`,
- valid token without required permission → `403`,
- valid token with `access_as_user` → controller executes.

`GET /api/health` is intentionally anonymous.

Integration tests replace Entra with a deterministic test authentication handler while preserving normal authorization behavior.

## Main Endpoints

```text
POST /api/incidents
GET  /api/incidents
GET  /api/incidents/{id}
GET  /api/incidents/{id}/analysis
POST /api/incidents/{id}/retry

POST   /api/runbooks
GET    /api/runbooks
GET    /api/runbooks/search
GET    /api/runbooks/{id}
PUT    /api/runbooks/{id}
DELETE /api/runbooks/{id}

POST /api/assistant/questions

GET /api/health
```

Engineer/Administrator role policies are added in Stage 14C.
