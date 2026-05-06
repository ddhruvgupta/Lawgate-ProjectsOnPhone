@description('Azure region for the App Service')
param location string

@description('App Service Plan name')
param appServicePlanName string

@description('Web App name — must be globally unique (becomes <name>.azurewebsites.net)')
param webAppName string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Key Vault URI — used to build @Microsoft.KeyVault() references in app settings')
param keyVaultUri string

@description('CORS allowed origin (Static Web App URL)')
param corsOrigin string

@description('Frontend base URL used in email links (Static Web App URL, e.g. https://<hostname>)')
param frontendBaseUrl string

@description('Azure External ID tenant ID — leave empty until setup-external-id.ps1 is run')
param externalIdTenantId string = ''

@description('Azure External ID API app client ID — leave empty until setup-external-id.ps1 is run')
param externalIdAudience string = ''

@description('Subnet resource ID for regional VNet integration — routes App Service outbound traffic through the VNet to reach PostgreSQL via private endpoint')
param subnetId string = ''

// ---------------------------------------------------------------------------
// App Service Plan — Linux B1 (upgrade to P1v3 for production traffic)
// ---------------------------------------------------------------------------

resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true // Required for Linux plans
  }
}

// ---------------------------------------------------------------------------
// Web App — .NET 10 on Linux with system-assigned managed identity
// ---------------------------------------------------------------------------

resource webApp 'Microsoft.Web/sites@2024-04-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    // Regional VNet integration — routes all outbound traffic through the VNet
    // so the app can reach PostgreSQL via its private endpoint without going over
    // the public internet. Empty string disables it (local dev / first deploy).
    virtualNetworkSubnetId: !empty(subnetId) ? subnetId : null
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      // Explicit startup command prevents Oryx from picking the wrong DLL when
      // the publish output contains multiple .runtimeconfig.json files
      // (e.g. BuildHost-netcore from Roslyn/EF Core tooling).
      appCommandLine: 'dotnet LegalDocSystem.API.dll'
      alwaysOn: true
      ftpsState: 'Disabled'
      http20Enabled: true
      minTlsVersion: '1.2'
      healthCheckPath: '/health'
      // Route ALL outbound traffic through the VNet (not just RFC 1918 addresses)
      vnetRouteAllEnabled: !empty(subnetId)
      appSettings: [
        // Application Insights — safe to store directly (not a secret)
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        // Database connection string via Key Vault reference
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/DatabaseConnectionString/)'
        }
        // JWT settings — secret key via Key Vault, non-secret values inline
        {
          name: 'Jwt__SecretKey'
          value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/JwtSecretKey/)'
        }
        {
          name: 'Jwt__Issuer'
          value: 'LegalDocSystem'
        }
        {
          name: 'Jwt__Audience'
          value: 'LegalDocSystemUsers'
        }
        {
          name: 'Jwt__ExpiryMinutes'
          value: '1440'
        }
        // Blob Storage — connection string via Key Vault reference
        // ASP.NET Core maps the env var ConnectionStrings__BlobStorage to the
        // ConnectionStrings:BlobStorage config key, which is read by
        // GetConnectionString("BlobStorage") in AzureBlobStorageService.
        {
          name: 'ConnectionStrings__BlobStorage'
          value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/AzureStorageConnectionString/)'
        }
        {
          name: 'BlobStorage__ContainerName'
          value: 'legal-documents'
        }
        // Azure Communication Services — connection string and sender domain via Key Vault reference
        {
          name: 'Email__AcsConnectionString'
          value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/AzureCommunicationServicesConnectionString/)'
        }
        {
          name: 'Email__SenderAddress'
          value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/AcsSenderDomain/)'
        }
        // CORS — allow the Static Web App frontend origin
        {
          name: 'Cors__AllowedOrigins__0'
          value: corsOrigin
        }
        // Frontend base URL — used to build links in verification/password-reset emails
        {
          name: 'App__FrontendBaseUrl'
          value: frontendBaseUrl
        }
        // Azure External ID (populated after running setup-external-id.ps1)
        {
          name: 'ExternalId__TenantId'
          value: externalIdTenantId
        }
        {
          name: 'ExternalId__Audience'
          value: externalIdAudience
        }
        // ASP.NET Core
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

output webAppName string = webApp.name
output defaultHostname string = webApp.properties.defaultHostName
output apiUrl string = 'https://${webApp.properties.defaultHostName}'

@description('Principal ID of the system-assigned managed identity — passed to Key Vault module for RBAC')
output principalId string = webApp.identity.principalId
