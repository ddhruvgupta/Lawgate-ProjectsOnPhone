<#
.SYNOPSIS
    Sets up Microsoft Entra External ID (CIAM) for Lawgate.

.DESCRIPTION
    This script performs all Microsoft Graph configuration for External ID:
      1. Validates you are signed in to the correct external tenant
      2. Creates the backend API app registration with 6 app roles
      3. Creates the frontend SPA app registration
      4. Grants the SPA admin consent to use the backend API
      5. Creates a combined sign-up / sign-in user flow
      6. Outputs the config values to paste into main.bicepparam

    PREREQUISITE — Create the External ID tenant first:
      a. Go to https://entra.microsoft.com
      b. Switch to your primary (workforce) tenant
      c. Click "Create a tenant" → "Customer (External)" → follow the wizard
      d. Copy the tenant ID from Overview → paste as -ExternalTenantId

.PARAMETER ExternalTenantId
    The tenant ID (GUID) of the External ID external tenant.

.PARAMETER ApiBaseUrl
    The backend API's production URL, e.g. https://lawgate-prod-api-abc123.azurewebsites.net
    Used to set the Application ID URI for the API app registration.

.PARAMETER FrontendUrl
    The frontend's production URL, e.g. https://lawgate-prod-frontend.azurestaticapps.net
    Used as a redirect URI for the SPA app registration.

.PARAMETER LocalhostPort
    The localhost port for the Vite dev server (default: 5174).
    Adds http://localhost:<port> as a redirect URI for local development.

