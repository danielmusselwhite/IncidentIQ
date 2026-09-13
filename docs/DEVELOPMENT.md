# IncidentIQ Development Guide

IncidentIQ supports two development modes:

1. **Normal local development** — Docker Compose, local Cosmos/Service Bus, and deterministic AI implementations.
2. **Targeted Azure-connected debugging** — run the API or Worker locally against real Azure resources when you specifically need to verify Azure OpenAI, Cosmos vector search, Service Bus, RBAC, or telemetry.

Use the first mode by default. Azure-connected debugging is deliberately opt-in because it can consume real cloud resources and, for Workers, can compete with deployed consumers.

For provisioning/teardown, see [IncidentIQ Azure Dev Environment Lifecycle](INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).

---

## Option 1 — Normal Local Development

### Prerequisites

- Docker Desktop
- Visual Studio with Docker Compose support, or Docker Compose CLI
- .NET 10 SDK
- Node.js / npm

### Configure `.env`

Create a repository-root `.env` file:

```env
COSMOS_EMULATOR_KEY=<COSMOS_EMULATOR_KEY>
SERVICEBUS_SQL_PASSWORD=<LOCAL_SQL_PASSWORD>
```

Do not commit `.env`.

### Start the stack

From the repository root:

```powershell
docker compose up --build
```

The local stack includes:

```text
React Web
ASP.NET Core API
.NET Worker
Cosmos DB Emulator
Service Bus Emulator
SQL Server for the Service Bus Emulator
```

Typical URLs:

```text
Web:                  http://localhost:5173
API Swagger:          https://localhost:7156/swagger
Cosmos Data Explorer: http://localhost:1234
```

### Deterministic Development AI

`DOTNET_ENVIRONMENT=Development` selects deterministic implementations:

```text
IIncidentAnalyzer
└── DevelopmentDummyIncidentAnalyzer

IEmbeddingGenerator
└── DevelopmentDummyEmbeddingGenerator

IOperationalAssistant
└── DevelopmentDummyOperationalAssistant
```

This lets the application exercise the same commands, handlers, persistence, messaging and response contracts without calling Azure OpenAI.

The current local Service Bus queues are:

```text
analyse-incident
index-runbook
index-historical-incident
```

### Main local flows

Incident processing:

```text
POST /api/incidents
→ Incident + outbox in Cosmos
→ Change Feed
→ analyse-incident
→ AnalyseIncidentWorker
→ grounded analysis handler
→ completed Incident + analysis
```

Runbook indexing:

```text
Runbook change
→ Change Feed
→ index-runbook
→ IndexRunbookWorker
→ chunk + embed
→ RunbookChunks
```

Historical Incident indexing:

```text
Completed Incident
→ Change Feed
→ index-historical-incident
→ IndexHistoricalIncidentWorker
→ embed
→ HistoricalIncidentVectors
```

Operational Assistant:

```text
React /assistant
→ POST /api/assistant/questions
→ retrieve historical Incidents + Runbook chunks
→ DevelopmentDummyOperationalAssistant
→ answer + evidence
```

Useful Cosmos containers:

```text
IncidentIQ
├── Incidents
├── Runbooks
├── RunbookChunks
├── HistoricalIncidentVectors
└── ChangeFeedLeases
```

`Runbooks` and `Incidents` remain source records. `RunbookChunks` and `HistoricalIncidentVectors` are derived retrieval data.

### Stop/reset

Keep local data:

```powershell
docker compose down
```

Remove containers and volumes:

```powershell
docker compose down -v
```

Use `-v` only when you intentionally want to reset emulator state.

---

## Option 2 — Targeted Azure-Connected Debugging

Use this when a local deterministic implementation cannot answer the question you are investigating, for example:

- Does the real Azure OpenAI schema/prompt work?
- Does Cosmos vector retrieval return the expected Azure results?
- Does the workload identity have the correct RBAC role?
- Can a real Service Bus sender/receiver publish or consume?
- Does Application Insights receive telemetry?

### Before you start

Ensure the dev environment exists:

```text
rg-incidentiq-dev
```

Then sign in:

```powershell
az login
```

The Azure AI clients use `DefaultAzureCredential`, so your signed-in developer identity needs the relevant Azure roles. Deployed API/Worker workloads use Managed Identity instead.

### Important safety rules

Running a local API against Azure is usually low risk because it is primarily request/response work.

Running a local **Worker** against shared Azure resources needs more care:

- A local Service Bus receiver can consume messages that the deployed Worker would otherwise process.
- A local Change Feed processor using the same lease container/processor name can share partitions with the deployed Worker.
- Local code can write real development data, complete messages, create vectors, or dead-letter failed work.

