targetScope = 'resourceGroup'

@description('Azure region in which the Azure OpenAI resource is created.')
param location string

@description('Project name used in resource naming.')
param projectName string

@description('Environment name used in resource naming.')
param environmentName string

@description('Tags applied to the Azure OpenAI resource.')
param tags object = {}

@description('Principal ID of the API managed identity that will call Azure OpenAI for semantic Runbook search.')
param apiPrincipalId string

@description('Principal ID of the Worker managed identity that will call Azure OpenAI.')
param workerPrincipalId string

// -----------------------------------------------------------------------------
// Incident analysis model
// -----------------------------------------------------------------------------

@description('Azure OpenAI model used for incident analysis.')
param modelName string = 'gpt-5-mini'

@description('Azure OpenAI incident analysis model version.')
param modelVersion string = '2025-08-07'

@description('Deployment name used by the application for incident analysis.')
param deploymentName string = 'incident-analysis'

@description('Azure OpenAI deployment SKU for incident analysis.')
param deploymentSkuName string = 'GlobalStandard'

@description('Incident analysis deployment capacity. For GlobalStandard, 10 represents 10K TPM.')
param deploymentCapacity int = 10

// -----------------------------------------------------------------------------
// Runbook embedding model
// -----------------------------------------------------------------------------

@description('Azure OpenAI embedding model used to vectorise Runbook chunks.')
param embeddingModelName string = 'text-embedding-3-small'

@description('Version of the Runbook embedding model.')
param embeddingModelVersion string = '1'

@description('Deployment name used when requesting Runbook embeddings.')
param embeddingDeploymentName string = 'runbook-embedding'

@description('Azure OpenAI deployment SKU for the embedding model.')
param embeddingDeploymentSkuName string = 'GlobalStandard'

@description('Runbook embedding deployment capacity.')
param embeddingDeploymentCapacity int = 10

var accountName = 'oai-${projectName}-${environmentName}-${uniqueString(resourceGroup().id)}'

// Built-in Cognitive Services OpenAI User role.
// Assignments are made at account scope so the API and Worker can invoke
// the model deployments required by their workloads.
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

    // Public access is acceptable for the development environment.
    // Authentication is still enforced through Microsoft Entra ID/RBAC.
    publicNetworkAccess: 'Enabled'

    // Required for token-based authentication and provides a stable endpoint.
    customSubDomainName: accountName
  }
}

// Deployment used to generate structured incident analysis.
resource analysisModelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-12-01' = {
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

    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
  }
}

// Embedding deployment used to convert Runbook chunks and semantic-search
// queries into vectors.
resource embeddingModelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-12-01' = {
  name: embeddingDeploymentName
  parent: openAiAccount

  sku: {
    name: embeddingDeploymentSkuName
    capacity: embeddingDeploymentCapacity
  }

  properties: {
    model: {
      format: 'OpenAI'
      name: embeddingModelName
      version: embeddingModelVersion
    }

    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
  }

  dependsOn: [
    // Serialize model deployments because Azure OpenAI can reject concurrent
    // deployment operations against the same account with RequestConflict.
    analysisModelDeployment
  ]
}

// The API requires Azure OpenAI access to generate embeddings for semantic
// Runbook search requests.
resource apiOpenAiUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAiAccount.id, apiPrincipalId, cognitiveServicesOpenAIUserRoleDefinitionId)
  scope: openAiAccount

  properties: {
    roleDefinitionId: cognitiveServicesOpenAIUserRoleDefinitionId
    principalId: apiPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// The Worker requires Azure OpenAI access for incident analysis and Runbook
// indexing embeddings.
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

output analysisDeploymentName string = analysisModelDeployment.name
output analysisModelName string = modelName
output analysisModelVersion string = modelVersion

output embeddingDeploymentName string = embeddingModelDeployment.name
output embeddingModelName string = embeddingModelName
output embeddingModelVersion string = embeddingModelVersion
