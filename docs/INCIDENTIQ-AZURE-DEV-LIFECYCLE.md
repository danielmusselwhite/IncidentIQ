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

## Values to Refresh After Redeploy

Values derived from recreated resources may change, especially:

```text
Cosmos:Key
AzureAI:Endpoint
APPLICATIONINSIGHTS_CONNECTION_STRING
ServiceBus:ConnectionString    (only when SAS authentication is used)
```

Deterministic resource/container names normally remain unchanged.

These values are mainly required when running the API or Worker locally against the recreated Azure environment.

The deployed Container Apps receive their Azure resource configuration through Bicep and authenticate to Azure services using Managed Identity.

Common Cosmos configuration:

```text
Cosmos:Endpoint
Cosmos:DatabaseName = IncidentIQ
Cosmos:IncidentsContainerName = Incidents
Cosmos:RunbooksContainerName = Runbooks
Cosmos:ChangeFeedLeasesContainerName = ChangeFeedLeases
```

Worker Service Bus configuration:

```text
ServiceBus:FullyQualifiedNamespace
ServiceBus:AnalyseIncidentQueueName = analyse-incident
ServiceBus:MaxDeliveryCount
```

Worker Azure AI configuration:

```text
AzureAI:Endpoint
AzureAI:DeploymentName = incident-analysis
AzureAI:ModelName = gpt-5-mini
```

When connection strings/API keys are not configured, the application uses `DefaultAzureCredential` and the local/Azure identity must have the required RBAC permissions.

## Retrieve Azure Configuration Values

### Cosmos Endpoint

```powershell
az cosmosdb show `
    --name "cosmos-incidentiq-dev-sw6lfgr7whyxm" `
    --resource-group "rg-incidentiq-dev" `
    --query documentEndpoint `
    --output tsv
```

### Cosmos Key

```powershell
az cosmosdb keys list `
    --name "cosmos-incidentiq-dev-sw6lfgr7whyxm" `
    --resource-group "rg-incidentiq-dev" `
    --type keys `
    --query primaryMasterKey `
    --output tsv
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

Example Worker values:

```powershell
dotnet user-secrets set "Cosmos:Key" "<COSMOS_KEY>" `
    --project src\IncidentIQ.Worker

dotnet user-secrets set "AzureAI:Endpoint" "<AZURE_AI_ENDPOINT>" `
    --project src\IncidentIQ.Worker

dotnet user-secrets set "AzureAI:DeploymentName" "incident-analysis" `
    --project src\IncidentIQ.Worker

dotnet user-secrets set "AzureAI:ModelName" "gpt-5-mini" `
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

The current environment includes:

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
│       └── ChangeFeedLeases       /id
├── Azure Service Bus
│   └── analyse-incident
│       └── $DeadLetterQueue
├── Azure OpenAI
│   └── incident-analysis deployment
├── API Managed Identity
├── Worker Managed Identity
├── Application Insights
└── Log Analytics
```

### 6. Refresh Local Configuration

Retrieve the recreated resource values using the commands above, update user-secrets where necessary, then run the API and Worker locally if required.

For runtime startup instructions, return to the [Development Guide](DEVELOPMENT.md).
