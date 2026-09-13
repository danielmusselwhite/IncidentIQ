# IncidentIQ Runtime Flows

These documents explain the main IncidentIQ paths at a level intended to be useful both for engineers and for architecture review.

The root README keeps the overall system deliberately high-level. Use these pages when you want to follow a request or background job through the concrete API, Application, Worker and Azure boundaries.

| Flow | Purpose |
| --- | --- |
| [Incident submission and analysis](incident-submission-and-analysis.md) | From `POST /api/incidents` to asynchronous grounded analysis |
| [Runbook indexing](runbook-indexing.md) | From editable Runbook to embedded search chunks |
| [Historical Incident indexing](historical-incident-indexing.md) | From completed Incident to searchable historical vector |
| [Grounded Incident analysis](grounded-incident-analysis.md) | How historical Incidents and Runbooks become evidence for AI analysis |
| [Operational Assistant](operational-assistant.md) | How stateless multi-turn questions are grounded and answered |

## Diagram conventions

- **Solid arrows** represent direct/synchronous calls.
- **Thick arrows** represent asynchronous Change Feed or Service Bus hand-offs.
- Source records such as Incidents and Runbooks are kept separate from derived vector-search records.
- Azure-specific implementations stay in Infrastructure; commands, handlers and retrieval interfaces stay in Application.
