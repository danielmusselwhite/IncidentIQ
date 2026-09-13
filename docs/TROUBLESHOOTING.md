# Troubleshooting

Common IncidentIQ development gotchas. For setup and Azure-connected debugging, see [Development](DEVELOPMENT.md).

## Scoped Application handler resolved from a Worker

```text
Cannot consume scoped service '...' from singleton 'IHostedService'
```

Hosted Workers are singletons while handlers/repositories are scoped.

Create a scope per Service Bus message (or Change Feed callback where appropriate) and resolve scoped handlers inside that scope rather than injecting them directly into the hosted service.

## Local code unexpectedly tries Azure credentials

Typical symptom:

```text
ManagedIdentityCredential authentication failed
```

Check the environment first:

```text
DOTNET_ENVIRONMENT=Development
```

Normal Development should use:

```text
DevelopmentDummyIncidentAnalyzer
DevelopmentDummyEmbeddingGenerator
DevelopmentDummyOperationalAssistant
```

Docker Compose does not use `launchSettings.json` to configure container environment variables.

## API fails to resolve `IOperationalAssistant`

If startup fails with:

```text
Unable to resolve service for type 'IOperationalAssistant'
```

the API is probably registering embedding-only dependencies.

The API now needs the full AI dependency registration because it serves both semantic retrieval and the Operational Assistant:

```text
Development → AddDevelopmentAIDependencies()
Azure       → AddAzureAIDependencies(...)
```

## Azure AI timeout, throttling or invalid response

The Azure adapters classify failures such as:

```text
Timeout
Throttled
ServiceFailure
ClientFailure
InvalidResponse
```

Check logs for failure category, duration, deployment and model. Prompt/evidence payloads are deliberately not logged.

For asynchronous Incident analysis, the Azure adapter rethrows and Service Bus remains the durable outer retry mechanism. For the synchronous Assistant, the error reaches the API's Problem Details handling.

## Azure OpenAI returns valid JSON but citations fail validation

The JSON schema only validates response shape. Application validation then checks request-specific `HI-*` and `RB-*` references.

If an answer contains a reference that was not supplied in the grounding context, treat the response as invalid rather than displaying an invented citation.

See [RAG & AI Design](RAG-AND-AI.md).

## Service Bus emulator will not start

Common symptoms:

```text
Connection refused
Name or service not known
Login failed for user 'sa'
```

Check the Service Bus emulator and SQL logs.

Ensure `.env` contains a valid `SERVICEBUS_SQL_PASSWORD`. If the SQL password changed after the volume was initialised, recreate the Service Bus SQL volume rather than wiping Cosmos unnecessarily.

Current queues are:

```text
analyse-incident
index-runbook
index-historical-incident
```

After changing emulator entity configuration, recreate/restart the emulator.

## Cosmos container has the wrong partition key or vector policy

Current important containers include:

```text
Incidents                  /incidentId
Runbooks                   /id
RunbookChunks              /runbookId
HistoricalIncidentVectors  /incidentId
ChangeFeedLeases           /id
```

Partition keys cannot be changed in place. For local emulator data, delete/recreate only the affected container after fixing configuration.

Vector-enabled containers must also be created with the expected vector policy/index. An older plain container may need to be recreated locally.

## Runbook indexing message is queued but chunks do not appear

Expected path:

```text
Runbooks Change Feed
→ RunbookIndexChangeFeedWorker
→ index-runbook
→ IndexRunbookWorker
→ IndexRunbookHandler
→ RunbookChunks
```

Check:

- the Change Feed relay is running,
- `index-runbook` exists,
- `ServiceBus:IndexRunbookQueueName` matches,
- `IndexRunbookWorker` is registered,
- Worker logs show the message being received,
- embedding generation succeeds.

## Completed Incidents are not appearing in historical retrieval

Expected path:

```text
Completed Incident
→ HistoricalIncidentIndexChangeFeedWorker
→ index-historical-incident
→ IndexHistoricalIncidentWorker
→ HistoricalIncidentVectors
```

Check:

- the historical Change Feed processor is registered,
- the `index-historical-incident` queue exists,
- the Worker identity can **send** to that queue as well as receive from it,
- historical vector documents are present,
- stored embeddings have the configured dimensions.

A relay can read the Change Feed successfully but still fail if its identity only has Service Bus receiver permission and not sender permission.

## Vector queries return rows with empty/default fields

Check the Infrastructure projection contract before assuming the vector query failed.

Cosmos result aliases must match the projection model used to materialise fields such as Runbook/Incident IDs, metadata, content and ranking values.

Provider-specific projection DTOs should then be explicitly mapped into Application retrieval models.

## Vector retrieval returns no useful matches

Check the pipeline in this order:

```text
source document exists
→ derived vector document exists
→ stored embedding dimensions are correct
→ query embedding dimensions are correct
→ vector policy/index is present
→ metadata filters match
→ topK is valid
```

Do not present the raw retrieval ranking value as a calibrated confidence percentage. It is an Infrastructure retrieval signal used for ranking/filtering.

## Local frontend fails CORS against an Azure-connected API

When the API runs in a non-Development environment, configure:

```text
Frontend:Origin = http://localhost:5173
```

Keep `VITE_API_BASE_URL` aligned with the local API address and protocol.

## Azure-connected API works until the Assistant is called

The API now uses:

```text
Azure OpenAI embeddings
Azure OpenAI chat / Operational Assistant
Cosmos vector retrieval
```

Verify:

- `AzureAI:Endpoint`,
- chat deployment/model settings,
- embedding deployment/model/dimensions,
- Cosmos container settings,
- your signed-in developer identity has Azure OpenAI permission.

`az login` is required when `DefaultAzureCredential` is expected to use your developer identity.

## Local Worker competes with the deployed Azure Worker

This is expected if both processes use the same Service Bus queues or Change Feed lease configuration.

A local Worker can:

- consume queue messages,
- share Change Feed partitions,
- write real dev data,
- complete or dead-letter work.

For focused debugging, stop the deployed development Worker or use isolated queue/database/lease configuration. Never point local Worker debugging at production resources.

## Service Bus works for one queue but not another

RBAC is scoped by the operation. A workload that receives from one queue may still lack permission to send to an indexing queue.

Check whether the failing path needs:

```text
Azure Service Bus Data Sender
Azure Service Bus Data Receiver
```

and confirm the role assignment is on the correct namespace/entity and identity.

## Change Feed processing appears stuck

Check:

- the monitored container,
- `ChangeFeedLeases`,
- processor name,
- lease/container permissions,
- whether another Worker instance owns the lease,
- whether the processor was intentionally configured to start from earlier history.

For local Azure-connected debugging, remember that a deployed Worker using the same lease container can legitimately own some partitions.

## Azure Cosmos / Service Bus settings changed but local process still uses old values

Check configuration precedence:

```text
appsettings
appsettings.<Environment>
user secrets
environment variables
launch profile
Docker Compose environment
```

For explicit Azure-connected runs, use `--no-launch-profile` so a Development launch profile does not silently override the environment you set.

## More help

- [Development Guide](DEVELOPMENT.md)
- [Azure Dev Lifecycle](INCIDENTIQ-AZURE-DEV-LIFECYCLE.md)
- [RAG & AI Design](RAG-AND-AI.md)
- [Runtime Flows](flows/README.md)
