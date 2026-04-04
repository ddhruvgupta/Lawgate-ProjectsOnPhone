@description('Static Web App name')
param staticWebAppName string

@description('Static Web App metadata location — limited to specific regions')
@allowed([
  'centralus'
  'eastus2'
  'eastasia'
  'westeurope'
  'westus2'
])
param location string = 'centralus'

// ---------------------------------------------------------------------------
// Azure Static Web Apps — Free tier (CI/CD via GitHub Actions)
// Upgrade to Standard tier for custom auth, private endpoints, etc.
// ---------------------------------------------------------------------------

resource staticWebApp 'Microsoft.Web/staticSites@2024-04-01' = {
  name: staticWebAppName
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    enterpriseGradeCdnStatus: 'Disabled'
    // Note: source/branch/buildProperties are left empty because deployment
    // is handled by the GitHub Actions workflow using the deployment token below.
    // This avoids needing a GitHub PAT during Bicep deployment.
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

output defaultHostname string = staticWebApp.properties.defaultHostname
output staticWebAppName string = staticWebApp.name

@description('Deployment token for GitHub Actions — store as AZURE_STATIC_WEB_APPS_API_TOKEN secret')
output deploymentToken string = staticWebApp.listSecrets().properties.apiKey
