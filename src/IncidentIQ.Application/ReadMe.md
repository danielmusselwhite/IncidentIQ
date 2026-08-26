# IncidentIQ.Application

`IncidentIQ.Application` contains the use cases and orchestration logic for IncidentIQ.

It sits between the application hosts (`IncidentIQ.Api` and `IncidentIQ.Worker`) and the core Domain model.

The Application layer defines **what the system needs to do**, while Infrastructure provides the concrete implementations for external systems such as Cosmos DB and Azure Service Bus.

---

## Responsibilities

Current responsibilities include:

- Incident creation and retrieval use cases.
- Incident analysis workflow orchestration.
- Runbook create/read/update/delete use cases.
- FluentValidation validators.
- Repository and messaging abstractions.
- Coordinating Domain behaviour without depending on Azure SDKs.

---

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

For example, incident creation currently works as:

```text
CreateIncidentHandler
      ↓
validate request
      ↓
create Incident domain object
      ↓
IIncidentRepository
      ↓
persist Incident
      ↓
IIncidentAnalysisQueue
      ↓
queue AnalyseIncident command
```

The Application layer does not know that Cosmos DB and Azure Service Bus provide the concrete implementations.

---

## Structure

The project is organised around application features and shared abstractions.

```text
IncidentIQ.Application/
├── Analyse/
│   ├── AnalyseIncidentCommand.cs
│   └── AnalyseIncidentHandler.cs
│
├── Common/
│   ├── Abstractions/
│   │   ├── IIncidentRepository.cs
│   │   ├── IRunbookRepository.cs
│   │   └── IIncidentAnalysisQueue.cs
│   └── Exceptions/
│
├── Incidents/
│   ├── Create/
│   ├── GetById/
│   └── GetAll/
│
├── Runbooks/
│   ├── Create/
│   ├── GetById/
│   ├── GetAll/
│   ├── Update/
│   └── Delete/
│
└── DependencyInjection.cs
```

Exact folder names may evolve as features are added, but the project remains organised around use cases rather than technical infrastructure.

---

## Incident Use Cases

Current Incident application behaviour includes:

```text
CreateIncident
GetIncidentById
GetAllIncidents
AnalyseIncident
```

### Create Incident

The create flow:

```text
CreateIncidentCommand
        ↓
FluentValidation
        ↓
Incident.Create()
        ↓
IIncidentRepository
        ↓
AnalyseIncidentCommand
        ↓
IIncidentAnalysisQueue
```

The created Incident begins in:

```text
Queued
```

### Analyse Incident

`AnalyseIncidentHandler` is invoked by the Worker after an `AnalyseIncidentCommand` is received.

The current lifecycle is:

```text
Queued
  ↓
Processing
  ↓
Completed
```

The handler currently proves the asynchronous processing lifecycle. Later stages will replace the placeholder processing step with the AI/RAG analysis pipeline.

---

## Runbook Use Cases

Current Runbook application behaviour includes:

```text
CreateRunbook
GetRunbookById
GetAllRunbooks
UpdateRunbook
DeleteRunbook
```

Runbook handlers work through `IRunbookRepository` and remain independent of Cosmos DB implementation details.

---

## Abstractions

The Application layer defines interfaces for functionality that depends on external systems.

Current examples include:

```text
IIncidentRepository
IRunbookRepository
IIncidentAnalysisQueue
```

Infrastructure provides the concrete implementations:

```text
IIncidentRepository
    ↓
CosmosIncidentRepository

IRunbookRepository
    ↓
CosmosRunbookRepository

IIncidentAnalysisQueue
    ↓
AzureServiceBusIncidentAnalysisQueue
```

This keeps Azure SDK dependencies out of the Application project.

---

## Validation

Incident and Runbook commands are validated using **FluentValidation**.

Validation happens before persistence or asynchronous work is started.

For example:

```text
Invalid command
      ↓
ValidationException
      ↓
No repository write
      ↓
No Service Bus message
```

The API converts these failures into ASP.NET Core Problem Details responses.

---

## Dependency Injection

`DependencyInjection.cs` registers Application handlers and validators.

Both the API and Worker call the Application registration method so they can resolve the handlers required by their respective workflows.

```text
IncidentIQ.Api
    ↓
AddApplicationDependencies()

IncidentIQ.Worker
    ↓
AddApplicationDependencies()
```

---

## Testing

Application behaviour is tested in:

```text
tests/IncidentIQ.Application.Tests
```

Tests focus on:

- Handler behaviour.
- Validation.
- Repository interaction.
- Messaging interaction.
- Incident lifecycle transitions.

External dependencies are mocked so the Application layer can be tested without Cosmos DB, Service Bus, or Azure.

---

## Design Approach

The Application project follows a few simple rules:

- Use cases live in handlers.
- Business rules remain in the Domain layer.
- External systems are accessed through abstractions.
- Azure SDKs do not belong in the Application layer.
- Validation happens before side effects.
- The API and Worker should delegate workflow logic rather than duplicating it.
