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

The bootstrap resource group contains the GitHub deployment identity and OIDC federation, so keeping it means the GitHub environment values do **not** need to change.

### Recreate the Dev Environment

1. Re-run the bootstrap Bicep so `rg-incidentiq-dev` and its GitHub RBAC assignment are recreated:

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

This recreates the Azure resources in `rg-incidentiq-dev`.

---

## Local `secrets.json` After Redeploy

The local API uses user-secrets when running directly against Azure.

After recreating `rg-incidentiq-dev`, update:

```json
{
  "Cosmos:Key": "<new Cosmos primary key>",
  "APPLICATIONINSIGHTS_CONNECTION_STRING": "<new App Insights connection string>"
}
```

These values normally stay unchanged:

```json
{
  "Cosmos:Endpoint": "https://cosmos-incidentiq-dev-sw6lfgr7whyxm.documents.azure.com:443/",
  "Cosmos:DatabaseName": "IncidentIQ",
  "Cosmos:IncidentsContainerName": "Incidents",
  "Cosmos:RunbooksContainerName": "Runbooks"
}
```

### Get the Cosmos Key

```powershell
az cosmosdb keys list `
    --name "cosmos-incidentiq-dev-sw6lfgr7whyxm" `
    --resource-group "rg-incidentiq-dev" `
    --type keys `
    --query primaryMasterKey `
    --output tsv
```

### Get the Application Insights Connection String

```powershell
az monitor app-insights component show `
    --app "appi-incidentiq-dev" `
    --resource-group "rg-incidentiq-dev" `
    --query connectionString `
    --output tsv
```

Set them with:

```powershell
cd src\IncidentIQ.Api

dotnet user-secrets set "Cosmos:Key" "<COSMOS_KEY>"
dotnet user-secrets set "APPLICATIONINSIGHTS_CONNECTION_STRING" "<APP_INSIGHTS_CONNECTION_STRING>"

cd ..\..
```

---

# Full Deployment From Scratch

Use this only when the bootstrap infrastructure does not exist.

## 1. Register Azure Resource Providers

Azure resource providers are registered at the **subscription level** and normally only need to be registered once.

They do **not** need to be registered again when `rg-incidentiq-dev` is deleted and recreated, instead they are **normally required only once per subscription**:

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

To verify their registration status:

```powershell
az provider show --namespace Microsoft.DocumentDB --query registrationState --output tsv
az provider show --namespace Microsoft.OperationalInsights --query registrationState --output tsv
az provider show --namespace Microsoft.Insights --query registrationState --output tsv
az provider show --namespace Microsoft.ServiceBus --query registrationState --output tsv
```

Each should return:

`Registered`

As additional Azure services are introduced, their required resource providers should be added here.

## 2. Deploy Bootstrap Infrastructure

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
└── GitHub deployment managed identity + OIDC

rg-incidentiq-dev
└── GitHub Contributor role assignment
```

## 3. Configure the GitHub `development` Environment

Set these GitHub environment secrets using the bootstrap deployment outputs:

```text
AZURE_CLIENT_ID       → clientId
AZURE_TENANT_ID       → tenantId
AZURE_SUBSCRIPTION_ID → subscriptionId
```

## 4. Deploy the Development Infrastructure

Run the **Deploy Development Infrastructure** GitHub Actions workflow.

This deploys the resources defined by:

```text
infra/main.bicep
└── infra/environments/dev.bicepparam
```

## 5. Refresh Local User-Secrets

Retrieve the new Cosmos key and Application Insights connection string using the commands above, then update the API user-secrets.

After this, running:

```powershell
dotnet run --project src\IncidentIQ.Api
```

uses Azure Cosmos DB, while Docker Compose continues to use the local Cosmos Emulator.
