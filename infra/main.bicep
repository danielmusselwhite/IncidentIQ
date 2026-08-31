// Deploy resources into the current resource group.
targetScope = 'resourceGroup'

// Common deployment parameters.
param location string = resourceGroup().location
param projectName string = 'incidentiq'
param environmentName string

// Common tags applied to all resources.
param tags object = {
  project: 'IncidentIQ'
  environment: environmentName
  managedBy: 'Bicep'
}

// Managed identity used by the API.
module apiIdentity './modules/api-identity.bicep' = {
  name: 'apiIdentity'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
  }
}

// Log Analytics workspace used for application logging and monitoring.
module logAnalytics './modules/log-analytics.bicep' = {
  name: 'logAnalytics'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
  }
}

// Application Insights is connected to the Log Analytics workspace.
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

// Managed identity used by the background worker.
module workerIdentity './modules/worker-identity.bicep' = {
  name: 'workerIdentity'

  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
  }
}

// Service Bus resources.
// Both the API and worker identities are passed in so the module can
// configure the appropriate permissions.
module serviceBus './modules/service-bus.bicep' = {
  name: 'serviceBus'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    workerPrincipalId: workerIdentity.outputs.principalId
    tags: tags
  }
}



// Cosmos DB resources.
// The API identity is passed in so the module can configure the required access.
module cosmos './modules/cosmos.bicep' = {
  name: 'cosmos'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
    apiPrincipalId: apiIdentity.outputs.principalId
    workerPrincipalId: workerIdentity.outputs.principalId
  }
}

// Resource names and connection details exposed to the deployment pipeline
// or other infrastructure that consumes these outputs.
output cosmosAccountName string = cosmos.outputs.accountName
output cosmosEndpoint string = cosmos.outputs.endpoint
output cosmosDatabaseName string = cosmos.outputs.databaseName
output cosmosIncidentsContainerName string = cosmos.outputs.incidentsContainerName
output logAnalyticsWorkspaceName string = logAnalytics.outputs.name
output applicationInsightsName string = applicationInsights.outputs.name
output apiIdentityName string = apiIdentity.outputs.name
output apiIdentityClientId string = apiIdentity.outputs.clientId
output cosmosRunbooksContainerName string = cosmos.outputs.runbooksContainerName
output cosmosChangeFeedLeasesContainerName string = cosmos.outputs.changeFeedLeasesContainerName
output serviceBusNamespaceName string = serviceBus.outputs.namespaceName
output serviceBusFullyQualifiedNamespace string = serviceBus.outputs.fullyQualifiedNamespace
output analyseIncidentQueueName string = serviceBus.outputs.analyseIncidentQueueName
output workerIdentityName string = workerIdentity.outputs.name
output workerIdentityClientId string = workerIdentity.outputs.clientId
