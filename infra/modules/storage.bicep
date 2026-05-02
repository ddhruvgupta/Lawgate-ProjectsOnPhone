@description('Azure region for the storage account')
param location string

@description('Storage account name — must be globally unique, 3-24 lowercase alphanumeric chars')
@minLength(3)
@maxLength(24)
param storageAccountName string

@description('Blob container name for legal documents')
param containerName string = 'legal-documents'

// ---------------------------------------------------------------------------
// Storage Account
// ---------------------------------------------------------------------------

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true // Shared key used for connection string — can switch to Entra auth later
    networkAcls: {
      defaultAction: 'Allow' // Tighten to Deny + VNet rules for production hardening
      bypass: 'AzureServices'
    }
  }
}

// ---------------------------------------------------------------------------
// Blob Service — soft delete & versioning for document safety
// ---------------------------------------------------------------------------

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 30 // Recover accidentally deleted blobs within 30 days
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    isVersioningEnabled: true // Immutable document versions
  }
}

// ---------------------------------------------------------------------------
// Legal documents container — no public access
// ---------------------------------------------------------------------------

resource legalDocumentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: containerName
  properties: {
    publicAccess: 'None'
    metadata: {
      purpose: 'legal-documents'
    }
  }
}

// ---------------------------------------------------------------------------
// Microsoft Defender for Storage — on-upload malware scanning
// ---------------------------------------------------------------------------

resource defenderForStorage 'Microsoft.Security/defenderForStorageSettings@2022-12-01-preview' = {
  name: 'current'
  scope: storageAccount
  properties: {
    isEnabled: true
    malwareScanning: {
      onUpload: {
        isEnabled: true
        capGBPerMonth: -1 // -1 = no cap; set a positive integer to limit monthly scan GB and control cost
      }
    }
    sensitiveDataDiscovery: {
      isEnabled: true
    }
    overrideSubscriptionLevelSettings: true // Apply these settings regardless of subscription-level Defender policy
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

output storageAccountName string = storageAccount.name
output storageAccountId string = storageAccount.id
