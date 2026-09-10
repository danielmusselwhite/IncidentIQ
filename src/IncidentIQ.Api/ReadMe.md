# IncidentIQ.Api

`IncidentIQ.Api` is the ASP.NET Core HTTP host used by the React frontend and other HTTP clients.

The API is intentionally thin. It owns transport concerns such as routing, contracts, validation/error translation, correlation IDs and host configuration, while business use cases and external-service contracts remain in `IncidentIQ.Application`.

## Responsibilities

Current API responsibilities include:

- Incident create/read endpoints.
- Persisted Incident analysis retrieval.
- Runbook CRUD endpoints.
- Runbook semantic/vector search through `GET /api/runbooks/search`.
- Backend retry/requeue endpoint for failed analysis.
- Mapping HTTP contracts to Application commands, handlers and abstractions.
- Problem Details error responses.
- Correlation ID creation/propagation.
- Health checks.
- Swagger/OpenAPI during development.
- CORS.
- Application Insights integration.

The API does **not** contain Cosmos query/persistence implementation details or Service Bus message-processing logic. Stage 11B does, however, make the API an Azure OpenAI client in non-Development environments because a search query must be embedded before Cosmos vector retrieval can run.

## How HTTP Work Crosses the Architecture

At a conceptual level, normal request/response use cases follow this direction:

```text
HTTP request
   ↓
Controller / API contract
   ↓
Application command/query or use case
   ↓
Application handler / orchestration
   ↓
Application abstraction
   ↓
Infrastructure implementation supplied by DI
   ↓
Cosmos / Azure OpenAI / other external system
```

The important boundary is that Application code depends on interfaces such as `IIncidentSubmissionStore`, `IIncidentAnalysisReader`, `IRunbookRepository`, `IEmbeddingGenerator` and `IRunbookChunkRetriever`; it does not depend on `Cosmos*` or Azure SDK classes.

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

The API does not publish the analysis command directly to Service Bus. The durable outbox entry is later observed through the Cosmos Change Feed and relayed by `IncidentOutboxWorker`.

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

## Runbook Semantic Search Flow

Stage 11B adds a synchronous read/search path over the derived `RunbookChunks` index:

```text
GET /api/runbooks/search
      ↓
HTTP search parameters
(query + optional service + topK)
      ↓
IEmbeddingGenerator
      ↓
query embedding (1536 dimensions)
      ↓
IRunbookChunkRetriever
      ↓
CosmosRunbookChunkRetriever
      ↓
Cosmos VectorDistance(...)
      ↓
ranked RunbookChunkMatch results
```

In Development, query embeddings are produced deterministically so local searches can be tested without calling Azure OpenAI. In Azure/non-Development, the API uses the `runbook-embedding` / `text-embedding-3-small` deployment through its Managed Identity.

`CosmosRunbookChunkRetriever` applies top-K retrieval and optional service filtering. Returned `Distance` is cosine distance, so **lower values mean greater vector similarity**; it should not be presented as a calibrated confidence score.

Retrieval telemetry records elapsed time and Cosmos Request Units (RUs), which makes Stage 11 useful for both relevance experiments and cost/performance measurement.

## Current Endpoints

Incident endpoints include:

```text
POST /api/incidents
GET  /api/incidents
GET  /api/incidents/{id}
GET  /api/incidents/{id}/analysis
```

Runbook endpoints include:

```text
POST   /api/runbooks
GET    /api/runbooks
GET    /api/runbooks/search
GET    /api/runbooks/{id}
PUT    /api/runbooks/{id}
DELETE /api/runbooks/{id}
```

Backend retry/requeue functionality is available for failed analysis; the Operations/Admin frontend for that capability is planned separately.

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

- **Contracts** define HTTP request/response shapes, including structured Incident analysis and Runbook-search responses.
- **Controllers** translate HTTP requests into Application-facing operations.
- **ExceptionHandling** converts application/domain errors into consistent Problem Details responses.

`IProblemDetailsService` is used for Problem Details serialization so HTTP errors use the expected `application/problem+json` media type.

## Correlation IDs

Incident creation creates or propagates a correlation ID which is stored in the analysis command and later added to Worker logging scope.

This allows one Incident-analysis workflow to be followed across the HTTP request, persisted outbox record, Service Bus command and Worker processing.

Runbook semantic search is synchronous and is measured separately through retrieval latency/RU telemetry.

## Development Behaviour

During normal local Development:

```text
Incident analysis      → DevelopmentDummyIncidentAnalyzer (Worker)
Runbook indexing       → DevelopmentDummyEmbeddingGenerator (Worker)
Runbook query embedding→ DevelopmentDummyEmbeddingGenerator (API)
Cosmos                 → local emulator
Service Bus            → local emulator
```

Using the same deterministic embedding strategy for indexing and query generation is important: vectors from incompatible embedding implementations are not meaningfully comparable.

During Azure/non-Development execution, the API receives embedding configuration through Bicep and uses its Managed Identity for Azure OpenAI and Cosmos access.

For local/Azure configuration instructions, see the [Development Guide](../../docs/DEVELOPMENT.md).

## Design Approach

- Controllers own HTTP concerns, not provider-specific persistence/query code.
- Application handlers/use cases orchestrate business behaviour where appropriate.
- Domain objects own business rules.
- Application defines external-service abstractions.
- Infrastructure implements Cosmos DB, Service Bus and Azure AI integrations.
- Asynchronous Incident analysis and Runbook indexing remain outside the HTTP request.
- Synchronous vector retrieval is exposed through a dedicated retriever abstraction rather than expanding `IRunbookRepository` into a search/vector API.
