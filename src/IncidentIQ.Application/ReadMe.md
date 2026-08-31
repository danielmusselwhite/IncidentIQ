# IncidentIQ.Application

`IncidentIQ.Application` contains the use cases and orchestration logic for IncidentIQ.

It sits between the application hosts (`IncidentIQ.Api` and `IncidentIQ.Worker`) and the Domain model.

The Application layer defines **what the system needs to do**. Infrastructure supplies concrete implementations for persistence and messaging.

## High-Level Flow

```text
API / Worker
    ↓
Application Handler
    ↓
Domain
    ↓
Application Abstraction
    ↓
Infrastructure Implementation
```

## Current Incident Use Cases

```text
CreateIncident
GetIncidentById
GetAllIncidents
AnalyseIncident
```

### Create Incident

```text
CreateIncidentHandler
      ↓
FluentValidation
      ↓
Incident.Create()
      ↓
create AnalyseIncidentCommand
      ↓
IIncidentSubmissionStore
```

`IIncidentSubmissionStore` represents one durable submission operation. Its Cosmos implementation atomically persists the Incident and analysis-outbox document.

The Application layer does not directly perform the Cosmos transactional batch and does not directly publish the create request to Service Bus.

### Analyse Incident

`AnalyseIncidentHandler` is invoked by `AnalyseIncidentWorker`.

```text
AnalyseIncidentCommand
      ↓
IIncidentRepository.GetByIdAsync
      ↓
StartProcessingAttempt
      ↓
persist Processing state
      ↓
IIncidentAnalyzer
      ↓
IncidentAnalysisResult
      ↓
MarkCompleted
      ↓
IIncidentAnalysisStore
      ↓
persist Completed Incident + analysis atomically
```

If processing ultimately exhausts retries, `MarkFailedAsync` persists the terminal `Failed` state.

Completed incidents are treated as a no-op to provide basic state-based idempotency.

## Current Runbook Use Cases

```text
CreateRunbook
GetRunbookById
GetAllRunbooks
UpdateRunbook
DeleteRunbook
```

Runbook handlers use `IRunbookRepository` and remain independent of Cosmos DB implementation details.

## Abstractions

Important Application abstractions currently include:

```text
IIncidentRepository
IIncidentSubmissionStore
IIncidentAnalyzer
IIncidentAnalysisStore
IRunbookRepository
IIncidentAnalysisQueue
```

Infrastructure implementations include:

```text
IIncidentRepository
└── CosmosIncidentRepository

IIncidentSubmissionStore
└── CosmosIncidentSubmissionStore

IIncidentAnalyzer
└── AzureIncidentAnalyzer

IIncidentAnalysisStore
└── CosmosIncidentAnalysisStore

IRunbookRepository
└── CosmosRunbookRepository

IIncidentAnalysisQueue
└── Service Bus implementation
```

`IIncidentAnalysisQueue` is used by the outbox relay to publish the persisted `AnalyseIncidentCommand`.

## Validation

Commands are validated with FluentValidation before side effects occur.

```text
Invalid command
      ↓
ValidationException
      ↓
no persistence
```

The API converts validation failures into Problem Details responses.

## Structure

```text
IncidentIQ.Application/
├── Common/
│   └── Abstractions/
├── Analyse/
├── Incidents/
│   ├── Create/
│   ├── GetById/
│   └── GetAll/
├── Runbooks/
└── DependencyInjection.cs
```

## Testing

Application behaviour is tested in:

```text
tests/IncidentIQ.Application.Tests
```

Mocks are used for Application abstractions so use-case behaviour can be tested without Azure resources.

See [tests/ReadMe.md](../../tests/ReadMe.md) for the wider testing strategy.
