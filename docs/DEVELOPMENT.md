# IncidentIQ Development Guide

IncidentIQ supports two primary development modes:

1. **Normal local development** — Docker Compose, local Cosmos/Service Bus, and deterministic AI implementations.
2. **Targeted Azure-connected debugging** — run the API, Worker, or evaluation tooling locally against selected real Azure resources when you specifically need to verify Azure OpenAI, Cosmos vector search, Service Bus, RBAC, telemetry, or model behaviour.

Use normal local development by default. Azure-connected debugging is deliberately opt-in because it can consume real cloud resources and, for Workers, can compete with deployed consumers.

The Azure-connected mode can be partial. For example, you can keep Cosmos and Service Bus local while using real Azure OpenAI. The .NET environment controls which AI implementation is registered; the configured endpoints determine whether the other dependencies are local or Azure.

For provisioning, teardown, and commands for retrieving Azure resource values, see [IncidentIQ Azure Dev Environment Lifecycle](INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).

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

Use deterministic AI for normal feature development because it is:

- fast,
- repeatable,
- available offline,
- free from Azure OpenAI request cost,
- suitable for automated tests and day-to-day debugging.

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
- Do real embeddings retrieve the expected evidence?
- Does Cosmos vector retrieval return the expected Azure results?
- Does the workload identity have the correct RBAC role?
- Can a real Service Bus sender/receiver publish or consume?
- Does Application Insights receive telemetry?

Targeted Azure-connected debugging does **not** require every dependency to be live at the same time.

A useful progression is:

```text
Normal development
→ local Cosmos + local Service Bus + deterministic AI

Targeted live-AI debugging
→ local Cosmos + local Service Bus + real Azure OpenAI

Full Azure-connected debugging
→ Azure Cosmos + Azure Service Bus + real Azure OpenAI
```

Prefer the narrowest mode that answers the question you are investigating.

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

For Azure OpenAI, a local developer normally needs:

```text
Cognitive Services OpenAI User
```

on the IncidentIQ Azure OpenAI resource.

### Important safety rules

Running a local API against Azure is usually lower risk because it is primarily request/response work.

Running a local **Worker** against shared Azure resources needs more care:

- A local Service Bus receiver can consume messages that the deployed Worker would otherwise process.
- A local Change Feed processor using the same lease container/processor name can share partitions with the deployed Worker.
- Local code can write real development data, complete messages, create vectors, or dead-letter failed work.

Prefer one of these when debugging a Worker:

1. temporarily stop the deployed development Worker,
2. use an isolated dev database/queue/lease configuration, or
3. test only the specific API/Azure dependency that you need.

Do not point local code at production resources.

---
## Microsoft Entra User Authentication

IncidentIQ distinguishes between **user authentication** and **Azure workload authentication**.

User authentication controls who may call the HTTP API:

```text
User
→ Microsoft Entra ID
→ React
→ access token
→ IncidentIQ API
```

Workload authentication controls which Azure resources the deployed API and Worker may access:

```text
API / Worker
→ Managed Identity
→ Cosmos / Service Bus / Azure OpenAI / ACR
```

Do not confuse the user's access token with the Managed Identity used by the deployed workloads.

### Entra Tenant

The IncidentIQ application registrations currently live in the existing Microsoft Entra tenant used by the Azure development subscription.

A separate IncidentIQ tenant is not required for the portfolio environment.

The application registrations are treated as stable tenant-level bootstrap configuration, while disposable Azure application infrastructure remains managed through Bicep.

### IncidentIQ API App Registration

Create the API registration in:

```text
Microsoft Entra ID
→ App registrations
→ New registration
```

Configuration:

```text
Name:
IncidentIQ API

Supported account types:
Accounts in this organizational directory only
```

No redirect URI or client secret is required for the API registration.

Record:

```text
Directory (tenant) ID
Application (client) ID
```

### Expose the API Scope

Under:

```text
IncidentIQ API
→ Expose an API
```

configure the Application ID URI:

```text
api://<API_CLIENT_ID>
```

Then create the delegated scope:

