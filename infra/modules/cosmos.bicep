// Cosmos DB account, application containers and workload data-plane RBAC.
targetScope = 'resourceGroup'

param location string
param projectName string
param environmentName string
param tags object

param databaseName string = 'IncidentIQ'
param incidentsContainerName string = 'Incidents'
param historicalIncidentVectorsContainerName string = 'HistoricalIncidentVectors'
param runbooksContainerName string = 'Runbooks'
param runbookChunksContainerName string = 'RunbookChunks'
param changeFeedLeasesContainerName string = 'ChangeFeedLeases'

@description('Number of dimensions stored in each semantic-search embedding vectors.')
param embeddingDimensions  int = 1536

param apiPrincipalId string
param workerPrincipalId string

var cosmosAccountName = 'cosmos-${projectName}-${environmentName}-${uniqueString(resourceGroup().id)}'

// Built-in Cosmos DB Data Contributor role. The API persists application data;
// the Worker reads Change Feeds, indexes Runbooks and processes Incidents.
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
    //
    // NoSQL vector search must be enabled at account level before vector-enabled
    // containers can be created.
    capabilities: [
      {
        name: 'EnableServerless'
      }
      {
        name: 'EnableNoSQLVectorSearch'
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

// Derived vector-search representation of completed historical Incidents.
//
// Each completed Incident is represented by one embedding document, allowing
// new Incidents to retrieve semantically similar historical Incidents.
resource historicalIncidentVectorsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2026-03-15' = {
  parent: database
  name: historicalIncidentVectorsContainerName

  properties: {
    resource: {
      id: historicalIncidentVectorsContainerName

      // One vector document exists per Incident.
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

        // Incident metadata remains normally indexed so vector similarity can
        // later be combined with service/environment/severity filters.
        includedPaths: [
          {
            path: '/*'
          }
        ]

        // Embeddings use the specialised vector index instead of the ordinary
        // Cosmos index.
        excludedPaths: [
          {
            path: '/embedding/*'
          }
        ]

        vectorIndexes: [
          {
            path: '/embedding'
            type: 'quantizedFlat'
          }
        ]
      }

      vectorEmbeddingPolicy: {
        vectorEmbeddings: [
          {
            path: '/embedding'
            dataType: 'float32'
            dimensions: embeddingDimensions 
            distanceFunction: 'cosine'
          }
        ]
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

      // Editable Runbooks remain the source of truth.
      // Derived vectorised chunks are stored separately in RunbookChunks.
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

// Derived vector-search representation of Runbooks.
//
// One Runbook is split into multiple chunks and each chunk receives an embedding.
// All chunks for the same Runbook share /runbookId, making re-indexing and cleanup
// operate against a single logical partition.
resource runbookChunksContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2026-03-15' = {
  parent: database
  name: runbookChunksContainerName

  properties: {
    resource: {
      id: runbookChunksContainerName

      partitionKey: {
        paths: [
          '/runbookId'
        ]
        kind: 'Hash'
        version: 2
      }

      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true

        // Metadata such as service, title and update time remains normally indexed,
        // allowing future vector queries to combine similarity with metadata filters.
        includedPaths: [
          {
            path: '/*'
          }
        ]

        // The embedding has its own specialised vector index, so exclude it from
        // the ordinary Cosmos index to avoid unnecessary write RU and latency.
        excludedPaths: [
          {
            path: '/embedding/*'
          }
        ]

        vectorIndexes: [
          {
            path: '/embedding'
            type: 'quantizedFlat'
          }
        ]
      }

      vectorEmbeddingPolicy: {
        vectorEmbeddings: [
          {
            path: '/embedding'
            dataType: 'float32'
            dimensions: embeddingDimensions 
            distanceFunction: 'cosine'
          }
        ]
      }
    }
  }
}

// SDK-managed checkpoint/ownership state used by Cosmos Change Feed Processors.
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
output historicalIncidentVectorsContainerName string = historicalIncidentVectorsContainer.name
output runbooksContainerName string = runbooksContainer.name
output runbookChunksContainerName string = runbookChunksContainer.name
output changeFeedLeasesContainerName string = changeFeedLeasesContainer.name
