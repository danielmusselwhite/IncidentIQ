// ACR resources.

targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param tags object
param acrSku string = 'Basic'

param apiPrincipalId string
param workerPrincipalId string

var acrName = 'acr${projectName}${environmentName}${uniqueString(resourceGroup().id)}'

// Azure Container Registry (ACR) resource definition.
resource acrResource 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: acrName
  location: location
  sku: {
    name: acrSku
  }
  properties: {
    adminUserEnabled: false
  }
  tags: tags
}

// Pull permissions for API and Worker managed identities.
var acrPullRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe172d538d' // built in ACR Pull role https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles/containers#acrpull
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
