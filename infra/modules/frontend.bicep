// Azure Static Web Apps resource used to host the React/Vite frontend.
targetScope = 'resourceGroup'

param location string = 'westeurope'  // Static Web Apps don't have uksouth so we use westeurope as the location
param projectName string
param environmentName string
param tags object

var staticWebAppName = 'swa-${projectName}-${environmentName}'

resource staticWebApp 'Microsoft.Web/staticSites@2025-03-01' = {
  name: staticWebAppName
  location: location
  tags: tags

  sku: {
    name: 'Free'
    tier: 'Free'
  }

  // Source/build configuration is intentionally omitted because GitHub Actions
  // builds the Vite application and uploads the pre-built dist directory.
  properties: {}
}

output id string = staticWebApp.id
output name string = staticWebApp.name
output defaultHostname string = staticWebApp.properties.defaultHostname
output url string = 'https://${staticWebApp.properties.defaultHostname}'
