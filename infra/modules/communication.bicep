@description('Name of the Azure Communication Services resource — globally unique, 1-63 alphanumeric and hyphen chars')
@minLength(1)
@maxLength(63)
param communicationServiceName string

@description('Data location — where communication data at rest is stored')
@allowed(['Africa', 'Asia Pacific', 'Australia', 'Brazil', 'Canada', 'Europe', 'France', 'Germany', 'India', 'Japan', 'Korea', 'Norway', 'Switzerland', 'UAE', 'UK', 'United States'])
param dataLocation string = 'India'

// ACS control-plane resources require location = 'global'
// Data residency is controlled separately via dataLocation
var acsLocation = 'global'

// ---------------------------------------------------------------------------
// Email Communication Service
// Required to send emails — hosts verified sender domains
// ---------------------------------------------------------------------------

resource emailService 'Microsoft.Communication/emailServices@2023-04-01' = {
  // take() ensures max 56 chars + '-email' stays within the 63-char limit
  name: '${take(communicationServiceName, 56)}-email'
  location: acsLocation
  properties: {
    dataLocation: dataLocation
  }
}

// ---------------------------------------------------------------------------
// Azure Managed Domain (free — no DNS setup required)
// Sender address pattern: DoNotReply@<mailFromSenderDomain>
// ---------------------------------------------------------------------------

resource azureManagedDomain 'Microsoft.Communication/emailServices/domains@2023-04-01' = {
  parent: emailService
  name: 'AzureManagedDomain'
  location: acsLocation
  properties: {
    domainManagement: 'AzureManaged'
  }
}

// ---------------------------------------------------------------------------
// Azure Communication Services (linked to the managed email domain)
// ---------------------------------------------------------------------------

resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: communicationServiceName
  location: acsLocation
  properties: {
    dataLocation: dataLocation
    linkedDomains: [
      azureManagedDomain.id
    ]
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

@description('Resource ID of the Communication Services instance')
output communicationServiceId string = communicationService.id

@description('Name of the Communication Services instance')
output communicationServiceName string = communicationService.name

@description('Primary connection string for the Communication Services instance — stored in Key Vault at deploy time, never surfaced after that')
#disable-next-line outputs-should-not-contain-secrets
output primaryConnectionString string = communicationService.listKeys().primaryConnectionString

@description('Azure Managed Domain sender domain — construct from address as DoNotReply@<senderDomain>')
output senderDomain string = azureManagedDomain.properties.mailFromSenderDomain
