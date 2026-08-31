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

param cosmosEndpoint string
param cosmosDatabaseName string
param cosmosIncidentsContainerName string
param cosmosRunbooksContainerName string
param cosmosChangeFeedLeasesContainerName string

param applicationInsightsConnectionString string

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
