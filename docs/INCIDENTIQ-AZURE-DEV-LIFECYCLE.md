# IncidentIQ Azure Dev Environment Lifecycle

This guide owns the lifecycle of the Azure development environment: create, delete, recreate, and refresh local Azure configuration.

For normal local/Docker startup instructions, see [Development Guide](DEVELOPMENT.md).

## Normal Dev Teardown / Redeploy

To stop Azure development costs, delete only:

```text
rg-incidentiq-dev
```

Keep:

```text
rg-incidentiq-bootstrap
```

`rg-incidentiq-bootstrap` contains the GitHub deployment identity and OIDC federation. Keeping it means the GitHub `development` environment identity values do not need to change.

Deleting `rg-incidentiq-dev` removes the disposable development resources such as Azure Container Registry, Container Apps, Static Web Apps, Cosmos DB, Service Bus, Azure OpenAI, monitoring resources, and workload identities.

### Recreate the Dev Environment

Re-run the bootstrap deployment so the development resource group and deployment RBAC are recreated:

```powershell
az deployment sub create `
    --name "incidentiq-bootstrap" `
    --location "uksouth" `
    --template-file "infra/bootstrap/main.bicep" `
    --parameters `
        githubOwner="danielmusselwhite" `
        githubOwnerId="56388919" `
        githubRepository="IncidentIQ" `
        githubRepositoryId="1342669343"
```

Then run the **Deploy Development** GitHub Actions workflow.

The workflow provisions or updates the Azure resources, builds and pushes the API and Worker images to ACR, deploys the Container Apps, and deploys the React frontend.

The normal environment deployment is defined by:

```text
infra/main.bicep
└── infra/environments/dev.bicepparam
```

### Cosmos NoSQL Vector Search

The `RunbookChunks` container uses a Cosmos DB vector policy and therefore requires the `EnableNoSQLVectorSearch` capability on the Cosmos account.

This is an **account-level Cosmos capability**. It is not registered using `az feature register`.

After a Cosmos account has been created, retrieve its name:

```powershell
$resourceGroup = "rg-incidentiq-dev"

$accountName = az cosmosdb list `
    --resource-group $resourceGroup `
    --query "[0].name" `
    --output tsv
```

Check the current account capabilities:

```powershell
az cosmosdb show `
    --resource-group $resourceGroup `
    --name $accountName `
    --query capabilities `
    --output json
```

For IncidentIQ, the expected capabilities are:

```json
[
  {
    "name": "EnableServerless"
  },
  {
    "name": "EnableNoSQLVectorSearch"
  }
]
```

If vector search is not enabled, update the account while preserving the existing Serverless capability:

```powershell
az cosmosdb update `
    --resource-group $resourceGroup `
    --name $accountName `
    --capabilities @("EnableServerless","EnableNoSQLVectorSearch")
```

Then confirm the capabilities again:

```powershell
az cosmosdb show `
    --resource-group $resourceGroup `
    --name $accountName `
    --query capabilities `
    --output json
```

Vector-search capability activation can take several minutes to propagate.

The `RunbookChunks` container must only be created once the vector-search capability is active because its vector embedding policy and vector index are defined when the container is created.

If an environment deployment provisions the Cosmos account but `RunbookChunks` fails because vector search is not yet active:

1. Enable or confirm `EnableNoSQLVectorSearch` using the commands above.
2. Wait for the capability to become active.
3. Re-run the **Deploy Development** workflow.

The Bicep definition remains the source of truth for the Cosmos account capability and vector-enabled container configuration.

## Values to Refresh After Redeploy

Values derived from recreated resources may change, especially:

```text
Cosmos:Key

AzureAI:Endpoint

APPLICATIONINSIGHTS_CONNECTION_STRING

ServiceBus:ConnectionString    (only when SAS authentication is used)
```

Deterministic resource/container/deployment names normally remain unchanged.

These values are mainly required when running the API or Worker locally against the recreated Azure environment. Normal Docker Compose development uses the local emulators and deterministic development AI implementations, so it does not require the Azure AI endpoint.

The deployed Container Apps receive Azure resource configuration through Bicep and authenticate to Azure services using Managed Identity.

Common Cosmos configuration:

```text
Cosmos:Endpoint
Cosmos:DatabaseName = IncidentIQ
Cosmos:IncidentsContainerName = Incidents
Cosmos:HistoricalIncidentVectorsContainerName = HistoricalIncidentVectors
Cosmos:RunbooksContainerName = Runbooks
Cosmos:RunbookChunksContainerName = RunbookChunks
Cosmos:ChangeFeedLeasesContainerName = ChangeFeedLeases
```

Worker Service Bus configuration:

