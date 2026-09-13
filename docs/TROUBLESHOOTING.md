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

## Runbook creation fails after adding vector ingestion

If `POST /api/runbooks` fails inside `CosmosRunbookRepository.CreateAsync`, verify the two Runbook containers have different partition keys:

```text
Runbooks       → /id
RunbookChunks  → /runbookId
```

A common mistake is accidentally creating `Runbooks` with `/runbookId` while adding the new vector container. Cosmos partition keys cannot be changed in place, so delete/recreate the affected local container after fixing `CosmosInitializer`.

`RunbookChunks` must also be created with its vector embedding policy/index from the start. If it was previously created as a plain container, delete/recreate only `RunbookChunks` after fixing local initialization.

## Runbook indexing message is queued but not consumed

If the Runbooks Change Feed publishes to `index-runbook` but no chunk documents appear:

- Confirm Worker `Program.cs` registers `AddHostedService<IndexRunbookWorker>()`.
- Confirm `ServiceBus:IndexRunbookQueueName` / `ServiceBus__IndexRunbookQueueName` is `index-runbook`.
- Confirm `infra/local/servicebus/Config.json` defines both `analyse-incident` and `index-runbook`.
- Restart/recreate the Service Bus emulator after changing its entity configuration.
- Look for `Starting IndexRunbook Service Bus processor.` in Worker logs.

The expected local pipeline is:

```text
Runbooks Change Feed
→ RunbookIndexChangeFeedWorker
→ index-runbook
→ IndexRunbookWorker
→ IndexRunbookHandler
→ RunbookChunks
```

## RunbookChunks delete fails with partition-key mismatch

`RunbookChunks` is partitioned by `/runbookId`. Deleting a chunk therefore uses the Runbook ID as the `PartitionKey`, not the chunk document ID. Prefer the existing replace/cleanup store operation so all derived chunks for a Runbook are handled consistently.

## Runbook vector search returns rows with empty/default fields

If Cosmos reports matching rows but the API returns empty `RunbookId`, `Title`, `Content`, or zero/default values, check the vector-query projection contract rather than the vector index first.

Stage 11B uses an explicit Infrastructure projection (`CosmosRunbookChunkMatchResult`) for fields such as `runbookId`, `chunkIndex`, `title`, `service`, `content`, and `vectordistance`. The JSON property names/aliases in the query result must match the deserialization model.

A valid Cosmos query can otherwise look like a retrieval failure even though the failure is only in result materialisation.

## Runbook vector search returns no useful matches

Check the pipeline in this order:

```text
Source Runbook exists
→ RunbookChunks exist for that runbookId
→ stored embedding length = 1536
→ query embedding length = 1536
→ /embedding vector policy uses cosine distance
→ vector index is present
→ optional service filter matches stored metadata
→ topK is greater than zero
```

In local `Development`, both indexing and query embeddings must use the deterministic `DevelopmentDummyEmbeddingGenerator`. Mixing vectors produced by different embedding implementations makes local similarity results meaningless even when the dimensions match.

## Runbook search works locally but fails in Azure

The Stage 11B search path runs in the API, so the deployed API now needs Azure OpenAI embedding configuration and permission; giving Azure OpenAI access only to the Worker is no longer sufficient.

Verify the API Container App receives:

```text
AzureAI:Endpoint
AzureAI:Embedding:DeploymentName
AzureAI:Embedding:ModelName
AzureAI:Embedding:Dimensions = 1536
```

and that the API managed identity can invoke the Azure OpenAI embedding deployment. Also confirm the API can read the `RunbookChunks` Cosmos container.

## Runbook vector search looks expensive or slow

`CosmosRunbookChunkRetriever` records retrieval latency and Cosmos request-unit (RU) consumption. Use those measurements together with top-K and metadata filters before changing the index strategy.

Do not interpret `Distance` as a confidence percentage: it is cosine distance used for ordering, where lower is more similar.

