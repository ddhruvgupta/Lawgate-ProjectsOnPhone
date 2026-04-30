<#
.SYNOPSIS
    Full Lawgate deployment orchestrator - deploys all Azure infrastructure
    via Bicep and then deploys the backend API and frontend.

.DESCRIPTION
    Steps:
      1. Check prerequisites (az CLI, Bicep)
      2. Login and select subscription
      3. Create resource group
      4. Deploy Bicep (prompts for secure params if not supplied)
      5. Add local IP to PostgreSQL firewall for migrations
      6. Run EF Core migrations against Azure PostgreSQL
      7. Zip-deploy the .NET backend
      8. Build and upload the React frontend
      9. Print summary

    Run setup-external-id.ps1 SEPARATELY after step 9, then re-run this
    script (or just re-deploy Bicep) to update the ExternalId app settings.

.PARAMETER ResourceGroup
    Azure Resource Group name (created if it does not exist).

.PARAMETER Location
    Azure region (e.g. centralindia, eastus, westeurope).

.PARAMETER Subscription
    Azure Subscription ID or name. If omitted, the current default is used.

.PARAMETER PostgresPassword
    PostgreSQL admin password. Prompted securely if not supplied.

.PARAMETER JwtSecret
    JWT signing secret (min 32 chars). Prompted securely if not supplied.

.PARAMETER SkipBicep
    Skip Bicep deployment (use when only redeploying the application code).

.PARAMETER SkipMigrations
    Skip EF Core migrations (use when database schema is already current).

.PARAMETER SkipBackend
    Skip backend zip-deploy.

.PARAMETER SkipFrontend
    Skip frontend build and upload.

.EXAMPLE
    .\deploy.ps1 -ResourceGroup lawgate-rg -Location centralindia

.EXAMPLE
    .\deploy.ps1 -ResourceGroup lawgate-rg -Location centralindia -SkipBicep -SkipMigrations
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $ResourceGroup,

    # Only required when the resource group does not yet exist.
    # Resource locations (Central India, East Asia, etc.) come from main.bicepparam.
    [string] $Location = '',

    [string] $Subscription = '',

    [string] $PostgresPassword = '',
    [string] $JwtSecret = '',

    # PostgreSQL admin login — defaults to reading from the server if not supplied.
    # Required when the server admin differs from 'lawgate_admin' (e.g. existing servers).
    [string] $PostgresAdminLogin = '',

    # Explicit Static Web App name - avoids non-deterministic list queries when
    # multiple SWAs exist in the resource group. Defaults to the Bicep naming
    # convention: "${appName}-${environment}-frontend" (e.g. lawgate-prod-frontend).
    [string] $StaticWebAppName = '',

    # Explicit PostgreSQL Flexible Server name — avoids picking an arbitrary server when
    # multiple exist in the resource group. Required when the resource group contains
    # more than one Flexible Server.
    [string] $PostgresServerName = '',

    # Explicit Key Vault name — avoids ambiguity when the resource group contains more
    # than one vault. If omitted, the script filters by the 'lg-kv-*' naming convention.
    [string] $KeyVaultName = '',

    [switch] $SkipBicep,
    [switch] $SkipMigrations,
    [switch] $SkipBackend,
    [switch] $SkipFrontend
)

