using '../main.bicep'

param location = 'uksouth'
param projectName = 'incidentiq'
param environmentName = 'dev'

param tags = {
  project: 'IncidentIQ'
  environment: 'dev'
  managedBy: 'Bicep'
}
