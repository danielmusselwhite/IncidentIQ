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
- retry/idempotency behaviour,
- indexing/retrieval and RAG context construction,
- evidence validation and Assistant orchestration.

**API**
- routing/contracts/Problem Details,
- Incident/Runbook/Assistant/Operations endpoints,
- authentication and delegated-scope enforcement,
- Engineer vs Administrator authorization,
- retry and failed-Incident operational boundaries,
- anonymous health check.

API tests use deterministic test authentication rather than real Entra tokens.

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

Real Azure behaviour is verified separately for Entra/RBAC, Azure OpenAI, Cosmos vector search, Service Bus, OpenTelemetry export and KEDA scaling.
