@description('Azure region — only used when creating a new VNet')
param location string

@description('Name for a new VNet — only used when existingVnetName is empty')
param vnetName string

@description('Name of an existing VNet to reuse. Set this to skip VNet creation and reference existing subnets and DNS zone.')
param existingVnetName string = ''

@description('App Service subnet name inside the existing VNet. Only used when existingVnetName is set.')
param existingAppServiceSubnetName string = 'lawgate-prod-webappAppSubnet'

@description('PostgreSQL Flexible Server subnet name inside the existing VNet. Only used when existingVnetName is set.')
param existingPostgresSubnetName string = 'lawgate-prod-webappDbSubnet'

var useExistingVnet = existingVnetName != ''
var resolvedVnetName = useExistingVnet ? existingVnetName : vnetName

// ===========================================================================
// NEW VNet path — only runs when existingVnetName is empty
// Creates VNet, delegated subnets, private DNS zone, and VNet link.
// ===========================================================================

resource newVnet 'Microsoft.Network/virtualNetworks@2024-01-01' = if (!useExistingVnet) {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: ['10.0.0.0/16']
    }
    subnets: [
      {
        name: 'app-service-subnet'
        properties: {
          addressPrefix: '10.0.1.0/24'
          delegations: [
            {
              name: 'app-service-delegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
      {
        name: 'postgres-subnet'
        properties: {
          addressPrefix: '10.0.2.0/24'
          delegations: [
            {
              name: 'postgres-delegation'
              properties: {
                serviceName: 'Microsoft.DBforPostgreSQL/flexibleServers'
              }
            }
          ]
        }
      }
    ]
  }
}

resource newDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = if (!useExistingVnet) {
  name: 'privatelink.postgres.database.azure.com'
  location: 'global'
}

resource newDnsVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = if (!useExistingVnet) {
  parent: newDnsZone
  name: '${vnetName}-postgres-link'
  location: 'global'
  properties: {
    virtualNetwork: {
      id: newVnet.id
    }
    registrationEnabled: false
  }
}

// ===========================================================================
// EXISTING VNet path — no resources created; subnet names map to what the
// existing manually-provisioned VNet (lawgate-prod-webappVnet) uses.
// ===========================================================================

// Subnet names differ between the existing VNet (manually provisioned) and
// new VNets created by this module.
var appServiceSubnetName = useExistingVnet ? existingAppServiceSubnetName : 'app-service-subnet'
var postgresSubnetName   = useExistingVnet ? existingPostgresSubnetName : 'postgres-subnet'

// ===========================================================================
// Outputs — built with resourceId() to avoid BCP318 on conditional resources
// ===========================================================================

@description('Subnet resource ID for App Service regional VNet integration')
output appServiceSubnetId string = resourceId('Microsoft.Network/virtualNetworks/subnets', resolvedVnetName, appServiceSubnetName)

@description('Subnet resource ID for PostgreSQL Flexible Server VNet injection')
output postgresSubnetId string = resourceId('Microsoft.Network/virtualNetworks/subnets', resolvedVnetName, postgresSubnetName)

@description('Private DNS zone resource ID for privatelink.postgres.database.azure.com')
output privateDnsZoneId string = resourceId('Microsoft.Network/privateDnsZones', 'privatelink.postgres.database.azure.com')

@description('VNet resource ID')
output vnetId string = resourceId('Microsoft.Network/virtualNetworks', resolvedVnetName)
