# IncidentIQ.Infrastructure

`IncidentIQ.Infrastructure` implements the external-service interfaces defined by Application.

It contains Cosmos DB persistence/vector retrieval, Azure Service Bus adapters, Azure OpenAI implementations, and deterministic Development AI implementations.

## Responsibilities

```text
Application interface
      ↓
Infrastructure implementation
      ↓
Cosmos / Service Bus / Azure OpenAI
```

Examples:

```text
IIncidentRepository              → CosmosIncidentRepository
IRunbookChunkRetriever           → CosmosRunbookChunkRetriever
IHistoricalIncidentRetriever     → CosmosHistoricalIncidentRetriever
IHistoricalIncidentVectorStore   → CosmosHistoricalIncidentVectorStore
IEmbeddingGenerator              → AzureEmbeddingGenerator
IIncidentAnalyzer                → AzureIncidentAnalyzer
IOperationalAssistant            → AzureOperationalAssistant
```

## Cosmos DB

Important containers:

| Container | Partition key | Purpose |
| --- | --- | --- |
| `Incidents` | `/incidentId` | Incident state, outbox and persisted analysis/evidence |
| `Runbooks` | `/id` | Editable Runbook source |
| `RunbookChunks` | `/runbookId` | Derived Runbook chunk vectors |
| `HistoricalIncidentVectors` | `/incidentId` | Derived completed-Incident vectors |
| `ChangeFeedLeases` | `/id` | Change Feed processor checkpoints/ownership |

Vector-query projection models remain inside Infrastructure and are explicitly mapped to Application retrieval models.

Raw vector ranking values are retrieval signals, not calibrated AI confidence scores.

## Service Bus

Infrastructure publishes/consumes commands for:

```text
analyse-incident
index-runbook
index-historical-incident
```

The API does not publish Incident analysis directly to Service Bus. Incident submission uses the Cosmos transactional outbox and Worker-side Change Feed relay.

## Azure OpenAI

Three provider-independent capabilities are implemented here:

```text
IEmbeddingGenerator
→ AzureEmbeddingGenerator

IIncidentAnalyzer
→ AzureIncidentAnalyzer

IOperationalAssistant
→ AzureOperationalAssistant
```

The shared Azure client/dependency wiring is reused rather than constructing separate clients per feature.

### Structured generation

`AzureIncidentAnalysisSchema` constrains Incident-analysis JSON output.

`AzureOperationalAssistantSchema` constrains Assistant answer sections and their evidence-reference arrays.

Infrastructure validates/deserialises the provider response, maps it to Application models, and leaves request-specific evidence-reference validation to Application.

### Prompt boundary

Retrieved Incident/Runbook content and previous conversation messages are treated as untrusted data. Azure prompts instruct the model not to follow embedded instructions or invent access to external operational systems.

### Resilience

Azure AI adapters use configured network/request timeouts, bounded SDK retry behaviour and failure classification for timeout, throttling, service/client failure and invalid responses.

They do not log raw prompts, Incident descriptions, Runbook content or model responses.

## Development implementations

Normal `Development` uses deterministic replacements:

```text
DevelopmentDummyIncidentAnalyzer
DevelopmentDummyEmbeddingGenerator
DevelopmentDummyOperationalAssistant
```

This keeps local orchestration deterministic while the rest of the application continues to use the same Application interfaces.

## Design approach

- Azure SDK details stop at the Infrastructure boundary.
- Source documents remain separate from rebuildable vector documents.
- Cosmos projection contracts are explicit and testable.
- Durable Service Bus retry behaviour remains outside Azure AI adapters.
- Static JSON schemas validate shape; Application validates request-specific evidence meaning.

See [Design Decisions](../../docs/DESIGN-DECISIONS.md) and [RAG & AI Design](../../docs/RAG-AND-AI.md).
