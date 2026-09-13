// Composition root for the disposable IncidentIQ application environment.
targetScope = 'resourceGroup'

// -----------------------------------------------------------------------------
// Common deployment parameters
// -----------------------------------------------------------------------------

param location string = resourceGroup().location
param projectName string = 'incidentiq'
param environmentName string

// Container images are overridden by the deployment workflow after the real
// API and Worker images have been pushed to ACR. Public images allow the
// infrastructure to be provisioned before ACR contains application images.
param apiImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
param workerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

// Keep the Service Bus queue and Worker retry configuration sourced from one value.
param serviceBusMaxDeliveryCount int = 5

param tags object = {
  project: 'IncidentIQ'
  environment: environmentName
  managedBy: 'Bicep'
}

// -----------------------------------------------------------------------------
// Azure AI - incident analysis
// -----------------------------------------------------------------------------

param azureAiLocation string = location

param azureAiModelName string = 'gpt-5-mini'
param azureAiModelVersion string = '2025-08-07'
param azureAiDeploymentName string = 'incident-analysis'
param azureAiDeploymentSkuName string = 'GlobalStandard'
param azureAiDeploymentCapacity int = 10

// -----------------------------------------------------------------------------
// Azure AI - Runbook embeddings
// -----------------------------------------------------------------------------

param azureAiEmbeddingModelName string = 'text-embedding-3-small'
param azureAiEmbeddingModelVersion string = '1'
param azureAiEmbeddingDeploymentName string = 'runbook-embedding'
param azureAiEmbeddingDeploymentSkuName string = 'GlobalStandard'
param azureAiEmbeddingDeploymentCapacity int = 10

// One value is shared with Cosmos and the application workloads so the stored
// vector policy and generated embedding dimensions cannot become inconsistent.
param azureAiEmbeddingDimensions int = 1536

// -----------------------------------------------------------------------------
// Workload identities
// -----------------------------------------------------------------------------

module apiIdentity './modules/api-identity.bicep' = {
  name: 'apiIdentity'

  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
  }
}

module workerIdentity './modules/worker-identity.bicep' = {
  name: 'workerIdentity'

  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
  }
}

// -----------------------------------------------------------------------------
// Observability
// -----------------------------------------------------------------------------

module logAnalytics './modules/log-analytics.bicep' = {
  name: 'logAnalytics'

  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
  }
}

module applicationInsights './modules/application-insights.bicep' = {
  name: 'applicationInsights'

  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    workspaceResourceId: logAnalytics.outputs.id
    tags: tags
  }
}

// -----------------------------------------------------------------------------
// Messaging
// -----------------------------------------------------------------------------

module serviceBus './modules/service-bus.bicep' = {
  name: 'serviceBus'

  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    workerPrincipalId: workerIdentity.outputs.principalId
    maxDeliveryCount: serviceBusMaxDeliveryCount
    tags: tags
  }
}

// -----------------------------------------------------------------------------
// Cosmos DB
// -----------------------------------------------------------------------------

// Cosmos stores the source application data, transactional Incident outbox,
// Runbook vector chunks and Change Feed Processor lease state.
module cosmos './modules/cosmos.bicep' = {
  name: 'cosmos'

  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags

    apiPrincipalId: apiIdentity.outputs.principalId
    workerPrincipalId: workerIdentity.outputs.principalId

    embeddingDimensions : azureAiEmbeddingDimensions
  }
}

// -----------------------------------------------------------------------------
// Container registry
// -----------------------------------------------------------------------------

module acr './modules/acr.bicep' = {
  name: 'acr'

  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags

    apiPrincipalId: apiIdentity.outputs.principalId
    workerPrincipalId: workerIdentity.outputs.principalId
  }
}

// -----------------------------------------------------------------------------
// Container Apps environment
// -----------------------------------------------------------------------------

module containerAppsEnvironment './modules/container-apps-environment.bicep' = {
  name: 'containerAppsEnvironment'

  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags

    logAnalyticsWorkspaceName: logAnalytics.outputs.name
  }
}

