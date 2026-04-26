using 'main.bicep'

// ===========================================================================
// Non-sensitive parameters — edit freely
// ===========================================================================

param appName = 'lawgate'
param environment = 'prod'

// Azure subscription: "Azure subscription 1"
// Resource group:    project-management  (created with External ID tenant)
// Deploy command:    .\scripts\deploy.ps1 -ResourceGroup project-management -Location centralindia

// Primary region — target audience is in India
param location = 'centralindia'

// Static Web App metadata location — centralindia is not supported; eastasia is the nearest allowed region
// Allowed: centralus | eastus2 | eastasia | westeurope | westus2
param staticWebAppLocation = 'eastasia'

// ===========================================================================
// Sensitive parameters — DO NOT commit real values to source control
//
// Pass these at deploy time via one of:
//   1. CLI prompt:  az deployment group create ... (omit params — prompts interactively)
//   2. Environment: set BICEP_PARAM_postgresAdminPassword and BICEP_PARAM_jwtSecretKey
//   3. Azure Key Vault at deploy time (advanced)
//
// Generate a secure password: [System.Web.Security.Membership]::GeneratePassword(24,4)
// Generate a JWT secret:       [Convert]::ToBase64String((New-Object byte[] 48 | %{[byte](Get-Random -Max 256)}))
// ===========================================================================

param postgresAdminPassword = ''   // REQUIRED — set via CLI or environment
param jwtSecretKey = ''            // REQUIRED — set via CLI or environment

// ===========================================================================
// External ID — populate AFTER running scripts/setup-external-id.ps1
// External ID Tenant: lawgateprojectmanagement.onmicrosoft.com
//   1. Open https://entra.microsoft.com → switch tenant → Lawgate-projectManagement
//   2. Copy the Tenant ID from Overview
//   3. Run: .\scripts\setup-external-id.ps1 -ExternalTenantId <id> -ApiBaseUrl <url> -FrontendUrl <url>
//   4. Paste the output values below
// ===========================================================================

param externalIdTenantId = 'bb793b27-afdc-48d2-9964-dc4a194d4c6e'     // lawgateprojectmanagement.onmicrosoft.com
param externalIdApiClientId = ''  // filled in after running setup-external-id.ps1
