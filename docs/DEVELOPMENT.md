# Development Guide

## Normal Local Development

Prerequisites:
- Docker Desktop
- .NET 10 SDK
- Node.js/npm
- Azure CLI for Entra/Azure-connected testing

Repository-root `.env`:

```env
COSMOS_EMULATOR_KEY=<COSMOS_EMULATOR_KEY>
SERVICEBUS_SQL_PASSWORD=<LOCAL_SQL_PASSWORD>
```

Start:

```powershell
docker compose up --build
```

```text
Web:                  http://localhost:5173
API Swagger:          https://localhost:7156/swagger
Cosmos Data Explorer: http://localhost:1234
```

`Development` uses deterministic Incident analysis, embeddings and Operational Assistant implementations by default.

## Microsoft Entra Setup

Create two single-tenant app registrations.

### IncidentIQ API

- Application ID URI: `api://<API_CLIENT_ID>`
- delegated scope: `access_as_user`
- app roles: `Engineer`, `Administrator`
- no redirect URI or client secret required

Assign test users an API app role. Administrators also satisfy Engineer-level policies.

### IncidentIQ Web

- platform: SPA
- redirect URIs: `http://localhost:5173` and the Azure Static Web Apps URL
- delegated permission: `IncidentIQ API / access_as_user`
- no client secret required
- leave implicit grant/public-client flows disabled

### API user-secrets

```powershell
dotnet user-secrets set "AzureAd:TenantId" "<TENANT_ID>" --project src\IncidentIQ.Api
dotnet user-secrets set "AzureAd:ClientId" "<API_CLIENT_ID>" --project src\IncidentIQ.Api
dotnet user-secrets set "Kestrel:Certificates:Development:Password" "<DEV_CERT_PASSWORD>" --project src\IncidentIQ.Api
```

### Frontend `.env.local`

```env
VITE_ENTRA_TENANT_ID=<TENANT_ID>
VITE_ENTRA_CLIENT_ID=<INCIDENTIQ_WEB_CLIENT_ID>
VITE_API_SCOPE=api://<INCIDENTIQ_API_CLIENT_ID>/access_as_user
```

Authentication flow:

```text
React/MSAL → Entra login → access token
→ Authorization: Bearer <token>
→ API validates JWT + access_as_user + role policy
```

- missing/invalid token → `401`,
- authenticated but missing scope/required role → `403`,
- `/api/health` remains anonymous.

## Local Configuration

Inspect .NET user-secrets:

```powershell
dotnet user-secrets list --project src\IncidentIQ.Api
dotnet user-secrets list --project src\IncidentIQ.Worker
dotnet user-secrets list --project tools\IncidentIQ.Evaluation
```

Committed `appsettings.json` files contain placeholders/defaults. Machine-specific values should come from user-secrets, Docker environment variables or deployed Container App configuration.

## Deterministic vs Live Azure AI

Development uses deterministic AI by default. For targeted Azure OpenAI testing:

```powershell
dotnet user-secrets set "Development:UseLiveAzureAI" "true" --project src\IncidentIQ.Api
dotnet user-secrets set "Development:UseLiveAzureAI" "true" --project src\IncidentIQ.Worker
```

Enable it only for the host being tested. Live AI also requires the `AzureAI:*` configuration and Azure authentication/RBAC.

Set it back to `false` when finished.

The `Testing` environment always uses deterministic AI so automated tests do not depend on Azure OpenAI.

## Targeted Azure-Connected Debugging

Use only when testing real Azure behaviour such as Azure OpenAI, Cosmos vector search, Service Bus, Managed Identity/RBAC or Application Insights.

```powershell
az login
$env:AZURE_TOKEN_CREDENTIALS = "dev"
```

A local developer calling Azure OpenAI normally needs `Cognitive Services OpenAI User`.

Common Azure AI values:

```powershell
dotnet user-secrets set "AzureAI:Endpoint" "<AZURE_AI_ENDPOINT>" --project src\IncidentIQ.Api
dotnet user-secrets set "AzureAI:DeploymentName" "incident-analysis" --project src\IncidentIQ.Api
dotnet user-secrets set "AzureAI:ModelName" "gpt-5-mini" --project src\IncidentIQ.Api
dotnet user-secrets set "AzureAI:Embedding:DeploymentName" "runbook-embedding" --project src\IncidentIQ.Api
dotnet user-secrets set "AzureAI:Embedding:ModelName" "text-embedding-3-small" --project src\IncidentIQ.Api
dotnet user-secrets set "AzureAI:Embedding:Dimensions" "1536" --project src\IncidentIQ.Api
```

Use the same Azure AI keys for the Worker when testing live analysis/indexing.

## Observability

Deployed API and Worker export OpenTelemetry to Application Insights when `APPLICATIONINSIGHTS_CONNECTION_STRING` is configured.

- API role name: `IncidentIQ.Api`
- Worker role name: `IncidentIQ.Worker`
- custom `ActivitySource`: `IncidentIQ`
- custom `Meter`: `IncidentIQ`

Detailed trace/metric verification and KQL: [Observability & Scaling](OBSERVABILITY.md).

## Worker Warning

A local Worker pointed at shared Azure resources can consume messages intended for the deployed Worker, share Change Feed leases, write real dev data and dead-letter work.

Stop the deployed Worker or use isolated queues/database/leases before targeted Worker debugging. Never point local Worker testing at production.

## Evaluation Tool

```powershell
$env:AZURE_TOKEN_CREDENTIALS = "dev"
dotnet run --project tools\IncidentIQ.Evaluation
```

See [Evaluation README](../tools/IncidentIQ.Evaluation/ReadMe.md).

## Tests

```powershell
dotnet test .\IncidentIQ.slnx

cd src\IncidentIQ.Web
npm run build
npm run lint
```

For recreated Azure resource values, see [Azure Dev Lifecycle](INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).
