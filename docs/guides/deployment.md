# Azure Deployment Guide

## Overview
Complete guide for deploying the Lawgate application to Azure with production-ready configurations.

## Architecture

```
Internet → Azure Front Door (CDN)
  ├─→ Azure Static Web Apps (Frontend)
  │   └─→ React SPA
  └─→ Azure App Service (Backend)
      └─→ .NET API
          └─→ Azure Database for PostgreSQL
```

## Prerequisites

### Azure Resources Needed
1. Resource Group
2. Azure App Service (Backend)
3. Azure Static Web Apps or Blob Storage (Frontend)
4. Azure Database for PostgreSQL Flexible Server
5. Azure Key Vault (Secrets)
6. Application Insights (Monitoring)
7. Azure Front Door (Optional - CDN/Load Balancer)

### Tools Required
- Azure CLI (`az`)
- Azure Developer CLI (`azd`)
- PowerShell
- Git

## Step 1: Setup Azure CLI

### Install Azure CLI
```powershell
# Download and install from: https://aka.ms/installazurecliwindows

# Verify installation
az --version

# Login
az login

# Set subscription
az account list --output table
az account set --subscription "<subscription-id>"
```

## Step 2: Create Resource Group

```powershell
# Variables
$resourceGroup = "lawgate-rg"
$location = "westus2"  # or eastus, northeurope, etc.

# Create resource group
az group create --name $resourceGroup --location $location
```

## Step 3: Deploy PostgreSQL Database

### Create Database Server
```powershell
# Variables
$dbServerName = "lawgate-db-server"
$dbName = "lawgate_db"
$dbAdmin = "lawgate_admin"
$dbPassword = "<generate-strong-password>"  # Use: pwgen or password manager

# Create PostgreSQL Flexible Server
az postgres flexible-server create `
  --resource-group $resourceGroup `
  --name $dbServerName `
  --location $location `
  --admin-user $dbAdmin `
  --admin-password $dbPassword `
  --sku-name Standard_B1ms `
  --tier Burstable `
  --storage-size 32 `
  --version 16 `
  --public-access 0.0.0.0 `
  --high-availability Disabled

# Create database
az postgres flexible-server db create `
  --resource-group $resourceGroup `
  --server-name $dbServerName `
  --database-name $dbName

# Configure firewall (allow Azure services)
az postgres flexible-server firewall-rule create `
  --resource-group $resourceGroup `
  --name $dbServerName `
  --rule-name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0
```

### Connection String
```
Host=lawgate-db-server.postgres.database.azure.com;
Database=lawgate_db;
Username=lawgate_admin;
Password=<your-password>;
SslMode=Require;
TrustServerCertificate=true
```

## Step 4: Create Key Vault (Secrets Management)

```powershell
$keyVaultName = "lawgate-kv-$(Get-Random)"  # Must be globally unique

# Create Key Vault
az keyvault create `
  --name $keyVaultName `
  --resource-group $resourceGroup `
  --location $location `
  --enable-rbac-authorization false

# Store secrets
az keyvault secret set `
  --vault-name $keyVaultName `
  --name "DatabaseConnectionString" `
  --value "Host=lawgate-db-server.postgres.database.azure.com;Database=lawgate_db;Username=lawgate_admin;Password=$dbPassword;SslMode=Require"

az keyvault secret set `
  --vault-name $keyVaultName `
  --name "JwtSecret" `
  --value "<generate-strong-jwt-secret-32chars>"
```

## Step 5: Deploy Backend (App Service)

### Create App Service Plan
```powershell
$appServicePlan = "lawgate-plan"
$webAppName = "lawgate-api-$(Get-Random)"  # Must be globally unique

# Create App Service Plan (Linux)
az appservice plan create `
  --name $appServicePlan `
  --resource-group $resourceGroup `
  --location $location `
  --sku B1 `
  --is-linux

# Create Web App
az webapp create `
  --name $webAppName `
  --resource-group $resourceGroup `
  --plan $appServicePlan `
  --runtime "DOTNET|8.0"
```

### Configure App Settings
```powershell
# Get Key Vault secrets as references
$dbConnStringRef = "@Microsoft.KeyVault(SecretUri=https://$keyVaultName.vault.azure.net/secrets/DatabaseConnectionString/)"
$jwtSecretRef = "@Microsoft.KeyVault(SecretUri=https://$keyVaultName.vault.azure.net/secrets/JwtSecret/)"

# Configure app settings
az webapp config appsettings set `
  --name $webAppName `
  --resource-group $resourceGroup `
  --settings `
    "ConnectionStrings__DefaultConnection=$dbConnStringRef" `
    "Jwt__Key=$jwtSecretRef" `
    "Jwt__Issuer=lawgate-api" `
    "Jwt__Audience=lawgate-client" `
    "Jwt__ExpiryMinutes=60" `
    "ASPNETCORE_ENVIRONMENT=Production"

