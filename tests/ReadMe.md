# IncidentIQ Tests

The `tests` folder contains automated tests for the IncidentIQ backend.

The goal is to keep business behaviour testable without requiring live Azure resources while still verifying the API through realistic ASP.NET Core integration tests.

---

## Test Projects

```text
tests/
├── IncidentIQ.Application.Tests/
├── IncidentIQ.Api.Tests/
└── IncidentIQ.Worker.Tests/
```

---

## Application Tests

`IncidentIQ.Application.Tests` focuses on application use cases and validation.

Current areas include:

- Incident creation.
- Incident validation.
- Incident analysis handling.
- Runbook create/read/update/delete behaviour.

Dependencies such as repositories and queues are normally mocked so tests remain isolated.

Example:

```text
CreateIncidentHandler
├── valid command creates incident
├── valid command queues analysis
└── invalid command does neither
```

---

## API Tests

`IncidentIQ.Api.Tests` uses ASP.NET Core `WebApplicationFactory` to start the API in memory.

External dependencies are replaced with test implementations such as:

```text
InMemoryIncidentRepository
InMemoryRunbookRepository
InMemoryIncidentAnalysisQueue
```

This allows tests to verify:

- HTTP status codes.
- Request/response models.
- Problem Details validation errors.
- Routing.
- Incident creation.
- Runbook CRUD.
- Analysis-command queueing.
- Correlation ID behaviour.

without connecting to Cosmos DB or Service Bus.

---

## Worker Tests

`IncidentIQ.Worker.Tests` is reserved for Worker-specific behaviour as the background-processing pipeline grows.

Low-level Azure Service Bus SDK behaviour is not intended to be heavily mocked.

The preferred split is:

```text
Application tests
→ business workflow

Docker/local integration
→ real Service Bus transport behaviour
```

---

## Running Tests

From the repository root:

```powershell
dotnet test
```

This runs all test projects in the solution.

---

## Testing Approach

IncidentIQ uses several levels of testing:

```text
Domain / Application unit tests
        ↓
API integration tests
        ↓
Local Docker end-to-end testing
```

The Docker environment can additionally verify the real local asynchronous path:

```text
API
 ↓
Cosmos Emulator
 ↓
Service Bus Emulator
 ↓
Worker
 ↓
Cosmos status update
```

As reliability and AI functionality are introduced, additional tests will cover retries, duplicate messages, failure handling, retrieval, and analysis behaviour.
