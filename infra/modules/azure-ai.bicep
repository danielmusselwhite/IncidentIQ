targetScope = 'resourceGroup'

@description('Azure region in which the Azure OpenAI resource is created.')
param location string

@description('Project name used in resource naming.')
param projectName string

@description('Environment name used in resource naming.')
param environmentName string

@description('Tags applied to the Azure OpenAI resource.')
param tags object = {}

@description('Principal ID of the Worker managed identity that will call Azure OpenAI.')
param workerPrincipalId string

@description('Azure OpenAI model name.')
param modelName string = 'gpt-5-mini'

@description('Azure OpenAI model version.')
param modelVersion string = '2025-08-07'

@description('Name used by the application when calling the Azure OpenAI deployment.')
param deploymentName string = 'incident-analysis'

@description('Azure OpenAI deployment SKU.')
param deploymentSkuName string = 'GlobalStandard'

@description('Deployment capacity. For GlobalStandard, 10 represents 10K TPM.')
param deploymentCapacity int = 10

var accountName = 'oai-${projectName}-${environmentName}-${uniqueString(resourceGroup().id)}'

// Built-in Cognitive Services OpenAI User role.
// Allows the Worker managed identity to invoke deployed Azure OpenAI models.
var cognitiveServicesOpenAIUserRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
)

resource openAiAccount 'Microsoft.CognitiveServices/accounts@2025-12-01' = {
  name: accountName
  location: location
  tags: tags
  kind: 'OpenAI'

  sku: {
    name: 'S0'
  }

  properties: {
    // Managed Identity / Microsoft Entra authentication only.
    disableLocalAuth: true

    // Public access is acceptable for the current development environment.
    // Authentication is still enforced through Microsoft Entra ID/RBAC.
    publicNetworkAccess: 'Enabled'

    // Required for token-based authentication and provides a stable endpoint.
    customSubDomainName: accountName
  }
}

resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-12-01' = {
  name: deploymentName
  parent: openAiAccount

  sku: {
    name: deploymentSkuName
    capacity: deploymentCapacity
  }

  properties: {
    model: {
      format: 'OpenAI'
      name: modelName
      version: modelVersion
    }

    // Allow Azure to move the deployment to a newer default model version when appropriate.
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
  }
}

resource workerOpenAiUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAiAccount.id, workerPrincipalId, cognitiveServicesOpenAIUserRoleDefinitionId)
  scope: openAiAccount

  properties: {
    roleDefinitionId: cognitiveServicesOpenAIUserRoleDefinitionId
    principalId: workerPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output id string = openAiAccount.id
output name string = openAiAccount.name
output endpoint string = openAiAccount.properties.endpoint
output deploymentName string = modelDeployment.name
output modelName string = modelName
output modelVersion string = modelVersion
