@description('Azure region for the database server')
param location string

@description('PostgreSQL Flexible Server name — must be globally unique, 3-63 chars')
@minLength(3)
@maxLength(63)
param serverName string

@description('Administrator login username')
param administratorLogin string = 'lawgate_admin'

@description('Administrator password — must meet PostgreSQL complexity requirements')
@secure()
param administratorLoginPassword string

@description('Name of the application database')
param databaseName string = 'lawgate_db'

@description('PostgreSQL version')
@allowed(['14', '15', '16'])
param postgresVersion string = '16'

@description('Compute SKU — Standard_B1ms is sufficient for dev/staging; upgrade for prod load')
param skuName string = 'Standard_B1ms'

@description('Backup retention in days')
@minValue(7)
@maxValue(35)
param backupRetentionDays int = 7

@description('Delegated subnet resource ID — required for VNet injection (private access mode). Leave empty to use public access.')
param delegatedSubnetResourceId string = ''

@description('Private DNS zone resource ID — required when delegatedSubnetResourceId is set')
param privateDnsZoneArmResourceId string = ''

// ---------------------------------------------------------------------------
// PostgreSQL Flexible Server
// ---------------------------------------------------------------------------

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' = {
  name: serverName
  location: location
  sku: {
    name: skuName
    tier: 'Burstable' // Must be Burstable for B-series SKUs; change to GeneralPurpose for production
  }
  properties: {
    version: postgresVersion
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    storage: {
      storageSizeGB: 32
      autoGrow: 'Enabled'
    }
    backup: {
      backupRetentionDays: backupRetentionDays
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled' // Burstable tier does not support HA; enable ZoneRedundant for GeneralPurpose
    }
    authConfig: {
      activeDirectoryAuth: 'Disabled'
      passwordAuth: 'Enabled'
    }
    maintenanceWindow: {
      customWindow: 'Enabled'
      dayOfWeek: 0 // Sunday
      startHour: 2
      startMinute: 0
    }
    // Private access (VNet injection): server lives in the delegated subnet with no public endpoint.
    // If subnet/DNS params are omitted the server falls back to public access — not recommended.
    network: !empty(delegatedSubnetResourceId) ? {
      delegatedSubnetResourceId: delegatedSubnetResourceId
      privateDnsZoneArmResourceId: privateDnsZoneArmResourceId
      publicNetworkAccess: 'Disabled'
    } : {
      publicNetworkAccess: 'Enabled'
    }
  }
}

// ---------------------------------------------------------------------------
// Application database
// ---------------------------------------------------------------------------

resource applicationDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview' = {
  parent: postgresServer
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

output serverName string = postgresServer.name
output serverFqdn string = postgresServer.properties.fullyQualifiedDomainName
output databaseName string = applicationDatabase.name
