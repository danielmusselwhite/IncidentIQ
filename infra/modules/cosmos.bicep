// Cosmos DB account, application containers and workload data-plane RBAC.
targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param tags object

param databaseName string = 'IncidentIQ'
param incidentsContainerName string = 'Incidents'
param runbooksContainerName string = 'Runbooks'
param changeFeedLeasesContainerName string = 'ChangeFeedLeases'

param apiPrincipalId string
param workerPrincipalId string

var cosmosAccountName = 'cosmos-${projectName}-${environmentName}-${uniqueString(resourceGroup().id)}'

// Built-in Cosmos DB Data Contributor role. The API persists application data;
// the Worker reads the Change Feed/leases and reads/updates Incident state.
var cosmosDataContributorRoleId = '00000000-0000-0000-0000-000000000002'
var cosmosDataContributorRoleDefinitionId = '${cosmosAccount.id}/sqlRoleDefinitions/${cosmosDataContributorRoleId}'

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2026-03-15' = {
  name: cosmosAccountName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'

  properties: {
    databaseAccountOfferType: 'Standard'

    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }

    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]

    // Serverless keeps the development environment usage-based rather than
    // provisioning dedicated throughput while the project is lightly used.
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]

    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    publicNetworkAccess: 'Enabled'
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2026-03-15' = {
  parent: cosmosAccount
  name: databaseName

  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource incidentsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2026-03-15' = {
  parent: database
  name: incidentsContainerName

  properties: {
    resource: {
      id: incidentsContainerName

      // Incident and outbox documents share incidentId so they can be written
      // together in a single Cosmos transactional batch.
      partitionKey: {
        paths: [
          '/incidentId'
        ]
        kind: 'Hash'
        version: 2
      }

      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: []
      }
    }
  }
}

resource runbooksContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2026-03-15' = {
  parent: database
  name: runbooksContainerName

  properties: {
    resource: {
      id: runbooksContainerName

      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
        version: 2
      }

      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: []
      }
    }
  }
}

// SDK-managed checkpoint/ownership state used by the Cosmos Change Feed Processor.
resource changeFeedLeasesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2026-03-15' = {
  parent: database
  name: changeFeedLeasesContainerName

  properties: {
    resource: {
      id: changeFeedLeasesContainerName

      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
        version: 2
      }
    }
  }
}

resource apiCosmosRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2026-03-15' = {
  parent: cosmosAccount
  name: guid(cosmosAccount.id, apiPrincipalId, cosmosDataContributorRoleId)

  properties: {
    principalId: apiPrincipalId
    roleDefinitionId: cosmosDataContributorRoleDefinitionId
    scope: cosmosAccount.id
  }
}

resource workerCosmosRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2026-03-15' = {
  parent: cosmosAccount
  name: guid(cosmosAccount.id, workerPrincipalId, cosmosDataContributorRoleId)

  properties: {
    principalId: workerPrincipalId
    roleDefinitionId: cosmosDataContributorRoleDefinitionId
    scope: cosmosAccount.id
  }
}

output accountName string = cosmosAccount.name
output endpoint string = cosmosAccount.properties.documentEndpoint
output databaseName string = database.name
output incidentsContainerName string = incidentsContainer.name
output runbooksContainerName string = runbooksContainer.name
output changeFeedLeasesContainerName string = changeFeedLeasesContainer.name
