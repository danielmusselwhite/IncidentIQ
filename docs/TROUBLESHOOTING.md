# Troubleshooting

Common IncidentIQ development gotchas. For full local setup, see [Development](./DEVELOPMENT.md).

## Scoped service used by Worker singleton

```text
Cannot consume scoped service 'AnalyseIncidentHandler' from singleton 'IHostedService'
```

- `AnalyseIncidentHandler` is scoped.
- `AnalyseIncidentWorker` is a singleton hosted service.
- Inject `IServiceScopeFactory`, create an async scope per Service Bus message, and resolve the handler inside that scope.
- Use the same per-message scoped handler for final-failure persistence during that message attempt.

## Local Worker tries Managed Identity

```text
ManagedIdentityCredential authentication failed ... 169.254.169.254
```

- Ensure Docker sets `DOTNET_ENVIRONMENT=Development`.
- Development should resolve `IIncidentAnalyzer` to `DevelopmentDummyIncidentAnalyzer`, not `AzureIncidentAnalyzer`.
- Ensure `COSMOS_EMULATOR_KEY` is set in `.env` and passed as `Cosmos__Key`.
- `launchSettings.json` does not configure Docker Compose containers.

## Azure AI is being called during normal local development

- Check `DOTNET_ENVIRONMENT` inside the Worker container.
- In `Development`, use `AddDevelopmentAIDependencies()`.
- Outside `Development`, `AddAzureAIDependencies()` requires Azure AI configuration and Azure authentication.

## Azure AI timeout / throttling / transient failure

The real analyzer classifies failures and lets them propagate to the Worker:

```text
Timeout
Throttled (429)
ServiceFailure (408 / no response / 5xx)
ClientFailure
InvalidResponse
```

- Check Worker/Application Insights logs for `FailureCategory`, `DurationMs`, deployment, and model.
- The Azure SDK performs only bounded retries; Service Bus remains the durable outer retry mechanism.
- Genuine Worker shutdown cancellation is allowed to propagate as cancellation rather than being logged as an AI failure.

## Service Bus emulator will not start

```text
Connection refused
Name or service not known
Login failed for user 'sa'
```

- Check the Service Bus emulator and SQL container logs.
- Ensure `.env` contains a valid `SERVICEBUS_SQL_PASSWORD` and both containers use the same value.
- If the password changed after SQL initialized, delete the `servicebus-sql-data` volume and recreate those containers.

## SQL password rejected

```text
Password validation failed
```

- Replace placeholder/weak passwords in `.env` with a strong local password that satisfies SQL Server policy.
- Recreate the Service Bus SQL volume afterwards if it initialized with the old value.

## Cosmos container has the wrong partition key

```text
Partition key path '/incidentId' does not match the existing container
```

- The `Incidents` container must use `/incidentId`.
- Cosmos partition keys cannot be changed in place.
- Delete/recreate the local `Incidents` container if an older `/id` version still exists.

## Docker Compose ignores launchSettings environment

```text
Worker environment: Production
```

- `launchSettings.json` does not configure Docker Compose containers.
- Set `DOTNET_ENVIRONMENT=Development` in the Compose environment/override for the Worker.
- Set `ASPNETCORE_ENVIRONMENT=Development` for the API when appropriate.

## Local frontend gets redirected and then fails CORS

If the frontend calls the local HTTP API and the API redirects to HTTPS, the redirected request can produce confusing CORS behaviour.

- Local Development/Testing should avoid HTTPS redirection in this workflow.
- Keep the frontend and configured `VITE_API_BASE_URL` aligned with the local API port/protocol.

## Local environment variables are still placeholders

```text
COSMOS_EMULATOR_KEY=<COSMOS_EMULATOR_KEY>
SERVICEBUS_SQL_PASSWORD=<LOCAL_SQL_PASSWORD>
```

- Replace placeholders in `.env` with real local values.
- The Cosmos emulator key must match the emulator's expected key; it is not an arbitrary secret.
- See [Development](./DEVELOPMENT.md).
