# Runtime Flows

| Flow | Purpose |
| --- | --- |
| [Incident submission and analysis](incident-submission-and-analysis.md) | Durable submission through grounded async analysis |
| [Runbook indexing](runbook-indexing.md) | Editable Runbook → vector chunks |
| [Historical Incident indexing](historical-incident-indexing.md) | Completed Incident → searchable vector |
| [Grounded Incident analysis](grounded-incident-analysis.md) | Historical + Runbook evidence → AI analysis |
| [Operational Assistant](operational-assistant.md) | Authenticated stateless RAG conversation |

Conventions:
- solid arrows: synchronous calls,
- thick arrows: Change Feed/Service Bus,
- source documents remain separate from derived vectors,
- Azure SDK details remain in Infrastructure.
