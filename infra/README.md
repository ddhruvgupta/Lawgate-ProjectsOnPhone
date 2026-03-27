# Lawgate — Azure Infrastructure (Bicep)

All infrastructure-as-code for deploying Lawgate to Azure.

## What gets created

| Resource | Details |
|---|---|
| **App Service Plan** | Linux B1 (upgrade to P1v3 for production load) |
| **App Service** | .NET 10 on Linux, managed identity, Key Vault refs |
| **Azure Static Web Apps** | Free tier — CI/CD via GitHub Actions |
| **Azure Database for PostgreSQL** | Flexible Server 16, Burstable B1ms |
| **Azure Blob Storage** | Standard LRS, soft delete, versioning |
| **Azure Key Vault** | RBAC mode, stores 3 secrets |
| **Log Analytics Workspace** | 30-day retention |
| **Application Insights** | Workspace-based, linked to Log Analytics |
| **Azure External ID** | App registrations + user flow (via `setup-external-id.ps1`) |

## File structure

```
infra/
  main.bicep              ← Entry point — orchestrates all modules
  main.bicepparam         ← Parameters (edit non-sensitive values here)
  modules/
    monitoring.bicep      ← Log Analytics + App Insights
    storage.bicep         ← Blob Storage + legal-documents container
    database.bicep        ← PostgreSQL Flexible Server
    keyvault.bicep        ← Key Vault + RBAC + 3 secrets
    app-service.bicep     ← App Service Plan + Web App (.NET 10)
    static-web-app.bicep  ← Frontend Static Web App
  scripts/
    deploy.ps1            ← Full deployment orchestrator
    setup-external-id.ps1 ← External ID tenant config + app registrations
```

## Prerequisites

```powershell
# Azure CLI
winget install Microsoft.AzureCLI

# Bicep (installed automatically by deploy.ps1, or manually)
az bicep install

# SWA CLI (for frontend deployment without GitHub Actions)
npm install -g @azure/static-web-apps-cli
```

## Step 1 — Edit parameters

Open `main.bicepparam` and update:
- `appName` — must be lowercase, 3-10 chars
- `location` — Azure region (e.g. `centralindia` for Indian data residency)
- `staticWebAppLocation` — one of: `centralus`, `eastus2`, `eastasia`, `westeurope`, `westus2`

Leave `externalIdTenantId` and `externalIdApiClientId` empty for the first deployment.

## Step 2 — Deploy infrastructure

```powershell
cd infra/scripts
.\deploy.ps1 -ResourceGroup lawgate-rg -Location centralindia
```

The script will:
1. Create the resource group
2. Run `az deployment group what-if` so you can review changes
3. Prompt for the Postgres password and JWT secret
4. Deploy all resources
5. Run EF Core migrations
6. Zip-deploy the .NET backend
7. Build and deploy the frontend

Secrets are never committed — they are passed at deploy time and stored in Key Vault. The App Service reads them via `@Microsoft.KeyVault()` references in app settings.

## Step 3 — Set up External ID

External ID external tenants must be created in the portal first:

1. Go to [https://entra.microsoft.com](https://entra.microsoft.com)
2. Click **"+"** → **Create a tenant** → select **"Customer (External)"**
3. Choose a subdomain name (e.g. `lawgate`) and region
4. Copy the **Tenant ID** from the Overview page

Then run the setup script:

```powershell
.\scripts\setup-external-id.ps1 `
    -ExternalTenantId "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" `
    -ApiBaseUrl       "https://lawgate-prod-api-abc123.azurewebsites.net" `
    -FrontendUrl      "https://lawgate-prod-frontend.azurestaticapps.net"
```

The script creates:
- **Lawgate API** app registration with 6 app roles (`CompanyOwner`, `Admin`, `User`, `Viewer`, `PlatformAdmin`, `PlatformSuperAdmin`)
- **Lawgate SPA** app registration with redirect URIs + admin consent
- A **sign-up / sign-in user flow**

It outputs config values to paste into `main.bicepparam`. Re-run `deploy.ps1` after that to push the External ID settings to the App Service.

## Step 4 — Frontend CI/CD via GitHub Actions

Static Web Apps use a deployment token rather than the zip workflow. Add the token output by `deploy.ps1` as a GitHub repository secret:

```
Settings → Secrets and variables → Actions → New repository secret
Name:  AZURE_STATIC_WEB_APPS_API_TOKEN
Value: <token from deploy output>
```

Create `.github/workflows/frontend.yml`:

```yaml
name: Deploy Frontend

on:
  push:
    branches: [main]
    paths: ['frontend/**']

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: upload
          app_location: /frontend
          output_location: dist
        env:
          VITE_API_URL: https://<your-api>.azurewebsites.net/api
```

## Key Vault secrets

| Secret name | Content |
|---|---|
| `DatabaseConnectionString` | PostgreSQL connection string |
| `JwtSecretKey` | JWT HS256 signing secret |
| `AzureStorageConnectionString` | Blob Storage connection string |

The App Service reads these via Key Vault references in app settings — the raw values are never in environment variables or logs.

## Upgrading for production

| Concern | Current | Recommended upgrade |
|---|---|---|
| App Service SKU | B1 | P1v3 / P2v3 |
| PostgreSQL SKU | Burstable B1ms | General Purpose D2s_v3 |
| PostgreSQL HA | Disabled | ZoneRedundant |
| Storage | Standard LRS | Standard ZRS |
| Key Vault purge protection | Disabled | Enabled |
| Key Vault network | Allow all | Deny + VNet service endpoint |
| Backend auth | Custom JWT | External ID token validation |
