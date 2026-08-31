# IncidentIQ.Api

`IncidentIQ.Api` is the ASP.NET Core HTTP API used by the React frontend.

It is intentionally thin: controllers handle HTTP concerns and delegate use-case behaviour to `IncidentIQ.Application`.

## Responsibilities

Current API responsibilities include:

- Incident create/read endpoints.
- Runbook CRUD endpoints.
- Mapping HTTP contracts to Application commands.
- Problem Details error responses.
- Correlation ID creation/propagation.
- Health checks.
- Swagger/OpenAPI during development.
- CORS.
- Initial Application Insights / OpenTelemetry integration.

The API does not directly contain Cosmos persistence logic or incident-analysis Worker logic.

## Incident Creation Flow

```text
POST /api/incidents
      ↓
IncidentsController
      ↓
CreateIncidentHandler
      ↓
IIncidentSubmissionStore
      ↓
Infrastructure persists:
├── Incident
└── AnalyseIncident Outbox
```

The API no longer publishes the analysis command directly to Service Bus. That happens asynchronously through the Cosmos Change Feed and `IncidentOutboxWorker`.

## Current Endpoints

Incident endpoints:

```text
POST /api/incidents
GET  /api/incidents
GET  /api/incidents/{id}
```

Runbook endpoints:

```text
POST   /api/runbooks
GET    /api/runbooks
GET    /api/runbooks/{id}
PUT    /api/runbooks/{id}
DELETE /api/runbooks/{id}
```

Health:

```text
GET /api/health
```

## Structure

```text
IncidentIQ.Api/
├── Contracts/
│   ├── Incidents/
│   └── Runbooks/
├── Controllers/
├── ExceptionHandling/
├── Properties/
├── Program.cs
└── appsettings.json
```

- **Contracts** define HTTP request/response shapes.
- **Controllers** translate HTTP requests into Application calls.
- **ExceptionHandling** converts application/domain errors into consistent Problem Details responses.

## Correlation IDs

Incident creation creates or propagates a correlation ID which is stored in the analysis command and later added to Worker logging scope.

This allows the same workflow to be traced across the HTTP request, persisted outbox record, Service Bus command, and Worker processing.

## Configuration

Configuration comes from normal ASP.NET Core providers such as appsettings, environment variables, and user-secrets.

For local/Azure configuration instructions, see the [Development Guide](../../docs/DEVELOPMENT.md).

## Design Approach

- Controllers handle HTTP concerns only.
- Application handlers orchestrate use cases.
- Domain objects own business rules.
- Infrastructure owns Cosmos DB and Service Bus implementations.
- Asynchronous processing remains outside the HTTP request.
