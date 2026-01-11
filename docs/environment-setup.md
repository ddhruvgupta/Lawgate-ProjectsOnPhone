# Environment Setup Guide

## Local Development Environment

### Windows Prerequisites

#### 1. Install .NET 8 SDK
```powershell
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0

# Verify installation
dotnet --version  # Should show 8.0.x
```

#### 2. Install Node.js & npm
```powershell
# Download from: https://nodejs.org/ (LTS version 20+)

# Verify installation
node --version  # Should show v20.x.x
npm --version   # Should show 10.x.x
```

#### 3. Install Docker Desktop
```powershell
# Download from: https://www.docker.com/products/docker-desktop

# Verify installation
docker --version
docker-compose --version
```

#### 4. Install PostgreSQL Tools (Optional)
```powershell
# Download from: https://www.postgresql.org/download/windows/

# Or use chocolatey
choco install postgresql16 --params '/Password:postgres'

# Verify
psql --version
```

#### 5. Install Entity Framework Tools
```powershell
dotnet tool install --global dotnet-ef

# Verify
dotnet ef --version
```

#### 6. Install Azure CLI (for deployment)
```powershell
# Download from: https://aka.ms/installazurecliwindows

# Verify
az --version
```

### VS Code Extensions

Install these extensions for optimal development experience:

```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.csdevkit",
    "ms-azuretools.vscode-docker",
    "ms-vscode.powershell",
    "dbaeumer.vscode-eslint",
    "esbenp.prettier-vscode",
    "bradlc.vscode-tailwindcss",
    "ms-vscode.vscode-typescript-next",
    "GitHub.copilot",
    "GitHub.copilot-chat"
  ]
}
```

Save as `.vscode/extensions.json` in project root.

## Project Setup

### 1. Clone Repository
```powershell
git clone <repository-url>
cd Lawgate-ProjectsOnPhone
```

### 2. Setup Database
```powershell
# Start PostgreSQL via Docker
docker-compose up -d postgres

# Wait for database to be ready
Start-Sleep -Seconds 10

# Run database recreation script
cd database
./recreate-database.ps1

# Or manually:
cd ../backend
dotnet ef database update
```

### 3. Setup Backend
```powershell
cd backend

# Restore packages
dotnet restore

# Configure user secrets (development only)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=lawgate_db;Username=lawgate_user;Password=lawgate_dev_password_change_in_production"
dotnet user-secrets set "Jwt:Key" "your-super-secret-jwt-key-at-least-32-characters-long-for-security"
dotnet user-secrets set "Jwt:Issuer" "lawgate-api"
dotnet user-secrets set "Jwt:Audience" "lawgate-client"

# Build
dotnet build

# Run
dotnet run
# API will be available at: http://localhost:5000
# Swagger UI at: http://localhost:5000/swagger
```

### 4. Setup Frontend
```powershell
cd frontend

# Install dependencies
npm install

# Create environment file
Copy-Item .env.example .env.local

# Edit .env.local and set:
# VITE_API_URL=http://localhost:5000/api

# Run development server
npm run dev
# Frontend will be available at: http://localhost:3000
```

### 5. Verify Setup
```powershell
# Check all containers
docker ps

# Test backend
curl http://localhost:5000/api/health
# Or open: http://localhost:5000/swagger

# Test frontend
# Open browser: http://localhost:3000
```

## Environment Files

### Backend: appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=lawgate_db;Username=lawgate_user;Password=lawgate_dev_password_change_in_production"
  },
  "Jwt": {
    "Key": "development-key-change-in-production-must-be-at-least-32-characters",
    "Issuer": "lawgate-api",
    "Audience": "lawgate-client",
    "ExpiryMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:5173"]
  }
}
```

⚠️ **Note**: Use User Secrets instead of committing this file!

### Frontend: .env.local
```env
VITE_API_URL=http://localhost:5000/api
VITE_APP_NAME=Lawgate
VITE_APP_VERSION=1.0.0
```

### Frontend: .env.production
```env
VITE_API_URL=https://api.lawgate.com/api
VITE_APP_NAME=Lawgate
VITE_APP_VERSION=1.0.0
```

## Common Development Tasks

### Starting Everything
```powershell
# Option 1: Use Docker Compose (recommended)
docker-compose up -d

# Option 2: Manual
# Terminal 1 - Database
docker-compose up postgres

# Terminal 2 - Backend
cd backend
dotnet watch run

# Terminal 3 - Frontend
cd frontend
npm run dev
```

### Stopping Everything
```powershell
# Stop Docker containers
docker-compose down

# Or stop without removing volumes
docker-compose stop
```

### Resetting Database
```powershell
# Complete reset
cd database
./recreate-database.ps1