# Resolve SWA name: explicit param wins; otherwise filter by the known naming
# convention suffix to avoid picking an arbitrary SWA from the resource group.
function Resolve-SwaName([string]$rg, [string]$hint) {
    if ($hint) { return $hint }
    $name = az staticwebapp list --resource-group $rg `
        --query "[?ends_with(name, '-frontend')].name | [0]" -o tsv 2>$null
    if (-not $name) { throw "Could not find a Static Web App ending in '-frontend' in resource group '$rg'. Use -StaticWebAppName to specify it explicitly." }
    return $name
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# Suppress az CLI WARNING messages (e.g. Bicep upgrade notices) so PS 5.1 does
# not convert them into terminating NativeCommandError records.
$env:AZURE_CORE_ONLY_SHOW_ERRORS = 'true'

$InfraRoot  = $PSScriptRoot | Split-Path -Parent   # infra/
$RepoRoot   = $InfraRoot    | Split-Path -Parent    # project root
$BackendDir = Join-Path $RepoRoot 'backend'
$FrontendDir= Join-Path $RepoRoot 'frontend'

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Write-Step([string]$n, [string]$msg) {
    Write-Host ""
    Write-Host "[$n] $msg" -ForegroundColor Cyan
    Write-Host ("-" * 60) -ForegroundColor DarkGray
}

function Require-Command([string]$cmd) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        throw "Required tool not found: '$cmd'. Please install it and re-run."
    }
}

function Get-SecureStringPlainText([System.Security.SecureString]$ss) {
    $ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($ss)
    try { return [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($ptr) }
    finally { [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

# Try to read a secret from the Key Vault in the given resource group.
# Returns $null if the vault doesn't exist yet or the secret is not found.
function Read-SecretFromKeyVault([string]$rg, [string]$secretName, [string]$keyVaultName = $null) {
    $kvName = $keyVaultName

    if (-not $kvName) {
        $savedEAP = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        $keyVaultNames = @(az keyvault list --resource-group $rg --query '[].name' -o tsv 2>$null)
        $ErrorActionPreference = $savedEAP

        if (-not $keyVaultNames -or $keyVaultNames.Count -eq 0) { return $null }

        # Prefer vaults matching the project naming convention (lg-kv-*)
        $matchingNames = @($keyVaultNames | Where-Object { $_ -like 'lg-kv-*' })
        $candidates = if ($matchingNames.Count -gt 0) { $matchingNames } else { $keyVaultNames }

        if ($candidates.Count -gt 1) {
            throw "Multiple Key Vaults found in resource group '$rg'. The deployment script cannot determine which vault to use automatically. Ensure the resource group contains only the intended Key Vault, or re-run with -KeyVaultName to specify the vault explicitly."
        }

        $kvName = $candidates[0]
    }

    if (-not $kvName) { return $null }
    $savedEAP2 = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    $value = az keyvault secret show --vault-name $kvName --name $secretName --query 'value' -o tsv 2>$null
    $ErrorActionPreference = $savedEAP2
    if ($value) { return $value }
    return $null
}

# ---------------------------------------------------------------------------
# Step 1 - Prerequisites
# ---------------------------------------------------------------------------

Write-Step 1 "Checking prerequisites"

Require-Command 'az'
Require-Command 'dotnet'
Require-Command 'node'
Require-Command 'npm'

# Ensure Bicep is installed and up-to-date (suppresses upgrade WARNING in later commands)
$savedEAP = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
az bicep install 2>&1 | Out-Null
az bicep upgrade 2>&1 | Out-Null
$ErrorActionPreference = $savedEAP
Write-Host "  az CLI   : $(az version --query '\"azure-cli\"' -o tsv)"
$savedEAP = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
$bicepVer = (az bicep version 2>$null) -join ' '
$ErrorActionPreference = $savedEAP
Write-Host "  Bicep    : $bicepVer"
Write-Host "  .NET     : $(dotnet --version)"
Write-Host "  Node     : $(node --version)"

# ---------------------------------------------------------------------------
# Step 2 - Login / subscription
# ---------------------------------------------------------------------------

Write-Step 2 "Azure authentication"

$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "  Not logged in - running az login..."
    az login | Out-Null
    $account = az account show | ConvertFrom-Json
}
Write-Host "  Logged in as: $($account.user.name)"
Write-Host "  Tenant:       $($account.tenantId)"

if ($Subscription) {
    az account set --subscription $Subscription
    $account = az account show | ConvertFrom-Json
}
Write-Host "  Subscription: $($account.name) ($($account.id))"

# ---------------------------------------------------------------------------
# Step 3 - Resource group
# ---------------------------------------------------------------------------

Write-Step 3 "Resource group: $ResourceGroup"

$rgExists = az group exists --name $ResourceGroup | ConvertFrom-Json
if (-not $rgExists) {
    if (-not $Location) { throw "-Location is required when the resource group does not yet exist." }
    Write-Host "  Creating resource group..."
    az group create --name $ResourceGroup --location $Location | Out-Null
    Write-Host "  Created: $ResourceGroup in $Location"
} else {
    Write-Host "  Already exists"
}

# ---------------------------------------------------------------------------
# Step 4 - Bicep deployment
# ---------------------------------------------------------------------------

if (-not $SkipBicep) {
    Write-Step 4 "Bicep infrastructure deployment"

    # Resolve secure params: CLI flag > Key Vault > interactive prompt.
    # On re-runs both values already exist in Key Vault so no prompt appears.
    if (-not $PostgresPassword) {
        Write-Host "  Looking up PostgreSQL password from Key Vault..."
        $dbConnStr = Read-SecretFromKeyVault $ResourceGroup 'DatabaseConnectionString' $KeyVaultName
        if ($dbConnStr) {
            # Parse Password=... from the Npgsql connection string
            $PostgresPassword = ([regex]'(?i)Password=([^;]+)').Match($dbConnStr).Groups[1].Value
        }
        if (-not $PostgresPassword) {
            $pgSecure = Read-Host "  Enter PostgreSQL admin password" -AsSecureString
            $PostgresPassword = Get-SecureStringPlainText $pgSecure
        } else {
            Write-Host "  PostgreSQL password read from Key Vault" -ForegroundColor DarkGray
        }
    }
    if (-not $JwtSecret) {
        Write-Host "  Looking up JWT secret from Key Vault..."
        $JwtSecret = Read-SecretFromKeyVault $ResourceGroup 'JwtSecretKey' $KeyVaultName
        if (-not $JwtSecret) {
            $jwtSecure = Read-Host "  Enter JWT secret key (min 32 chars)" -AsSecureString
            $JwtSecret = Get-SecureStringPlainText $jwtSecure
        } else {
            Write-Host "  JWT secret read from Key Vault" -ForegroundColor DarkGray
        }
    }
    if ($JwtSecret.Length -lt 32) {
        throw "JWT secret must be at least 32 characters"
    }

    $bicepMain   = Join-Path $InfraRoot 'main.bicep'
    $bicepParams = Join-Path $InfraRoot 'main.bicepparam'

    Write-Host "  Running what-if first..."
    $savedEAP2 = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    az deployment group what-if `
        --resource-group $ResourceGroup `
        --template-file $bicepMain `
        --parameters $bicepParams `
        --parameters postgresAdminPassword=$PostgresPassword `
        --parameters jwtSecretKey=$JwtSecret `
        --no-pretty-print 2>&1 | Where-Object { $_ -notmatch '^WARNING' } | Select-Object -Last 30
    $ErrorActionPreference = $savedEAP2

    $confirm = Read-Host "  Proceed with deployment? (y/N)"
    if ($confirm -ne 'y') { Write-Host "  Deployment cancelled."; return }

    Write-Host "  Deploying..."
    $deployOutput = az deployment group create `
        --resource-group $ResourceGroup `
        --template-file $bicepMain `
        --parameters $bicepParams `
        --parameters postgresAdminPassword=$PostgresPassword `
        --parameters jwtSecretKey=$JwtSecret `
        --query 'properties.outputs' `
        --output json | ConvertFrom-Json

    $script:ApiUrl               = $deployOutput.apiUrl.value
    $script:FrontendUrl          = $deployOutput.frontendUrl.value
    $script:DbServerFqdn         = $deployOutput.databaseServerFqdn.value
    $script:StorageAccountName   = $deployOutput.storageAccountName.value

    # Retrieve the SWA deployment token securely (intentionally not in Bicep outputs)
    $swaName = Resolve-SwaName $ResourceGroup $StaticWebAppName
    $script:StaticWebAppToken = az staticwebapp secrets list --name $swaName --query 'properties.apiKey' -o tsv

    Write-Host ""
    Write-Host "  Bicep deployment complete:" -ForegroundColor Green
    Write-Host "    API URL    : $($script:ApiUrl)"
    Write-Host "    Frontend   : $($script:FrontendUrl)"
    Write-Host "    DB Server  : $($script:DbServerFqdn)"
} else {
    Write-Step 4 "Skipping Bicep deployment (--SkipBicep)"
    # Attempt to read outputs from a prior deployment
    Write-Host "  Fetching outputs from last deployment..."
    $deployOutput = az deployment group show `
        --resource-group $ResourceGroup `
        --name 'main' `
        --query 'properties.outputs' `
        --output json 2>$null | ConvertFrom-Json

    if ($deployOutput) {
        $script:ApiUrl            = $deployOutput.apiUrl.value
        $script:FrontendUrl       = $deployOutput.frontendUrl.value
        $script:DbServerFqdn      = $deployOutput.databaseServerFqdn.value
        $script:StorageAccountName= $deployOutput.storageAccountName.value
        # Retrieve the SWA deployment token securely (intentionally not in Bicep outputs)
        $swaName = Resolve-SwaName $ResourceGroup $StaticWebAppName
        $script:StaticWebAppToken = az staticwebapp secrets list --name $swaName --query 'properties.apiKey' -o tsv
        Write-Host "    API URL   : $($script:ApiUrl)"
        Write-Host "    Frontend  : $($script:FrontendUrl)"
    } else {
        Write-Host "  No prior deployment found - some steps may fail without outputs"
    }
}

