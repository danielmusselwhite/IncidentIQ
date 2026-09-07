# IncidentIQ.Application

`IncidentIQ.Application` contains the use cases and orchestration logic for IncidentIQ.

It sits between the application hosts (`IncidentIQ.Api` and `IncidentIQ.Worker`) and the Domain model. The Application layer defines **what the system needs to do**; Infrastructure supplies concrete implementations for persistence, messaging, and AI.

## High-Level Flow

```text
API / Worker
    ↓
Application Handler
    ↓
Domain rules + Application models
    ↓
Application abstraction
    ↓
Infrastructure implementation
```

## Current Incident Use Cases

```text
CreateIncident
GetIncidentById
GetAllIncidents
GetIncidentAnalysisById
AnalyseIncident
RetryAnalyseIncident
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

### Read Persisted Analysis

```text
GetIncidentAnalysisByIdHandler
      ↓
IIncidentAnalysisReader
      ↓
IncidentAnalysisResult?
```

The handler throws the Application-level not-found exception when no persisted analysis exists; the API converts that into Problem Details.

### AI Contracts

Provider-independent structured analysis models live in Application:

```text
IncidentAnalysisInput
IncidentAnalysisResult
LikelyCause
RecommendedAction
IIncidentAnalyzer
```

Azure SDK types remain outside Application.

## Current Runbook Use Cases

```text
CreateRunbook
GetRunbookById
GetAllRunbooks
UpdateRunbook
DeleteRunbook
```

Runbook handlers use `IRunbookRepository` and remain independent of Cosmos DB implementation details.

## Important Abstractions

```text
IIncidentRepository
IIncidentSubmissionStore
IIncidentAnalyzer
IIncidentAnalysisStore
IIncidentAnalysisReader
IRunbookRepository
IIncidentAnalysisQueue
```

Infrastructure implementations currently include:

```text
IIncidentRepository
└── CosmosIncidentRepository

IIncidentSubmissionStore
└── CosmosIncidentSubmissionStore

IIncidentAnalyzer
├── DevelopmentDummyIncidentAnalyzer   (Development)
└── AzureIncidentAnalyzer              (non-Development Worker)

IIncidentAnalysisStore
└── CosmosIncidentAnalysisStore

IIncidentAnalysisReader
└── CosmosIncidentAnalysisReader

IRunbookRepository
└── CosmosRunbookRepository

IIncidentAnalysisQueue
└── AzureServiceBusIncidentAnalysisQueue
```

`IIncidentAnalysisQueue` is used by the outbox relay to publish the persisted `AnalyseIncidentCommand`.

## Dependency-Injection Boundary

Most Application dependencies are host-agnostic, but `AnalyseIncidentHandler` is registered by the Worker host because it requires the Worker-specific `IIncidentAnalyzer` choice.

The Service Bus hosted service creates a DI scope per message and resolves the scoped handler within that message scope.

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
│   ├── GetAll/
│   └── ...analysis/retry use cases
├── Runbooks/
└── DependencyInjection.cs
```

## Testing

Application behaviour is tested in:

```text
tests/IncidentIQ.Application.Tests
```

Mocks/fakes are used for Application abstractions so use-case behaviour can be tested without Azure resources.

See [tests/ReadMe.md](../../tests/ReadMe.md) for the wider testing strategy.