# Enable managed identity
az webapp identity assign `
  --name $webAppName `
  --resource-group $resourceGroup

# Grant Key Vault access to managed identity
$principalId = az webapp identity show --name $webAppName --resource-group $resourceGroup --query principalId -o tsv

az keyvault set-policy `
  --name $keyVaultName `
  --object-id $principalId `
  --secret-permissions get list
```

### Deploy Backend Code
```powershell
# From project root, navigate to backend
cd backend

# Publish
dotnet publish -c Release -o ./publish

# Create deployment ZIP
Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip -Force

# Deploy
az webapp deployment source config-zip `
  --resource-group $resourceGroup `
  --name $webAppName `
  --src ./deploy.zip

# Apply database migrations (run locally targeting Azure DB)
$env:ConnectionStrings__DefaultConnection = "<azure-connection-string>"
dotnet ef database update

# Or connect and run migrations via Azure CLI
```

## Step 6: Deploy Frontend (Static Web App)

### Option A: Azure Static Web Apps (Recommended)

```powershell
$staticWebAppName = "lawgate-frontend"

# Create Static Web App
az staticwebapp create `
  --name $staticWebAppName `
  --resource-group $resourceGroup `
  --location $location `
  --source . `
  --branch main `
  --app-location "/frontend" `
  --output-location "dist" `
  --api-location "/backend"  # Optional: Azure Functions API

# Configure environment variables
az staticwebapp appsettings set `
  --name $staticWebAppName `
  --setting-names "VITE_API_URL=https://$webAppName.azurewebsites.net/api"
```

### Option B: Blob Storage + CDN (Cost-effective)

```powershell
$storageAccount = "lawgatesa$(Get-Random)"
$cdnProfile = "lawgate-cdn"
$cdnEndpoint = "lawgate"

# Create storage account
az storage account create `
  --name $storageAccount `
  --resource-group $resourceGroup `
  --location $location `
  --sku Standard_LRS `
  --kind StorageV2

# Enable static website hosting
az storage blob service-properties update `
  --account-name $storageAccount `
  --static-website `
  --index-document index.html `
  --404-document index.html

# Build frontend
cd ../frontend
npm install
npm run build

# Upload to blob storage
az storage blob upload-batch `
  --account-name $storageAccount `
  --destination '$web' `
  --source ./dist

# Get website URL
$websiteUrl = az storage account show `
  --name $storageAccount `
  --resource-group $resourceGroup `
  --query "primaryEndpoints.web" `
  --output tsv

Write-Host "Frontend URL: $websiteUrl"
```

## Step 7: Configure Custom Domain (Optional)

### For App Service
```powershell
# Add custom domain
az webapp config hostname add `
  --webapp-name $webAppName `
  --resource-group $resourceGroup `
  --hostname "api.yourdomain.com"

# Enable HTTPS
az webapp config ssl bind `
  --name $webAppName `
  --resource-group $resourceGroup `
  --certificate-thumbprint <thumbprint> `
  --ssl-type SNI
```

### For Static Web App
```powershell
# Add custom domain
az staticwebapp hostname set `
  --name $staticWebAppName `
  --resource-group $resourceGroup `
  --hostname "www.yourdomain.com"
```

## Step 8: Setup Monitoring

### Application Insights
```powershell
$appInsightsName = "lawgate-insights"

# Create Application Insights
az monitor app-insights component create `
  --app $appInsightsName `
  --location $location `
  --resource-group $resourceGroup `
  --application-type web

# Get instrumentation key
$instrumentationKey = az monitor app-insights component show `
  --app $appInsightsName `
  --resource-group $resourceGroup `
  --query instrumentationKey `
  --output tsv

# Configure backend to use App Insights
az webapp config appsettings set `
  --name $webAppName `
  --resource-group $resourceGroup `
  --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=$instrumentationKey"
```

## Step 9: Configure CORS (Backend)

```powershell
# Allow frontend origin
az webapp cors add `
  --name $webAppName `
  --resource-group $resourceGroup `
  --allowed-origins "https://$staticWebAppName.azurestaticapps.net"
```

## Step 10: Setup CI/CD (GitHub Actions)

### Backend CI/CD
Create `.github/workflows/backend-deploy.yml`:

```yaml
name: Deploy Backend to Azure

