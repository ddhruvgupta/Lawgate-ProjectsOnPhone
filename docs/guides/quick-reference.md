# Quick Reference Card

Keep this handy for common commands and tasks!

## 🚀 Starting Development

```powershell
# Start everything (recommended — all services in Docker)
docker-compose up -d
# API:     http://localhost:5059
# Swagger: http://localhost:5059/swagger
# App:     http://localhost:5174

# OR — run locally (outside Docker)

# Backend (Terminal 1)
cd backend
dotnet watch run --project LegalDocSystem.API
# API: http://localhost:5059
# Swagger: http://localhost:5059/swagger

# Frontend (Terminal 2)
cd frontend
npm run dev
# App: http://localhost:5173
```

## 🗄️ Database Commands

```powershell
# ⭐ RECREATE DATABASE (The Magic Command!)
cd database
./recreate-database.ps1

# Create new migration
cd backend
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback
dotnet ef database update PreviousMigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script -o migration.sql
```

## 🔧 Backend Commands

```powershell
# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run

# Hot reload (recommended)
dotnet watch run

# Clean build
dotnet clean
Remove-Item -Recurse -Force bin, obj
dotnet restore
dotnet build

# Run with seed data
dotnet run --seed-data

# User Secrets
dotnet user-secrets init
dotnet user-secrets set "Key" "Value"
dotnet user-secrets list
```

## 🎨 Frontend Commands

```powershell
# Install dependencies
npm install

# Development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Lint
npm run lint

# Type check
npm run type-check

# Clean install
Remove-Item -Recurse -Force node_modules
Remove-Item package-lock.json
npm install
```

## 🐳 Docker Commands

```powershell
# Start all services
docker-compose up -d

# Start specific service
docker-compose up -d postgres

# Stop all services
docker-compose down

# Stop and remove volumes (data will be lost!)
docker-compose down -v

# View logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f postgres

# Rebuild containers
docker-compose build --no-cache
docker-compose up -d

# Check running containers
docker ps

# Check all containers (including stopped)
docker ps -a

# Connect to PostgreSQL
docker exec -it lawgate-postgres psql -U lawgate_user -d lawgate_db
```

## 📊 PostgreSQL Commands

```powershell
# Connect to database
psql -h localhost -U lawgate_user -d lawgate_db
# Password: lawgate_dev_password_change_in_production

# Inside psql:
\dt                    # List all tables
\d table_name          # Describe table
\du                    # List users
\l                     # List databases
\c database_name       # Connect to database
\q                     # Quit

# Backup database
pg_dump -U lawgate_user -h localhost lawgate_db > backup.sql

# Restore database
psql -U lawgate_user -h localhost lawgate_db < backup.sql
```

## 🔐 Default Dev Users

All seeded users have `IsEmailVerified = true`.

| Email | Password | Role |
|-------|----------|------|
| `admin@demolawfirm.com` | `Admin@123` | CompanyOwner |
| `jane.doe@demolawfirm.com` | `User@123` | User |
| `admin@lawgate.io` | `LawgatePlatform@1` | PlatformAdmin |
| `superadmin@lawgate.io` | `LawgateSuperAdmin@1` | PlatformSuperAdmin |

⚠️ **These are for development only. Never use in production.**

## 🌐 API Endpoints

### Authentication
- `POST /api/auth/register` — Register new company + owner
- `POST /api/auth/login` — Login (returns access + refresh token)
- `POST /api/auth/refresh` — Refresh access token
- `GET  /api/auth/me` — Get current user
- `POST /api/auth/forgot-password` — Request password reset email
- `POST /api/auth/reset-password` — Reset password with token
- `POST /api/auth/verify-email` — Verify email with token
- `POST /api/auth/resend-verification` — Resend verification email

### Projects
- `GET    /api/projects` — List company projects
- `GET    /api/projects/{id}` — Get project
- `POST   /api/projects` — Create project
- `PUT    /api/projects/{id}` — Update project
- `DELETE /api/projects/{id}` — Delete project (Owner/Admin only)

### Documents
- `POST /api/documents/upload-url` — Generate SAS upload URL
- `POST /api/documents/{id}/confirm` — Confirm upload
- `GET  /api/documents/{id}` — Get document
- `GET  /api/documents/{id}/download-url` — Generate SAS download URL
- `GET  /api/documents/project/{projectId}` — List project documents
- `DELETE /api/documents/{id}` — Delete document

### Users
- `GET  /api/users` — List company users (Admin/Owner)
- `GET  /api/users/{id}` — Get user
- `POST /api/users` — Create user (Admin/Owner)
- `POST /api/users/{id}/toggle-status` — Activate/deactivate user

### Company
- `GET /api/companies/me` — Get current company
- `PUT /api/companies/me` — Update company (Owner only)

### Audit
- `GET /api/audit` — Get audit logs (Owner/Admin only)

### Platform Admin
- `GET /api/admin/companies` — List all companies (PlatformAdmin only)

