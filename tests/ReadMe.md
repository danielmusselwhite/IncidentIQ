# IncidentIQ Tests

The `tests` folder contains the automated backend tests for IncidentIQ.

The test strategy keeps business behaviour fast and isolated while using local emulators for transport-level end-to-end verification.

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
- Transactional-outbox submission orchestration through `IIncidentSubmissionStore`.
- Incident analysis lifecycle and attempt metadata.
- Completed-state idempotency.
- Final failure handling.
- Runbook create/read/update/delete behaviour.

External dependencies are mocked so these tests do not require Cosmos DB, Service Bus, or Azure.

For example:

```text
CreateIncidentHandler
├── valid command creates Incident
├── valid command creates AnalyseIncidentCommand
├── both are passed to IIncidentSubmissionStore
└── invalid command performs no persistence
```

## API Tests

`IncidentIQ.Api.Tests` uses ASP.NET Core `WebApplicationFactory`.

External dependencies are replaced by in-memory implementations such as:

```text
InMemoryIncidentRepository
InMemoryIncidentSubmissionStore
InMemoryRunbookRepository
```

API tests verify HTTP behaviour including:

- Status codes and routing.
- Request/response contracts.
- Problem Details validation responses.
- Incident creation and retrieval.
- Durable analysis-request creation at the Application boundary.
- Correlation IDs.
- Runbook CRUD.

The API tests intentionally do **not** assert that the API directly publishes to Service Bus. Incident submission now persists the analysis request through the outbox boundary.

## Worker Tests

`IncidentIQ.Worker.Tests` is for Worker-specific behaviour such as message handling, retries, failure handling, and outbox relay behaviour.

Low-level Azure SDK behaviour should not be heavily mocked when an emulator-backed integration test gives a more useful result.

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
```

### Local End-to-End Flow

```text
API
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
Cosmos status update
```

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

This verifies that a temporarily unavailable Worker/Service Bus publishing path does not lose the analysis request.

Additional Stage 8 tests should cover duplicate delivery, retry exhaustion, DLQ behaviour, and deliberate admin requeue.
