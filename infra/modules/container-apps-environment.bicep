// Shared Azure Container Apps Environment for the API and Worker.
targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param tags object
param logAnalyticsWorkspaceName string

var containerAppsEnvironmentName = 'cae-${projectName}-${environmentName}'

// Reference the workspace provisioned by the parent deployment so Container Apps
// can send platform/application logs to the same Log Analytics workspace.
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2026-01-01' = {
  name: containerAppsEnvironmentName
  location: location
  tags: tags

  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

output id string = containerAppsEnvironment.id
output name string = containerAppsEnvironment.name
output defaultDomain string = containerAppsEnvironment.properties.defaultDomain
