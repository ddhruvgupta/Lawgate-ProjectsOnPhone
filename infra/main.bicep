targetScope = 'resourceGroup'

// ===========================================================================
// Parameters
// ===========================================================================

@description('Azure region for most resources (not Static Web App — see staticWebAppLocation)')
param location string = resourceGroup().location

@description('Static Web App metadata location — must be one of the allowed regions')
@allowed(['centralus', 'eastus2', 'eastasia', 'westeurope', 'westus2'])
param staticWebAppLocation string = 'centralus'

@description('Application name prefix — lowercase alphanumeric, 3-10 chars (e.g. "lawgate")')
@minLength(3)
@maxLength(10)
param appName string = 'lawgate'

@description('Deployment environment tag')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'prod'

@description('PostgreSQL administrator password')
@secure()
param postgresAdminPassword string

@description('JWT signing secret key — minimum 32 characters, generated from a CSPRNG')
@secure()
param jwtSecretKey string

@description('Azure External ID external tenant ID — populate after running scripts/setup-external-id.ps1')
param externalIdTenantId string = ''

@description('Azure External ID backend API app client ID — populate after running scripts/setup-external-id.ps1')
param externalIdApiClientId string = ''

// ===========================================================================
// Variables
// ===========================================================================

// uniqueString produces a deterministic 13-char lowercase alphanumeric hash
// scoped to this resource group — re-running always produces the same names
var suffix = uniqueString(resourceGroup().id)
var namePrefix = '${appName}-${environment}'

// Key Vault name pre-computed so app-service.bicep can reference it without
// waiting for the Key Vault module to deploy (avoids a circular dependency)
var keyVaultName = 'lg-kv-${take(suffix, 10)}'
var keyVaultUri = 'https://${keyVaultName}.vault.azure.net/'

// ===========================================================================
// Modules
// ===========================================================================

// --- Monitoring (no dependencies) -------------------------------------------

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    workspaceName: '${namePrefix}-logs'
    appInsightsName: '${namePrefix}-appinsights'
  }
}

// --- Storage (no dependencies) -----------------------------------------------

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    // lg + env(4) + 8 hex chars = 14 chars — within 3-24 Storage naming rules
    storageAccountName: 'lg${environment}${take(suffix, 8)}'
    containerName: 'legal-documents'
  }
}

// --- Database (no dependencies) ----------------------------------------------

module database 'modules/database.bicep' = {
  name: 'database'
  params: {
    location: location
    serverName: '${namePrefix}-db-${take(suffix, 6)}'
    administratorLogin: 'lawgate_admin'
    administratorLoginPassword: postgresAdminPassword
    databaseName: 'lawgate_db'
  }
}

// --- Static Web App (no dependencies) ----------------------------------------

module staticWebApp 'modules/static-web-app.bicep' = {
  name: 'staticWebApp'
  params: {
    staticWebAppName: '${namePrefix}-frontend'
    location: staticWebAppLocation
  }
}

// --- App Service (depends on: monitoring, staticWebApp) ----------------------
// Key Vault URI is pre-computed so it can be passed here without waiting for
// the Key Vault module. RBAC on the vault is set up in the keyvault module.

module appService 'modules/app-service.bicep' = {
  name: 'appService'
  params: {
    location: location
    appServicePlanName: '${namePrefix}-plan'
    webAppName: '${namePrefix}-api-${take(suffix, 6)}'
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    keyVaultUri: keyVaultUri
    corsOrigin: 'https://${staticWebApp.outputs.defaultHostname}'
    externalIdTenantId: externalIdTenantId
    externalIdAudience: externalIdApiClientId
  }
}

// --- Communication Services (no dependencies) --------------------------------

module communication 'modules/communication.bicep' = {
  name: 'communication'
  params: {
    communicationServiceName: '${namePrefix}-acs-${take(suffix, 6)}'
    dataLocation: 'India'
  }
}

// --- Key Vault (depends on: database, storage, appService, communication) ----
// Deployed last so it has the App Service principal ID for RBAC assignment
// and can call listKeys() on the storage account

module keyvault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    keyVaultName: keyVaultName
    databaseConnectionString: 'Host=${database.outputs.serverFqdn};Database=lawgate_db;Username=lawgate_admin;Password=${postgresAdminPassword};SslMode=Require;TrustServerCertificate=false'
    jwtSecretKey: jwtSecretKey
    storageAccountName: storage.outputs.storageAccountName
    appServicePrincipalId: appService.outputs.principalId
    communicationServiceConnectionString: communication.outputs.primaryConnectionString
    acsSenderDomain: communication.outputs.senderDomain
  }
}

// ===========================================================================
// Outputs
// ===========================================================================

@description('Backend API URL')
output apiUrl string = appService.outputs.apiUrl

@description('Frontend Static Web App URL')
output frontendUrl string = 'https://${staticWebApp.outputs.defaultHostname}'

@description('Key Vault name (for referencing secrets post-deployment)')
output keyVaultName string = keyVaultName

@description('PostgreSQL server fully qualified domain name')
output databaseServerFqdn string = database.outputs.serverFqdn

@description('Storage account name')
output storageAccountName string = storage.outputs.storageAccountName

@description('Azure Communication Services resource name')
output communicationServiceName string = communication.outputs.communicationServiceName

@description('App Insights connection string (safe — not a secret)')
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString

@description('Static Web App deployment token — add to GitHub Secrets as AZURE_STATIC_WEB_APPS_API_TOKEN')
output staticWebAppDeploymentToken string = staticWebApp.outputs.deploymentToken
