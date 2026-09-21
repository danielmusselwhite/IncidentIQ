# IncidentIQ.Domain

Core business entities and state rules. No ASP.NET, Azure SDK, Cosmos, Service Bus or AI-provider dependencies.

## Incident

Lifecycle:

```text
Queued → Processing → Completed
                 └──→ Failed
```

Tracks attempt/failure timestamps and exposes domain methods such as `StartProcessingAttempt`, `MarkCompleted` and `MarkFailed`.

## Runbook

Editable operational guidance and source of truth. Vector chunks are derived outside Domain.

## Rules

- Business state transitions belong here where practical.
- Persistence/HTTP/provider concerns stay outside Domain.
- AI result models currently live in Application because they are use-case output rather than core entity state.
