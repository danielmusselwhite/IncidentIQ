# IncidentIQ.Infrastructure

Implements Application interfaces using Cosmos DB, Service Bus and Azure OpenAI.

## Cosmos

- Source persistence: Incidents, Runbooks.
- Derived vectors: RunbookChunks, HistoricalIncidentVectors.
- ChangeFeedLeases for processor checkpoints.
- Provider-specific query projections are mapped to Application models.

## Service Bus

Queues:

```text
analyse-incident
index-runbook
index-historical-incident
```

Incident submission uses the Cosmos outbox; the API does not directly publish analysis work.

## Azure OpenAI

Implements:
- `IEmbeddingGenerator`
- `IIncidentAnalyzer`
- `IOperationalAssistant`

Structured schemas validate output shape; Application validates request-specific evidence references.

## Development

Deterministic alternatives:

```text
DevelopmentDummyIncidentAnalyzer
DevelopmentDummyEmbeddingGenerator
DevelopmentDummyOperationalAssistant
```

## Principles

- Azure SDK details stop here.
- Source records stay separate from derived vector data.
- AI retries are bounded; Service Bus owns durable async retry.
- Raw prompts/evidence/model responses are not logged.
