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

// Note: The deployment token (API key) is intentionally NOT output here.
// Exposing secrets in ARM deployment outputs stores them in Azure deployment history,
// where they are readable by anyone with read access to the resource group.
// Retrieve the token securely after deployment:
//   az staticwebapp secrets list --name <app-name> --query "properties.apiKey" -o tsv
// Then store the value as the AZURE_STATIC_WEB_APPS_API_TOKEN GitHub Actions secret.
