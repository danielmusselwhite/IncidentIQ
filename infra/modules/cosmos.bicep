targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param tags object

param databaseName string = 'IncidentIQ'
param incidentsContainerName string = 'Incidents'

param apiPrincipalId string

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

output accountName string = cosmosAccount.name
output endpoint string = cosmosAccount.properties.documentEndpoint
output databaseName string = database.name
output incidentsContainerName string = incidentsContainer.name