```text
Scope name:
access_as_user

Who can consent:
Admins and users

Admin consent display name:
Access IncidentIQ API

Admin consent description:
Allows the application to access IncidentIQ on behalf of the signed-in user.

User consent display name:
Access IncidentIQ

User consent description:
Allows this application to access IncidentIQ on your behalf.

State:
Enabled
```

The resulting scope identifier is:

```text
api://<API_CLIENT_ID>/access_as_user
```

The React application will request this scope when frontend authentication is configured.

### Local API Configuration

The API reads:

```text
AzureAd:Instance
AzureAd:TenantId
AzureAd:ClientId
AzureAd:Scopes
```

`appsettings.json` contains the non-environment-specific values:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "",
    "ClientId": "",
    "Scopes": "access_as_user"
  }
}
```

Configure the tenant and API client IDs locally:

```powershell
dotnet user-secrets set "AzureAd:TenantId" "<TENANT_ID>" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "AzureAd:ClientId" "<API_CLIENT_ID>" `
    --project src\IncidentIQ.Api
```

Tenant IDs and client IDs are identifiers rather than credentials. User-secrets are still useful for keeping machine/environment-specific configuration out of committed files.

No API client secret is required.

### How API Authentication Works

Protected requests must include an OAuth access token:

```http
Authorization: Bearer <ACCESS_TOKEN>
```

The request flows through:

```text
HTTP request
      ↓
UseAuthentication()
      ↓
JWT bearer authentication
      ↓
Microsoft.Identity.Web
      ↓
validate token
      ↓
construct ClaimsPrincipal
      ↓
HttpContext.User
      ↓
UseAuthorization()
      ↓
require authenticated user
      ↓
require access_as_user
      ↓
controller
```

Token validation verifies properties including:

```text
signature
issuer / tenant
audience
expiration
```

The API then requires the delegated scope:

```text
access_as_user
```

All controller endpoints are currently mapped through:

```csharp
app.MapControllers()
    .RequireAuthorization()
    .RequireScope("access_as_user");
```

The resulting behaviour is:

```text
missing / invalid access token
→ 401 Unauthorized

valid authenticated token without required scope
→ 403 Forbidden

valid token with access_as_user
→ request reaches the controller
```

`GET /api/health` remains anonymous because it is mapped separately from the controllers.

### Current Stage 14A Limitation

The API authentication boundary is implemented, but the React application does not yet acquire access tokens.

Until frontend authentication is added, normal React API requests will therefore receive:

```text
401 Unauthorized
```

This is expected.

The next authentication stage adds a separate SPA registration and MSAL integration so React can:

```text
detect current authentication state
        ↓
sign the user into Microsoft Entra
        ↓
request access_as_user
        ↓
receive an access token
        ↓
send Authorization: Bearer <token>
        ↓
call the IncidentIQ API
```

### Automated Authentication Tests

API integration tests do not request real Microsoft Entra tokens.

The test server replaces production JWT authentication with a deterministic test scheme.

```text
Test request
→ TestAuthenticationHandler
→ test ClaimsPrincipal
→ normal ASP.NET authorization
→ controller
```

This allows the suite to verify:

```text
anonymous request → 401
authenticated + correct scope → success
authenticated + incorrect scope → 403
health check without authentication → success
```

without depending on Microsoft Entra or network connectivity during automated test runs.

Real Entra token verification is performed separately through local and Azure end-to-end authentication testing.

---

## Local Configuration and User-Secrets

Use .NET user-secrets or environment variables for local Azure-connected configuration. Never place keys, connection strings or tokens in committed settings files.

User-secrets are stored outside the repository, so they are suitable for machine-specific development configuration.

Inspect the secrets already configured for a project with:

```powershell
dotnet user-secrets list --project src\IncidentIQ.Api
dotnet user-secrets list --project src\IncidentIQ.Worker
dotnet user-secrets list --project tools\IncidentIQ.Evaluation
```

### Common Cosmos configuration

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

### Service Bus configuration

```text
ServiceBus:FullyQualifiedNamespace
ServiceBus:AnalyseIncidentQueueName
ServiceBus:IndexRunbookQueueName
ServiceBus:IndexHistoricalIncidentQueueName
ServiceBus:MaxDeliveryCount
```

### Azure AI configuration

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

The current development infrastructure uses:

