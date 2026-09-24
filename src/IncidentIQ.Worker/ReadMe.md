# IncidentIQ.Worker

Hosts Cosmos Change Feed relays and Service Bus consumers. Business workflows remain in Application handlers.

## Hosted Flows

```text
IncidentOutboxWorker
→ analyse-incident
→ AnalyseIncidentWorker

RunbookIndexChangeFeedWorker
→ index-runbook
→ IndexRunbookWorker

HistoricalIncidentIndexChangeFeedWorker
→ index-historical-incident
→ IndexHistoricalIncidentWorker
```

## Reliability

- Hosted services create a DI scope per message/callback.
- Messages complete only after successful processing.
- Failures propagate to Service Bus for redelivery/DLQ.
- Stable IDs + duplicate detection + state checks provide duplicate-safe at-least-once processing.
- `MaxConcurrentCalls = 1` per analysis Worker instance; horizontal replicas provide additional concurrency.

## Observability

The Worker exports OpenTelemetry with role name `IncidentIQ.Worker`.

Custom spans cover:

```text
incident.outbox.relay
incident.analysis
incident.analysis.retrieve_context
incident.analysis.generate
incident.analysis.persist
```

Custom metrics cover queue wait, total processing, AI duration and terminal failures. W3C trace context restored from the persisted analysis command links Worker activity back to the initiating API request.

## Scaling

Azure Container Apps uses KEDA on the `analyse-incident` queue:

```text
min replicas:       1
max replicas:       3
target queue depth: 2 messages / replica
polling interval:   15 seconds
```

The minimum remains 1 because this host also runs Cosmos Change Feed processors. Scaling to zero would stop the relays that create Service Bus work.

## Development

Development defaults to deterministic AI/embeddings and local emulators. Live Azure Worker debugging should use isolated queues/leases or stop the deployed Worker first.

See [Development](../../docs/DEVELOPMENT.md) and [Observability](../../docs/OBSERVABILITY.md).
