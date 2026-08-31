using '../main.bicep'

param location = 'uksouth'
param projectName = 'incidentiq'
param environmentName = 'dev'
param serviceBusMaxDeliveryCount = 5

param tags = {
  project: 'IncidentIQ'
  environment: 'dev'
  managedBy: 'Bicep'
}

param azureAiLocation = 'uksouth'
param azureAiModelName = 'gpt-5-mini'
param azureAiModelVersion = '2025-08-07'
param azureAiDeploymentName = 'incident-analysis'
param azureAiDeploymentSkuName = 'GlobalStandard'
param azureAiDeploymentCapacity = 10
