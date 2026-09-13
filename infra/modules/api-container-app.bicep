// Public ASP.NET Core API hosted in Azure Container Apps.
targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param tags object

param containerAppsEnvironmentId string

param apiIdentityResourceId string
param apiIdentityClientId string

param acrLoginServer string
param image string

// Cosmos DB
param cosmosEndpoint string
param cosmosDatabaseName string
param cosmosIncidentsContainerName string
param cosmosHistoricalIncidentVectorsContainerName string
param cosmosRunbooksContainerName string
param cosmosRunbookChunksContainerName string
param cosmosChangeFeedLeasesContainerName string

// Azure OpenAI
param azureAiEndpoint string
param azureAiDeploymentName string
param azureAiModelName string

// Runbook embeddings
param azureAiEmbeddingDeploymentName string
param azureAiEmbeddingModelName string
param azureAiEmbeddingDimensions int

param applicationInsightsConnectionString string

// Origin URL of the frontend application, used for production CORS.
param frontendOrigin string

var containerAppName = 'ca-${projectName}-api-${environmentName}'

resource apiContainerApp 'Microsoft.App/containerApps@2026-01-01' = {
  name: containerAppName
  location: location
  tags: tags

  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${apiIdentityResourceId}': {}
    }
  }

  properties: {
    environmentId: containerAppsEnvironmentId

    configuration: {
      activeRevisionsMode: 'Single'

      // The frontend calls the API over the public HTTPS endpoint. ACA terminates
      // TLS and forwards traffic to the ASP.NET container on port 8080.
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }

      // Pull ACR images with the API's user-assigned managed identity.
      registries: [
        {
          server: acrLoginServer
          identity: apiIdentityResourceId
        }
      ]
    }

    template: {
      containers: [
        {
          name: 'api'
          image: image

          env: [
            // Select the user-assigned identity when DefaultAzureCredential runs
            // inside the Container App.
            {
              name: 'AZURE_CLIENT_ID'
              value: apiIdentityClientId
            }

            {
              name: 'ASPNETCORE_HTTP_PORTS'
              value: '8080'
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

            // Shared Azure OpenAI configuration.
            // DeploymentName and ModelName are also supplied because they are
            // required by AzureAIOptions validation.
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

            // Runbook semantic-search embedding configuration.
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

            // Observability
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsightsConnectionString
            }

            // Frontend
            {
              name: 'Frontend__Origin'
              value: frontendOrigin
            }
          ]

          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]

      // The stateless HTTP API can scale to zero when the dev environment is idle.
      scale: {
        minReplicas: 0
        maxReplicas: 2
      }
    }
  }
}

output id string = apiContainerApp.id
output name string = apiContainerApp.name
output fqdn string = apiContainerApp.properties.configuration.ingress.fqdn
output url string = 'https://${apiContainerApp.properties.configuration.ingress.fqdn}'