# Or manually
docker-compose down -v
docker-compose up -d postgres
cd backend
dotnet ef database update
```

### Updating Dependencies

#### Backend
```powershell
cd backend
dotnet list package --outdated
dotnet add package <PackageName>
dotnet restore
```

#### Frontend
```powershell
cd frontend
npm outdated
npm update
npm install
```

### Creating New Migrations
```powershell
cd backend

# Add migration
dotnet ef migrations add <MigrationName>

# Apply migration
dotnet ef database update

# Rollback
dotnet ef database update <PreviousMigrationName>

# Remove last migration (if not applied)
dotnet ef migrations remove
```

## Troubleshooting

### Port Already in Use
```powershell
# Find process using port 5000
netstat -ano | findstr :5000

# Kill process
taskkill /PID <process-id> /F
```

### Docker Issues
```powershell
# Restart Docker Desktop

# Remove all containers and volumes
docker-compose down -v

# Rebuild containers
docker-compose build --no-cache
docker-compose up -d
```

### Database Connection Issues
```powershell
# Check if PostgreSQL is running
docker ps | findstr postgres

# Check logs
docker logs lawgate-postgres

# Connect to database
docker exec -it lawgate-postgres psql -U lawgate_user -d lawgate_db

# Inside psql:
\dt  # List tables
\d users  # Describe users table
\q  # Quit
```

### Frontend Build Issues
```powershell
# Clear cache
Remove-Item -Recurse -Force node_modules
Remove-Item package-lock.json
npm install

# Clear Vite cache
Remove-Item -Recurse -Force node_modules/.vite
```

### Backend Build Issues
```powershell
# Clean and rebuild
dotnet clean
Remove-Item -Recurse -Force bin, obj
dotnet restore
dotnet build
```

## Testing

### Backend Tests
```powershell
cd backend/Backend.Tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true
```

### Frontend Tests
```powershell
cd frontend
npm test

# With coverage
npm run test:coverage

# E2E tests
npm run test:e2e
```

## Git Workflow

### Initial Commit
```powershell
git init
git add .
git commit -m "Initial project setup"
git branch -M main
git remote add origin <repository-url>
git push -u origin main
```

### Development Workflow
```powershell
# Create feature branch
git checkout -b feature/new-feature

# Make changes, commit
git add .
git commit -m "Add new feature"

# Push to remote
git push origin feature/new-feature

# Create pull request on GitHub
```

## VS Code Workspace Settings

Create `.vscode/settings.json`:

```json
{
  "editor.formatOnSave": true,
  "editor.defaultFormatter": "esbenp.prettier-vscode",
  "editor.codeActionsOnSave": {
    "source.fixAll.eslint": true
  },
  "[csharp]": {
    "editor.defaultFormatter": "ms-dotnettools.csharp"
  },
  "[typescript]": {
    "editor.defaultFormatter": "esbenp.prettier-vscode"
  },
  "[typescriptreact]": {
    "editor.defaultFormatter": "esbenp.prettier-vscode"
  },
  "files.exclude": {
    "**/node_modules": true,
    "**/bin": true,
    "**/obj": true
  },
  "search.exclude": {
    "**/node_modules": true,
    "**/bin": true,
    "**/obj": true
  },
  "tailwindCSS.experimental.classRegex": [
    ["clsx\\(([^)]*)\\)", "(?:'|\"|`)([^']*)(?:'|\"|`)"]
  ]
}
```

## Default Test Users

After running `recreate-database.ps1`, these users are available:

- **Admin**: 
  - Email: admin@lawgate.com
  - Password: Admin@123

- **User**: 
  - Email: user@lawgate.com
  - Password: User@123

⚠️ **Note**: These are deleted automatically in production!

## Performance Tips

### Development
- Use `dotnet watch run` for backend hot reload
- Use `npm run dev` for frontend HMR
- Keep Docker Desktop resource limits reasonable (4GB RAM, 2 CPUs minimum)

### Production
- Use Release configuration: `dotnet publish -c Release`
- Optimize frontend build: `npm run build`
- Enable response compression
- Use CDN for static assets

## Security Reminders

1. ❌ **Never commit** `.env.local` or `appsettings.Development.json` with real secrets
2. ✅ **Use** User Secrets for local development
3. ✅ **Use** Azure Key Vault for production
4. ❌ **Never** expose database port (5432) to internet
5. ✅ **Always** use HTTPS in production
6. ✅ **Rotate** secrets regularly

## Next Steps

After setup is complete:
1. ✅ Test login with default users
2. ✅ Verify Swagger documentation works
3. ✅ Check frontend connects to backend
4. ✅ Create your first migration
5. ✅ Deploy to Azure (see `azure-deployment.md`)

## Resources

- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [React Documentation](https://react.dev/)
- [Vite Documentation](https://vitejs.dev/)
- [Tailwind CSS](https://tailwindcss.com/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Docker Documentation](https://docs.docker.com/)
