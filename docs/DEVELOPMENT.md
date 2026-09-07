# IncidentIQ Development Guide

IncidentIQ supports two main development modes:

1. **Fully local application workflow** — Docker Compose with Cosmos DB and Service Bus emulators plus `DevelopmentDummyIncidentAnalyzer`. This is the normal day-to-day mode and does **not** require Azure OpenAI credentials.
2. **Azure-connected verification** — API/Worker run against the Azure development environment when you specifically want to verify real Cosmos DB, Service Bus, Azure OpenAI, RBAC, or Application Insights behaviour.

For Azure resource creation, teardown, and secret refresh instructions, see [IncidentIQ Azure Dev Environment Lifecycle](INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).

---

## Option 1 — Fully Local Development

### Prerequisites

- Docker Desktop
- Visual Studio with Container/Compose support, or Docker Compose CLI
- .NET 10 SDK
- Node.js / npm

### 1. Configure `.env`

Create a `.env` file in the repository root:

```env
COSMOS_EMULATOR_KEY=<COSMOS_EMULATOR_KEY>
SERVICEBUS_SQL_PASSWORD=<LOCAL_SQL_PASSWORD>
```

`.env` is used by Docker Compose and must remain outside source control.

The SQL password must satisfy SQL Server password complexity requirements.

### 2. Start the Application

The project supports Visual Studio Docker Compose debugging. Select the Docker Compose debug target and start debugging, or run:

```powershell
docker compose up --build
```

The compose environment starts the API, Worker, React frontend, Cosmos DB Emulator, Service Bus Emulator, and the SQL Server dependency used by the Service Bus Emulator.

The Worker must run with:

```text
DOTNET_ENVIRONMENT=Development
```

In Development, dependency injection selects:

```text
IIncidentAnalyzer
└── DevelopmentDummyIncidentAnalyzer
```

so the complete analysis workflow remains deterministic and local.

The Service Bus Emulator queue is defined in:

```text
infra/local/servicebus/Config.json
```

Current queue:

```text
analyse-incident
```

### 3. Local URLs

Typical local endpoints are:

```text
Web:                  http://localhost:5173
API Swagger:          https://localhost:7156/swagger
Cosmos Data Explorer: http://localhost:1234
```

The frontend uses `VITE_API_BASE_URL` to locate the API.

In Development, the API avoids HTTPS redirection for the local HTTP frontend/API path where required by the Docker setup, preventing CORS problems caused by an HTTP → HTTPS redirect.

### 4. Local Runtime Flow

```text
React
  ↓
POST /api/incidents
  ↓
API / Application
  ↓
Cosmos transactional batch
  ├── Incident (Queued)
  └── AnalyseIncident Outbox
          ↓
     Cosmos Change Feed
          ↓
   IncidentOutboxWorker
          ↓
 Service Bus Emulator
          ↓
 AnalyseIncidentWorker
          ↓
 DevelopmentDummyIncidentAnalyzer
          ↓
 Completed Incident + structured analysis
          ↓
      Cosmos
          ↓
React polls status then fetches /analysis
```

The dummy analyzer still returns the same Application-level `IncidentAnalysisResult` shape used by Azure OpenAI, so API persistence/retrieval and frontend rendering are exercised locally.

### 5. Local Cosmos Data Explorer

Useful containers include:

```text
IncidentIQ
├── Incidents
├── Runbooks
└── ChangeFeedLeases
```

`Incidents` uses `/incidentId` and contains:

```text
IncidentDocument
IncidentAnalysisOutboxDocument
IncidentAnalysisDocument
```

`ChangeFeedLeases` is SDK-managed state used by the Cosmos Change Feed Processor.

### Stop the Environment

Stop containers while retaining persisted volumes:

```powershell
docker compose down
```

Remove containers and persisted volumes:

```powershell
docker compose down -v
```

Use `-v` only when you intentionally want to reset emulator state. If only the Service Bus SQL state is invalid, prefer recreating only its SQL data volume rather than wiping Cosmos data too.

