// Azure Service Bus namespace, AnalyseIncident queue and Worker messaging RBAC.
targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param tags object
param workerPrincipalId string

param analyseIncidentQueueName string = 'analyse-incident'
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

    // Keep SAS available while Managed Identity/RBAC is being verified end-to-end.
    // This can be disabled during the later security-hardening stage.
    disableLocalAuth: false

    zoneRedundant: false
  }
}

resource analyseIncidentQueue 'Microsoft.ServiceBus/namespaces/queues@2026-01-01' = {
  parent: serviceBusNamespace
  name: analyseIncidentQueueName

  properties: {
    // PeekLock initially owns a delivery for one minute. The Worker SDK can renew
    // the lock while a longer-running analysis is still being processed.
    lockDuration: 'PT1M'

    // Service Bus moves a message to the DLQ after the configured delivery limit.
    // The same value is also passed to the Worker application configuration.
    maxDeliveryCount: maxDeliveryCount

    // Analysis commands should not remain actionable indefinitely.
    defaultMessageTimeToLive: 'P1D'
    deadLetteringOnMessageExpiration: true

    // Suppress repeated MessageIds published within the duplicate-detection window.
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'

    requiresSession: false
    enableBatchedOperations: true
    enablePartitioning: false
    status: 'Active'
  }
}

// Built-in Azure Service Bus Data Sender role. Required by IncidentOutboxWorker.
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

// Built-in Azure Service Bus Data Receiver role. Required by AnalyseIncidentWorker.
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
