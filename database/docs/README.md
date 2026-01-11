# Database Documentation

## Overview
PostgreSQL database with Entity Framework Core migrations for easy recreation and version control.

## Quick Database Recreation (THE SOLUTION!)

### Option 1: Using PowerShell Script (Recommended)
```powershell
cd database
./recreate-database.ps1
```

### Option 2: Using Docker
```powershell
docker-compose down -v
docker-compose up -d postgres
cd backend
dotnet ef database update
dotnet run --seed-data
```

### Option 3: Manual Steps
```powershell
# 1. Drop existing database
psql -U lawgate_user -h localhost -c "DROP DATABASE IF EXISTS lawgate_db;"
psql -U lawgate_user -h localhost -c "CREATE DATABASE lawgate_db;"

# 2. Apply migrations
cd ../backend
dotnet ef database update

# 3. Seed data
dotnet run --seed-data
```

## Database Schema

### Current Version: 1.0.0
Last Migration: `Initial_Create`

### Tables
- **Users** - User accounts and authentication
- **Roles** - User roles and permissions
- **AuditLogs** - Audit trail for all operations

### Migrations History
All migrations are in: `backend/Migrations/`

## Connection Strings

### Development (Local)
```
Host=localhost;Database=lawgate_db;Username=lawgate_user;Password=lawgate_dev_password_change_in_production
```

### Azure Production
```
Host=lawgate-db.postgres.database.azure.com;Database=lawgate_db;Username=lawgate_admin;Password=<from-keyvault>;SslMode=Require
```

## Backup & Restore

### Create Backup
```powershell
# Full backup
pg_dump -U lawgate_user -h localhost lawgate_db > backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql

# Schema only
pg_dump -U lawgate_user -h localhost --schema-only lawgate_db > schema.sql

# Data only
pg_dump -U lawgate_user -h localhost --data-only lawgate_db > data.sql
```

### Restore Backup
```powershell
psql -U lawgate_user -h localhost lawgate_db < backup_file.sql
```

## Seeding Data

Seed data is defined in: `backend/Data/DbSeeder.cs`

### Default Users (Development Only)
- Admin: admin@lawgate.com / Admin@123
- User: user@lawgate.com / User@123

⚠️ **Security**: These are deleted in production automatically!

## Entity Framework Core Commands

### Add New Migration
```powershell
cd backend
dotnet ef migrations add <MigrationName>
```

### Update Database
```powershell
dotnet ef database update
```

### Rollback Migration
```powershell
dotnet ef database update <PreviousMigrationName>
```

### Remove Last Migration (if not applied)
```powershell
dotnet ef migrations remove
```

### Generate SQL Script
```powershell
dotnet ef migrations script -o migration.sql
```

## Troubleshooting

### "Database already exists" Error
```powershell
# Drop and recreate
cd database
./recreate-database.ps1
```

### Migration Conflicts
```powershell
# Reset migrations (WARNING: Data loss!)
cd backend
Remove-Item Migrations -Recurse
dotnet ef migrations add Initial
dotnet ef database update
```

### Connection Issues
1. Check Docker container: `docker ps`
2. Check PostgreSQL logs: `docker logs lawgate-postgres`
3. Verify connection string in `appsettings.Development.json`

## Performance Optimization

### Indexes
All critical indexes are defined in Entity Configuration classes in `backend/Data/Configurations/`

### Query Performance
Use `dotnet ef dbcontext optimize` to generate compiled models

## Security Best Practices

1. **Never commit** `appsettings.Production.json` with real credentials
2. Use **Azure Key Vault** for production secrets
3. Enable **SSL/TLS** for all database connections in production
4. Use **managed identities** when possible
5. Implement **row-level security** for multi-tenant data

## Monitoring

### Azure Database for PostgreSQL
- Enable **Query Performance Insights**
- Set up **alerts** for high CPU/memory
- Configure **automated backups** (7-35 days retention)

## Cost Optimization (Azure)

### Development/Staging
- Use **Burstable B1ms** tier (~$16/month)
- Stop when not in use (serverless option)

### Production
- Start with **General Purpose D2s_v3** (~$150/month)
- Enable **auto-scaling** for read replicas if needed
- Use **Azure Hybrid Benefit** if applicable

## Documentation Updates
When you modify the database schema:
1. Update this README
2. Update `schema-changelog.md`
3. Add migration documentation in `migrations/`
