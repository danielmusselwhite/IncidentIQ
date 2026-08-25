targetScope = 'resourceGroup'

param location string = resourceGroup().location
param projectName string = 'incidentiq'
param environmentName string

param tags object = {
  project: 'IncidentIQ'
  environment: environmentName
  managedBy: 'Bicep'
}

module cosmos './modules/cosmos.bicep' = {
  name: 'cosmos'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
  }
}

output cosmosAccountName string = cosmos.outputs.accountName
output cosmosEndpoint string = cosmos.outputs.endpoint
output cosmosDatabaseName string = cosmos.outputs.databaseName
output cosmosIncidentsContainerName string = cosmos.outputs.incidentsContainerName
