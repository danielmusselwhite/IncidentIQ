# IncidentIQ Tests

IncidentIQ uses fast Application tests for orchestration/business behaviour, API tests for HTTP boundaries, and Worker tests for messaging/reliability. Real provider behaviour is verified with targeted local/Azure smoke tests rather than deeply mocking Azure SDK internals.

## Test projects

```text
tests/
├── IncidentIQ.Application.Tests/
├── IncidentIQ.Api.Tests/
└── IncidentIQ.Worker.Tests/
```

## Application tests

Current areas include:

- Incident creation/validation and transactional-outbox orchestration.
- Analysis lifecycle, retry/failure and completed-state idempotency.
- Runbook CRUD, chunking, indexing and retrieval boundaries.
- Historical Incident indexing/retrieval orchestration.
- Grounding-context construction.
- Reuse of one embedding across retrieval paths.
- `HI-*` / `RB-*` evidence-reference validation.
- Operational Assistant handler/conversation-history behaviour.

External systems are mocked/faked through Application interfaces.

## API tests

`IncidentIQ.Api.Tests` uses `WebApplicationFactory` where appropriate and verifies:

- routing/status codes,
- request/response contracts,
- Problem Details,
- Incident/Runbook endpoints,
- semantic search,
- Assistant request/response mapping,
- answer-scoped historical Incident and Runbook evidence.

Provider-specific Cosmos/Azure OpenAI behaviour is not reproduced inside HTTP tests.

## Worker tests

Worker tests cover boundaries such as:

- Service Bus message settlement/redelivery,
- outbox relay behaviour,
- Runbook indexing relay/consumer behaviour,
- historical Incident indexing relay/consumer behaviour,
- failure propagation and DLQ boundaries,
- scoped handler resolution per message.

## Running tests

From the repository root:

```powershell
dotnet test .\IncidentIQ.slnx
```

Build the frontend separately:

```powershell
cd src\IncidentIQ.Web
npm run build
```

## Verification layers

```text
Application unit tests
        ↓
API tests
        ↓
Worker/reliability tests
        ↓
local Docker end-to-end verification
        ↓
targeted Azure verification
```

Local Development uses deterministic AI, which is ideal for verifying orchestration and UI behaviour.

Azure verification is reserved for provider-specific behaviour such as real embeddings, Cosmos vector queries, Azure OpenAI schemas/prompts, RBAC and Service Bus delivery.

See [Development](../docs/DEVELOPMENT.md) for safe Azure-connected debugging.
