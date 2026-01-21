# 🚀 Quick Start Guide - Legal Document Management System

## Start the Application

### 1. Start PostgreSQL Database
```powershell
docker-compose up -d postgres
```

### 2. Start the API Server
```powershell
cd backend\LegalDocSystem.API
dotnet run
```

The API will start at: **http://localhost:5059**

### 3. Access Swagger Documentation
Open in browser: **http://localhost:5059/swagger**

---

## Test Endpoints

### Health Check
```powershell
Invoke-RestMethod -Uri "http://localhost:5059/health" -Method GET
```

### Database Connection Test
```powershell
Invoke-RestMethod -Uri "http://localhost:5059/api/test/database" -Method GET
```

### Create Test Company
```powershell
Invoke-RestMethod -Uri "http://localhost:5059/api/test/seed-company" -Method POST
```

### Create Test User
```powershell
Invoke-RestMethod -Uri "http://localhost:5059/api/test/seed-user" -Method POST
```

### List All Companies
```powershell
Invoke-RestMethod -Uri "http://localhost:5059/api/test/companies" -Method GET
```

### List All Users
```powershell
Invoke-RestMethod -Uri "http://localhost:5059/api/test/users" -Method GET
```

---

## Test Credentials

**Email:** admin@lawfirm.com  
**Password:** Admin123!  
**Role:** CompanyOwner  
**Company:** Test Law Firm

---

## Database Commands

### Connect to Database
```powershell
docker exec -it lawgate-postgres psql -U lawgate_user -d lawgate_db
```

### List All Tables
```powershell
docker exec -it lawgate-postgres psql -U lawgate_user -d lawgate_db -c "\dt"
```

### View Users
```powershell
docker exec -it lawgate-postgres psql -U lawgate_user -d lawgate_db -c "SELECT * FROM \`"Users\`";"
```

### View Companies
```powershell
docker exec -it lawgate-postgres psql -U lawgate_user -d lawgate_db -c "SELECT * FROM \`"Companies\`";"
```

---

## Entity Framework Commands

### Create New Migration
```powershell
cd backend
dotnet ef migrations add MigrationName --project LegalDocSystem.Infrastructure --startup-project LegalDocSystem.API
```

### Apply Migrations
```powershell
dotnet ef database update --project LegalDocSystem.Infrastructure --startup-project LegalDocSystem.API
```

### Remove Last Migration
```powershell
dotnet ef migrations remove --project LegalDocSystem.Infrastructure --startup-project LegalDocSystem.API
```

---

## Database Recreation (After Long Break)

```powershell
cd database
.\recreate-database.ps1
```

This script will:
1. Check if PostgreSQL is running
2. Drop existing database
3. Create fresh database
4. Apply all migrations
5. Seed initial data

---

## Build & Run

### Build Solution
```powershell
cd backend
dotnet build
```

### Run Tests (once implemented)
```powershell
dotnet test
```

### Run API
```powershell
cd LegalDocSystem.API
dotnet run
```

### Run with Hot Reload
```powershell
dotnet watch run
```

---

## Docker Commands

### Start All Services
```powershell
docker-compose up -d
```

### Stop All Services
```powershell
docker-compose down
```

### View Logs
```powershell
docker-compose logs -f
```

### Rebuild and Start
```powershell
docker-compose up -d --build
```

---

## Project Structure

```
backend/
├── LegalDocSystem.Domain/          # Entities, Enums, Value Objects
│   ├── Common/
│   ├── Entities/
│   └── Enums/
├── LegalDocSystem.Application/     # Business Logic, DTOs, Services
├── LegalDocSystem.Infrastructure/  # Database, EF Core, External Services
│   └── Data/
└── LegalDocSystem.API/            # Controllers, Middleware
    └── Controllers/
```

---

## Useful Links

- **API:** http://localhost:5059
- **Swagger:** http://localhost:5059/swagger
- **Health Check:** http://localhost:5059/health
- **PostgreSQL:** localhost:5432

---

## Common Issues

### Issue: "Cannot connect to database"
**Solution:** Make sure PostgreSQL is running
```powershell
docker-compose up -d postgres
```

### Issue: "Migration failed"
**Solution:** Check connection string in appsettings.json
```
Host=localhost;Port=5432;Database=lawgate_db;Username=lawgate_user;Password=lawgate_dev_password_change_in_production
```

### Issue: "Port already in use"
**Solution:** Stop other processes using port 5059 or change port in launchSettings.json

---

## Next Development Steps

1. ✅ Backend structure complete
2. ✅ Database configured and tested
3. ⏳ Implement Application Layer services
4. ⏳ Create authentication controllers
5. ⏳ Initialize React frontend
6. ⏳ Implement Azure Blob Storage

---

**Last Updated:** January 12, 2026
