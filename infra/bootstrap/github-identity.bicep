targetScope = 'resourceGroup'

param location string
param identityName string
param githubRepository string
param githubEnvironment string

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

resource githubFederation 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: identity
  name: 'github-${githubEnvironment}'
  properties: {
    issuer: 'https://token.actions.githubusercontent.com'
    audiences: [
      'api://AzureADTokenExchange'
    ]
    subject: 'repo:${githubRepository}:environment:${githubEnvironment}'
  }
}

output clientId string = identity.properties.clientId
output principalId string = identity.properties.principalId
