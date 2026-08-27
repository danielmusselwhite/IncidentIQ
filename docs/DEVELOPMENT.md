# IncidentIQ Development Guide

IncidentIQ supports two development modes:

1. **Fully local** — Docker Compose, Cosmos DB Emulator, and Service Bus Emulator.
2. **Azure-connected** — API and Worker run locally against the Azure development environment.

For Azure resource creation, teardown, and secret refresh instructions, see [IncidentIQ Azure Dev Environment Lifecycle](INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).

---

## Option 1 — Fully Local

This is the normal day-to-day development mode.

### Prerequisites

- Docker Desktop
- .NET 10 SDK
- Node.js / npm

### 1. Configure `.env`

Create a `.env` file in the repository root:

```env
COSMOS_EMULATOR_KEY=<COSMOS_EMULATOR_KEY>
SERVICEBUS_SQL_PASSWORD=<LOCAL_SQL_PASSWORD>
```

`.env` is used by Docker Compose and must remain outside source control.

### 2. Start the Backend

From the repository root:

```powershell
docker compose up --build
```

This starts the API, Worker, Cosmos DB Emulator, Service Bus Emulator, and the SQL Server dependency used by the Service Bus Emulator.

The Service Bus Emulator queue is defined in:

```text
infra/local/servicebus/Config.json
```

Current queue:

```text
analyse-incident
```

### 3. Start the Frontend

In another terminal:

```powershell
cd src\IncidentIQ.Web
npm install
npm run dev
```

The frontend normally runs at:

```text
http://localhost:5173
```

and uses `VITE_API_BASE_URL` to locate the API.

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
   Cosmos Incident update
          ↓
Queued → Processing → Completed / Failed
```

The Incident Detail page polls the API while processing is active, so status changes appear automatically.

### 5. Local Cosmos Data Explorer

The Cosmos vNext emulator Data Explorer is normally available at:

```text
http://localhost:1234
```

Useful containers include:

```text
IncidentIQ
├── Incidents
├── Runbooks
└── ChangeFeedLeases
```

`Incidents` contains both Incident and analysis-outbox documents. `ChangeFeedLeases` is SDK-managed state used by the Cosmos Change Feed Processor.

### Stop the Environment

Stop containers while retaining normal persisted volumes:

```powershell
docker compose down
```

Remove containers and persisted volumes:

```powershell
docker compose down -v
```

Use `-v` only when you intentionally want to reset local emulator data, such as after a Cosmos partition-key change.

---

## Option 2 — Run Locally Against Azure

Use this mode to verify real Azure Cosmos DB, Service Bus, authentication/RBAC, and telemetry behaviour.

### 1. Ensure the Dev Environment Exists

The development environment is:

```text
rg-incidentiq-dev
```

See [IncidentIQ Azure Dev Environment Lifecycle](INCIDENTIQ-AZURE-DEV-LIFECYCLE.md) for deployment and recreation steps.

### 2. Login

```powershell
az login
```

`DefaultAzureCredential` can then use your local Azure identity where supported.

### 3. Configure Local Settings

The API and Worker use separate .NET user-secrets stores.

Common Cosmos settings include:

```text
Cosmos:Endpoint
Cosmos:Key
Cosmos:DatabaseName
Cosmos:IncidentsContainerName
Cosmos:RunbooksContainerName
Cosmos:ChangeFeedLeasesContainerName
```

The Worker additionally requires Service Bus configuration:

```text
ServiceBus:FullyQualifiedNamespace
ServiceBus:AnalyseIncidentQueueName
ServiceBus:MaxDeliveryCount
```

If SAS authentication is being used instead of `DefaultAzureCredential`:

```text
ServiceBus:ConnectionString
```

The API can additionally use:

```text
APPLICATIONINSIGHTS_CONNECTION_STRING
```

For exact Azure CLI commands and values that need refreshing after redeployment, see the [Azure Dev Lifecycle](INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).

### 4. Start the API

```powershell
dotnet run --project src\IncidentIQ.Api
```

### 5. Start the Worker

```powershell
dotnet run --project src\IncidentIQ.Worker
```

The Worker runs both:

```text
IncidentOutboxWorker
└── Cosmos Change Feed → Service Bus

AnalyseIncidentWorker
└── Service Bus → Incident processing
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

## Which Mode Should I Use?

Use **Docker Compose** for normal feature development and local end-to-end testing.

Use **Azure-connected local execution** when verifying real Azure integration, RBAC, Cosmos behaviour, Service Bus behaviour, or telemetry.

For automated and manual reliability testing, see [tests/README.md](../tests/README.md).
