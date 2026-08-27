# IncidentIQ Development Guide

This guide explains how to run IncidentIQ in its two supported development modes:

1. **Fully local** using Docker, Cosmos DB Emulator, and Service Bus Emulator.
2. **Locally against Azure** using the deployed development resources.

---

# Option 1 — Run Fully Locally

This is the normal day-to-day development mode.

It uses:

- React frontend
- ASP.NET Core API
- .NET Worker
- Cosmos DB Emulator
- Service Bus Emulator

```text
React
  ↓
IncidentIQ.Api
  ↓
Cosmos DB Emulator
  ↓
Service Bus Emulator
  ↓
IncidentIQ.Worker
  ↓
Cosmos DB Emulator
```

## Prerequisites

Install:

- Docker Desktop
- .NET 10 SDK
- Node.js / npm

---

## 1. Configure `.env`

Create a `.env` file in the repository root if one does not already exist.

Example:

```env
COSMOS_EMULATOR_KEY=<COSMOS_EMULATOR_KEY>
SERVICEBUS_SQL_PASSWORD=<LOCAL_SQL_PASSWORD>
```

The `.env` file is used by Docker Compose and should remain outside source control.

Ensure it is ignored by Git:

```gitignore
.env
```

---

## 2. Start the Backend

From the repository root:

```powershell
docker compose up --build
```

This starts:

```text
IncidentIQ.Api
IncidentIQ.Worker
Cosmos DB Emulator
Service Bus Emulator
Service Bus SQL Server
```

The Cosmos Emulator uses persistent Docker volumes, so local data is retained between normal container restarts.

The Service Bus Emulator recreates its configured queues from:

```text
infra/local/servicebus/Config.json
```

The current queue is:

```text
analyse-incident
```

---

## 3. Start the Frontend

Open another terminal:

```powershell
cd src\IncidentIQ.Web
npm install
npm run dev
```

The frontend normally runs at:

```text
http://localhost:5173
```

The frontend API URL is configured through:

```env
VITE_API_BASE_URL=https://localhost:7156
```

---

## 4. Open the Application

Typical local URLs:

```text
Frontend: http://localhost:5173
API:      https://localhost:7156
Swagger:  https://localhost:7156/swagger
```

---

## Expected Local Flow

Submitting an incident should result in:

```text
React
  ↓
POST /api/incidents
  ↓
API stores Incident as Queued
  ↓
Cosmos DB Emulator
  ↓
AnalyseIncident command
  ↓
Service Bus Emulator
  ↓
Worker receives command
  ↓
Incident becomes Processing
  ↓
Incident becomes Completed
  ↓
Cosmos DB Emulator
```

The frontend currently requires a manual refresh to see asynchronous status changes.

Automatic processing-status polling will be added later.

---

## Stop the Local Environment

Stop the running containers with:

```powershell
docker compose down
```

This does not normally delete the persistent Cosmos data volume.

To remove volumes as well:

```powershell
docker compose down -v
```

Only use `-v` when you intentionally want to remove local persisted emulator data.

---

# Option 2 — Run Locally Against Azure

Use this mode when you want to test the application against the real Azure development resources.

It uses:

- React frontend running locally
- ASP.NET Core API running locally
- .NET Worker running locally
- Azure Cosmos DB
- Azure Service Bus
- Application Insights

```text
React
  ↓
Local IncidentIQ.Api
  ↓
Azure Cosmos DB
  ↓
Azure Service Bus
  ↓
Local IncidentIQ.Worker
  ↓
Azure Cosmos DB
```

---

## 1. Ensure the Azure Dev Environment Exists

The Azure development resource group must be deployed first:

```text
rg-incidentiq-dev
```

For deployment, teardown, recreation, and configuration instructions see:

```text
docs/INCIDENTIQ-AZURE-DEV-LIFECYCLE.md
```

---

## 2. Login to Azure

```powershell
az login
```

This allows `DefaultAzureCredential` to use your local Azure developer identity where Managed Identity / Entra authentication is supported.

---

## 3. Configure API User-Secrets

The API has its own .NET user-secrets store.

Typical configuration includes:

```json
{
  "Cosmos:Endpoint": "https://<COSMOS_ACCOUNT>.documents.azure.com:443/",
  "Cosmos:Key": "<COSMOS_KEY>",
  "Cosmos:DatabaseName": "IncidentIQ",
  "Cosmos:IncidentsContainerName": "Incidents",
  "Cosmos:RunbooksContainerName": "Runbooks",
  "Cosmos:ChangeFeedLeasesContainerName": "ChangeFeedLeases",
  "ServiceBus:FullyQualifiedNamespace": "<SERVICE_BUS_NAMESPACE>.servicebus.windows.net",
  "ServiceBus:AnalyseIncidentQueueName": "analyse-incident",
  "APPLICATIONINSIGHTS_CONNECTION_STRING": "<APP_INSIGHTS_CONNECTION_STRING>"
}
```

