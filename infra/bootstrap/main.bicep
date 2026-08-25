targetScope = 'subscription'

param location string = 'uksouth'
param projectName string = 'incidentiq'
param environmentName string = 'dev'

param githubOwner string
param githubOwnerId string
param githubRepository string
param githubRepositoryId string
param githubEnvironment string = 'development'

var bootstrapResourceGroupName = 'rg-${projectName}-bootstrap'
var developmentResourceGroupName = 'rg-${projectName}-${environmentName}'

resource bootstrapResourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: bootstrapResourceGroupName
  location: location
  tags: {
    project: 'IncidentIQ'
    purpose: 'Bootstrap'
    managedBy: 'Bicep'
  }
}

resource developmentResourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: developmentResourceGroupName
  location: location
  tags: {
    project: 'IncidentIQ'
    environment: environmentName
    managedBy: 'Bicep'
  }
}

module githubIdentity './github-identity.bicep' = {
  name: 'githubDeploymentIdentity'
  scope: bootstrapResourceGroup
  params: {
    location: location
    identityName: 'id-${projectName}-github-${environmentName}'

    githubOwner: githubOwner
    githubOwnerId: githubOwnerId

    githubRepository: githubRepository
    githubRepositoryId: githubRepositoryId

    githubEnvironment: githubEnvironment
  }
}

module deploymentRole './deployment-role.bicep' = {
  name: 'githubDeploymentRole'
  scope: developmentResourceGroup
  params: {
    principalId: githubIdentity.outputs.principalId
  }
}

output clientId string = githubIdentity.outputs.clientId
output tenantId string = tenant().tenantId
output subscriptionId string = subscription().subscriptionId
output developmentResourceGroupName string = developmentResourceGroup.name