```text
ServiceBus:FullyQualifiedNamespace
ServiceBus:AnalyseIncidentQueueName = analyse-incident
ServiceBus:IndexRunbookQueueName = index-runbook
ServiceBus:MaxDeliveryCount
```

Worker Azure AI configuration:

```text
AzureAI:Endpoint
AzureAI:DeploymentName = incident-analysis
AzureAI:ModelName = gpt-5-mini
```

Worker Runbook embedding configuration:

```text
AzureAI:Embedding:DeploymentName = runbook-embedding
AzureAI:Embedding:ModelName = text-embedding-3-small
AzureAI:Embedding:Dimensions = 1536
```

The application defaults its Azure AI resilience settings to:

```text
AzureAI:MaxRetries = 2
AzureAI:NetworkTimeoutSeconds = 60
AzureAI:RequestTimeoutSeconds = 90
```

These are application configuration values rather than separate Azure resources and can be overridden through normal configuration when required.

When keys/connection strings are not configured, the application uses `DefaultAzureCredential` and the local/Azure identity must have the required RBAC permissions.

## Retrieve Azure Configuration Values

### Cosmos Account Name

```powershell
az cosmosdb list `
    --resource-group "rg-incidentiq-dev" `
    --query "[0].name" `
    --output tsv
```

### Cosmos Endpoint

```powershell
$accountName = az cosmosdb list `
    --resource-group "rg-incidentiq-dev" `
    --query "[0].name" `
    --output tsv

az cosmosdb show `
    --name $accountName `
    --resource-group "rg-incidentiq-dev" `
    --query documentEndpoint `
    --output tsv
```

### Cosmos Key

```powershell
$accountName = az cosmosdb list `
    --resource-group "rg-incidentiq-dev" `
    --query "[0].name" `
    --output tsv

az cosmosdb keys list `
    --name $accountName `
    --resource-group "rg-incidentiq-dev" `
    --type keys `
    --query primaryMasterKey `
    --output tsv
```

### Cosmos Capabilities

```powershell
$accountName = az cosmosdb list `
    --resource-group "rg-incidentiq-dev" `
    --query "[0].name" `
    --output tsv

az cosmosdb show `
    --resource-group "rg-incidentiq-dev" `
    --name $accountName `
    --query capabilities `
    --output json
```

Expected:

```text
EnableServerless
EnableNoSQLVectorSearch
```

### Application Insights Connection String

```powershell
az monitor app-insights component show `
    --app "appi-incidentiq-dev" `
    --resource-group "rg-incidentiq-dev" `
    --query connectionString `
    --output tsv
```

### Service Bus Namespace

```powershell
az servicebus namespace list `
    --resource-group "rg-incidentiq-dev" `
    --query "[0].name" `
    --output tsv
```

Use the returned namespace as:

```text
<namespace-name>.servicebus.windows.net
```

### Azure AI Endpoint

```powershell
az cognitiveservices account list `
    --resource-group "rg-incidentiq-dev" `
    --query "[?kind=='OpenAI'].properties.endpoint | [0]" `
    --output tsv
```

### Service Bus Connection String

Only required when using SAS authentication instead of `DefaultAzureCredential`:

```powershell
az servicebus namespace authorization-rule keys list `
    --resource-group "rg-incidentiq-dev" `
    --namespace-name "<SERVICE_BUS_NAMESPACE>" `
    --name "RootManageSharedAccessKey" `
    --query primaryConnectionString `
    --output tsv
```

## Update User-Secrets

Example API values that commonly need refreshing:

```powershell
dotnet user-secrets set "Cosmos:Key" "<COSMOS_KEY>" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "APPLICATIONINSIGHTS_CONNECTION_STRING" "<APP_INSIGHTS_CONNECTION_STRING>" `
    --project src\IncidentIQ.Api
```

Example Worker values for real Azure integration:

```powershell
dotnet user-secrets set "Cosmos:Key" "<COSMOS_KEY>" `
    --project src\IncidentIQ.Worker

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

If SAS Service Bus authentication is being used:

```powershell
dotnet user-secrets set "ServiceBus:ConnectionString" "<SERVICE_BUS_CONNECTION_STRING>" `
    --project src\IncidentIQ.Worker
```

## Full Deployment From Scratch

Use this when the bootstrap infrastructure does not yet exist.

### 1. Login

```powershell
az login
```

### 2. Register Required Resource Providers

```powershell
az provider register --namespace Microsoft.DocumentDB --wait
az provider register --namespace Microsoft.OperationalInsights --wait
az provider register --namespace Microsoft.Insights --wait
az provider register --namespace Microsoft.ServiceBus --wait
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.ContainerRegistry --wait
az provider register --namespace Microsoft.Web --wait
az provider register --namespace Microsoft.ManagedIdentity --wait
az provider register --namespace Microsoft.CognitiveServices --wait
```

