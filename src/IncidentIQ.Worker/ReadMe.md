# IncidentIQ.Worker

`IncidentIQ.Worker` hosts IncidentIQ's asynchronous Change Feed relays and Service Bus consumers.

The Worker owns transport/execution boundaries; business orchestration remains in Application handlers.

## Hosted flows

```text
IncidentOutboxWorker
→ Incidents Change Feed → analyse-incident

AnalyseIncidentWorker
→ analyse-incident → AnalyseIncidentHandler

RunbookIndexChangeFeedWorker
→ Runbooks Change Feed → index-runbook

IndexRunbookWorker
→ index-runbook → IndexRunbookHandler

HistoricalIncidentIndexChangeFeedWorker
→ Incidents Change Feed → index-historical-incident

IndexHistoricalIncidentWorker
→ index-historical-incident → IndexHistoricalIncidentHandler
```

## Incident analysis

`IncidentOutboxWorker` observes durable analysis outbox records and publishes `AnalyseIncidentCommand`.

`AnalyseIncidentWorker` receives the command, creates a DI scope, resolves `AnalyseIncidentHandler`, and completes the Service Bus message only after the handler succeeds.

The handler now performs grounded analysis using historical Incident and Runbook evidence before persisting the completed analysis.

## Runbook indexing

```text
Runbook change
→ Change Feed relay
→ index-runbook
→ IndexRunbookWorker
→ load latest Runbook
→ chunk + embed
→ replace RunbookChunks
```

Failures propagate to Service Bus so durable redelivery/DLQ behaviour remains the outer retry boundary.

## Historical Incident indexing

```text
Completed Incident
→ historical Change Feed relay
→ index-historical-incident
→ IndexHistoricalIncidentWorker
→ embed title/description/symptoms
→ HistoricalIncidentVectors
```

The historical relay uses its own Change Feed processor/checkpoint so it can evolve independently from the Incident outbox relay.

## Scope and lifetime

Hosted services are singletons. Application handlers/repositories are scoped.

Service Bus consumers therefore create a DI scope per message and resolve handlers inside that scope.

## Reliability

IncidentIQ assumes at-least-once delivery:

```text
stable command/message IDs
+ Service Bus duplicate detection
+ application state checks
+ bounded redelivery
+ DLQ
```

Azure AI failures are not swallowed inside the Worker. Short transport retries happen inside the Azure SDK/adapter, then processing failures propagate back to Service Bus.

## Development

In `Development`, the Worker uses deterministic AI/embeddings plus local Cosmos/Service Bus.

In non-Development environments it uses Azure OpenAI, Azure Cosmos DB and Azure Service Bus through managed identity/configured credentials.

For live-Azure debugging warnings about shared queues and Change Feed leases, see [Development](../../docs/DEVELOPMENT.md).
