# Troubleshooting

## `401 Unauthorized`

Check:
- the user is signed in through `IncidentIQ Web`,
- React requested `api://<API_CLIENT_ID>/access_as_user`,
- the request contains `Authorization: Bearer ...`,
- API `AzureAd:TenantId` and `AzureAd:ClientId` match `IncidentIQ API`.

## `403 Forbidden`

The user is authenticated but lacks the required scope or application role.

- all controller APIs require `access_as_user`,
- normal Incident/Runbook/Assistant routes require `Engineer` or `Administrator`,
- Operations and retry require `Administrator`.

Do not treat `403` as a sign-in failure.

## First page load fails, refresh works

If API calls fail immediately after Microsoft login but work after refresh, verify `apiClient.ts` can fall back from `getActiveAccount()` to `getAllAccounts()` and set the cached account active before acquiring a token.

This avoids an MSAL startup race where authentication has completed but no active account was selected yet.

## Entra redirect mismatch (`AADSTS50011`)

`IncidentIQ Web` must register the exact SPA origin being used:

```text
http://localhost:5173
https://<your-static-web-app>.azurestaticapps.net
```

## Operations endpoints return `500`

`OperationsController` depends on both `GetFailedIncidentsHandler` and `GetOperationsSummaryHandler`. Ensure both are registered in `AddApplicationDependencies()`.

A missing constructor dependency prevents the controller from being created, so every action on that controller can fail.

## Local code unexpectedly uses Managed Identity

Normal local development should use `DOTNET_ENVIRONMENT=Development`.

For deliberate Azure-connected testing:

```powershell
az login
$env:AZURE_TOKEN_CREDENTIALS = "dev"
```

## Scoped handler injected into hosted Worker

Hosted services are singletons; Application handlers/repositories are scoped. Create a DI scope per Service Bus message/Change Feed callback and resolve scoped handlers inside it.

## Service Bus emulator fails

Check SQL/emulator logs, `SERVICEBUS_SQL_PASSWORD`, queue names and stale SQL volumes after password changes.

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

```text
Runbooks Change Feed → index-runbook → IndexRunbookWorker → RunbookChunks
Completed Incident → index-historical-incident → IndexHistoricalIncidentWorker → HistoricalIncidentVectors
```

Check Change Feed processor registration, queue names, sender/receiver RBAC, Worker logs and embeddings.

## Change Feed appears stuck

Check the monitored container, `ChangeFeedLeases`, processor name, Cosmos permissions and whether another Worker owns the lease.

## Azure AI failures

Verify `AzureAI:Endpoint`, chat/embedding deployment names, embedding dimensions and Azure OpenAI RBAC.

Adapters classify timeout, throttling, service/client failures and invalid responses without logging prompt payloads.

## Custom metric does not appear

The process emitting the metric must subscribe to the `IncidentIQ` meter. Worker telemetry uses `AddMeter(IncidentIqTelemetry.MeterName)`; the API must do the same for API-side metrics such as administrator retries.

Also verify `APPLICATIONINSIGHTS_CONNECTION_STRING` and allow time for telemetry ingestion.

## KEDA does not scale the Worker

Check:
- the `analyse-incident` queue actually has backlog,
- the scaling rule uses the correct Service Bus namespace/queue,
- the Worker user-assigned identity is configured on the scaling rule,
- the identity has queue-scoped Service Bus Data Owner access,
- `minReplicas: 1`, `maxReplicas: 3`, `messageCount: '2'` are deployed.

The Worker intentionally does not scale to zero because it also hosts Cosmos Change Feed relays.

## Local Worker competes with Azure Worker

A local Worker using shared queues/leases can consume real dev work. Stop the deployed Worker or use isolated infrastructure.

## Configuration seems ignored

.NET configuration precedence:

```text
appsettings.json
→ appsettings.<Environment>.json
→ user-secrets
→ environment variables
→ launch profile / Docker environment
```

Use `--no-launch-profile` for explicit non-Development runs.
