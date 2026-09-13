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
param cosmosHistoricalIncidentVectorsContainerName string
param cosmosRunbooksContainerName string
param cosmosRunbookChunksContainerName string
param cosmosChangeFeedLeasesContainerName string

param serviceBusFullyQualifiedNamespace string
param analyseIncidentQueueName string
param indexRunbookQueueName string
param indexHistoricalIncidentQueueName string
param maxDeliveryCount int = 5

param applicationInsightsConnectionString string

// Incident analysis Azure OpenAI configuration.
param azureAiEndpoint string
param azureAiDeploymentName string
param azureAiModelName string

// Runbook embedding Azure OpenAI configuration.
param azureAiEmbeddingDeploymentName string
param azureAiEmbeddingModelName string
param azureAiEmbeddingDimensions int = 1536

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

    // No ingress is configured because the Worker only performs background work.
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

            // Cosmos DB
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
              name: 'Cosmos__HistoricalIncidentVectorsContainerName'
              value: cosmosHistoricalIncidentVectorsContainerName
            }
            {
              name: 'Cosmos__RunbooksContainerName'
              value: cosmosRunbooksContainerName
            }
            {
              name: 'Cosmos__RunbookChunksContainerName'
              value: cosmosRunbookChunksContainerName
            }
            {
              name: 'Cosmos__ChangeFeedLeasesContainerName'
              value: cosmosChangeFeedLeasesContainerName
            }

            // Service Bus
            {
              name: 'ServiceBus__FullyQualifiedNamespace'
              value: serviceBusFullyQualifiedNamespace
            }
            {
              name: 'ServiceBus__AnalyseIncidentQueueName'
              value: analyseIncidentQueueName
            }
            {
              name: 'ServiceBus__IndexRunbookQueueName'
              value: indexRunbookQueueName
            }
            {
              name: 'ServiceBus__IndexHistoricalIncidentQueueName'
              value: indexHistoricalIncidentQueueName
            }
            {
              name: 'ServiceBus__MaxDeliveryCount'
              value: string(maxDeliveryCount)
            }

            // Application Insights
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsightsConnectionString
            }

            // Azure OpenAI account + incident analysis deployment
            {
              name: 'AzureAI__Endpoint'
              value: azureAiEndpoint
            }
            {
              name: 'AzureAI__DeploymentName'
              value: azureAiDeploymentName
            }
            {
              name: 'AzureAI__ModelName'
              value: azureAiModelName
            }

            // Azure OpenAI Runbook embedding deployment.
            //
            // Double underscores map to nested .NET configuration:
            // AzureAI:Embedding:DeploymentName, etc.
            {
              name: 'AzureAI__Embedding__DeploymentName'
              value: azureAiEmbeddingDeploymentName
            }
            {
              name: 'AzureAI__Embedding__ModelName'
              value: azureAiEmbeddingModelName
            }
            {
              name: 'AzureAI__Embedding__Dimensions'
              value: string(azureAiEmbeddingDimensions)
            }
          ]

          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]

      // Keep exactly one Worker running for now because the Change Feed Processor
      // must continuously monitor Cosmos. KEDA scaling remains a later stage.
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

output id string = workerContainerApp.id
output name string = workerContainerApp.name
