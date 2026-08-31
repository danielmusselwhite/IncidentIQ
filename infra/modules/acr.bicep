// Azure Container Registry and pull permissions for the application workloads.
targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param tags object
param acrSku string = 'Basic'

param apiPrincipalId string
param workerPrincipalId string

// ACR names are globally unique and may contain only alphanumeric characters.
var acrName = 'acr${projectName}${environmentName}${uniqueString(resourceGroup().id)}'

resource acrResource 'Microsoft.ContainerRegistry/registries@2025-04-01' = {
  name: acrName
  location: location
  tags: tags

  sku: {
    name: acrSku
  }

  properties: {
    adminUserEnabled: false
    anonymousPullEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

// Built-in AcrPull role used by both Container Apps to authenticate without
// registry usernames or passwords.
var acrPullRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe172d538d'
)

resource apiAcrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acrResource.id, apiPrincipalId, acrPullRoleDefinitionId)
  scope: acrResource

  properties: {
    principalId: apiPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleDefinitionId
  }
}

resource workerAcrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acrResource.id, workerPrincipalId, acrPullRoleDefinitionId)
  scope: acrResource

  properties: {
    principalId: workerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleDefinitionId
  }
}

output acrId string = acrResource.id
output acrName string = acrResource.name
output acrLoginServer string = acrResource.properties.loginServer