---

## Option 2 — Run Locally Against Azure

Use this mode to verify real Azure dependencies and deployed-style authentication/telemetry.

### 1. Ensure the Dev Environment Exists

The disposable development environment is:

```text
rg-incidentiq-dev
```

See [IncidentIQ Azure Dev Environment Lifecycle](INCIDENTIQ-AZURE-DEV-LIFECYCLE.md) for deployment and recreation steps.

### 2. Login

```powershell
az login
```

`DefaultAzureCredential` can then use your developer Azure identity where supported.

### 3. Configure Local Settings

The API and Worker use separate .NET user-secrets stores.

Common Cosmos settings:

```text
Cosmos:Endpoint
Cosmos:Key
Cosmos:DatabaseName
Cosmos:IncidentsContainerName
Cosmos:RunbooksContainerName
Cosmos:ChangeFeedLeasesContainerName
```

Worker Service Bus settings:

```text
ServiceBus:FullyQualifiedNamespace
ServiceBus:AnalyseIncidentQueueName
ServiceBus:MaxDeliveryCount
```

Azure AI settings:

```text
AzureAI:Endpoint
AzureAI:DeploymentName
AzureAI:ModelName
```

The analyzer also has bounded-resilience options with application defaults:

```text
AzureAI:MaxRetries = 2
AzureAI:NetworkTimeoutSeconds = 60
AzureAI:RequestTimeoutSeconds = 90
```

These can be overridden through normal configuration if needed.

To use the real Azure analyzer while running the Worker locally, run the Worker in a **non-Development** environment and provide the Azure AI settings. `Development` intentionally selects the deterministic analyzer.

The signed-in Azure identity must have the permissions required by the resources it accesses, including `Cognitive Services OpenAI User` for Azure OpenAI.

If SAS authentication is being used instead of `DefaultAzureCredential` for Service Bus:

```text
ServiceBus:ConnectionString
```

Application Insights can be configured with:

```text
APPLICATIONINSIGHTS_CONNECTION_STRING
```

For exact Azure CLI commands and values that may need refreshing after redeployment, see the [Azure Dev Lifecycle](INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).

### 4. Start the API

```powershell
dotnet run --project src\IncidentIQ.Api
```

### 5. Start the Worker

For normal Development/dummy-AI behaviour:

```powershell
dotnet run --project src\IncidentIQ.Worker
```

For real Azure AI verification, set the Worker environment to a non-Development value before starting it. For example in PowerShell:

```powershell
$env:DOTNET_ENVIRONMENT = "Production"
dotnet run --project src\IncidentIQ.Worker
```

The Worker runs both:

```text
IncidentOutboxWorker
└── Cosmos Change Feed → Service Bus

AnalyseIncidentWorker
└── Service Bus → IIncidentAnalyzer → analysis persistence
```

### 6. Start the Frontend

```powershell
cd src\IncidentIQ.Web
npm install
npm run dev
```

Open:

```text
http://localhost:5173
```

---

## AI Resilience Behaviour

The real Azure analyzer intentionally uses two resilience layers with different responsibilities:

```text
Azure OpenAI SDK
→ small bounded retry policy
→ individual network timeout

AzureIncidentAnalyzer
→ overall request timeout
→ failure classification + structured log
→ rethrow

Service Bus
→ durable message redelivery
→ DLQ after retry exhaustion
```

The analyzer does not swallow Azure failures. This allows the existing Worker/Service Bus reliability flow to remain the outer retry mechanism.

## Which Mode Should I Use?

Use **Docker Compose + DevelopmentDummyIncidentAnalyzer** for normal feature development and local end-to-end testing.

Use **Azure-connected execution** when verifying real Azure OpenAI, Cosmos DB, Service Bus, Managed Identity/RBAC, or telemetry behaviour.

For automated and manual testing, see [tests/ReadMe.md](../tests/ReadMe.md).
