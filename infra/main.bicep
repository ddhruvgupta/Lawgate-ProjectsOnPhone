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

@description('Existing PostgreSQL flexible server name — set to empty string to create a new one')
param existingDbServerName string = ''

@description('PostgreSQL administrator login username for the existing server')
param dbAdminLogin string = 'lawgate_admin'

@description('Existing VNet name — set to reuse a pre-existing VNet and its subnets instead of creating a new one')
param existingVnetName string = ''

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
// Use environment() to avoid hardcoded cloud URLs (supports sovereign clouds)
// Note: az.environment().suffixes.keyvaultDns already includes the leading dot
// (e.g. '.vault.azure.net'), so we must NOT add another dot before it.
var keyVaultUri = 'https://${keyVaultName}${az.environment().suffixes.keyvaultDns}/'

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

// --- VNet (no dependencies) -------------------------------------------------
// Provisions the VNet first so the postgres-subnet and private DNS zone IDs
// are available before the database module runs.

module vnet 'modules/vnet.bicep' = {
  name: 'vnet'
  params: {
    location: location
    vnetName: '${namePrefix}-vnet'
    existingVnetName: existingVnetName
  }
}

// --- Database (depends on: vnet) --------------------------------------------
// If an existing server name is supplied, reference it; otherwise create a new
// VNet-injected server via the database module.

var useExistingDb = existingDbServerName != ''

resource existingDbServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' existing = if (useExistingDb) {
  name: useExistingDb ? existingDbServerName : 'placeholder'
}

// Ensure the application database exists on the existing server
resource existingAppDb 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview' = if (useExistingDb) {
  parent: existingDbServer
  name: 'lawgate_db'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

var newDbServerName = '${namePrefix}-db-${take(suffix, 6)}'

module database 'modules/database.bicep' = if (!useExistingDb) {
  name: 'database'
  params: {
    location: location
    serverName: newDbServerName
    administratorLogin: dbAdminLogin
    administratorLoginPassword: postgresAdminPassword
    databaseName: 'lawgate_db'
    // VNet injection — server has no public endpoint; all traffic via private IP
    delegatedSubnetResourceId: vnet.outputs.postgresSubnetId
    privateDnsZoneArmResourceId: vnet.outputs.privateDnsZoneId
  }
}

// FQDN computed directly from server name — avoids accessing conditional module output (BCP318)
var dbServerFqdn = useExistingDb
  ? '${existingDbServerName}.postgres.database.azure.com'
  : '${newDbServerName}.postgres.database.azure.com'

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
    // VNet integration — outbound traffic from the App Service routes through
    // this delegated subnet, enabling it to reach the PostgreSQL private endpoint
    subnetId: vnet.outputs.appServiceSubnetId
  }
}

// --- Communication Services (existing — already provisioned in this resource group) -------
// lawgate-prod-acs and lawgate-prod-acs-email were created manually before the IaC was written.
// Referencing them here avoids creating duplicate resources. If you ever need to recreate
// them via IaC, delete these existing references and uncomment the module block below.

resource existingAcs 'Microsoft.Communication/communicationServices@2023-04-01' existing = {
  name: 'lawgate-prod-acs'
}

resource existingEmailService 'Microsoft.Communication/emailServices@2023-04-01' existing = {
  name: 'lawgate-prod-acs-email'
}

resource existingManagedDomain 'Microsoft.Communication/emailServices/domains@2023-04-01' existing = {
  parent: existingEmailService
  name: 'AzureManagedDomain'
}

// Uncomment to recreate ACS from scratch (only if existing resources above are deleted first):
// module communication 'modules/communication.bicep' = {
//   name: 'communication'
//   params: {
//     communicationServiceName: '${namePrefix}-acs-${take(suffix, 6)}'
//     dataLocation: 'India'
//   }
// }

// --- Key Vault (depends on: database, storage, appService, communication) ----
// Deployed last so it has the App Service principal ID for RBAC assignment
// and can call listKeys() on the storage account

module keyvault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    keyVaultName: keyVaultName
    databaseConnectionString: 'Host=${dbServerFqdn};Database=lawgate_db;Username=${dbAdminLogin};Password=${postgresAdminPassword};SslMode=Require;TrustServerCertificate=false'
    jwtSecretKey: jwtSecretKey
    storageAccountName: storage.outputs.storageAccountName
    appServicePrincipalId: appService.outputs.principalId
    communicationServiceConnectionString: existingAcs.listKeys().primaryConnectionString
    acsSenderDomain: existingManagedDomain.properties.mailFromSenderDomain
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
output databaseServerFqdn string = dbServerFqdn

@description('Storage account name')
output storageAccountName string = storage.outputs.storageAccountName

@description('Azure Communication Services resource name')
output communicationServiceName string = existingAcs.name

@description('App Insights connection string (safe — not a secret)')
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString

// staticWebAppDeploymentToken is intentionally NOT output here — see infra/modules/static-web-app.bicep
// for the secure retrieval instructions (az staticwebapp secrets list).
