# IncidentIQ.Application

Application owns use cases, orchestration, validation and provider-independent interfaces.

```text
API / Worker
→ Application
→ Domain + interfaces
← Infrastructure implementations
```

## Main Boundaries

| Type | Examples |
| --- | --- |
| Repository | `IIncidentRepository`, `IRunbookRepository` |
| Store | `IIncidentSubmissionStore`, `IIncidentAnalysisStore`, `IRunbookChunkStore` |
| Retriever | `IRunbookChunkRetriever`, `IHistoricalIncidentRetriever` |
| Queue | `IIncidentAnalysisQueue`, `IRunbookIndexQueue` |
| AI | `IIncidentAnalyzer`, `IEmbeddingGenerator`, `IOperationalAssistant` |

## Key Use Cases

- Incident creation + transactional outbox.
- Grounded asynchronous Incident analysis.
- Runbook indexing/search.
- Historical Incident indexing/retrieval.
- Operational Assistant orchestration.
- Request-scoped evidence validation.

Application does not depend on Azure SDK types.
