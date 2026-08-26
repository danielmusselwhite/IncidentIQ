# IncidentIQ.Domain

`IncidentIQ.Domain` contains the core business model and business rules for IncidentIQ.

It has no dependency on ASP.NET Core, Cosmos DB, Service Bus, React, or other infrastructure concerns.

The Domain project represents the application concepts themselves rather than how they are stored or transported.

---

## Responsibilities

Current domain responsibilities include:

- Incident modelling.
- Incident severity and status.
- Incident lifecycle rules.
- Runbook modelling.
- Controlled state transitions.

---

## Current Models

### Incident

Represents a technical incident submitted for analysis.

Typical data includes:

- Title.
- Description.
- Service.
- Environment.
- Severity.
- Symptoms.
- Status.
- Created timestamp.
- Processing/completion timestamps.

The current lifecycle includes:

```text
Queued
  ↓
Processing
  ↓
Completed
```

Failure handling will be expanded as the messaging reliability workflow is implemented.

### Runbook

Represents editable operational guidance used to investigate and resolve incidents.

Typical data includes:

- Title.
- Description.
- Service.
- Content.
- Created timestamp.
- Updated timestamp.

Editable Runbooks are intentionally kept separate from future vectorised `RunbookChunk` documents.

---

## Structure

```text
IncidentIQ.Domain/
├── Incidents/
│   ├── Incident.cs
│   ├── IncidentSeverity.cs
│   └── IncidentStatus.cs
│
└── Runbooks/
    └── Runbook.cs
```

---

## Design Approach

The Domain project follows a few simple rules:

- No Azure dependencies.
- No persistence concerns.
- No HTTP concerns.
- Business state changes happen through domain methods where practical.
- Domain objects should remain valid regardless of how they are stored.

This keeps the core business model reusable and testable independently from the rest of the application.
