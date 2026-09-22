# IncidentIQ.Worker

Hosts Change Feed relays and Service Bus consumers. Business workflows remain in Application handlers.

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

## Development

`Development` defaults to deterministic AI/embeddings and local emulators. Live Azure Worker debugging should use isolated queues/leases or stop the deployed Worker first.

See [Development](../../docs/DEVELOPMENT.md).
