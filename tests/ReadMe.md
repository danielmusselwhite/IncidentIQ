# IncidentIQ Tests

## Projects

```text
IncidentIQ.Application.Tests
IncidentIQ.Api.Tests
IncidentIQ.Worker.Tests
```

## Coverage

**Application**
- validation/state transitions,
- transactional outbox orchestration,
- retry/idempotency behavior,
- indexing/retrieval,
- RAG context construction,
- evidence validation,
- Assistant orchestration.

**API**
- routing/contracts/Problem Details,
- Incident/Runbook/Assistant endpoints,
- authentication: `401`, valid scope, invalid scope,
- anonymous health check.

API tests use a deterministic test authentication handler rather than real Entra tokens.

**Worker**
- message settlement/redelivery,
- Change Feed relays,
- indexing consumers,
- DLQ/failure boundaries,
- scoped handler resolution.

## Run

```powershell
dotnet test .\IncidentIQ.slnx

cd src\IncidentIQ.Web
npm run build
npm run lint
```

Real Azure behavior is verified separately for embeddings, Cosmos vector search, Entra, RBAC and Service Bus.
