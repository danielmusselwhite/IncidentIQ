// Cosmos DB resources.
// The API and Worker identities are granted the required Cosmos DB access.

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

var cosmosDataContributorRoleId = '00000000-0000-0000-0000-000000000002' // Cosmos built in Data Contributor role definition ID

var cosmosDataContributorRoleDefinitionId = '${cosmosAccount.id}/sqlRoleDefinitions/${cosmosDataContributorRoleId}'

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

output accountName string = cosmosAccount.name
output endpoint string = cosmosAccount.properties.documentEndpoint
output databaseName string = database.name
output incidentsContainerName string = incidentsContainer.name
output runbooksContainerName string = runbooksContainer.name
output changeFeedLeasesContainerName string = changeFeedLeasesContainer.name
