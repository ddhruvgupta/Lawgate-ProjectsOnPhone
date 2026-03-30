# Deployment

## Target Architecture

```
Internet → Azure Front Door (optional CDN)
  ├─ Azure Static Web Apps  → React SPA (built with Vite)
  └─ Azure App Service      → ASP.NET Core API
       └─ Azure Database for PostgreSQL (Flexible Server)
            Azure Key Vault  → secrets
            Application Insights → monitoring
```

---

## Production Dockerfiles

The repo contains two Dockerfiles:

- `backend/Dockerfile` — multi-stage production build for the API
- `backend/Dockerfile.dev` — development image used by Docker Compose (hot reload via `dotnet watch`)

The frontend does not have a production Dockerfile because it is a static site built with `npm run build` and deployed to Azure Static Web Apps or Blob Storage.

---

## Building production images

```bash
# Backend
docker build -t lawgate-api:latest ./backend -f ./backend/Dockerfile

# Frontend (build assets, then deploy static files — no Docker needed)
cd frontend
npm ci
npm run build       # outputs to frontend/dist/
```

---

## Azure Resources Required

1. Resource Group
2. Azure App Service Plan (Linux, B1 or higher)
3. Azure Web App (.NET 10 runtime)
4. Azure Database for PostgreSQL Flexible Server (PostgreSQL 16)
5. Azure Key Vault (for secrets)
6. Azure Static Web Apps or Storage Account (frontend)
7. Application Insights (monitoring)

---

## Step-by-step Azure deployment

### 1. Create resource group and PostgreSQL

```powershell
$rg = "lawgate-rg"
$location = "centralindia"

az group create --name $rg --location $location

az postgres flexible-server create `
  --resource-group $rg `
  --name lawgate-db `
  --location $location `
  --admin-user lawgate_admin `
  --admin-password "<strong-password>" `
  --sku-name Standard_B1ms `
  --tier Burstable `
  --storage-size 32 `
  --version 16 `
  --public-access 0.0.0.0

az postgres flexible-server db create `
  --resource-group $rg `
  --server-name lawgate-db `
  --database-name lawgate_db
```

### 2. Create Key Vault and store secrets

```powershell
$kv = "lawgate-kv-$(Get-Random)"

az keyvault create --name $kv --resource-group $rg --location $location

az keyvault secret set --vault-name $kv --name "DbConnectionString" `
  --value "Host=lawgate-db.postgres.database.azure.com;Database=lawgate_db;Username=lawgate_admin;Password=<password>;SslMode=Require"

az keyvault secret set --vault-name $kv --name "JwtSecretKey" `
  --value "<random-32-char-secret>"
```

### 3. Create App Service and configure

```powershell
$appName = "lawgate-api-$(Get-Random)"

az appservice plan create --name lawgate-plan --resource-group $rg `
  --location $location --sku B1 --is-linux

az webapp create --name $appName --resource-group $rg `
  --plan lawgate-plan --runtime "DOTNET|10.0"

# Enable managed identity
az webapp identity assign --name $appName --resource-group $rg

# Grant Key Vault access to the managed identity
$principalId = az webapp identity show --name $appName --resource-group $rg `
  --query principalId -o tsv

az keyvault set-policy --name $kv --object-id $principalId `
  --secret-permissions get list

# Configure app settings (Key Vault references)
az webapp config appsettings set --name $appName --resource-group $rg --settings `
  "ConnectionStrings__DefaultConnection=@Microsoft.KeyVault(SecretUri=https://$kv.vault.azure.net/secrets/DbConnectionString/)" `
  "Jwt__SecretKey=@Microsoft.KeyVault(SecretUri=https://$kv.vault.azure.net/secrets/JwtSecretKey/)" `
  "Jwt__Issuer=lawgate-api" `
  "Jwt__Audience=lawgate-client" `
  "ASPNETCORE_ENVIRONMENT=Production"
```

### 4. Apply database migrations

Run migrations from your local machine targeting the Azure database:

```bash
cd backend
ConnectionStrings__DefaultConnection="<azure-connection-string>" \
  dotnet ef database update \
  --project LegalDocSystem.Infrastructure \
  --startup-project LegalDocSystem.API
```

Or include migration execution in the deployment pipeline. **Do not use `DbSeeder` in production** — it is guarded by `IsDevelopment()` in `Program.cs`.

### 5. Deploy backend

```powershell
cd backend
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip -Force

az webapp deployment source config-zip `
  --resource-group $rg --name $appName --src ./deploy.zip
```

### 6. Deploy frontend

```bash
cd frontend
npm ci
VITE_API_URL="https://$appName.azurewebsites.net/api" npm run build
```

**Option A — Azure Static Web Apps (recommended):**
```powershell
az staticwebapp create --name lawgate-frontend --resource-group $rg `
  --location $location

# Deploy via GitHub Actions (SWA generates a workflow automatically)
# Or use the SWA CLI: swa deploy ./dist
```

**Option B — Blob Storage + CDN:**
```powershell
$sa = "lawgatesa$(Get-Random)"
az storage account create --name $sa --resource-group $rg --sku Standard_LRS --kind StorageV2
az storage blob service-properties update --account-name $sa --static-website `
  --index-document index.html --404-document index.html
az storage blob upload-batch --account-name $sa --destination '$web' --source ./dist
```

---

## Environment Variables (Production)

| Key | Source |
|-----|--------|
| `ConnectionStrings__DefaultConnection` | Key Vault reference |
| `Jwt__SecretKey` | Key Vault reference |
| `Jwt__Issuer` | App setting: `lawgate-api` |
| `Jwt__Audience` | App setting: `lawgate-client` |
| `ConnectionStrings__AzureStorage` | App setting or Key Vault |
| `ASPNETCORE_ENVIRONMENT` | App setting: `Production` |

In `Production`, `Program.cs` does **not** run `DbSeeder` and does **not** expose `TestController` endpoints.

---

## GitHub Actions CI/CD (planned)

Create `.github/workflows/backend.yml`:

```yaml
name: Deploy Backend

on:
  push:
    branches: [main]
    paths: ['backend/**']

env:
  DOTNET_VERSION: '10.0.x'
  AZURE_WEBAPP_NAME: lawgate-api

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - run: dotnet restore
        working-directory: backend
      - run: dotnet test backend/ --no-restore
      - run: dotnet publish backend/LegalDocSystem.API -c Release -o publish
      - uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: publish
```

---

## Cost Estimates

| Environment | Resources | Monthly (approx.) |
|-------------|-----------|-------------------|
| Dev/Staging | PostgreSQL B1ms + App Service B1 + Static Web App Free | ~$30 |
| Production (small) | PostgreSQL D2s_v3 + App Service P1v2 + Static Web App Standard | ~$250–300 |

---

## Pre-production Checklist

- [ ] Remove/disable `TestController` (already done — it's dev-only, but verify)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] All secrets in Key Vault; none in app settings or code
- [ ] HTTPS-only enforced on App Service
- [ ] CORS restricted to production frontend origin
- [ ] Database migrations applied; seed data removed
- [ ] Application Insights configured
- [ ] Backup policy set on PostgreSQL (7-day retention minimum)
- [ ] Health check endpoint verified: `GET /health`
