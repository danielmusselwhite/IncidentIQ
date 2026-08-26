# IncidentIQ Azure Dev Environment Lifecycle

## Normal Dev Teardown / Redeploy

To stop Azure development costs, delete only:

```text
rg-incidentiq-dev
```

Keep:

```text
rg-incidentiq-bootstrap
```

The bootstrap resource group contains the GitHub deployment identity and OIDC federation, so keeping it means the GitHub `development` environment values do **not** need to change.

Deleting `rg-incidentiq-dev` removes the development resources, including Cosmos DB, Service Bus, monitoring resources, and the API/Worker managed identities.

### Recreate the Dev Environment

1. Re-run the bootstrap Bicep so `rg-incidentiq-dev` and the GitHub deployment identity's resource-group RBAC assignments are recreated:

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

2. Run the **Deploy Development Infrastructure** GitHub Actions workflow.

This recreates the resources defined in `infra/main.bicep`, including Cosmos DB, Service Bus, monitoring resources, managed identities, and their application RBAC assignments.

---

## Local Configuration After Redeploy

IncidentIQ has two local development modes:

```text
Docker Compose
├── Cosmos DB Emulator
└── Service Bus Emulator
```

```text
dotnet run
├── Azure Cosmos DB
└── Azure Service Bus
```

### Docker Compose

Docker Compose uses environment variables and the local `.env` file rather than .NET user-secrets.

Typical `.env` values include:

```env
COSMOS_EMULATOR_KEY=<local Cosmos emulator key>
SERVICEBUS_SQL_PASSWORD=<local Service Bus emulator SQL password>
```

The `.env` file must remain outside source control.

### API User-Secrets

When running `IncidentIQ.Api` directly against Azure, the API requires Azure Cosmos, Service Bus, and Application Insights configuration.

After recreating `rg-incidentiq-dev`, refresh:

```json
{
  "Cosmos:Key": "<new Cosmos primary key>",
  "APPLICATIONINSIGHTS_CONNECTION_STRING": "<new App Insights connection string>"
}
```

The following normally remain unchanged because the resource and container names are deterministic:

```json
{
  "Cosmos:Endpoint": "https://cosmos-incidentiq-dev-sw6lfgr7whyxm.documents.azure.com:443/",
  "Cosmos:DatabaseName": "IncidentIQ",
  "Cosmos:IncidentsContainerName": "Incidents",
  "Cosmos:RunbooksContainerName": "Runbooks",
  "ServiceBus:FullyQualifiedNamespace": "<service-bus-namespace>.servicebus.windows.net",
  "ServiceBus:AnalyseIncidentQueueName": "analyse-incident"
}
```

Service Bus authentication uses `DefaultAzureCredential` when no `ServiceBus:ConnectionString` is configured. The local developer identity therefore needs the required Service Bus RBAC permissions.

If a local Service Bus connection string is being used instead, it should also be refreshed after the Service Bus namespace is recreated:

```json
{
  "ServiceBus:ConnectionString": "<new Service Bus connection string>"
}
```

### Worker User-Secrets

`IncidentIQ.Worker` has its own user-secrets store.

When running the Worker directly against Azure, configure the same Cosmos and Service Bus settings it needs to consume commands and update incidents:

```json
{
  "Cosmos:Endpoint": "https://cosmos-incidentiq-dev-sw6lfgr7whyxm.documents.azure.com:443/",
  "Cosmos:Key": "<new Cosmos primary key>",
  "Cosmos:DatabaseName": "IncidentIQ",
  "Cosmos:IncidentsContainerName": "Incidents",
  "Cosmos:RunbooksContainerName": "Runbooks",
  "ServiceBus:FullyQualifiedNamespace": "<service-bus-namespace>.servicebus.windows.net",
  "ServiceBus:AnalyseIncidentQueueName": "analyse-incident"
}
```

If the Worker uses a local Service Bus connection string instead of `DefaultAzureCredential`, also configure:

```json
{
  "ServiceBus:ConnectionString": "<new Service Bus connection string>"
}
```

---

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

Use the returned value as:

```text
<namespace-name>.servicebus.windows.net
```

### Service Bus Connection String

Only required when local development is using SAS authentication instead of `DefaultAzureCredential`.

```powershell
az servicebus namespace authorization-rule keys list `
    --resource-group "rg-incidentiq-dev" `
    --namespace-name "<SERVICE_BUS_NAMESPACE>" `
    --name "RootManageSharedAccessKey" `
    --query primaryConnectionString `
    --output tsv