### Health
- `GET /health` — Health check

## 📁 File Locations

### Configuration
- Backend secrets: `dotnet user-secrets` (see below) — never edit `appsettings.json` directly
- Frontend env: `frontend/.env.local`
- Docker: `docker-compose.yml`
- Database: `database/recreate-database.ps1`

### Documentation
- Index: `docs/index.md`
- AI context: `docs/ai-context/main.md`
- Azure deployment: `docs/deployment.md`
- Environment setup: `docs/environment-setup.md`
- Checklist: `docs/implementation-checklist.md`

### Setting User Secrets (backend)
```powershell
cd backend
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=lawgate_db;Username=lawgate_user;Password=lawgate_dev_password_change_in_production"
dotnet user-secrets set "Jwt:SecretKey" "your-super-secret-jwt-key-at-least-32-chars-long"
```

## 🐛 Troubleshooting

### Port Already in Use
```powershell
# Find process
netstat -ano | findstr :5000
netstat -ano | findstr :3000

# Kill process
taskkill /PID <pid> /F
```

### Database Connection Failed
```powershell
# Check PostgreSQL is running
docker ps | findstr postgres

# Restart PostgreSQL
docker-compose restart postgres

# Recreate database
cd database && ./recreate-database.ps1
```

### Backend Won't Start
```powershell
# Check connection string
dotnet user-secrets list

# Clean rebuild
dotnet clean
Remove-Item -Recurse -Force bin, obj
dotnet restore
dotnet build
```

### Frontend Won't Build
```powershell
# Clear cache
Remove-Item -Recurse -Force node_modules/.vite
Remove-Item -Recurse -Force dist

# Reinstall
npm install
```

### Docker Issues
```powershell
# Restart Docker Desktop
# Then:
docker-compose down -v
docker-compose build --no-cache
docker-compose up -d
```

## 🚀 Deployment

### Build Production

**Backend:**
```powershell
cd backend
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip
```

**Frontend:**
```powershell
cd frontend
npm run build
# Output in: ./dist
```

### Deploy to Azure
See: `docs/deployment.md`

### Run Tests
```powershell
# All backend tests
cd backend && dotnet test

# Unit tests only
dotnet test LegalDocSystem.UnitTests

# Integration tests only (requires Docker)
dotnet test LegalDocSystem.IntegrationTests

# Frontend tests
cd frontend && npm test
```

## 📊 Connection Strings

### Development (Docker)
```
Host=localhost;Port=5432;Database=lawgate_db;Username=lawgate_user;Password=lawgate_dev_password_change_in_production
```

### Azure Production
```
Host=<server>.postgres.database.azure.com;Database=lawgate_db;Username=<admin>;Password=<from-keyvault>;SslMode=Require;TrustServerCertificate=false
```

## 💡 Pro Tips

1. **Start with `docker-compose up -d`** — runs the full stack including Azurite (blob storage)
2. **Use User Secrets, never commit passwords** — `dotnet user-secrets set "Jwt:SecretKey" "..."`
3. **Swagger UI for API testing: http://localhost:5059/swagger**
4. **Check Docker logs:** `docker logs lawgate-backend -f`
5. **Verification emails go to logs in dev:** `docker exec lawgate-backend ls logs/emails/`
6. **Test migrations on local before production**
7. **Commit often, push regularly**
8. **Use `dotnet watch run` for hot reload**
9. **Use `npm run dev` for frontend HMR**

## 🆘 Emergency Recovery

**Lost everything? Follow these steps:**

```powershell
# 1. Ensure prerequisites installed (see docs/environment-setup.md)

# 2. Clone/navigate to project
cd Lawgate-ProjectsOnPhone

# 3. Start full Docker stack (handles everything — DB, Azurite, backend, frontend)
docker-compose up -d

# 4. If running outside Docker, set backend secrets first:
cd backend
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=lawgate_db;Username=lawgate_user;Password=lawgate_dev_password_change_in_production"
dotnet user-secrets set "Jwt:SecretKey" "your-super-secret-jwt-key-at-least-32-chars-long"
dotnet watch run --project LegalDocSystem.API

# 5. Setup frontend (in new terminal, only if running outside Docker)
cd frontend
npm install
# Create .env.local with: VITE_API_URL=http://localhost:5059/api
npm run dev

# 6. Test: Open http://localhost:5174 (Docker) or http://localhost:5173 (local)
# Login with: admin@demolawfirm.com / Admin@123
```

**Time to working system: ~15 minutes** ✨

## 📞 Key Resources

- **.NET Docs**: https://docs.microsoft.com/dotnet/
- **React Docs**: https://react.dev/
- **Vite Docs**: https://vitejs.dev/
- **Tailwind**: https://tailwindcss.com/
- **EF Core**: https://docs.microsoft.com/ef/core/
- **PostgreSQL**: https://www.postgresql.org/docs/
- **Azure**: https://docs.microsoft.com/azure/

---

**Print this or keep it open for quick reference!** 📌
