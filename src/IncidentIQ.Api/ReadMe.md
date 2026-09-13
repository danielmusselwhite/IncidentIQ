# IncidentIQ.Api

`IncidentIQ.Api` is the ASP.NET Core HTTP host used by the React frontend and other clients.

The API stays deliberately thin: it owns HTTP concerns, while use cases live in `IncidentIQ.Application` and Azure-specific implementations live in `IncidentIQ.Infrastructure`.

## Responsibilities

Current API responsibilities include:

- Incident create/read endpoints and persisted analysis retrieval.
- Runbook CRUD and semantic search.
- `POST /api/assistant/questions` for the Operational Assistant.
- Request/response contract mapping.
- Validation and Problem Details error responses.
- Correlation IDs, health checks, CORS, Swagger/OpenAPI and Application Insights wiring.

The API does not contain Cosmos SQL, Service Bus processing logic or Azure SDK-specific business rules.

## Request flow

```text
HTTP request
→ Controller / API contract
→ Application command/query/handler
→ Application abstraction
→ Infrastructure implementation
→ Cosmos / Azure OpenAI
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

## Main endpoints

```text
Incidents
POST /api/incidents
GET  /api/incidents
GET  /api/incidents/{id}
GET  /api/incidents/{id}/analysis

Runbooks
POST   /api/runbooks
GET    /api/runbooks
GET    /api/runbooks/search
GET    /api/runbooks/{id}
PUT    /api/runbooks/{id}
DELETE /api/runbooks/{id}

Assistant
POST /api/assistant/questions

Health
GET /api/health
```

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

## Development behaviour

In `Development`, the API registers the full deterministic AI dependency set:

```text
IEmbeddingGenerator
→ DevelopmentDummyEmbeddingGenerator

IOperationalAssistant
→ DevelopmentDummyOperationalAssistant
```

In non-Development environments it uses Azure embeddings and `AzureOperationalAssistant`. The API therefore requires Azure OpenAI access for both semantic retrieval and Assistant generation.

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

## Design approach

- Controllers own HTTP concerns only.
- Application owns orchestration and provider-independent contracts.
- Infrastructure owns Cosmos/Azure OpenAI implementations.
- Long-running Incident analysis remains asynchronous.
- Synchronous semantic retrieval and Assistant questions remain request/response paths.
