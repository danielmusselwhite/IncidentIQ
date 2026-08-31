targetScope = 'resourceGroup'

param principalId string

// Control-plane access required to create/update the development resources.
var contributorRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'b24988ac-6180-42a0-ab88-20f7382dd24c'
)

// Allows the deployment identity to create the workload role assignments defined
// by the application Bicep modules.
var rbacAdministratorRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'f58310d9-a9f6-439a-9e8d-f62e7b41a168'
)

// Data-plane ACR permission required by GitHub Actions to push API/Worker images.
var acrPushRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '8311e382-0749-4cb8-b61a-304f252e45ec'
)

resource contributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, principalId, contributorRoleDefinitionId)

  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: contributorRoleDefinitionId
  }
}

resource rbacAdministratorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, principalId, rbacAdministratorRoleDefinitionId)

  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: rbacAdministratorRoleDefinitionId
  }
}

resource acrPushRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, principalId, acrPushRoleDefinitionId)

  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPushRoleDefinitionId
  }
}