```text
AzureAI:DeploymentName = incident-analysis
AzureAI:ModelName = gpt-5-mini

AzureAI:Embedding:DeploymentName = runbook-embedding
AzureAI:Embedding:ModelName = text-embedding-3-small
AzureAI:Embedding:Dimensions = 1536
```

The API needs the full Azure AI dependency set because it performs query embeddings **and** serves the Operational Assistant.

### Retrieve the Azure OpenAI endpoint

The endpoint changes when the disposable Azure development environment is recreated.

Retrieve the current value with:

```powershell
az cognitiveservices account list `
    --resource-group "rg-incidentiq-dev" `
    --query "[?kind=='OpenAI'].properties.endpoint | [0]" `
    --output tsv
```

For the complete Azure resource lookup/redeployment guide, see [Azure Dev Lifecycle](INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).

### Configure live Azure AI for the API

Set the values on the API project when you want the locally running API to use real Azure embeddings and the real Operational Assistant:

```powershell
dotnet user-secrets set "AzureAI:Endpoint" "<AZURE_AI_ENDPOINT>" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "AzureAI:DeploymentName" "incident-analysis" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "AzureAI:ModelName" "gpt-5-mini" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "AzureAI:Embedding:DeploymentName" "runbook-embedding" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "AzureAI:Embedding:ModelName" "text-embedding-3-small" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "AzureAI:Embedding:Dimensions" "1536" `
    --project src\IncidentIQ.Api
```

If the local React app calls the API while it is running outside `Development`, also configure:

```powershell
dotnet user-secrets set "Frontend:Origin" "http://localhost:5173" `
    --project src\IncidentIQ.Api
```

### Configure live Azure AI for the Worker

Set the same AI values on the Worker when testing real analysis or indexing:

```powershell
dotnet user-secrets set "AzureAI:Endpoint" "<AZURE_AI_ENDPOINT>" `
    --project src\IncidentIQ.Worker

dotnet user-secrets set "AzureAI:DeploymentName" "incident-analysis" `
    --project src\IncidentIQ.Worker

dotnet user-secrets set "AzureAI:ModelName" "gpt-5-mini" `
    --project src\IncidentIQ.Worker

dotnet user-secrets set "AzureAI:Embedding:DeploymentName" "runbook-embedding" `
    --project src\IncidentIQ.Worker

dotnet user-secrets set "AzureAI:Embedding:ModelName" "text-embedding-3-small" `
    --project src\IncidentIQ.Worker

dotnet user-secrets set "AzureAI:Embedding:Dimensions" "1536" `
    --project src\IncidentIQ.Worker
```

Only point the Worker at shared Azure Cosmos/Service Bus when you deliberately want full Azure-connected behaviour.

### Configure live embeddings for `IncidentIQ.Evaluation`

The Stage 13 evaluation tool deliberately uses the real Azure embedding model while keeping its controlled synthetic corpus and vector retrieval isolated from the normal application containers.

Initialise user-secrets once if the evaluation project does not yet have a `UserSecretsId`:

```powershell
dotnet user-secrets init `
    --project tools\IncidentIQ.Evaluation
```

Configure:

```powershell
dotnet user-secrets set "AzureAI:Endpoint" "<AZURE_AI_ENDPOINT>" `
    --project tools\IncidentIQ.Evaluation

dotnet user-secrets set "AzureAI:DeploymentName" "incident-analysis" `
    --project tools\IncidentIQ.Evaluation

dotnet user-secrets set "AzureAI:ModelName" "gpt-5-mini" `
    --project tools\IncidentIQ.Evaluation

dotnet user-secrets set "AzureAI:Embedding:DeploymentName" "runbook-embedding" `
    --project tools\IncidentIQ.Evaluation

dotnet user-secrets set "AzureAI:Embedding:ModelName" "text-embedding-3-small" `
    --project tools\IncidentIQ.Evaluation

dotnet user-secrets set "AzureAI:Embedding:Dimensions" "1536" `
    --project tools\IncidentIQ.Evaluation
```

The evaluation runner currently needs the chat deployment/model values because the shared Azure AI options validate them, even though Stage 13B itself only calls the embedding deployment.

No Azure OpenAI API key is required. Authentication is through `DefaultAzureCredential`.

