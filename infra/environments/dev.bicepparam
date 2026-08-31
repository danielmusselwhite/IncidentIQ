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