Prefer one of these when debugging a Worker:

1. temporarily stop the deployed development Worker,
2. use an isolated dev database/queue/lease configuration, or
3. test only the specific API/Azure dependency that you need.

Do not point local code at production resources.

### Configuration

Use .NET user secrets or environment variables. Never place keys, connection strings or tokens in committed settings files.

Common Cosmos configuration:

```text
Cosmos:Endpoint
Cosmos:Key
Cosmos:DatabaseName
Cosmos:IncidentsContainerName
Cosmos:HistoricalIncidentVectorsContainerName
Cosmos:RunbooksContainerName
Cosmos:RunbookChunksContainerName
Cosmos:ChangeFeedLeasesContainerName
```

Service Bus:

```text
ServiceBus:FullyQualifiedNamespace
ServiceBus:AnalyseIncidentQueueName
ServiceBus:IndexRunbookQueueName
ServiceBus:IndexHistoricalIncidentQueueName
ServiceBus:MaxDeliveryCount
```

Azure AI:

```text
AzureAI:Endpoint
AzureAI:DeploymentName
AzureAI:ModelName
AzureAI:Embedding:DeploymentName
AzureAI:Embedding:ModelName
AzureAI:Embedding:Dimensions
AzureAI:MaxRetries
AzureAI:NetworkTimeoutSeconds
AzureAI:RequestTimeoutSeconds
```

The API now needs the full Azure AI dependency set because it performs query embeddings **and** serves the Operational Assistant.

If the local React app calls a non-Development API, configure:

```text
Frontend:Origin = http://localhost:5173
```

Application Insights is optional:

```text
APPLICATIONINSIGHTS_CONNECTION_STRING
```

### Why a non-Development environment is required

`Development` intentionally chooses the deterministic AI implementations. To exercise real Azure OpenAI, run the relevant host in a non-Development environment.

The launch profile can override environment settings, so use `--no-launch-profile` for an explicit Azure-connected run.

### Run the API locally against Azure

After supplying the Azure configuration:

```powershell
$env:DOTNET_ENVIRONMENT = "Production"
dotnet run --project src\IncidentIQ.Api --no-launch-profile
```

Then start the frontend normally:

```powershell
cd src\IncidentIQ.Web
npm install
npm run dev
```

Useful verification targets:

```text
GET/POST normal Incident/Runbook endpoints
Runbook semantic search
POST /api/assistant/questions
real Azure OpenAI embeddings
real Azure OpenAI Assistant generation
real Cosmos vector retrieval
```

### Run the Worker locally against Azure

Only do this after considering the shared-queue/change-feed warning above.

```powershell
$env:DOTNET_ENVIRONMENT = "Production"
dotnet run --project src\IncidentIQ.Worker --no-launch-profile
```

The Worker hosts the asynchronous relays/consumers for:

```text
Incident outbox relay
Incident analysis
Runbook indexing relay + consumer
Historical Incident indexing relay + consumer
```

### Authentication notes

Typical Azure-connected permissions include:

- Azure OpenAI role such as `Cognitive Services OpenAI User`.
- Cosmos permissions required by the configured authentication method.
- Service Bus sender/receiver roles for the queues the local process actually uses.

A process may start successfully and still fail later when it reaches a dependency for which its identity lacks a role.

### Azure AI resilience

The real Azure AI adapters use bounded resilience:

```text
Azure OpenAI SDK
→ small transport retry / network timeout

Azure AI adapter
→ overall request timeout
→ classify failure
→ structured logging
→ rethrow
```

For asynchronous Incident processing, Service Bus remains the durable outer retry mechanism. For the synchronous Assistant, failures return through the API's Problem Details path.

---

## Which Mode Should I Use?

| Goal | Recommended mode |
| --- | --- |
| UI/API feature work | Local Development |
| Messaging/handler logic | Local Development |
| Deterministic RAG orchestration tests | Local Development |
| Real Azure OpenAI prompt/schema verification | Azure-connected API/Worker |
| Azure Cosmos vector behaviour | Azure-connected API/Worker |
| Service Bus RBAC or delivery behaviour | Azure-connected, deliberately isolated |
| Managed Identity verification | Deployed Azure environment |
| Application Insights verification | Azure-connected or deployed Azure |

For exact Azure resource values and redeployment commands, see [Azure Dev Lifecycle](INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).

For common failures, see [Troubleshooting](TROUBLESHOOTING.md).

## Breakpoints

Useful breakpoint groups are available in the repository breakpoint export under `docs/other/breakpoints.xml` when present. Import them into the IDE if useful; otherwise normal breakpoints around handlers, retrievers, queue senders/receivers and Azure AI adapters are sufficient.
