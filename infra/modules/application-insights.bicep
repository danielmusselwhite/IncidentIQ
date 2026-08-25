targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param workspaceResourceId string
param tags object

var applicationInsightsName = 'appi-${projectName}-${environmentName}'

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  tags: tags
  kind: 'web'

  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspaceResourceId
  }
}

output name string = applicationInsights.name
output id string = applicationInsights.id
output connectionString string = applicationInsights.properties.ConnectionString
