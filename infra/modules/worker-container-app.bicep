// Background .NET Worker hosted in Azure Container Apps.
targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param tags object

param containerAppsEnvironmentId string

param workerIdentityResourceId string
param workerIdentityClientId string

param acrLoginServer string
param image string

param cosmosEndpoint string
param cosmosDatabaseName string
param cosmosIncidentsContainerName string
param cosmosRunbooksContainerName string
param cosmosChangeFeedLeasesContainerName string

param serviceBusFullyQualifiedNamespace string
param analyseIncidentQueueName string
param maxDeliveryCount int = 5

param applicationInsightsConnectionString string

var containerAppName = 'ca-${projectName}-worker-${environmentName}'

resource workerContainerApp 'Microsoft.App/containerApps@2026-01-01' = {
  name: containerAppName
  location: location
  tags: tags

  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${workerIdentityResourceId}': {}
    }
  }

  properties: {
    environmentId: containerAppsEnvironmentId

    configuration: {
      activeRevisionsMode: 'Single'

      // Pull ACR images with the Worker's user-assigned managed identity.
      registries: [
        {
          server: acrLoginServer
          identity: workerIdentityResourceId
        }
      ]
    }

    // No ingress is configured: both hosted services are background consumers.
    template: {
      containers: [
        {
          name: 'worker'
          image: image

          env: [
            // Select the user-assigned identity when DefaultAzureCredential runs
            // inside the Container App.
            {
              name: 'AZURE_CLIENT_ID'
              value: workerIdentityClientId
            }
            {
              name: 'Cosmos__Endpoint'
              value: cosmosEndpoint
            }
            {
              name: 'Cosmos__DatabaseName'
              value: cosmosDatabaseName
            }
            {
              name: 'Cosmos__IncidentsContainerName'
              value: cosmosIncidentsContainerName
            }
            {
              name: 'Cosmos__RunbooksContainerName'
              value: cosmosRunbooksContainerName
            }
            {
              name: 'Cosmos__ChangeFeedLeasesContainerName'
              value: cosmosChangeFeedLeasesContainerName
            }
            {
              name: 'ServiceBus__FullyQualifiedNamespace'
              value: serviceBusFullyQualifiedNamespace
            }
            {
              name: 'ServiceBus__AnalyseIncidentQueueName'
              value: analyseIncidentQueueName
            }
            {
              name: 'ServiceBus__MaxDeliveryCount'
              value: string(maxDeliveryCount)
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsightsConnectionString
            }
          ]

          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]

      // Keep exactly one Worker running for now because IncidentOutboxWorker must
      // continuously run the Cosmos Change Feed Processor. Queue/KEDA scaling is
      // intentionally deferred to the later scaling stage.
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

output id string = workerContainerApp.id
output name string = workerContainerApp.name
