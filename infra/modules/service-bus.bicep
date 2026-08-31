// Service Bus resources.
// The Worker identity is granted permissions to send analysis commands
// and receive them for processing.
targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param tags object
param workerPrincipalId string

param analyseIncidentQueueName string = 'analyse-incident'

var namespaceName = 'sb-${projectName}-${environmentName}-${uniqueString(resourceGroup().id)}'

// Create the Service Bus namespace
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

    // Keep SAS available temporarily while we introduce and verify
    // Managed Identity/RBAC. We can disable local auth afterwards.
    disableLocalAuth: false

    zoneRedundant: false
  }
}

// Within the namespace, create the queue for incident analysis commands
resource analyseIncidentQueue 'Microsoft.ServiceBus/namespaces/queues@2026-01-01' = {
  parent: serviceBusNamespace
  name: analyseIncidentQueueName

  properties: {
    // A Worker owns a message for up to one minute while processing it.
    lockDuration: 'PT1M'

    // After five unsuccessful deliveries, Service Bus automatically
    // moves the message into this queue's dead-letter subqueue.
    maxDeliveryCount: 5

    // Analysis commands should not remain actionable indefinitely.
    defaultMessageTimeToLive: 'P1D'
    deadLetteringOnMessageExpiration: true

    // Helps prevent duplicate commands when the same MessageId is
    // accidentally sent again within this window.
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'

    requiresSession: false
    enableBatchedOperations: true
    enablePartitioning: false
    status: 'Active'
  }
}

// Assign the Worker identity the Built-In Service Bus Data Sender role for this queue, so it can send messages to it.
var serviceBusDataSenderRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
)
resource workerSenderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(analyseIncidentQueue.id, workerPrincipalId, serviceBusDataSenderRoleDefinitionId)

  scope: analyseIncidentQueue

  properties: {
    principalId: workerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: serviceBusDataSenderRoleDefinitionId
  }
}

// Assign the Worker identity the Built-In Service Bus Data Receiver role for this queue, so it can receive messages from it.
var serviceBusDataReceiverRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
)
resource workerReceiverRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(analyseIncidentQueue.id, workerPrincipalId, serviceBusDataReceiverRoleDefinitionId)

  scope: analyseIncidentQueue

  properties: {
    principalId: workerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: serviceBusDataReceiverRoleDefinitionId
  }
}

output namespaceName string = serviceBusNamespace.name
output fullyQualifiedNamespace string = '${serviceBusNamespace.name}.servicebus.windows.net'
output analyseIncidentQueueName string = analyseIncidentQueue.name
