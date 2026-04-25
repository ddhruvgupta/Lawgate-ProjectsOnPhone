<#
.SYNOPSIS
    Full Lawgate deployment orchestrator — deploys all Azure infrastructure
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

    [Parameter(Mandatory)]
    [string] $Location,

    [string] $Subscription = '',

    [string] $PostgresPassword = '',
    [string] $JwtSecret = '',

    [switch] $SkipBicep,
    [switch] $SkipMigrations,
    [switch] $SkipBackend,
    [switch] $SkipFrontend
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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

# ---------------------------------------------------------------------------
# Step 1 — Prerequisites
# ---------------------------------------------------------------------------

Write-Step 1 "Checking prerequisites"

Require-Command 'az'
Require-Command 'dotnet'
Require-Command 'node'
Require-Command 'npm'

# Ensure Bicep is installed as az extension
az bicep install 2>&1 | Out-Null
Write-Host "  az CLI   : $(az version --query '\"azure-cli\"' -o tsv)"
Write-Host "  Bicep    : $(az bicep version --query 'bicepVersion' -o tsv 2>$null)"
Write-Host "  .NET     : $(dotnet --version)"
Write-Host "  Node     : $(node --version)"

# ---------------------------------------------------------------------------
# Step 2 — Login / subscription
# ---------------------------------------------------------------------------

Write-Step 2 "Azure authentication"

$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "  Not logged in — running az login..."
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
# Step 3 — Resource group
# ---------------------------------------------------------------------------

Write-Step 3 "Resource group: $ResourceGroup"

$rgExists = az group exists --name $ResourceGroup | ConvertFrom-Json
if (-not $rgExists) {
    Write-Host "  Creating resource group..."
    az group create --name $ResourceGroup --location $Location | Out-Null
    Write-Host "  Created: $ResourceGroup in $Location"
} else {
    Write-Host "  Already exists"
}

# ---------------------------------------------------------------------------
# Step 4 — Bicep deployment
# ---------------------------------------------------------------------------

if (-not $SkipBicep) {
    Write-Step 4 "Bicep infrastructure deployment"

    # Collect secure params interactively if not supplied
    if (-not $PostgresPassword) {
        $pgSecure  = Read-Host "  Enter PostgreSQL admin password" -AsSecureString
        $PostgresPassword = Get-SecureStringPlainText $pgSecure
    }
    if (-not $JwtSecret) {
        $jwtSecure = Read-Host "  Enter JWT secret key (min 32 chars)" -AsSecureString
        $JwtSecret = Get-SecureStringPlainText $jwtSecure
    }
    if ($JwtSecret.Length -lt 32) {
        throw "JWT secret must be at least 32 characters"
    }

    $bicepMain   = Join-Path $InfraRoot 'main.bicep'
    $bicepParams = Join-Path $InfraRoot 'main.bicepparam'

    Write-Host "  Running what-if first..."
    az deployment group what-if `
        --resource-group $ResourceGroup `
        --template-file $bicepMain `
        --parameters $bicepParams `
        --parameters postgresAdminPassword=$PostgresPassword `
        --parameters jwtSecretKey=$JwtSecret `
        --no-pretty-print 2>&1 | Select-Object -Last 20

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
    $swaName = az staticwebapp list --resource-group $ResourceGroup --query '[0].name' -o tsv
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
        $swaName = az staticwebapp list --resource-group $ResourceGroup --query '[0].name' -o tsv
        $script:StaticWebAppToken = az staticwebapp secrets list --name $swaName --query 'properties.apiKey' -o tsv
        Write-Host "    API URL   : $($script:ApiUrl)"
        Write-Host "    Frontend  : $($script:FrontendUrl)"
    } else {
        Write-Host "  No prior deployment found — some steps may fail without outputs"
    }
}

# ---------------------------------------------------------------------------
# Step 5 — Add local IP to PostgreSQL firewall for migrations
# ---------------------------------------------------------------------------

if (-not $SkipMigrations) {
    Write-Step 5 "Opening PostgreSQL firewall for local migrations"

    $myIp = (Invoke-RestMethod 'https://api.ipify.org?format=json').ip
    Write-Host "  Local IP: $myIp"

    # Find the PostgreSQL server in the resource group
    $pgServer = az postgres flexible-server list `
        --resource-group $ResourceGroup `
        --query '[0].name' -o tsv

    if (-not $pgServer) { throw "No PostgreSQL server found in $ResourceGroup" }

    az postgres flexible-server firewall-rule create `
        --resource-group $ResourceGroup `
        --name $pgServer `
        --rule-name "LocalMigrations-$(Get-Date -Format 'yyyyMMddHHmm')" `
        --start-ip-address $myIp `
        --end-ip-address $myIp | Out-Null
    Write-Host "  Firewall rule added for $myIp → $pgServer"

    # ---------------------------------------------------------------------------
    # Step 6 — EF Core migrations
    # ---------------------------------------------------------------------------

    Write-Step 6 "Running EF Core migrations"

    $dbPassword = if ($PostgresPassword) { $PostgresPassword } else {
        $pgSecure = Read-Host "  Enter PostgreSQL admin password for migrations" -AsSecureString
        Get-SecureStringPlainText $pgSecure
    }

    $migrationsConnStr = "Host=$($script:DbServerFqdn);Database=lawgate_db;Username=lawgate_admin;Password=$dbPassword;SslMode=Require;TrustServerCertificate=false"
    $env:ConnectionStrings__DefaultConnection = $migrationsConnStr

    Push-Location (Join-Path $BackendDir 'LegalDocSystem.API')
    try {
        dotnet ef database update --no-build 2>&1
        if ($LASTEXITCODE -ne 0) {
            # No release build present — build first
            Write-Host "  Build required before migrations..."
            dotnet build --configuration Release --no-restore 2>&1 | Where-Object { $_ -notmatch '^Build succeeded' }
            dotnet ef database update 2>&1
        }
        Write-Host "  Migrations applied successfully" -ForegroundColor Green
    } finally {
        Pop-Location
        Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
    }

    # Remove the temporary firewall rule
    az postgres flexible-server firewall-rule delete `
        --resource-group $ResourceGroup `
        --name $pgServer `
        --rule-name "LocalMigrations-$(Get-Date -Format 'yyyyMMddHHmm')" `
        --yes 2>$null
    Write-Host "  Temporary firewall rule removed"
} else {
    Write-Step "5+6" "Skipping migrations (--SkipMigrations)"
}

# ---------------------------------------------------------------------------
# Step 7 — Deploy backend via zip-deploy
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
# Step 8 — Build and deploy frontend
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
        npm run build 2>&1 | Where-Object { $_ -match 'error|warning|built in' }

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
            Write-Host "  Or push to GitHub — the CI workflow uses:" -ForegroundColor Yellow
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
