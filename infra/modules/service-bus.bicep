targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param tags object

param analyseIncidentQueueName string = 'analyse-incident'

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

    // Keep SAS available temporarily while we introduce and verify
    // Managed Identity/RBAC. We can disable local auth afterwards.
    disableLocalAuth: false

    zoneRedundant: false
  }
}

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

output namespaceName string = serviceBusNamespace.name
output fullyQualifiedNamespace string = '${serviceBusNamespace.name}.servicebus.windows.net'
output analyseIncidentQueueName string = analyseIncidentQueue.name