```

---

## Update User-Secrets

### API

```powershell
dotnet user-secrets set "Cosmos:Key" "<COSMOS_KEY>" `
    --project src\IncidentIQ.Api

dotnet user-secrets set "APPLICATIONINSIGHTS_CONNECTION_STRING" "<APP_INSIGHTS_CONNECTION_STRING>" `
    --project src\IncidentIQ.Api
```

If required:

```powershell
dotnet user-secrets set "ServiceBus:ConnectionString" "<SERVICE_BUS_CONNECTION_STRING>" `
    --project src\IncidentIQ.Api
```

### Worker

```powershell
dotnet user-secrets set "Cosmos:Key" "<COSMOS_KEY>" `
    --project src\IncidentIQ.Worker
```

If required:

```powershell
dotnet user-secrets set "ServiceBus:ConnectionString" "<SERVICE_BUS_CONNECTION_STRING>" `
    --project src\IncidentIQ.Worker
```

---

# Full Deployment From Scratch

Use this when the IncidentIQ bootstrap infrastructure does not yet exist.

## 1. Login to Azure

```powershell
az login
```

## 2. Register Azure Resource Providers

Azure resource providers are registered at the **subscription level** and normally only need to be registered once.

They do **not** need to be registered again when `rg-incidentiq-dev` is deleted and recreated.

Register the providers currently required by IncidentIQ:

```powershell
az provider register --namespace Microsoft.DocumentDB --wait
az provider register --namespace Microsoft.OperationalInsights --wait
az provider register --namespace Microsoft.Insights --wait
az provider register --namespace Microsoft.ServiceBus --wait
```

These providers are used for:

```text
Microsoft.DocumentDB
└── Azure Cosmos DB

Microsoft.OperationalInsights
└── Log Analytics

Microsoft.Insights
└── Application Insights

Microsoft.ServiceBus
└── Azure Service Bus
```

Verify their registration:

```powershell
az provider show --namespace Microsoft.DocumentDB --query registrationState --output tsv
az provider show --namespace Microsoft.OperationalInsights --query registrationState --output tsv
az provider show --namespace Microsoft.Insights --query registrationState --output tsv
az provider show --namespace Microsoft.ServiceBus --query registrationState --output tsv
```

Each should return:

```text
Registered
```

As additional Azure services are introduced, their required providers should be added here.

## 3. Deploy Bootstrap Infrastructure

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

This creates the bootstrap and development resource groups and configures the GitHub deployment identity:

```text
rg-incidentiq-bootstrap
└── GitHub deployment managed identity
    └── GitHub OIDC federated credential

rg-incidentiq-dev
└── GitHub deployment identity RBAC
    ├── Contributor
    └── Role Based Access Control Administrator
```

The RBAC Administrator role is scoped to `rg-incidentiq-dev` so the deployment workflow can create application role assignments without receiving subscription-wide RBAC permissions.

## 4. Configure the GitHub `development` Environment

Set these GitHub environment secrets using the bootstrap deployment outputs:

```text
AZURE_CLIENT_ID       → clientId
AZURE_TENANT_ID       → tenantId
AZURE_SUBSCRIPTION_ID → subscriptionId
```

No client secret is required because GitHub authenticates to Azure using OIDC.

## 5. Deploy the Development Infrastructure

Run the **Deploy Development Infrastructure** GitHub Actions workflow.

The workflow deploys:

```text
infra/main.bicep
└── infra/environments/dev.bicepparam
```

Current development infrastructure includes:

```text
rg-incidentiq-dev
├── Cosmos DB
│   ├── Incidents
│   └── Runbooks
├── Service Bus
│   └── analyse-incident
│       └── Dead-letter subqueue
├── API Managed Identity
├── Worker Managed Identity
├── Log Analytics
└── Application Insights
```

Service Bus RBAC is configured so:

```text
API Managed Identity
└── Azure Service Bus Data Sender
    └── analyse-incident

Worker Managed Identity
└── Azure Service Bus Data Receiver
    └── analyse-incident
```

## 6. Refresh Local Configuration

Retrieve the Cosmos, Service Bus, and Application Insights values using the commands above and update the API/Worker user-secrets as required.

After this:

```powershell
dotnet run --project src\IncidentIQ.Api
dotnet run --project src\IncidentIQ.Worker
```

can use the Azure development resources.

Docker Compose remains fully local:

```text
Docker Compose
├── IncidentIQ.Api
├── IncidentIQ.Worker
├── Cosmos DB Emulator
└── Service Bus Emulator
```