Set individual values using:

```powershell
dotnet user-secrets set "Cosmos:Endpoint" "<COSMOS_ENDPOINT>" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "Cosmos:Key" "<COSMOS_KEY>" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "Cosmos:DatabaseName" "IncidentIQ" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "Cosmos:IncidentsContainerName" "Incidents" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "Cosmos:RunbooksContainerName" "Runbooks" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "ServiceBus:FullyQualifiedNamespace" "<SERVICE_BUS_NAMESPACE>.servicebus.windows.net" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "ServiceBus:AnalyseIncidentQueueName" "analyse-incident" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "APPLICATIONINSIGHTS_CONNECTION_STRING" "<APP_INSIGHTS_CONNECTION_STRING>" `
    --project src\IncidentIQ.Api
```

If local execution uses a Service Bus connection string rather than `DefaultAzureCredential`, also configure:

```powershell
dotnet user-secrets set "ServiceBus:ConnectionString" "<SERVICE_BUS_CONNECTION_STRING>" `
    --project src\IncidentIQ.Api
```

---

## 4. Configure Worker User-Secrets

The Worker has a separate user-secrets store.

It requires Cosmos and Service Bus configuration because it:

```text
consumes AnalyseIncident commands
        ↓
loads Incidents from Cosmos
        ↓
updates Incident status
```

Typical configuration:

```json
{
  "Cosmos:Endpoint": "https://<COSMOS_ACCOUNT>.documents.azure.com:443/",
  "Cosmos:Key": "<COSMOS_KEY>",
  "Cosmos:DatabaseName": "IncidentIQ",
  "Cosmos:IncidentsContainerName": "Incidents",
  "Cosmos:RunbooksContainerName": "Runbooks",
  "ServiceBus:FullyQualifiedNamespace": "<SERVICE_BUS_NAMESPACE>.servicebus.windows.net",
  "ServiceBus:AnalyseIncidentQueueName": "analyse-incident"
}
```

Set values using:

```powershell
dotnet user-secrets set "Cosmos:Endpoint" "<COSMOS_ENDPOINT>" `
    --project src\IncidentIQ.Worker

dotnet user-secrets set "Cosmos:Key" "<COSMOS_KEY>" `
    --project src\IncidentIQ.Worker

dotnet user-secrets set "Cosmos:DatabaseName" "IncidentIQ" `
    --project src\IncidentIQ.Worker

dotnet user-secrets set "Cosmos:IncidentsContainerName" "Incidents" `
    --project src\IncidentIQ.Worker

dotnet user-secrets set "Cosmos:RunbooksContainerName" "Runbooks" `
    --project src\IncidentIQ.Worker

dotnet user-secrets set "ServiceBus:FullyQualifiedNamespace" "<SERVICE_BUS_NAMESPACE>.servicebus.windows.net" `
    --project src\IncidentIQ.Worker

dotnet user-secrets set "ServiceBus:AnalyseIncidentQueueName" "analyse-incident" `
    --project src\IncidentIQ.Worker
```

If required:

```powershell
dotnet user-secrets set "ServiceBus:ConnectionString" "<SERVICE_BUS_CONNECTION_STRING>" `
    --project src\IncidentIQ.Worker
```

---

## 5. Start the API

From the repository root:

```powershell
dotnet run --project src\IncidentIQ.Api
```

The API will now use the Azure development resources configured through user-secrets.

---

## 6. Start the Worker

Open another terminal:

```powershell
dotnet run --project src\IncidentIQ.Worker
```

The Worker should connect to:

```text
Azure Service Bus
└── analyse-incident
```

and update incidents in Azure Cosmos DB.

---

## 7. Start the Frontend

Open another terminal:

```powershell
cd src\IncidentIQ.Web
npm install
npm run dev
```

Then open:

```text
http://localhost:5173
```

---

# Getting Azure Configuration Values

For exact commands used to retrieve:

- Cosmos endpoint
- Cosmos key
- Service Bus namespace
- Service Bus connection string
- Application Insights connection string

see:

```text
docs/INCIDENTIQ-AZURE-DEV-LIFECYCLE.md
```

That document also explains which values need to be refreshed after `rg-incidentiq-dev` is recreated.

---

# Choosing a Development Mode

Use **Docker Compose** for normal development:

```text
Fast
Self-contained
No Azure dependency
No ongoing Azure resource cost
```

Use **local execution against Azure** when you need to verify:

```text
Real Cosmos behaviour
Real Service Bus behaviour
Azure authentication / RBAC
Application Insights telemetry
Azure integration
```

The intended workflow is therefore:

```text
Everyday development
→ Docker Compose

Azure integration verification
→ dotnet run against rg-incidentiq-dev
```