// -----------------------------------------------------------------------------
// Frontend
// -----------------------------------------------------------------------------

module frontend './modules/frontend.bicep' = {
  name: 'frontend'

  params: {
    // Static Web Apps does not currently offer UK South.
    location: 'westeurope'

    projectName: projectName
    environmentName: environmentName
    tags: tags
  }
}

// -----------------------------------------------------------------------------
// Azure OpenAI
// -----------------------------------------------------------------------------

// Both model deployments live under one Azure OpenAI account.
// The API uses embeddings for semantic Runbook search, while the Worker uses
// embeddings for indexing and the chat deployment for incident analysis.
module azureAi './modules/azure-ai.bicep' = {
  name: 'azureAi'

  params: {
    location: azureAiLocation
    projectName: projectName
    environmentName: environmentName
    tags: tags

    apiPrincipalId: apiIdentity.outputs.principalId
    workerPrincipalId: workerIdentity.outputs.principalId

    // Incident analysis
    modelName: azureAiModelName
    modelVersion: azureAiModelVersion
    deploymentName: azureAiDeploymentName
    deploymentSkuName: azureAiDeploymentSkuName
    deploymentCapacity: azureAiDeploymentCapacity

    // Runbook embeddings
    embeddingModelName: azureAiEmbeddingModelName
    embeddingModelVersion: azureAiEmbeddingModelVersion
    embeddingDeploymentName: azureAiEmbeddingDeploymentName
    embeddingDeploymentSkuName: azureAiEmbeddingDeploymentSkuName
    embeddingDeploymentCapacity: azureAiEmbeddingDeploymentCapacity
  }
}

// -----------------------------------------------------------------------------
// API Container App
// -----------------------------------------------------------------------------

module apiContainerApp './modules/api-container-app.bicep' = {
  name: 'apiContainerApp'

  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags

    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id

    apiIdentityResourceId: apiIdentity.outputs.id
    apiIdentityClientId: apiIdentity.outputs.clientId

    acrLoginServer: acr.outputs.acrLoginServer
    image: apiImage

    // Cosmos
    cosmosEndpoint: cosmos.outputs.endpoint
    cosmosDatabaseName: cosmos.outputs.databaseName
    cosmosIncidentsContainerName: cosmos.outputs.incidentsContainerName
    cosmosHistoricalIncidentVectorsContainerName: cosmos.outputs.historicalIncidentVectorsContainerName
    cosmosRunbooksContainerName: cosmos.outputs.runbooksContainerName
    cosmosRunbookChunksContainerName: cosmos.outputs.runbookChunksContainerName
    cosmosChangeFeedLeasesContainerName: cosmos.outputs.changeFeedLeasesContainerName

    // Shared Azure OpenAI configuration
    azureAiEndpoint: azureAi.outputs.endpoint
    azureAiDeploymentName: azureAi.outputs.analysisDeploymentName
    azureAiModelName: azureAi.outputs.analysisModelName

    // Runbook embedding AI
    azureAiEmbeddingDeploymentName: azureAi.outputs.embeddingDeploymentName
    azureAiEmbeddingModelName: azureAi.outputs.embeddingModelName
    azureAiEmbeddingDimensions: azureAiEmbeddingDimensions

    // Observability
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString

    frontendOrigin: frontend.outputs.url
  }
}

// -----------------------------------------------------------------------------
// Worker Container App
// -----------------------------------------------------------------------------

module workerContainerApp './modules/worker-container-app.bicep' = {
  name: 'workerContainerApp'

  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags

    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id

    workerIdentityResourceId: workerIdentity.outputs.id
    workerIdentityClientId: workerIdentity.outputs.clientId

    acrLoginServer: acr.outputs.acrLoginServer
    image: workerImage

