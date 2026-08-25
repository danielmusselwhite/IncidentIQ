targetScope = 'resourceGroup'

param location string = resourceGroup().location
param projectName string = 'incidentiq'
param environmentName string

param tags object = {
  project: 'IncidentIQ'
  environment: environmentName
  managedBy: 'Bicep'
}

output environmentName string = environmentName
output location string = location