on:
  push:
    branches: [main]
    paths:
      - 'backend/**'

env:
  AZURE_WEBAPP_NAME: lawgate-api
  DOTNET_VERSION: '8.0.x'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Restore dependencies
        run: dotnet restore
        working-directory: ./backend
      
      - name: Build
        run: dotnet build --configuration Release --no-restore
        working-directory: ./backend
      
      - name: Publish
        run: dotnet publish -c Release -o ./publish
        working-directory: ./backend
      
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./backend/publish
```

### Frontend CI/CD
Create `.github/workflows/frontend-deploy.yml`:

```yaml
name: Deploy Frontend to Azure

on:
  push:
    branches: [main]
    paths:
      - 'frontend/**'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Node
        uses: actions/setup-node@v3
        with:
          node-version: '20'
      
      - name: Install dependencies
        run: npm ci
        working-directory: ./frontend
      
      - name: Build
        run: npm run build
        working-directory: ./frontend
        env:
          VITE_API_URL: https://lawgate-api.azurewebsites.net/api
      
      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "/frontend"
          output_location: "dist"
```

## Cost Estimation (Monthly)

### Development/Testing
- PostgreSQL Flexible Server (B1ms): ~$16
- App Service (B1): ~$13
- Static Web App (Free tier): $0
- Key Vault: $0.03 per 10k operations
- **Total: ~$30/month**

### Production (Small Scale)
- PostgreSQL (D2s_v3): ~$150
- App Service (P1v2): ~$80
- Static Web App (Standard): $9
- Application Insights: ~$5-20
- Azure Front Door: ~$35 + data transfer
- **Total: ~$280-320/month**

## Scaling Strategy

### Vertical Scaling
```powershell
# Scale up App Service
az appservice plan update `
  --name $appServicePlan `
  --resource-group $resourceGroup `
  --sku P1v3

# Scale up Database
az postgres flexible-server update `
  --name $dbServerName `
  --resource-group $resourceGroup `
  --sku-name Standard_D4s_v3
```

### Horizontal Scaling
```powershell
# Scale out App Service instances
az appservice plan update `
  --name $appServicePlan `
  --resource-group $resourceGroup `
  --number-of-workers 3

# Enable autoscaling
az monitor autoscale create `
  --resource-group $resourceGroup `
  --resource $webAppName `
  --resource-type Microsoft.Web/sites `
  --name autoscale-rules `
  --min-count 1 `
  --max-count 5 `
  --count 2
```

## Troubleshooting

### Backend not starting
```powershell
# Check logs
az webapp log tail `
  --name $webAppName `
  --resource-group $resourceGroup

# Check app settings
az webapp config appsettings list `
  --name $webAppName `
  --resource-group $resourceGroup
```

### Database connection issues
- Verify firewall rules allow App Service
- Check connection string format
- Ensure SSL is enabled (`SslMode=Require`)
- Verify managed identity has Key Vault access

### Frontend not loading
- Check CORS settings on backend
- Verify `VITE_API_URL` is correct
- Check browser console for errors
- Verify static website hosting is enabled

## Security Checklist

- [ ] Enable HTTPS only on App Service
- [ ] Configure custom domain with SSL certificate
- [ ] Use managed identities instead of connection strings
- [ ] Store all secrets in Key Vault
- [ ] Configure network security groups
- [ ] Enable Azure DDoS Protection
- [ ] Set up Azure AD authentication for admin access
- [ ] Enable diagnostic logging
- [ ] Configure backup policies
- [ ] Implement rate limiting
- [ ] Review and restrict CORS origins
- [ ] Enable Application Gateway WAF (Web Application Firewall)

## Maintenance Tasks

### Database Backups
```powershell
# Configure automated backups
az postgres flexible-server update `
  --name $dbServerName `
  --resource-group $resourceGroup `
  --backup-retention 7  # days

# Manual backup
az postgres flexible-server backup create `
  --name $dbServerName `
  --resource-group $resourceGroup `
  --backup-name manual-backup-$(Get-Date -Format 'yyyyMMdd')
```

### Update Dependencies
```powershell
# Backend
cd backend
dotnet list package --outdated
dotnet add package <PackageName>

# Frontend
cd frontend
npm outdated
npm update
```

## Documentation
- Azure Portal: https://portal.azure.com
- Azure CLI Docs: https://docs.microsoft.com/cli/azure/
- App Service: https://docs.microsoft.com/azure/app-service/
- Static Web Apps: https://docs.microsoft.com/azure/static-web-apps/
