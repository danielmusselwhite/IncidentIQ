# IncidentIQ.Domain

`IncidentIQ.Domain` contains the core business models and business rules for IncidentIQ.

It has no dependency on ASP.NET Core, Cosmos DB, Service Bus, React, or Azure SDKs.

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
- No persistence-specific behaviour.
- No HTTP concerns.
- Business state transitions belong in the domain where practical.
- Domain objects remain valid regardless of how they are stored or transported.