These registrations are subscription-level and normally only need to be completed once.

Cosmos NoSQL vector search does **not** use `az feature register`. It is enabled on the individual Cosmos account using the `EnableNoSQLVectorSearch` capability.

### 3. Deploy Bootstrap Infrastructure

```powershell
az deployment sub create `
    --name "incidentiq-bootstrap" `
    --location "uksouth" `
    --template-file "infra/bootstrap/main.bicep" `
    --parameters `
        githubOwner="danielmusselwhite" `
        githubOwnerId="56388919" `
        githubRepository="IncidentIQ" `
        githubRepositoryId="1342669343"
```

This creates:

```text
rg-incidentiq-bootstrap
└── GitHub deployment managed identity
    └── GitHub OIDC federated credential

rg-incidentiq-dev
└── deployment identity RBAC
    ├── Contributor
    ├── Role Based Access Control Administrator
    └── AcrPush
```

The RBAC Administrator role is scoped to `rg-incidentiq-dev`.

`AcrPush` allows the GitHub deployment identity to push the API and Worker container images into the development Azure Container Registry.

Bootstrap is intentionally separate from normal application deployment. Re-run it manually when bootstrap-level identity, OIDC federation, or deployment RBAC changes.

### 4. Configure the GitHub `development` Environment

Set the bootstrap deployment outputs as GitHub environment secrets:

```text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
```

No client secret is required because GitHub authenticates through OIDC.

### 5. Deploy the Development Environment

Run the **Deploy Development** GitHub Actions workflow.

The deployment workflow:

```text
tests
→ Bicep validation + What-If
→ provision/update infrastructure
→ build + push API/Worker images
→ deploy Container Apps
→ build + deploy React frontend
```

The Bicep environment definition includes:

```text
rg-incidentiq-dev

├── Azure Container Registry

├── Azure Container Apps Environment
│   ├── API Container App
│   └── Worker Container App

├── Azure Static Web Apps

├── Azure Cosmos DB
│   └── IncidentIQ
│       ├── Incidents              /incidentId
│       ├── Runbooks               /id
│       ├── RunbookChunks          /runbookId (vector-enabled)
│       └── ChangeFeedLeases       /id

├── Azure Service Bus
│   ├── analyse-incident
│   │   └── $DeadLetterQueue
│   └── index-runbook
│       └── $DeadLetterQueue

├── Azure OpenAI
│   ├── incident-analysis deployment (gpt-5-mini)
│   └── runbook-embedding deployment (text-embedding-3-small)

├── API Managed Identity

├── Worker Managed Identity

├── Application Insights

└── Log Analytics
```

### 6. Confirm Cosmos Vector Search

Once the Cosmos account exists:

```powershell
$resourceGroup = "rg-incidentiq-dev"

$accountName = az cosmosdb list `
    --resource-group $resourceGroup `
    --query "[0].name" `
    --output tsv
```

Check its capabilities:

```powershell
az cosmosdb show `
    --resource-group $resourceGroup `
    --name $accountName `
    --query capabilities `
    --output json
```

The account should contain:

```text
EnableServerless
EnableNoSQLVectorSearch
```

If `EnableNoSQLVectorSearch` is missing:

```powershell
az cosmosdb update `
    --resource-group $resourceGroup `
    --name $accountName `
    --capabilities @("EnableServerless","EnableNoSQLVectorSearch")
```

Wait for the capability to become active, then rerun the **Deploy Development** workflow so the vector-enabled `RunbookChunks` container can be provisioned.

### 7. Verify the Deployment

After deployment, submit an Incident through the frontend and verify:

```text
Queued
→ Processing
→ real Azure OpenAI analysis
→ Completed
→ persisted analysis returned by GET /api/incidents/{id}/analysis
→ analysis displayed in React
```

Worker/Application Insights logs should also contain structured AI completion/failure telemetry such as duration, deployment/model, and a failure category when applicable.

For Runbook ingestion verification, create or update a Runbook and confirm:

```text
Runbook persisted
→ Runbooks Change Feed
→ index-runbook
→ IndexRunbookWorker
→ text-embedding-3-small
→ RunbookChunks populated
→ embedding length = 1536
```

Edit the Runbook and confirm stale chunks are replaced.

Delete the Runbook and confirm its derived `RunbookChunks` are removed.

### 8. Refresh Local Configuration

Retrieve recreated resource values using the commands above, update user-secrets where necessary, then run the API and Worker locally if required.

For runtime startup instructions, return to the [Development Guide](DEVELOPMENT.md).