.EXAMPLE
    .\setup-external-id.ps1 `
        -ExternalTenantId "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" `
        -ApiBaseUrl "https://lawgate-prod-api-abc123.azurewebsites.net" `
        -FrontendUrl "https://lawgate-prod-frontend.azurestaticapps.net"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9a-f]{8}-([0-9a-f]{4}-){3}[0-9a-f]{12}$')]
    [string] $ExternalTenantId,

    # ApiBaseUrl and FrontendUrl are optional for Phase 6 / local dev.
    # Redirect URIs can be updated later in the Entra admin center once production URLs are known.
    [string] $ApiBaseUrl = '',

    [string] $FrontendUrl = '',

    [int] $LocalhostPort = 5174
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Write-Step([string]$msg) {
    Write-Host ""
    Write-Host ">>> $msg" -ForegroundColor Cyan
}

function Write-Success([string]$msg) {
    Write-Host "    OK  $msg" -ForegroundColor Green
}

function Write-Warn([string]$msg) {
    Write-Host "    WARN $msg" -ForegroundColor Yellow
}

# Calls the Microsoft Graph beta or v1.0 endpoint using the active az CLI session
function Invoke-Graph {
    param(
        [string] $Method = 'GET',
        [string] $Uri,
        [string] $Body = $null,
        [string] $Version = 'v1.0'
    )
    $fullUri = "https://graph.microsoft.com/$Version/$Uri"
    $args = @('rest', '--method', $Method, '--uri', $fullUri,
              '--resource', 'https://graph.microsoft.com')
    if ($Body) {
        $args += @('--body', $Body, '--headers', 'Content-Type=application/json')
    }
    $response = az @args | ConvertFrom-Json
    return $response
}

# ---------------------------------------------------------------------------
# Step 1 — Verify az CLI session is targeting the external tenant
# ---------------------------------------------------------------------------

Write-Step "Verifying Azure CLI session"

$currentAccount = $null
try { $currentAccount = az account show 2>$null | ConvertFrom-Json } catch { }
if (-not $currentAccount) {
    Write-Host "Not logged in. Signing in to the external tenant..." -ForegroundColor Yellow
    az login --tenant $ExternalTenantId --allow-no-subscriptions | Out-Null
    $currentAccount = az account show | ConvertFrom-Json
}

if ($currentAccount.tenantId -ne $ExternalTenantId) {
    Write-Host "  Current tenant: $($currentAccount.tenantId)"
    Write-Host "  Switching to External ID tenant: $ExternalTenantId" -ForegroundColor Yellow
    az login --tenant $ExternalTenantId --allow-no-subscriptions | Out-Null
    $currentAccount = az account show | ConvertFrom-Json
}

if ($currentAccount.tenantId -ne $ExternalTenantId) {
    throw "Failed to authenticate to external tenant $ExternalTenantId"
}
Write-Success "Signed in to tenant $ExternalTenantId ($($currentAccount.name))"

# ---------------------------------------------------------------------------
# Step 2 — Create backend API app registration with app roles
# ---------------------------------------------------------------------------

Write-Step "Creating backend API app registration"

# App ID URI uses api:// scheme with the tenant ID
$apiIdentifierUri = "api://$ExternalTenantId/lawgate-api"

# The 6 Lawgate roles — must match LegalDocSystem.Domain.Enums.UserRole
$appRoles = @(
    @{
        id                = [System.Guid]::NewGuid().ToString()
        allowedMemberTypes = @('User')
        displayName        = 'Company Owner'
        description        = 'Company owner with full access to all company data'
        value              = 'CompanyOwner'
        isEnabled          = $true
    },
    @{
        id                = [System.Guid]::NewGuid().ToString()
        allowedMemberTypes = @('User')
        displayName        = 'Administrator'
        description        = 'Administrator with elevated privileges within a company'
        value              = 'Admin'
        isEnabled          = $true
    },
    @{
        id                = [System.Guid]::NewGuid().ToString()
        allowedMemberTypes = @('User')
        displayName        = 'User'
        description        = 'Standard user with basic access'
        value              = 'User'
        isEnabled          = $true
    },
    @{
        id                = [System.Guid]::NewGuid().ToString()
        allowedMemberTypes = @('User')
        displayName        = 'Viewer'
        description        = 'Read-only access'
        value              = 'Viewer'
        isEnabled          = $true
    },
    @{
        id                = [System.Guid]::NewGuid().ToString()
        allowedMemberTypes = @('User')
        displayName        = 'Platform Admin'
        description        = 'Lawgate platform admin - view all customers, users and projects'
        value              = 'PlatformAdmin'
        isEnabled          = $true
    },
    @{
        id                = [System.Guid]::NewGuid().ToString()
        allowedMemberTypes = @('User')
        displayName        = 'Platform Super Admin'
        description        = 'Lawgate super admin - full visibility including customer documents'
        value              = 'PlatformSuperAdmin'
        isEnabled          = $true
    }
) | ConvertTo-Json -Depth 5 -Compress

# The user_impersonation scope lets the SPA call the API on behalf of the user
$userImpersonationScopeId = [System.Guid]::NewGuid().ToString()

$apiAppBody = @{
    displayName            = 'Lawgate API'
    identifierUris         = @($apiIdentifierUri)
    signInAudience         = 'AzureADMyOrg'
    appRoles               = ($appRoles | ConvertFrom-Json)
    api                    = @{
        requestedAccessTokenVersion = 2
        oauth2PermissionScopes      = @(
            @{
                id                      = $userImpersonationScopeId
                adminConsentDescription = 'Allow the application to access Lawgate API on behalf of the signed-in user.'
                adminConsentDisplayName = 'Access Lawgate API'
                userConsentDescription  = 'Allow the application to access Lawgate API on your behalf.'
                userConsentDisplayName  = 'Access Lawgate API'
                isEnabled               = $true
                type                    = 'User'
                value                   = 'user_impersonation'
            }
        )
    }
} | ConvertTo-Json -Depth 10 -Compress

$apiApp = Invoke-Graph -Method 'POST' -Uri 'applications' -Body $apiAppBody
$apiClientId = $apiApp.appId
$apiObjectId = $apiApp.id
Write-Success "Created API app registration: $($apiApp.displayName) (clientId: $apiClientId)"

# Create the accompanying service principal (required for role assignments)
$apiSpBody = @{ appId = $apiClientId } | ConvertTo-Json -Compress
$apiSp = Invoke-Graph -Method 'POST' -Uri 'servicePrincipals' -Body $apiSpBody
Write-Success "Created API service principal (id: $($apiSp.id))"

# ---------------------------------------------------------------------------
# Step 3 — Create frontend SPA app registration
# ---------------------------------------------------------------------------

Write-Step "Creating frontend SPA app registration"

$redirectUris = @("http://localhost:${LocalhostPort}/auth/callback")
if ($FrontendUrl) { $redirectUris += "$FrontendUrl/auth/callback" }

$spaAppBody = @{
    displayName    = 'Lawgate SPA'
    signInAudience = 'AzureADMyOrg'
    spa            = @{
        redirectUris = $redirectUris
    }
    requiredResourceAccess = @(
        @{
            resourceAppId  = $apiClientId
            resourceAccess = @(
                @{
                    id   = $userImpersonationScopeId
                    type = 'Scope'
                }
            )
        }
    )
} | ConvertTo-Json -Depth 10 -Compress

$spaApp = Invoke-Graph -Method 'POST' -Uri 'applications' -Body $spaAppBody
$spaClientId = $spaApp.appId
Write-Success "Created SPA app registration: $($spaApp.displayName) (clientId: $spaClientId)"

# Create service principal for the SPA
$spaSpBody = @{ appId = $spaClientId } | ConvertTo-Json -Compress
$spaSp = Invoke-Graph -Method 'POST' -Uri 'servicePrincipals' -Body $spaSpBody
Write-Success "Created SPA service principal (id: $($spaSp.id))"

# ---------------------------------------------------------------------------
# Step 4 — Admin consent: grant SPA access to API scope
# Required so users are not prompted for consent on first sign-in
# ---------------------------------------------------------------------------

Write-Step "Granting admin consent (SPA → API user_impersonation)"

Start-Sleep -Seconds 3  # Wait for service principals to fully propagate

$consentBody = @{
    clientId    = $spaSp.id
    consentType = 'AllPrincipals'
    resourceId  = $apiSp.id
    scope       = 'user_impersonation'
} | ConvertTo-Json -Compress

try {
    Invoke-Graph -Method 'POST' -Uri 'oauth2PermissionGrants' -Body $consentBody | Out-Null
    Write-Success "Admin consent granted"
} catch {
    Write-Warn "Could not grant admin consent automatically. Grant it manually:"
    Write-Warn "  Entra admin center → App registrations → Lawgate SPA → API permissions → Grant admin consent"
}

# ---------------------------------------------------------------------------
# Step 5 — Create sign-up / sign-in user flow (CIAM)
# Uses the Graph beta endpoint — user flows are CIAM-specific
# ---------------------------------------------------------------------------

Write-Step "Creating sign-up / sign-in user flow"

$userFlowBody = @{
    '@odata.type'             = '#microsoft.graph.externalUsersSelfServiceSignUpEventsFlow'
    displayName               = 'Lawgate_SUSI'
    description               = 'Sign-up and sign-in flow for Lawgate'
    onAuthenticationMethodLoadStart = @{
        '@odata.type' = '#microsoft.graph.onAuthenticationMethodLoadStartExternalUsersSelfServiceSignUp'
        identityProviders = @(
            @{ '@odata.type' = '#microsoft.graph.builtInIdentityProvider'; id = 'EmailPassword-OAUTH2-v2' }
        )
    }
    onInteractiveAuthFlowStart = @{
        '@odata.type'                     = '#microsoft.graph.onInteractiveAuthFlowStartExternalUsersSelfServiceSignUp'
        isSignUpAllowed                   = $true
    }
    onAttributeCollection = @{
        '@odata.type' = '#microsoft.graph.onAttributeCollectionExternalUsersSelfServiceSignUp'
        attributes    = @(
            @{ id = 'city' },
            @{ id = 'displayName' },
            @{ id = 'givenName' },
            @{ id = 'surname' }
        )
        attributeCollectionPage = @{
            customStringsFileId = $null
            views               = @(
                @{
                    title   = $null
                    inputs  = @(
                        @{ attribute = 'givenName';   label = 'First Name'; inputType = 'text';     isHidden = $false; isRequired = $true;  validationRegEx = $null; writeToDirectory = $true }
                        @{ attribute = 'surname';     label = 'Last Name';  inputType = 'text';     isHidden = $false; isRequired = $true;  validationRegEx = $null; writeToDirectory = $true }
                        @{ attribute = 'displayName'; label = 'Display Name'; inputType = 'text';   isHidden = $false; isRequired = $true;  validationRegEx = $null; writeToDirectory = $true }
                    )
                }
            )
        }
    }
    onUserCreateStart = @{
        '@odata.type' = '#microsoft.graph.onUserCreateStartExternalUsersSelfServiceSignUp'
        userTypeToCreate = 'member'
    }
} | ConvertTo-Json -Depth 15 -Compress

try {
    $userFlow = Invoke-Graph -Method 'POST' `
        -Uri "identity/authenticationEventsFlows" `
        -Body $userFlowBody `
        -Version 'beta'
    $flowName = if ($userFlow.displayName) { $userFlow.displayName } else { 'Lawgate_SUSI' }
    Write-Success "Created user flow: $flowName"

    # Associate the SPA app with the user flow
    $includeBody = @{
        '@odata.type' = '#microsoft.graph.authenticationConditionApplication'
        appId         = $spaClientId
    } | ConvertTo-Json -Compress
    Invoke-Graph -Method 'POST' `
        -Uri "identity/authenticationEventsFlows/$($userFlow.id)/conditions/applications/includeApplications" `
        -Body $includeBody `
        -Version 'beta' | Out-Null
    Write-Success "Associated SPA with user flow"
} catch {
    Write-Warn "User flow creation failed (beta API may have changed). Create it manually:"
    Write-Warn "  Entra admin center → External Identities → User flows → New user flow"
    Write-Warn "  Add 'Lawgate SPA' ($spaClientId) to the flow's applications"
}

# ---------------------------------------------------------------------------
# Step 6 — Output configuration
# ---------------------------------------------------------------------------

Write-Step "Setup complete — configuration values"

$authorityBase = "https://$ExternalTenantId.ciamlogin.com/$ExternalTenantId"

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Green
Write-Host "  Copy these values into infra/main.bicepparam:" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  param externalIdTenantId    = '$ExternalTenantId'"   -ForegroundColor White
Write-Host "  param externalIdApiClientId = '$apiClientId'"         -ForegroundColor White
Write-Host ""
Write-Host "=================================================================" -ForegroundColor Green
Write-Host "  Frontend .env (VITE variables):" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  VITE_AUTH_TENANT_ID=$ExternalTenantId"             -ForegroundColor White
Write-Host "  VITE_AUTH_CLIENT_ID=$spaClientId"                  -ForegroundColor White
Write-Host "  VITE_AUTH_AUTHORITY=$authorityBase"                 -ForegroundColor White
Write-Host "  VITE_AUTH_REDIRECT_URI=$FrontendUrl/auth/callback"  -ForegroundColor White
Write-Host "  VITE_AUTH_SCOPES=$apiIdentifierUri/user_impersonation" -ForegroundColor White
Write-Host ""
Write-Host "=================================================================" -ForegroundColor Green
Write-Host "  Backend appsettings (already set via Bicep app settings):" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  ExternalId__TenantId  = $ExternalTenantId"          -ForegroundColor White
Write-Host "  ExternalId__Audience  = $apiClientId"               -ForegroundColor White
Write-Host "  ExternalId__Authority = $authorityBase/v2.0"        -ForegroundColor White
Write-Host ""

# Save to a local file for reference (never committed - in .gitignore)
$ts = Get-Date -Format 'yyyy-MM-dd HH:mm'
$configLines = @(
    "# Generated by setup-external-id.ps1 on $ts",
    "# DO NOT COMMIT THIS FILE",
    "",
    "[BicepParams]",
    "externalIdTenantId    = $ExternalTenantId",
    "externalIdApiClientId = $apiClientId",
    "",
    "[FrontendEnv]",
    "VITE_AUTH_TENANT_ID=$ExternalTenantId",
    "VITE_AUTH_CLIENT_ID=$spaClientId",
    "VITE_AUTH_AUTHORITY=$authorityBase",
    "VITE_AUTH_REDIRECT_URI=$FrontendUrl/auth/callback",
    "VITE_AUTH_SCOPES=$apiIdentifierUri/user_impersonation",
    "",
    "[BackendAppSettings]",
    "ExternalId__TenantId=$ExternalTenantId",
    "ExternalId__Audience=$apiClientId",
    "ExternalId__Authority=$authorityBase/v2.0",
    "",
    "[AppRegistrations]",
    "ApiClientId=$apiClientId",
    "ApiObjectId=$apiObjectId",
    "SpaClientId=$spaClientId",
    "ApiIdentifierUri=$apiIdentifierUri",
    "UserImpersonationScopeId=$userImpersonationScopeId"
)
$outputPath = Join-Path $PSScriptRoot 'external-id-config.local.ini'
$configLines | Set-Content -Path $outputPath -Encoding UTF8
Write-Host "  Full config saved to: $outputPath" -ForegroundColor DarkGray
Write-Host "  (Add external-id-config.local.ini to .gitignore if not already present)" -ForegroundColor DarkGray
