# IncidentIQ Tests

The `tests` folder contains the automated backend tests for IncidentIQ.

The strategy keeps business behaviour fast and isolated while using emulator/deployed smoke tests for transport/provider integration where that gives more value than deeply mocking Azure SDK internals.

## Test Projects

```text
tests/
├── IncidentIQ.Application.Tests/
├── IncidentIQ.Api.Tests/
└── IncidentIQ.Worker.Tests/
```

## Application Tests

`IncidentIQ.Application.Tests` focuses on use cases, validation, and domain/application behaviour.

Current areas include:

- Incident creation and validation.
- Transactional-outbox submission through `IIncidentSubmissionStore`.
- Incident analysis lifecycle and attempt metadata.
- Completed-state idempotency.
- Final failure handling.
- Structured analysis persistence orchestration.
- Runbook create/read/update/delete behaviour.

External dependencies are mocked/faked so these tests do not require Cosmos DB, Service Bus, or Azure OpenAI.

## API Tests

`IncidentIQ.Api.Tests` uses ASP.NET Core `WebApplicationFactory`.

External dependencies are replaced by in-memory implementations such as:

```text
InMemoryIncidentRepository
InMemoryIncidentSubmissionStore
InMemoryIncidentAnalysisReader
InMemoryRunbookRepository
```

API tests verify HTTP behaviour including:

- Status codes and routing.
- Request/response contracts.
- Problem Details validation/error responses.
- Incident creation and retrieval.
- Durable analysis-request creation at the Application boundary.
- Persisted analysis retrieval through `GET /api/incidents/{id}/analysis`.
- Missing analysis returning 404 Problem Details.
- Correlation IDs.
- Runbook CRUD.

The API tests intentionally do **not** assert that the API directly publishes to Service Bus. Incident submission persists the analysis request through the outbox boundary.

## Worker Tests

`IncidentIQ.Worker.Tests` covers Worker-specific behaviour such as:

- Message handling/settlement.
- Retry and redelivery behaviour.
- Final failure handling and DLQ behaviour.
- Outbox relay behaviour.
- Duplicate/idempotent processing boundaries.
- Propagation of analysis failures back to the Service Bus processing boundary.

The Azure SDK's own retry implementation is not re-tested exhaustively. IncidentIQ tests the application/Worker boundary, while real Azure verification and structured telemetry provide confidence in provider-specific behaviour.

## Running Tests

From the repository root:

```powershell
dotnet test
```

To run one project:

```powershell
dotnet test tests\IncidentIQ.Application.Tests
dotnet test tests\IncidentIQ.Api.Tests
dotnet test tests\IncidentIQ.Worker.Tests
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

### Local End-to-End Flow

```text
React / API
 ↓
Cosmos Emulator
 ├── Incident
 └── Outbox
      ↓
 Change Feed
      ↓
 Outbox Worker
      ↓
Service Bus Emulator
      ↓
Analysis Worker
      ↓
DevelopmentDummyIncidentAnalyzer
      ↓
Completed Incident + analysis
      ↓
GET /api/incidents/{id}/analysis
```

This locally exercises the complete asynchronous architecture without an Azure OpenAI dependency.

### Important Outbox Reliability Check

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

### Azure AI Verification

For provider-specific verification, run/deploy the non-Development Worker and confirm:

```text
Queued
→ Processing
→ Azure OpenAI structured response
→ Completed + persisted analysis
→ API returns analysis
→ frontend displays analysis
```

Application Insights/Worker logs should contain the Stage 10 AI duration/success/failure metadata without logging raw Incident/prompt/model-response payloads.