Application Insights is optional for local debugging:

```text
APPLICATIONINSIGHTS_CONNECTION_STRING
```

---

## Selecting Deterministic vs Live AI

### Deterministic / in-memory AI

`Development` intentionally selects the deterministic AI implementations.

Run normally through Docker Compose or the existing Development launch profiles.

This is the default for:

- UI/API feature work,
- messaging and handler debugging,
- deterministic RAG orchestration,
- most unit/integration testing,
- development that does not need real model behaviour.

### Live Azure AI with otherwise local infrastructure

To exercise real Azure OpenAI, run the relevant host in a non-Development environment while leaving Cosmos and Service Bus configured for the local emulators.

The environment selects the AI implementation; the Cosmos and Service Bus configuration still determines where those dependencies live.

The launch profile can override environment settings, so use `--no-launch-profile`:

```powershell
$env:DOTNET_ENVIRONMENT = "Production"
dotnet run --project src\IncidentIQ.Api --no-launch-profile
```

or:

```powershell
$env:DOTNET_ENVIRONMENT = "Production"
dotnet run --project src\IncidentIQ.Worker --no-launch-profile
```

This is useful for testing:

```text
real Azure embeddings
real Incident analysis
real Operational Assistant generation
Azure OpenAI authentication/RBAC
structured-output behaviour
```

without also moving the application's source data and queues into Azure.

> Note: the API only runs its local `CosmosInitializer` automatically in `Development`. If you run the API as `Production` while still pointing it at the local Cosmos emulator, initialise the local emulator/container state first using the normal Development stack.

### Fully Azure-connected debugging

Use this only when the behaviour under investigation depends on Azure infrastructure itself.

Configure the API/Worker with the Azure Cosmos and/or Service Bus values, then run in a non-Development environment:

```powershell
$env:DOTNET_ENVIRONMENT = "Production"
dotnet run --project src\IncidentIQ.Api --no-launch-profile
```

For the Worker:

```powershell
$env:DOTNET_ENVIRONMENT = "Production"
dotnet run --project src\IncidentIQ.Worker --no-launch-profile
```

Useful verification targets include:

```text
real Cosmos vector retrieval
real Service Bus delivery
real Change Feed behaviour
Azure RBAC
Application Insights telemetry
real Azure OpenAI
```

When running the Worker this way, first consider whether the deployed Worker should be stopped to prevent competing consumers.

---

## Run the API Locally Against Azure

After supplying the required Azure configuration:

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

---

## Run the Worker Locally Against Azure

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

---

## Azure Resource Authentication Notes

Typical Azure-connected permissions include:

- Azure OpenAI `Cognitive Services OpenAI User`.
- Cosmos permissions required by the configured authentication method.
- Service Bus sender/receiver roles for the queues the local process actually uses.

A process may start successfully and still fail later when it reaches a dependency for which its identity lacks a role.

Remember the distinction:

```text
Local process
→ DefaultAzureCredential
→ developer Azure identity

Deployed API / Worker
→ Managed Identity
→ workload-specific Azure RBAC
```

---

## Azure AI Resilience

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
| UI/API feature work | Normal local development |
| Messaging/handler logic | Normal local development |
| Deterministic RAG orchestration tests | Normal local development |
| Stage 13 retrieval evaluation | Evaluation tool + live Azure embeddings |
| Real Azure OpenAI prompt/schema verification | Live-AI local debugging |
| Real embedding behaviour | Live-AI local debugging |
| Azure Cosmos vector behaviour | Fully Azure-connected debugging |
| Service Bus RBAC or delivery behaviour | Fully Azure-connected, deliberately isolated |
| Managed Identity verification | Deployed Azure environment |
| Application Insights verification | Azure-connected or deployed Azure |

For exact Azure resource values and redeployment commands, see [Azure Dev Lifecycle](INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).

For common failures, see [Troubleshooting](TROUBLESHOOTING.md).

---

## Breakpoints

Useful breakpoint groups are available in the repository breakpoint export under `docs/other/breakpoints.xml` when present. Import them into the IDE if useful; otherwise normal breakpoints around handlers, retrievers, queue senders/receivers and Azure AI adapters are sufficient.
