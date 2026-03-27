@description('Azure region for Key Vault')
param location string

@description('Key Vault name — must be globally unique, 3-24 alphanumeric and hyphen chars')
@minLength(3)
@maxLength(24)
param keyVaultName string

@description('PostgreSQL connection string to store as a secret')
@secure()
param databaseConnectionString string

@description('JWT secret key (min 32 characters)')
@secure()
param jwtSecretKey string

@description('Storage account name — used to retrieve keys for the connection string')
param storageAccountName string

@description('Principal ID of the App Service managed identity that needs read access')
param appServicePrincipalId string

// ---------------------------------------------------------------------------
// Reference the existing storage account to build connection string
// ---------------------------------------------------------------------------

resource existingStorageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

// ---------------------------------------------------------------------------
// Key Vault
// ---------------------------------------------------------------------------

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true        // Use Azure RBAC instead of vault access policies
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true   // Allow Bicep to write secrets on deployment
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: false         // Set to true for production once stable
    networkAcls: {
      defaultAction: 'Allow'             // Restrict to VNet/IPs for production hardening
      bypass: 'AzureServices'
    }
  }
}

// ---------------------------------------------------------------------------
// RBAC: App Service managed identity → Key Vault Secrets User
// Can read secret values but cannot manage secrets
// ---------------------------------------------------------------------------

resource appServiceSecretUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, appServicePrincipalId, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    // Key Vault Secrets User built-in role
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4633458b-17de-408a-b874-0445c86b69e6'
    )
    principalId: appServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ---------------------------------------------------------------------------
// Secrets
// ---------------------------------------------------------------------------

resource dbConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'DatabaseConnectionString'
  properties: {
    value: databaseConnectionString
    attributes: {
      enabled: true
    }
  }
}

resource jwtSecretKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'JwtSecretKey'
  properties: {
    value: jwtSecretKey
    attributes: {
      enabled: true
    }
  }
}

resource storageConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'AzureStorageConnectionString'
  properties: {
    // listKeys() retrieves the storage key at deploy time — value is stored securely in Key Vault
    value: 'DefaultEndpointsProtocol=https;AccountName=${existingStorageAccount.name};AccountKey=${existingStorageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
    attributes: {
      enabled: true
    }
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

output keyVaultUri string = keyVault.properties.vaultUri
output keyVaultName string = keyVault.name
output keyVaultId string = keyVault.id
