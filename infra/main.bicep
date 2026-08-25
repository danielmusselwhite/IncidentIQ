targetScope = 'resourceGroup'

param location string = resourceGroup().location
param projectName string = 'incidentiq'
param environmentName string

param tags object = {
  project: 'IncidentIQ'
  environment: environmentName
  managedBy: 'Bicep'
}

module apiIdentity './modules/api-identity.bicep' = {
  name: 'apiIdentity'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
  }
}

module cosmos './modules/cosmos.bicep' = {
  name: 'cosmos'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
    apiPrincipalId: apiIdentity.outputs.principalId
  }
}

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

output cosmosAccountName string = cosmos.outputs.accountName
output cosmosEndpoint string = cosmos.outputs.endpoint
output cosmosDatabaseName string = cosmos.outputs.databaseName
output cosmosIncidentsContainerName string = cosmos.outputs.incidentsContainerName
output logAnalyticsWorkspaceName string = logAnalytics.outputs.name
output applicationInsightsName string = applicationInsights.outputs.name
output apiIdentityName string = apiIdentity.outputs.name
output apiIdentityClientId string = apiIdentity.outputs.clientId
output cosmosRunbooksContainerName string = cosmos.outputs.runbooksContainerName
