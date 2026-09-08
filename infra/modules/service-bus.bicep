// Azure Service Bus namespace, application queues and Worker messaging RBAC.
targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param tags object
param workerPrincipalId string

param analyseIncidentQueueName string = 'analyse-incident'
param indexRunbookQueueName string = 'index-runbook'

param maxDeliveryCount int = 5

var namespaceName = 'sb-${projectName}-${environmentName}-${uniqueString(resourceGroup().id)}'

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2026-01-01' = {
  name: namespaceName
  location: location
  tags: tags

  sku: {
    name: 'Standard'
    tier: 'Standard'
  }

  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'

    // Keep SAS available while the local emulator/development configuration
    // still uses connection-string authentication.
    disableLocalAuth: false

    zoneRedundant: false
  }
}

// Durable queue used by the asynchronous Incident analysis workflow.
resource analyseIncidentQueue 'Microsoft.ServiceBus/namespaces/queues@2026-01-01' = {
  parent: serviceBusNamespace
  name: analyseIncidentQueueName

  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: maxDeliveryCount

    defaultMessageTimeToLive: 'P1D'
    deadLetteringOnMessageExpiration: true

    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'

    requiresSession: false
    enableBatchedOperations: true
    enablePartitioning: false
    status: 'Active'
  }
}

// Durable queue used to asynchronously generate and persist vectorised
// representations of Runbooks.
resource indexRunbookQueue 'Microsoft.ServiceBus/namespaces/queues@2026-01-01' = {
  parent: serviceBusNamespace
  name: indexRunbookQueueName

  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: maxDeliveryCount

    defaultMessageTimeToLive: 'P1D'
    deadLetteringOnMessageExpiration: true

    // The publisher uses RunbookId + source update timestamp as MessageId,
    // allowing repeated publication of the same Runbook revision to be suppressed.
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'

    requiresSession: false
    enableBatchedOperations: true
    enablePartitioning: false
    status: 'Active'
  }
}

// Built-in Azure Service Bus Data Sender role.
var serviceBusDataSenderRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
)

// Built-in Azure Service Bus Data Receiver role.
var serviceBusDataReceiverRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
)

// Incident outbox relay publishes AnalyseIncident commands.
resource workerAnalyseIncidentSenderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(analyseIncidentQueue.id, workerPrincipalId, serviceBusDataSenderRoleDefinitionId)

  scope: analyseIncidentQueue

  properties: {
    principalId: workerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: serviceBusDataSenderRoleDefinitionId
  }
}

// AnalyseIncidentWorker consumes AnalyseIncident commands.
resource workerAnalyseIncidentReceiverRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(analyseIncidentQueue.id, workerPrincipalId, serviceBusDataReceiverRoleDefinitionId)

  scope: analyseIncidentQueue

  properties: {
    principalId: workerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: serviceBusDataReceiverRoleDefinitionId
  }
}

// Runbook Change Feed relay publishes IndexRunbook commands.
resource workerIndexRunbookSenderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(indexRunbookQueue.id, workerPrincipalId, serviceBusDataSenderRoleDefinitionId)

  scope: indexRunbookQueue

  properties: {
    principalId: workerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: serviceBusDataSenderRoleDefinitionId
  }
}

// IndexRunbookWorker will consume IndexRunbook commands.
resource workerIndexRunbookReceiverRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(indexRunbookQueue.id, workerPrincipalId, serviceBusDataReceiverRoleDefinitionId)

  scope: indexRunbookQueue

  properties: {
    principalId: workerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: serviceBusDataReceiverRoleDefinitionId
  }
}

output namespaceName string = serviceBusNamespace.name
output fullyQualifiedNamespace string = '${serviceBusNamespace.name}.servicebus.windows.net'
output analyseIncidentQueueName string = analyseIncidentQueue.name
output indexRunbookQueueName string = indexRunbookQueue.name