# ---------------------------------------------------------------------------
# Step 5 - Add local IP to PostgreSQL firewall for migrations
# ---------------------------------------------------------------------------

if (-not $SkipMigrations) {
    # Detect whether the PostgreSQL server uses VNet injection (private access).
    # VNet-injected servers have no public DNS entry — the FQDN only resolves
    # inside the VNet. Migrations run automatically on App Service startup instead.
    $pgServer = if ($PostgresServerName) {
        $PostgresServerName
    } else {
        $name = az postgres flexible-server list `
            --resource-group $ResourceGroup `
            --query '[0].name' -o tsv 2>$null

        if ([string]::IsNullOrWhiteSpace($name)) {
            throw "No Azure Database for PostgreSQL Flexible Server was found in resource group '$ResourceGroup'. Cannot continue with firewall configuration or migrations."
        }

        # Warn when multiple servers exist — the first was chosen non-deterministically.
        $serverCount = @(az postgres flexible-server list --resource-group $ResourceGroup --query '[].name' -o tsv 2>$null).Count
        if ($serverCount -gt 1) {
            Write-Warning "Multiple PostgreSQL Flexible Servers found in '$ResourceGroup'. Using '$name'. Pass -PostgresServerName to select explicitly."
        }
        $name
    }

    $isVnetInjected = $false
    if ($pgServer) {
        $delegatedSubnet = az postgres flexible-server show `
            --name $pgServer --resource-group $ResourceGroup `
            --query 'network.delegatedSubnetResourceId' -o tsv 2>$null
        $isVnetInjected = -not [string]::IsNullOrEmpty($delegatedSubnet)
    }

    if ($isVnetInjected) {
        Write-Step "5+6" "Skipping local migrations — VNet-injected server"
        Write-Host "  Server '$pgServer' uses private access (VNet injection)." -ForegroundColor DarkYellow
        Write-Host "  Its FQDN has no public DNS entry and cannot be reached from this machine." -ForegroundColor DarkYellow
        Write-Host "  Migrations will be applied automatically on App Service startup via Program.cs." -ForegroundColor DarkYellow
    } else {
        Write-Step 5 "Opening PostgreSQL firewall for local migrations"

        $myIp = (Invoke-RestMethod 'https://api.ipify.org?format=json').ip
        Write-Host "  Local IP: $myIp"

        # $pgServer already resolved above

    # Ensure the server is started (it may be stopped)
    $pgState = az postgres flexible-server show --name $pgServer --resource-group $ResourceGroup --query 'state' -o tsv 2>$null
    if ($pgState -eq 'Stopped') {
        Write-Host "  Starting PostgreSQL server..."
        $savedEAP3 = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        az postgres flexible-server start --name $pgServer --resource-group $ResourceGroup 2>&1 | Out-Null
        $ErrorActionPreference = $savedEAP3
        Start-Sleep -Seconds 30
    }

    # Enable public access if needed (private-access servers reject firewall rules)
    $netType = az postgres flexible-server show --name $pgServer --resource-group $ResourceGroup --query 'network.publicNetworkAccess' -o tsv 2>$null
    if ($netType -ne 'Enabled') {
        Write-Host "  Enabling public network access on server for migrations..."
        $savedEAP3 = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        az postgres flexible-server update --name $pgServer --resource-group $ResourceGroup --public-access enabled 2>&1 | Out-Null
        $ErrorActionPreference = $savedEAP3
        Write-Host "  Public access enabled"
    }

    $savedEAP3 = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    $firewallRuleName = "LocalMigrations-$(Get-Date -Format 'yyyyMMddHHmm')"
    az postgres flexible-server firewall-rule create `
        --resource-group $ResourceGroup `
        --name $pgServer `
        --rule-name $firewallRuleName `
        --start-ip-address $myIp `
        --end-ip-address $myIp 2>&1 | Out-Null
    $ErrorActionPreference = $savedEAP3
    Write-Host "  Firewall rule added for $myIp -> $pgServer"

    # ---------------------------------------------------------------------------
    # Step 6 - EF Core migrations
    # ---------------------------------------------------------------------------

    Write-Step 6 "Running EF Core migrations"

    $dbPassword = if ($PostgresPassword) { $PostgresPassword } else {
        $pgSecure = Read-Host "  Enter PostgreSQL admin password for migrations" -AsSecureString
        Get-SecureStringPlainText $pgSecure
    }

    # Resolve admin login: prefer explicitly-set DbAdminLogin param, fall back to server config
    $resolvedAdminLogin = if ($PostgresAdminLogin) { $PostgresAdminLogin } else {
        az postgres flexible-server show --name $pgServer --resource-group $ResourceGroup --query 'administratorLogin' -o tsv 2>$null
    }

    $migrationsConnStr = "Host=$($script:DbServerFqdn);Database=lawgate_db;Username=${resolvedAdminLogin};Password=$dbPassword;SslMode=Require;TrustServerCertificate=false"
    $env:ConnectionStrings__DefaultConnection = $migrationsConnStr

    Push-Location (Join-Path $BackendDir 'LegalDocSystem.API')
    try {
        $savedEAP4 = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        dotnet ef database update --no-build 2>&1 | Where-Object { $_ -notmatch 'NU1902|NU1903|NU1904' }
        $efExitCode = $LASTEXITCODE
        $ErrorActionPreference = $savedEAP4
        if ($efExitCode -ne 0) {
            # No release build present - build first
            Write-Host "  Build required before migrations..."
            dotnet build --configuration Release --no-restore 2>&1 | Where-Object { $_ -match 'error|warning|succeeded|failed' -and $_ -notmatch 'NU190' }
            $savedEAP4b = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
            dotnet ef database update 2>&1 | Where-Object { $_ -notmatch 'NU1902|NU1903|NU1904' }
            if ($LASTEXITCODE -ne 0) { $ErrorActionPreference = $savedEAP4b; throw "EF migrations failed" }
            $ErrorActionPreference = $savedEAP4b
        }
        Write-Host "  Migrations applied successfully" -ForegroundColor Green
    } finally {
        Pop-Location
        Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
    }

    # Remove the temporary firewall rule using the same name captured at creation.
    az postgres flexible-server firewall-rule delete `
        --resource-group $ResourceGroup `
        --name $pgServer `
        --rule-name $firewallRuleName `
        --yes 2>$null
    Write-Host "  Temporary firewall rule removed"
    } # end non-VNet-injected migrations block
} else {
    Write-Step "5+6" "Skipping migrations (--SkipMigrations)"
}

# ---------------------------------------------------------------------------
# Step 7 - Deploy backend via zip-deploy
# ---------------------------------------------------------------------------

if (-not $SkipBackend) {
    Write-Step 7 "Building and deploying backend"

    $publishDir = Join-Path $BackendDir 'publish'
    $zipPath    = Join-Path $BackendDir 'deploy-backend.zip'

    Push-Location $BackendDir
    try {
        Write-Host "  Publishing .NET app..."
        dotnet publish LegalDocSystem.API/LegalDocSystem.API.csproj `
            --configuration Release `
            --output $publishDir `
            --runtime linux-x64 `
            --self-contained false 2>&1 | Where-Object { $_ -match 'error|warning|Published' }

        Write-Host "  Creating zip..."
        if (Test-Path $zipPath) { Remove-Item $zipPath }
        Compress-Archive -Path "$publishDir/*" -DestinationPath $zipPath

        Write-Host "  Deploying to App Service..."
        $webAppName = az webapp list `
            --resource-group $ResourceGroup `
            --query '[0].name' -o tsv
        az webapp deploy `
            --resource-group $ResourceGroup `
            --name $webAppName `
            --src-path $zipPath `
            --type zip | Out-Null

        Write-Host "  Backend deployed to $($script:ApiUrl)" -ForegroundColor Green
    } finally {
        Pop-Location
        if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
        if (Test-Path $zipPath)    { Remove-Item $zipPath }
    }
} else {
    Write-Step 7 "Skipping backend deployment (--SkipBackend)"
}

# ---------------------------------------------------------------------------
# Step 8 - Build and deploy frontend
# ---------------------------------------------------------------------------

if (-not $SkipFrontend) {
    Write-Step 8 "Building and uploading frontend"

    Push-Location $FrontendDir
    try {
        Write-Host "  Installing dependencies..."
        npm ci --silent

        # Inject the API URL so the Vite build knows where to reach the backend
        $env:VITE_API_URL = "$($script:ApiUrl)/api"

        Write-Host "  Building React app (production)..."
        $savedEAP8 = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        npm run build 2>&1 | Where-Object { $_ -match 'error|warning|built in' }
        $buildExit = $LASTEXITCODE
        $ErrorActionPreference = $savedEAP8
        if ($buildExit -ne 0) { throw "Frontend build failed (exit $buildExit)" }

        # Static Web Apps are deployed via the deployment token from Bicep.
        # If the Static Web Apps CLI is present, use it; otherwise print instructions.
        if (Get-Command 'swa' -ErrorAction SilentlyContinue) {
            Write-Host "  Deploying via SWA CLI..."
            $env:SWA_CLI_DEPLOYMENT_TOKEN = $script:StaticWebAppToken
            swa deploy ./dist `
                --deployment-token $script:StaticWebAppToken `
                --env production 2>&1
        } else {
            Write-Host ""
            Write-Host "  Static Web Apps CLI (swa) not found. To deploy the frontend:" -ForegroundColor Yellow
            Write-Host "    npm install -g @azure/static-web-apps-cli" -ForegroundColor Yellow
            Write-Host "    swa deploy frontend/dist --deployment-token <token>" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "  Or push to GitHub - the CI workflow uses:" -ForegroundColor Yellow
            Write-Host "    AZURE_STATIC_WEB_APPS_API_TOKEN = $($script:StaticWebAppToken.Substring(0,8))..." -ForegroundColor Yellow
            Write-Host "  Add this as a repository secret named AZURE_STATIC_WEB_APPS_API_TOKEN" -ForegroundColor Yellow
        }
    } finally {
        Pop-Location
        Remove-Item Env:VITE_API_URL -ErrorAction SilentlyContinue
        Remove-Item Env:SWA_CLI_DEPLOYMENT_TOKEN -ErrorAction SilentlyContinue
    }
} else {
    Write-Step 8 "Skipping frontend deployment (--SkipFrontend)"
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Green
Write-Host "  Deployment complete!" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
Write-Host "  API       : $($script:ApiUrl)"           -ForegroundColor White
Write-Host "  Frontend  : $($script:FrontendUrl)"      -ForegroundColor White
Write-Host "  DB Server : $($script:DbServerFqdn)"     -ForegroundColor White
Write-Host ""
Write-Host "  Next steps:" -ForegroundColor Cyan
Write-Host "  1. Run scripts/setup-external-id.ps1 if External ID is not yet configured"
Write-Host "  2. Update main.bicepparam with externalIdTenantId + externalIdApiClientId"
Write-Host "  3. Re-run this script with -SkipMigrations -SkipBackend -SkipFrontend to push"
Write-Host "     the External ID app settings to the App Service"
Write-Host "  4. Add AZURE_STATIC_WEB_APPS_API_TOKEN to GitHub repository secrets"
Write-Host ""
