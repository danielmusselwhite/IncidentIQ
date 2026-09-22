# Troubleshooting

## `401 Unauthorized`

Check:
- the user is signed in through `IncidentIQ Web`,
- React requested `api://<API_CLIENT_ID>/access_as_user`,
- the request contains `Authorization: Bearer ...`,
- API `AzureAd:TenantId` and `AzureAd:ClientId` match `IncidentIQ API`.

## `403 Forbidden`

The user is authenticated but lacks a required scope/role.

- Stage 14A/B requires `access_as_user`.
- Stage 14C adds Engineer/Administrator roles.

Do not treat `403` as a sign-in failure.

## Entra redirect mismatch (`AADSTS50011`)

`IncidentIQ Web` must register the exact SPA origin being used:

```text
http://localhost:5173
https://<your-static-web-app>.azurestaticapps.net
```

## Invalid React hook call after adding MSAL

- `useMsal()` and other hooks must be inside a React component.
- `<App />` must be below `<MsalProvider>`.
- Confirm one React version with `npm list react react-dom`.

## Local code unexpectedly uses Managed Identity

Normal local development should use `DOTNET_ENVIRONMENT=Development`.

For deliberate Azure-connected testing:

```powershell
az login
$env:AZURE_TOKEN_CREDENTIALS = "dev"
```

## Scoped handler injected into hosted Worker

Hosted services are singletons; Application handlers/repositories are scoped.

Create a DI scope per Service Bus message/Change Feed callback and resolve scoped handlers inside it.

## Service Bus emulator fails

Check:
- SQL/emulator logs,
- `SERVICEBUS_SQL_PASSWORD`,
- queue names,
- stale SQL volume after a password change.

Current queues:

```text
analyse-incident
index-runbook
index-historical-incident
```

## Vector retrieval returns no useful matches

Check in order:
- source record exists,
- derived vector document exists,
- embedding dimensions match,
- vector policy/index exists,
- metadata filters match,
- `topK` is valid.

Vector distance is a ranking signal, not model confidence.

## Indexing is not happening

Runbook:

```text
Runbooks Change Feed
→ RunbookIndexChangeFeedWorker
→ index-runbook
→ IndexRunbookWorker
→ RunbookChunks
```

Historical Incident:

```text
Completed Incident
→ HistoricalIncidentIndexChangeFeedWorker
→ index-historical-incident
→ IndexHistoricalIncidentWorker
→ HistoricalIncidentVectors
```

Check relay registration, queue names, sender/receiver RBAC, Worker logs and embeddings.

## Change Feed appears stuck

Check:
- monitored container,
- `ChangeFeedLeases`,
- processor name,
- Cosmos permissions,
- whether another Worker owns the lease.

## Azure AI failures

Verify:
- `AzureAI:Endpoint`,
- chat/embedding deployment names,
- embedding dimensions,
- Azure OpenAI RBAC.

Adapters classify timeout, throttling, service/client failures and invalid responses without logging prompt payloads.

## Local Worker competes with Azure Worker

A local Worker using shared queues/leases can consume real dev work. Stop the deployed Worker or use isolated infrastructure.

## Configuration seems ignored

Remember .NET configuration precedence:

```text
appsettings.json
→ appsettings.<Environment>.json
→ user-secrets
→ environment variables
→ launch profile / Docker environment
```

Use `--no-launch-profile` for explicit non-Development runs.
