# Quick Reference Card

Keep this handy for common commands and tasks!

## 🚀 Starting Development

```powershell
# Start everything
docker-compose up -d

# Backend (Terminal 1)
cd backend
dotnet watch run
# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger

# Frontend (Terminal 2)
cd frontend
npm run dev
# App: http://localhost:3000
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

## 🔐 Default Test Users

**Admin Account:**
- Email: `admin@lawgate.com`
- Password: `Admin@123`

**User Account:**
- Email: `user@lawgate.com`
- Password: `User@123`

⚠️ **These are for development only!**

## 🌐 API Endpoints

### Authentication
- POST `/api/auth/register` - Register new user
- POST `/api/auth/login` - Login
- POST `/api/auth/refresh` - Refresh token
- POST `/api/auth/logout` - Logout

### Users
- GET `/api/users` - List users (Admin)
- GET `/api/users/{id}` - Get user
- PUT `/api/users/{id}` - Update user
- DELETE `/api/users/{id}` - Delete user (Admin)

### Health
- GET `/api/health` - Health check

## 📁 File Locations

### Configuration
- Backend: `backend/appsettings.Development.json` (Use User Secrets!)
- Frontend: `frontend/.env.local`
- Docker: `docker-compose.yml`
- Database: `database/recreate-database.ps1`

### Documentation
- Main: `docs/claude-main-context.md`
- Setup: `SETUP-COMPLETE.md`
- Azure: `docs/azure-deployment.md`
- Environment: `docs/environment-setup.md`
- Checklist: `IMPLEMENTATION-CHECKLIST.md`

### Component Docs
- Frontend: `frontend/docs/README.md`, `frontend/docs/claude.md`
- Backend: `backend/docs/README.md`, `backend/docs/claude.md`
- Database: `database/docs/README.md`, `database/docs/claude.md`

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
See: `docs/azure-deployment.md`

## 📊 Connection Strings

### Development
```
Host=localhost;Database=lawgate_db;Username=lawgate_user;Password=lawgate_dev_password_change_in_production
```

### Azure Production
```
Host=<server>.postgres.database.azure.com;Database=lawgate_db;Username=<admin>;Password=<from-keyvault>;SslMode=Require
```

## 💡 Pro Tips

1. **Always run `database/recreate-database.ps1` when returning after a break**
2. **Use User Secrets, never commit passwords**
3. **Check Swagger UI for API testing: http://localhost:5000/swagger**
4. **Keep documentation updated as you build**
5. **Test migrations on local before production**
6. **Commit often, push regularly**
7. **Use `dotnet watch run` for hot reload**
8. **Use `npm run dev` for frontend HMR**

## 🆘 Emergency Recovery

**Lost everything? Follow these steps:**

```powershell
# 1. Ensure prerequisites installed (see docs/environment-setup.md)

# 2. Clone/navigate to project
cd Lawgate-ProjectsOnPhone

# 3. Start Docker
docker-compose up -d postgres

# 4. Recreate database
cd database
./recreate-database.ps1

# 5. Setup backend secrets
cd ../backend
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=lawgate_db;Username=lawgate_user;Password=lawgate_dev_password_change_in_production"
dotnet user-secrets set "Jwt:Key" "your-super-secret-jwt-key-at-least-32-chars-long"

# 6. Start backend
dotnet run

# 7. Setup frontend (in new terminal)
cd ../frontend
npm install
# Create .env.local with: VITE_API_URL=http://localhost:5000/api
npm run dev

# 8. Test: Open http://localhost:3000
# Login with: admin@lawgate.com / Admin@123
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
