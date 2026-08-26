# IncidentIQ.Api

`IncidentIQ.Api` is the ASP.NET Core Web API for IncidentIQ.

It provides the HTTP interface used by the React frontend and delegates application behaviour to the `IncidentIQ.Application` layer.

The API does not contain persistence or business logic directly. Instead, it acts as the entry point for HTTP requests and translates them into application commands and queries.

---

## Responsibilities

Current responsibilities include:

- Incident CRUD/read endpoints.
- Runbook CRUD endpoints.
- Request validation through the Application layer.
- ASP.NET Core Problem Details error responses.
- Correlation ID generation and propagation.
- Queueing incident analysis work through the Application abstraction.
- Health checks.
- Swagger / OpenAPI during development.
- CORS configuration.
- Initial Application Insights / OpenTelemetry integration.

---

## High-Level Flow

```text
React Frontend
      ↓
ASP.NET Core Controller
      ↓
Application Handler
      ↓
Application Abstraction
      ↓
Infrastructure Implementation
```

For incident creation:

```text
POST /api/incidents
      ↓
IncidentsController
      ↓
CreateIncidentHandler
      ↓
Cosmos persistence
      ↓
AnalyseIncident command
      ↓
Service Bus
```

---

## Structure

```text
IncidentIQ.Api/
├── Contracts/
│   ├── Incidents/
│   └── Runbooks/
│
├── Controllers/
├── ExceptionHandling/
├── Properties/
├── Program.cs
└── appsettings.json
```

### Contracts

Contains API request and response models used at the HTTP boundary.

### Controllers

Accept HTTP requests and delegate work to Application handlers.

### ExceptionHandling

Contains the global exception handler used to convert application/domain errors into consistent Problem Details responses.

---

## Current Endpoints

Incident endpoints include:

```text
POST /api/incidents
GET  /api/incidents
GET  /api/incidents/{id}
```

Runbook endpoints include:

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

---

## Configuration

The API receives configuration through ASP.NET Core configuration providers.

Local Docker development uses environment variables.

Direct local execution can use .NET user-secrets for Azure resources.

Important configuration areas currently include:

```text
Cosmos
ServiceBus
APPLICATIONINSIGHTS_CONNECTION_STRING
```

---

## Design Approach

The API intentionally remains thin:

- Controllers handle HTTP concerns.
- Application handlers contain use-case orchestration.
- Domain objects contain business rules.
- Infrastructure handles Cosmos DB and Azure Service Bus.
- Azure-specific implementation details stay outside the API project.
