// Shared Log Analytics workspace for Container Apps and Application Insights.
targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param tags object

var workspaceName = 'log-${projectName}-${environmentName}'

resource workspace 'Microsoft.OperationalInsights/workspaces@2025-07-01' = {
  name: workspaceName
  location: location
  tags: tags

  properties: {
    retentionInDays: 30
  }

  sku: {
    name: 'PerGB2018'
  }
}

output name string = workspace.name
output id string = workspace.id
