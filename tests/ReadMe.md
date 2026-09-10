# IncidentIQ Tests

The `tests` folder contains IncidentIQ's automated backend tests.

The strategy keeps Domain/Application behaviour fast and isolated, uses API/Worker tests at host boundaries, and reserves local/Azure smoke verification for provider integration that is more valuable to exercise end-to-end than to deeply mock.

## Test Projects

```text
tests/
├── IncidentIQ.Application.Tests/
├── IncidentIQ.Api.Tests/
└── IncidentIQ.Worker.Tests/
```

## Application Tests

`IncidentIQ.Application.Tests` focuses on use cases, validation and provider-independent behaviour.

Current areas include:

- Incident creation and validation.
- Transactional-outbox submission through `IIncidentSubmissionStore`.
- Incident analysis lifecycle and attempt metadata.
- Completed-state idempotency.
- Final failure handling.
- Structured analysis persistence orchestration.
- Runbook create/read/update/delete behaviour.
- Deterministic Runbook chunking.
- Runbook indexing orchestration through `IEmbeddingGenerator` and `IRunbookChunkStore`.
- Runbook vector-retrieval behaviour, including top-K/filter/no-result boundaries where exercised through Application abstractions.

External dependencies are mocked/faked so these tests do not require Cosmos DB, Service Bus or Azure OpenAI.

## API Tests

`IncidentIQ.Api.Tests` uses ASP.NET Core `WebApplicationFactory`.

External dependencies are replaced by in-memory/fake Application implementations where appropriate, for example:

```text
IIncidentRepository
IIncidentSubmissionStore
IIncidentAnalysisReader
IRunbookRepository
IEmbeddingGenerator
IRunbookChunkRetriever
```

API tests cover HTTP concerns such as:

- Status codes and routing.
- Request/response contracts.
- Problem Details validation/error responses.
- Incident creation and retrieval.
- Durable analysis-request creation at the Application boundary.
- Persisted analysis retrieval through `GET /api/incidents/{id}/analysis`.
- Missing analysis returning 404 Problem Details.
- Correlation IDs.
- Runbook CRUD.
- Runbook semantic-search HTTP behaviour where applicable.

The API tests intentionally do **not** assert that the API directly publishes Incident work to Service Bus. Incident submission persists the analysis request through the outbox boundary.

Provider-specific Cosmos vector SQL/projection behaviour is better verified through focused Infrastructure/local integration checks than by reproducing Cosmos internals in an API mock.

## Worker Tests

`IncidentIQ.Worker.Tests` covers Worker-specific behaviour such as:

- Service Bus message handling/settlement.
- Retry and redelivery behaviour.
- Final failure handling and DLQ behaviour.
- Incident outbox relay behaviour.
- Runbook Change Feed/index-command relay behaviour.
- `IndexRunbookWorker` message boundaries.
- Duplicate/idempotent processing boundaries.
- Propagation of failures back to the Service Bus processing boundary.

The Azure SDK's own retry implementation is not re-tested exhaustively. IncidentIQ tests its own boundary behaviour, while real Azure verification and structured telemetry provide confidence in provider-specific execution.

## Running Tests

From the repository root:

```powershell
dotnet test .\IncidentIQ.slnx
```

To run one project:

```powershell
dotnet test .\tests\IncidentIQ.Application.Tests
dotnet test .\tests\IncidentIQ.Api.Tests
dotnet test .\tests\IncidentIQ.Worker.Tests
```

## Test Boundaries

```text
Domain / Application unit tests
        ↓
API integration tests
        ↓
Worker / reliability tests
        ↓
Local Docker end-to-end verification
        ↓
Azure smoke / integration verification
```

## Local End-to-End Verification

### Incident Analysis

```text
React / API
 ↓
Cosmos Emulator
 ├── Incident
 └── Outbox
      ↓
 Change Feed
      ↓
 IncidentOutboxWorker
      ↓
Service Bus Emulator
      ↓
AnalyseIncidentWorker
      ↓
DevelopmentDummyIncidentAnalyzer
      ↓
Completed Incident + analysis
      ↓
GET /api/incidents/{id}/analysis
```

This exercises the complete asynchronous Incident-analysis architecture without Azure OpenAI.

### Runbook Ingestion

```text
Create / update Runbook
      ↓
Runbooks Change Feed
      ↓
index-runbook (Service Bus Emulator)
      ↓
IndexRunbookWorker
      ↓
RunbookChunker
      ↓
DevelopmentDummyEmbeddingGenerator
      ↓
RunbookChunks with 1536-dimensional vectors
```

Editing the same Runbook should replace stale chunks rather than append duplicates. Deleting the Runbook should remove derived chunks before the source document is deleted.

### Runbook Vector Retrieval

After at least one Runbook has been indexed, verify the complete Stage 11B path:

```text
GET /api/runbooks/search
      ↓
DevelopmentDummyEmbeddingGenerator
      ↓
1536-dimensional query vector
      ↓
CosmosRunbookChunkRetriever
      ↓
VectorDistance + optional service filter + topK
      ↓
RunbookChunkMatch[]
```

Checks should include:

- relevant indexed chunks are returned;
- `topK` limits the result set;
- service filtering excludes unrelated services;
- an empty/no-match case is handled cleanly;
- returned `RunbookId`, `ChunkIndex`, `Title`, `Service`, `Content` and `Distance` fields are populated correctly;
- lower `Distance` values rank as more similar;
- retrieval logs expose latency and Cosmos Request Units (RUs).

The projection/deserialization check is especially useful because Cosmos can return valid query rows while a mismatched projection model still materialises empty/default CLR properties.

## Important Outbox Reliability Check

A useful manual test is:

```text
Stop Worker
    ↓
Submit Incident
    ↓
Incident + Outbox are persisted
    ↓
Incident remains Queued
    ↓
Start Worker
    ↓
Change Feed relay resumes
    ↓
Incident reaches Completed
```

This verifies that a temporarily unavailable relay/analysis Worker does not lose the durable analysis request.

## Azure Verification

### Incident Analysis

For provider-specific verification, deploy/run the non-Development Worker and confirm:

```text
Queued
→ Processing
→ Azure OpenAI structured response
→ Completed + persisted analysis
→ API returns analysis
→ frontend displays analysis
```

Application Insights/Worker logs should contain AI duration/success/failure metadata without logging raw Incident, prompt or model-response payloads.

### Runbook Ingestion and Retrieval

Azure Stage 11 verification covers both halves:

```text
Ingestion
Runbook create/update
→ IndexRunbookWorker
→ runbook-embedding / text-embedding-3-small
→ 1536-dimensional vectors persisted in RunbookChunks

Retrieval
GET /api/runbooks/search
→ API Managed Identity
→ runbook-embedding query vector
→ Cosmos VectorDistance
→ ranked RunbookChunkMatch results
```

Also verify stale-chunk replacement after an edit, deletion cleanup, optional service filtering, top-K behaviour, retrieval latency/RU telemetry, and correct result projection/deserialization.
