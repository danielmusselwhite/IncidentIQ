targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param tags object

var identityName = 'id-${projectName}-api-${environmentName}'

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

output id string = identity.id
output clientId string = identity.properties.clientId
output principalId string = identity.properties.principalId
output name string = identity.name
