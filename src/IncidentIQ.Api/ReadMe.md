# IncidentIQ.Api

`IncidentIQ.Api` is the ASP.NET Core HTTP host used by the React frontend and other clients.

The API stays deliberately thin: it owns HTTP concerns, authentication and authorization boundaries, while use cases live in `IncidentIQ.Application` and Azure-specific implementations live in `IncidentIQ.Infrastructure`.

## Responsibilities

Current API responsibilities include:

- Microsoft Entra user authentication and access-token validation.
- Delegated `access_as_user` authorization for application endpoints.
- Incident create/read endpoints and persisted analysis retrieval.
- Runbook CRUD and semantic search.
- `POST /api/assistant/questions` for the Operational Assistant.
- Request/response contract mapping.
- Validation and Problem Details error responses.
- Correlation IDs, health checks, CORS, Swagger/OpenAPI and Application Insights wiring.

The API does not contain Cosmos SQL, Service Bus processing logic or Azure SDK-specific business rules.

## Request Flow

Protected application requests pass through authentication and authorization before reaching a controller:

```text
HTTP request
      ↓
Authorization: Bearer <access token>
      ↓
ASP.NET Core authentication
      ↓
Microsoft.Identity.Web
      ↓
validate Entra access token
      ↓
authenticated ClaimsPrincipal
      ↓
authorization
      ↓
require access_as_user
      ↓
Controller / API contract
      ↓
Application command/query/handler
      ↓
Application abstraction
      ↓
Infrastructure implementation
      ↓
Cosmos / Azure OpenAI
```

Important Application abstractions used by the API include:

```text
IIncidentSubmissionStore
IIncidentAnalysisReader
IRunbookRepository
IEmbeddingGenerator
IRunbookChunkRetriever
IHistoricalIncidentRetriever
IOperationalAssistant
```

## User Authentication

IncidentIQ uses Microsoft Entra ID to authenticate users of the application.

The API is registered in Microsoft Entra as:

```text
IncidentIQ API
```

and exposes the delegated scope:

```text
access_as_user
```

ASP.NET Core is configured with `Microsoft.Identity.Web` and JWT bearer authentication.

```text
Microsoft Entra
      ↓
signed JWT access token
      ↓
Authorization: Bearer <token>
      ↓
IncidentIQ API
      ↓
signature / issuer / audience / lifetime validation
      ↓
HttpContext.User
```

A valid access token causes ASP.NET Core to create an authenticated `ClaimsPrincipal` and assign it to `HttpContext.User`.

Authentication answers:

```text
Who is making this request?
```

Authorization then answers:

```text
Is that identity allowed to perform this operation?
```

All controller endpoints are currently mapped with:

```csharp
app.MapControllers()
    .RequireAuthorization()
    .RequireScope("access_as_user");
```

This means every endpoint mapped from `IncidentsController`, `RunbooksController` and `AssistantController` requires:

1. a valid authenticated Microsoft Entra identity; and
2. an access token containing the delegated `access_as_user` scope.

The resulting behaviour is:

```text
No valid access token
        ↓
401 Unauthorized

Valid token without access_as_user
        ↓
403 Forbidden

Valid token with access_as_user
        ↓
Controller executes
```

The health endpoint is mapped separately and deliberately remains anonymous:

```text
GET /api/health
```

This allows Container Apps and health probes to verify application health without acquiring an Entra access token.

## User Identity vs Workload Identity

User authentication is separate from the Managed Identity already used by IncidentIQ workloads.

```text
User authentication

Engineer
   ↓
Microsoft Entra
   ↓
React
   ↓
Bearer access token
   ↓
ASP.NET Core API


Workload authentication

API / Worker
   ↓
Managed Identity
   ↓
Cosmos DB
Azure OpenAI
Service Bus
ACR
```

The user's Entra access token determines whether the HTTP request may enter the API.

The API and Worker Managed Identities determine whether those workloads may access Azure resources.

These are separate trust boundaries.

## Microsoft Entra Configuration

The API reads its Entra configuration from:

```text
AzureAd:Instance
AzureAd:TenantId
AzureAd:ClientId
AzureAd:Scopes
```

The current scope is:

```text
access_as_user
```

`TenantId` and `ClientId` identify the Entra tenant and API registration. They are configuration values rather than secrets.

For local configuration and Entra setup, see [Development](../../docs/DEVELOPMENT.md).

## Authentication Testing

Integration tests do not authenticate against the real Microsoft Entra service.

The test host replaces JWT authentication with a deterministic `TestAuthenticationHandler` that constructs a test `ClaimsPrincipal`.

This preserves the important API boundary:

```text
authentication
      ↓
authorization
      ↓
controller
```

without requiring network access or real Microsoft credentials during automated tests.

The tests verify:

```text
anonymous request
→ 401 Unauthorized

authenticated request + access_as_user
→ endpoint executes

authenticated request + incorrect scope
→ 403 Forbidden

anonymous /api/health request
→ 200 OK
```

The fake authentication handler exists only in the `Testing` environment. Production and normal local execution continue to use Microsoft Entra JWT bearer authentication.

## Main Endpoints

All application endpoints below require authentication and the `access_as_user` delegated scope.

```text
Incidents
POST /api/incidents
GET  /api/incidents
GET  /api/incidents/{id}
GET  /api/incidents/{id}/analysis
POST /api/incidents/{id}/retry

Runbooks
POST   /api/runbooks
GET    /api/runbooks
GET    /api/runbooks/search
GET    /api/runbooks/{id}
PUT    /api/runbooks/{id}
DELETE /api/runbooks/{id}

Assistant
POST /api/assistant/questions
```

The health endpoint remains anonymous:

```text
GET /api/health
```

More granular Engineer and Administrator role-based authorization is introduced separately so authentication and role policy remain distinct concerns.

## Operational Assistant

The Assistant endpoint accepts the current question, optional service/environment filters and recent conversation history.

```text
AssistantController
→ AskOperationalQuestionHandler
→ OperationalQuestionContextBuilder
→ embedding + historical Incident / Runbook retrieval
→ IOperationalAssistant
→ answer + exact retrieved evidence
```

Conversation history is supplied by the client for continuity but is not treated as grounding evidence. Evidence references such as `HI-1` and `RB-1` are scoped to the response that produced them.

See [Operational Assistant flow](../../docs/flows/operational-assistant.md).

## Development Behaviour

In `Development`, the API registers the full deterministic AI dependency set:

```text
IEmbeddingGenerator
→ DevelopmentDummyEmbeddingGenerator

IOperationalAssistant
→ DevelopmentDummyOperationalAssistant
```

In non-Development environments it uses Azure embeddings and `AzureOperationalAssistant`.

The AI implementation changes between environments, but the user-authentication boundary remains Microsoft Entra authentication.

For configuration and live-Azure debugging, see [Development](../../docs/DEVELOPMENT.md).

## Structure

```text
IncidentIQ.Api/
├── Contracts/
│   ├── Incidents/
│   ├── Runbooks/
│   └── Assistant/
├── Controllers/
├── ExceptionHandling/
├── Program.cs
└── appsettings.json
```

## Design Approach

- Controllers own HTTP concerns only.
- Microsoft Entra owns user authentication.
- ASP.NET Core owns HTTP authorization boundaries.
- Application owns orchestration and provider-independent contracts.
- Infrastructure owns Cosmos/Azure OpenAI implementations.
- User authentication remains separate from Azure workload Managed Identity.
- Long-running Incident analysis remains asynchronous.
- Synchronous semantic retrieval and Assistant questions remain request/response paths.