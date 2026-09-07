# IncidentIQ.Domain

`IncidentIQ.Domain` contains the core business models and business rules for IncidentIQ.

It has no dependency on ASP.NET Core, Cosmos DB, Service Bus, React, Azure OpenAI, or Azure SDKs.

## Responsibilities

Current domain responsibilities are centred on Incidents and Runbooks.

### Incident

Represents a technical incident submitted for asynchronous analysis.

The lifecycle is:

```text
Queued
  ↓
Processing
  ↓
Completed

Queued / Processing
  ↓
Failed
```

The Incident also tracks processing metadata such as:

```text
AttemptCount
LastAttemptAt
ProcessingStartedAt
CompletedAt
FailedAt
FailureReason
```

State changes are controlled through domain methods such as:

```text
StartProcessingAttempt()
MarkCompleted()
MarkFailed()
```

This keeps lifecycle rules out of controllers, repositories, and Worker transport code.

### Runbook

Represents editable operational guidance used to investigate and resolve incidents.

Editable Runbooks remain separate from the future vectorised `RunbookChunk` persistence used by the RAG pipeline.

### Why AI Results Are Not Domain Entities

The current structured AI result (`IncidentAnalysisResult`, likely causes, recommended actions) lives in Application rather than Domain.

The Domain owns the Incident business lifecycle; the generated analysis is an output of an application use case and may evolve as providers/retrieval strategies change. Keeping it outside Domain avoids coupling core business entities to the current AI representation.

## Structure

```text
IncidentIQ.Domain/
├── Incidents/
│   ├── Incident.cs
│   ├── IncidentSeverity.cs
│   └── IncidentStatus.cs
└── Runbooks/
    └── Runbook.cs
```

## Design Rules

- No Azure dependencies.
- No AI-provider SDK types.
- No persistence-specific behaviour.
- No HTTP concerns.
- Business state transitions belong in the Domain where practical.
- Domain objects remain valid regardless of how they are stored, transported, or analysed.