    // Cosmos
    cosmosEndpoint: cosmos.outputs.endpoint
    cosmosDatabaseName: cosmos.outputs.databaseName
    cosmosIncidentsContainerName: cosmos.outputs.incidentsContainerName
    cosmosHistoricalIncidentVectorsContainerName: cosmos.outputs.historicalIncidentVectorsContainerName
    cosmosRunbooksContainerName: cosmos.outputs.runbooksContainerName
    cosmosRunbookChunksContainerName: cosmos.outputs.runbookChunksContainerName
    cosmosChangeFeedLeasesContainerName: cosmos.outputs.changeFeedLeasesContainerName

    // Service Bus
    serviceBusFullyQualifiedNamespace: serviceBus.outputs.fullyQualifiedNamespace
    analyseIncidentQueueName: serviceBus.outputs.analyseIncidentQueueName
    indexRunbookQueueName: serviceBus.outputs.indexRunbookQueueName
    indexHistoricalIncidentQueueName: serviceBus.outputs.indexHistoricalIncidentQueueName
    maxDeliveryCount: serviceBusMaxDeliveryCount

    // Observability
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString

    // Incident analysis AI
    azureAiEndpoint: azureAi.outputs.endpoint
    azureAiDeploymentName: azureAi.outputs.analysisDeploymentName
    azureAiModelName: azureAi.outputs.analysisModelName

    // Runbook embedding AI
    azureAiEmbeddingDeploymentName: azureAi.outputs.embeddingDeploymentName
    azureAiEmbeddingModelName: azureAi.outputs.embeddingModelName
    azureAiEmbeddingDimensions: azureAiEmbeddingDimensions
  }
}

// -----------------------------------------------------------------------------
// Outputs
// -----------------------------------------------------------------------------

output cosmosAccountName string = cosmos.outputs.accountName
output cosmosEndpoint string = cosmos.outputs.endpoint
output cosmosDatabaseName string = cosmos.outputs.databaseName
output cosmosIncidentsContainerName string = cosmos.outputs.incidentsContainerName
output cosmosHistoricalIncidentVectorsContainerName string = cosmos.outputs.historicalIncidentVectorsContainerName
output cosmosRunbooksContainerName string = cosmos.outputs.runbooksContainerName
output cosmosRunbookChunksContainerName string = cosmos.outputs.runbookChunksContainerName
output cosmosChangeFeedLeasesContainerName string = cosmos.outputs.changeFeedLeasesContainerName

output logAnalyticsWorkspaceName string = logAnalytics.outputs.name
output applicationInsightsName string = applicationInsights.outputs.name

output apiIdentityName string = apiIdentity.outputs.name
output apiIdentityClientId string = apiIdentity.outputs.clientId

output workerIdentityName string = workerIdentity.outputs.name
output workerIdentityClientId string = workerIdentity.outputs.clientId

output serviceBusNamespaceName string = serviceBus.outputs.namespaceName
output serviceBusFullyQualifiedNamespace string = serviceBus.outputs.fullyQualifiedNamespace
output analyseIncidentQueueName string = serviceBus.outputs.analyseIncidentQueueName
output indexRunbookQueueName string = serviceBus.outputs.indexRunbookQueueName
output indexHistoricalIncidentQueueName string = serviceBus.outputs.indexHistoricalIncidentQueueName

output acrId string = acr.outputs.acrId
output acrName string = acr.outputs.acrName
output acrLoginServer string = acr.outputs.acrLoginServer

output containerAppsEnvironmentId string = containerAppsEnvironment.outputs.id
output containerAppsEnvironmentName string = containerAppsEnvironment.outputs.name
output containerAppsEnvironmentDefaultDomain string = containerAppsEnvironment.outputs.defaultDomain

output apiContainerAppName string = apiContainerApp.outputs.name
output apiUrl string = apiContainerApp.outputs.url

output workerContainerAppName string = workerContainerApp.outputs.name

output frontendName string = frontend.outputs.name
output frontendUrl string = frontend.outputs.url

output azureAiAccountName string = azureAi.outputs.name
output azureAiEndpoint string = azureAi.outputs.endpoint
output azureAiAnalysisDeploymentName string = azureAi.outputs.analysisDeploymentName
output azureAiEmbeddingDeploymentName string = azureAi.outputs.embeddingDeploymentName
