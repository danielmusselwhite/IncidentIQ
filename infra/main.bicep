// Composition root for the disposable IncidentIQ application environment.
targetScope = 'resourceGroup'

// Common deployment parameters.
param location string = resourceGroup().location
param projectName string = 'incidentiq'
param environmentName string

// Container images are overridden by the deployment workflow after the real
// API and Worker images have been pushed to ACR. Public images allow the
// infrastructure to be provisioned for the first time before ACR contains them.
param apiImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
param workerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

// Keep the queue and Worker retry configuration sourced from the same value.
param serviceBusMaxDeliveryCount int = 5

// Common tags applied to application resources.
param tags object = {
  project: 'IncidentIQ'
  environment: environmentName
  managedBy: 'Bicep'
}

// Workload identities used by the API and Worker Container Apps.
module apiIdentity './modules/api-identity.bicep' = {
  name: 'apiIdentity'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
  }
}

module workerIdentity './modules/worker-identity.bicep' = {
  name: 'workerIdentity'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
  }
}

// Shared observability resources.
module logAnalytics './modules/log-analytics.bicep' = {
  name: 'logAnalytics'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
  }
}

module applicationInsights './modules/application-insights.bicep' = {
  name: 'applicationInsights'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    workspaceResourceId: logAnalytics.outputs.id
    tags: tags
  }
}

// Service Bus carries durable AnalyseIncident commands. The Worker owns both
// publication from the outbox relay and consumption for analysis processing.
module serviceBus './modules/service-bus.bicep' = {
  name: 'serviceBus'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    workerPrincipalId: workerIdentity.outputs.principalId
    maxDeliveryCount: serviceBusMaxDeliveryCount
    tags: tags
  }
}

// Cosmos stores Incidents, Runbooks, transactional outbox documents and the
// Change Feed Processor lease state. Both API and Worker require data access.
module cosmos './modules/cosmos.bicep' = {
  name: 'cosmos'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
    apiPrincipalId: apiIdentity.outputs.principalId
    workerPrincipalId: workerIdentity.outputs.principalId
  }
}

// ACR stores the API and Worker container images. Workload identities receive
// pull-only access; the GitHub deployment identity receives push access via the
// bootstrap resource-group RBAC assignment.
module acr './modules/acr.bicep' = {
  name: 'acr'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
    apiPrincipalId: apiIdentity.outputs.principalId
    workerPrincipalId: workerIdentity.outputs.principalId
  }
}

// Shared Container Apps Environment for the API and Worker, connected to the
// existing Log Analytics workspace for platform/application logs.
module containerAppsEnvironment './modules/container-apps-environment.bicep' = {
  name: 'containerAppsEnvironment'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags
    logAnalyticsWorkspaceName: logAnalytics.outputs.name
  }
}

// Static hosting for the React/Vite frontend. The built frontend is uploaded by
// GitHub Actions after Bicep has provisioned the Static Web App resource.
module frontend './modules/frontend.bicep' = {
  name: 'frontend'
  params: {
    location: 'westeurope' // Static Web Apps don't have uksouth so we use westeurope as the location
    projectName: projectName
    environmentName: environmentName
    tags: tags
  }
}

// Public HTTP API. It uses its managed identity for Cosmos and ACR access.
module apiContainerApp './modules/api-container-app.bicep' = {
  name: 'apiContainerApp'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags

    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id

    apiIdentityResourceId: apiIdentity.outputs.id
    apiIdentityClientId: apiIdentity.outputs.clientId

    acrLoginServer: acr.outputs.acrLoginServer
    image: apiImage

    cosmosEndpoint: cosmos.outputs.endpoint
    cosmosDatabaseName: cosmos.outputs.databaseName
    cosmosIncidentsContainerName: cosmos.outputs.incidentsContainerName
    cosmosRunbooksContainerName: cosmos.outputs.runbooksContainerName
    cosmosChangeFeedLeasesContainerName: cosmos.outputs.changeFeedLeasesContainerName

    applicationInsightsConnectionString: applicationInsights.outputs.connectionString

    frontendOrigin: frontend.outputs.url
  }
}

// Background Worker. It has no ingress and hosts both the Change Feed outbox
// relay and Service Bus analysis consumer.
module workerContainerApp './modules/worker-container-app.bicep' = {
  name: 'workerContainerApp'
  params: {
    location: location
    projectName: projectName
    environmentName: environmentName
    tags: tags

    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id

    workerIdentityResourceId: workerIdentity.outputs.id
    workerIdentityClientId: workerIdentity.outputs.clientId

    acrLoginServer: acr.outputs.acrLoginServer
    image: workerImage

    cosmosEndpoint: cosmos.outputs.endpoint
    cosmosDatabaseName: cosmos.outputs.databaseName
    cosmosIncidentsContainerName: cosmos.outputs.incidentsContainerName
    cosmosRunbooksContainerName: cosmos.outputs.runbooksContainerName
    cosmosChangeFeedLeasesContainerName: cosmos.outputs.changeFeedLeasesContainerName

    serviceBusFullyQualifiedNamespace: serviceBus.outputs.fullyQualifiedNamespace
    analyseIncidentQueueName: serviceBus.outputs.analyseIncidentQueueName
    maxDeliveryCount: serviceBusMaxDeliveryCount

    applicationInsightsConnectionString: applicationInsights.outputs.connectionString
  }
}

// Outputs consumed by deployment workflows and operational tooling.
output cosmosAccountName string = cosmos.outputs.accountName
output cosmosEndpoint string = cosmos.outputs.endpoint
output cosmosDatabaseName string = cosmos.outputs.databaseName
output cosmosIncidentsContainerName string = cosmos.outputs.incidentsContainerName
output cosmosRunbooksContainerName string = cosmos.outputs.runbooksContainerName
output cosmosChangeFeedLeasesContainerName string = cosmos.outputs.changeFeedLeasesContainerName

output logAnalyticsWorkspaceName string = logAnalytics.outputs.name
output applicationInsightsName string = applicationInsights.outputs.name

output apiIdentityName string = apiIdentity.outputs.name
output apiIdentityClientId string = apiIdentity.outputs.clientId
output workerIdentityName string = workerIdentity.outputs.name
output workerIdentityClientId string = workerIdentity.outputs.clientId

output serviceBusNamespaceName string = serviceBus.outputs.namespaceName
output serviceBusFullyQualifiedNamespace string = serviceBus.outputs.fullyQualifiedNamespace
output analyseIncidentQueueName string = serviceBus.outputs.analyseIncidentQueueName

output acrId string = acr.outputs.acrId
output acrName string = acr.outputs.acrName
output acrLoginServer string = acr.outputs.acrLoginServer

output containerAppsEnvironmentId string = containerAppsEnvironment.outputs.id
output containerAppsEnvironmentName string = containerAppsEnvironment.outputs.name
output containerAppsEnvironmentDefaultDomain string = containerAppsEnvironment.outputs.defaultDomain

output apiContainerAppName string = apiContainerApp.outputs.name
output apiUrl string = apiContainerApp.outputs.url
output workerContainerAppName string = workerContainerApp.outputs.name

output frontendName string = frontend.outputs.name
output frontendUrl string = frontend.outputs.url
