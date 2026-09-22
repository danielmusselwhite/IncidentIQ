# Azure Dev Environment Lifecycle

## Cost-Saving Teardown

Delete:

```text
rg-incidentiq-dev
```

Keep:

```text
rg-incidentiq-bootstrap
```

The bootstrap group contains the GitHub deployment identity/OIDC configuration; the dev group is disposable.

## Recreate

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

## First-Time Subscription Setup

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

GitHub `development` environment values:

```text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
```

GitHub authenticates through OIDC; no client secret is required.

## Cosmos Vector Search

Expected account capabilities:

```text
EnableServerless
EnableNoSQLVectorSearch
```

Check:

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

If required:

```powershell
az cosmosdb update `
  --resource-group "rg-incidentiq-dev" `
  --name $accountName `
  --capabilities @("EnableServerless","EnableNoSQLVectorSearch")
```

Wait for propagation before reprovisioning vector-enabled containers.

## Values That May Change After Recreation

```text
Cosmos:Endpoint
Cosmos:Key
AzureAI:Endpoint
APPLICATIONINSIGHTS_CONNECTION_STRING
ServiceBus:FullyQualifiedNamespace
ServiceBus:ConnectionString (SAS only)
```

Deployed API/Worker resources receive configuration through Bicep and use Managed Identity. These values mainly matter for local Azure-connected debugging.

## Useful Lookup Commands

Cosmos endpoint:

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

Cosmos key:

```powershell
az cosmosdb keys list `
  --name $accountName `
  --resource-group "rg-incidentiq-dev" `
  --type keys `
  --query primaryMasterKey `
  --output tsv
```

Azure OpenAI endpoint:

```powershell
az cognitiveservices account list `
  --resource-group "rg-incidentiq-dev" `
  --query "[?kind=='OpenAI'].properties.endpoint | [0]" `
  --output tsv
```

Application Insights:

```powershell
az monitor app-insights component show `
  --app "appi-incidentiq-dev" `
  --resource-group "rg-incidentiq-dev" `
  --query connectionString `
  --output tsv
```

Service Bus namespace:

```powershell
az servicebus namespace list `
  --resource-group "rg-incidentiq-dev" `
  --query "[0].name" `
  --output tsv
```

## Deployment Verification

After deployment verify:

- React sign-in through Microsoft Entra.
- Authenticated React → API calls.
- Incident `Queued → Processing → Completed`.
- Runbook indexing and semantic search.
- Historical Incident indexing/retrieval.
- Grounded analysis with valid `HI-*` / `RB-*`.
- Operational Assistant.
- Application Insights telemetry.

For local configuration, see [Development](DEVELOPMENT.md).
