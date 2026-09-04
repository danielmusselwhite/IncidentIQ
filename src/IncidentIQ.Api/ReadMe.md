# IncidentIQ.Api

`IncidentIQ.Api` is the ASP.NET Core HTTP API used by the React frontend.

It is intentionally thin: controllers handle HTTP concerns and delegate use-case behaviour to `IncidentIQ.Application`.

## Responsibilities

Current API responsibilities include:

- Incident create/read endpoints.
- Persisted Incident analysis retrieval.
- Runbook CRUD endpoints.
- Backend retry/requeue endpoint for failed analysis.
- Mapping HTTP contracts to Application commands/handlers.
- Problem Details error responses.
- Correlation ID creation/propagation.
- Health checks.
- Swagger/OpenAPI during development.
- CORS.
- Initial Application Insights integration.

The API does not contain Cosmos persistence logic or Worker message-processing logic.

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
CosmosIncidentSubmissionStore
      ↓
Cosmos transactional batch
├── Incident
└── AnalyseIncident Outbox
```

The API does not publish the analysis command directly to Service Bus. That happens asynchronously through the Cosmos Change Feed and `IncidentOutboxWorker`.

## Analysis Read Flow

```text
GET /api/incidents/{id}/analysis
      ↓
IncidentsController
      ↓
GetIncidentAnalysisByIdHandler
      ↓
IIncidentAnalysisReader
      ↓
CosmosIncidentAnalysisReader
      ↓
point read analysis-{incidentId}
```

This keeps Incident state retrieval and persisted analysis retrieval as separate concerns.

## Current Endpoints

Incident endpoints include:

```text
POST /api/incidents
GET  /api/incidents
GET  /api/incidents/{id}
GET  /api/incidents/{id}/analysis
```

Runbook endpoints:

```text
POST   /api/runbooks
GET    /api/runbooks
GET    /api/runbooks/{id}
PUT    /api/runbooks/{id}
DELETE /api/runbooks/{id}
```

The reliability stage also provides backend retry/requeue functionality for failed analysis; the Operations/Admin frontend for that capability is planned for Stage 16.

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

- **Contracts** define HTTP request/response shapes, including the structured analysis response.
- **Controllers** translate HTTP requests into Application calls.
- **ExceptionHandling** converts application/domain errors into consistent Problem Details responses.

`IProblemDetailsService` is used for Problem Details serialization so HTTP errors use the expected `application/problem+json` media type.

## Correlation IDs

Incident creation creates or propagates a correlation ID which is stored in the analysis command and later added to Worker logging scope.

This allows the same workflow to be followed across the HTTP request, persisted outbox record, Service Bus command, and Worker processing.

## Development Behaviour

During local Development/Testing, HTTPS redirection is not forced for the local frontend/API workflow. This avoids an HTTP → HTTPS redirect causing misleading CORS failures when Vite calls the configured local API URL.

For local/Azure configuration instructions, see the [Development Guide](../../docs/DEVELOPMENT.md).

## Design Approach

- Controllers handle HTTP concerns only.
- Application handlers orchestrate use cases.
- Domain objects own business rules.
- Infrastructure owns Cosmos DB, Service Bus, and Azure AI implementations.
- Asynchronous processing remains outside the HTTP request.
- Analysis persistence is exposed through a dedicated read abstraction rather than expanding the Incident repository into unrelated responsibilities.
