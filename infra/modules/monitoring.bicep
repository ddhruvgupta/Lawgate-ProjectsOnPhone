@description('Azure region for all resources')
param location string

@description('Name for the Log Analytics Workspace')
param workspaceName string

@description('Name for the Application Insights instance')
param appInsightsName string

@description('Log retention in days (min 30, max 730 for free tier)')
@minValue(30)
@maxValue(730)
param retentionDays int = 30

// ---------------------------------------------------------------------------
// Log Analytics Workspace — backing store for App Insights
// ---------------------------------------------------------------------------

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: retentionDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// ---------------------------------------------------------------------------
// Application Insights — linked to the Log Analytics Workspace
// ---------------------------------------------------------------------------

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    RetentionInDays: retentionDays
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

@description('App Insights connection string — safe to use in app settings directly')
output appInsightsConnectionString string = appInsights.properties.ConnectionString

@description('App Insights resource ID')
output appInsightsId string = appInsights.id

@description('Log Analytics Workspace resource ID')
output workspaceId string = logAnalyticsWorkspace.id
