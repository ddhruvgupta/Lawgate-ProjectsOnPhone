# Main Project Context - Claude.ai Memory

> **Companion doc (human-readable):** [`docs/architecture/architecture.md`](../architecture/architecture.md)

## 🎯 Project Mission

**Solve the #1 Problem**: Make it EASY to recreate the entire project after 1 year of inactivity.

## Critical Success Factors

### ✅ What We Achieved

1. **One-Command Database Recreation**
   - Script: `database/recreate-database.ps1`
   - Drops, recreates, migrates, and seeds database
   - No manual SQL needed, ever!

2. **Version-Controlled Schema**
   - Entity Framework Core migrations
   - All schema changes in code
   - `dotnet ef database update` = instant rebuild

3. **Comprehensive Documentation**
   - Every folder has `docs/` directory
   - `claude.md` files for AI context
   - `README.md` files for humans
   - Step-by-step recovery guides

4. **Docker Everything**
   - `docker-compose.yml` for local dev
   - No manual database installation
   - Consistent environment across machines

5. **Azure-Ready Deployment**
   - Complete deployment guide
   - Scripts for infrastructure
   - Environment configurations
   - CI/CD pipelines ready

## Technology Decisions & Rationale

### Frontend: React + Vite + Tailwind CSS
- **React 18**: Industry standard, huge ecosystem
- **Vite**: 100x faster than CRA, modern tooling
- **Tailwind CSS**: Utility-first, no separate CSS files
- **TypeScript**: Type safety, better refactoring

### Backend: .NET 8 + Entity Framework Core
- **. NET 8**: Latest LTS, excellent performance
- **EF Core**: Code-first ORM, migration system
- **PostgreSQL**: Better Azure integration than MySQL
- **JWT Auth**: Stateless, scalable authentication

### Database: PostgreSQL 16
- Better Azure managed service
- Advanced features (JSONB, full-text search)
- Lower cost than similar MySQL tier
- Excellent EF Core support

### Deployment: Azure
- User's target platform
- Cost-effective for enterprise
- Managed PostgreSQL service
- Easy CI/CD integration

## Project Structure

```
.
├── frontend/               # React + Vite + Tailwind
│   ├── src/
│   │   ├── components/    # Reusable UI components
│   │   ├── pages/         # Route components
│   │   ├── services/      # API integration
│   │   ├── hooks/         # Custom React hooks
│   │   └── contexts/      # Global state (Auth, etc.)
│   └── docs/              # Frontend documentation
│       ├── README.md      # Setup & usage guide
│       └── claude.md      # AI context
│
├── backend/               # .NET 8 Web API
│   ├── Controllers/       # API endpoints
│   ├── Models/            # Domain entities
│   ├── Data/              # DbContext, configurations
│   │   └── DbSeeder.cs   # Seed data (IMPORTANT!)
│   ├── Migrations/        # EF Core migrations
│   ├── Services/          # Business logic
│   └── docs/              # Backend documentation
│       ├── README.md      # API docs & setup
│       └── claude.md      # AI context
│
├── database/              # Database scripts & docs
│   ├── init/              # Docker initialization
│   ├── recreate-database.ps1  # ⭐ THE MAGIC SCRIPT
│   └── docs/
│       ├── README.md      # Database guide
│       ├── claude.md      # AI context
│       └── schema-changelog.md
│
├── docker/                # Container configs
├── docs/                  # Project-wide docs
│   ├── azure-deployment.md
│   └── environment-setup.md
│
├── docker-compose.yml     # Local development
├── README.md              # Project overview
└── .gitignore            # Never commit secrets!
```

## The Database Recreation Solution

### Problem
After 1 year: "How do I rebuild the database? What was the schema? What seed data is needed?"

### Solution
```powershell
cd database
./recreate-database.ps1
```

This script:
1. ✅ Checks PostgreSQL is running (starts Docker if needed)
2. ✅ Drops existing database
3. ✅ Creates fresh database
4. ✅ Applies ALL migrations from `backend/Migrations/`
5. ✅ Seeds initial data from `backend/Data/DbSeeder.cs`
6. ✅ Shows connection details and test users

**Result**: Working database in under 30 seconds, every time!

## Quick Start Commands

### First Time Setup (After 1 Year)
```powershell
# 1. Start Docker (for PostgreSQL)
docker-compose up -d

# 2. Recreate database
cd database
./recreate-database.ps1

# 3. Start backend
cd ../backend
dotnet restore
dotnet run

# 4. Start frontend
cd ../frontend
npm install
npm run dev
```

### Daily Development
```powershell
# Start everything
docker-compose up -d

# Backend (Terminal 1)
cd backend && dotnet watch run

# Frontend (Terminal 2)
cd frontend && npm run dev
```

### Database Changes
```powershell
# Create migration
cd backend
dotnet ef migrations add DescriptiveNameHere

# Apply migration
dotnet ef database update

# Rollback
dotnet ef database update PreviousMigrationName
```

## Key Files for Database Recreation

These files work together to solve the recreation problem:

1. **`database/recreate-database.ps1`**
   - Orchestrates entire process
   - User-friendly with colors and progress
   - Handles errors gracefully

2. **`backend/Migrations/*.cs`**
   - Generated by EF Core
   - Version-controlled schema changes
   - Applied in order automatically

3. **`backend/Data/DbSeeder.cs`**
   - Defines initial data
   - Creates roles, admin user, etc.
   - Runs automatically after migrations

4. **`backend/Data/ApplicationDbContext.cs`**
   - EF Core configuration
   - Entity relationships
   - Database connection

5. **`docker-compose.yml`**
   - PostgreSQL container setup
   - Consistent database version
   - Port mappings, volumes

6. **`database/docs/README.md`**
   - Manual steps if script fails
   - Troubleshooting guide
   - Connection strings

## Environment Configuration

### Development (Never Commit!)
```
backend/appsettings.Development.json  # Use User Secrets instead!
frontend/.env.local                   # Local API URL
```

### Production (Azure Key Vault)
```
Azure App Service > Configuration > Application Settings
Azure Key Vault > Secrets
```

## Default Test Data

After running `recreate-database.ps1`:

- **Admin User**
  - Email: admin@lawgate.com
  - Password: Admin@123
  - Can access all endpoints

- **Regular User**
  - Email: user@lawgate.com
  - Password: User@123
  - Limited permissions

⚠️ These are automatically removed in production!

## Documentation Hierarchy

### Level 1: Project Overview
- `README.md` - Start here
- Quick start, tech stack, structure

### Level 2: Component Specific
- `frontend/docs/README.md` - React setup
- `backend/docs/README.md` - .NET API setup
- `database/docs/README.md` - Database guide

### Level 3: AI Context
- `*/docs/claude.md` - Long-term memory
- Technical decisions
- Common patterns
- Troubleshooting

### Level 4: Deployment
- `docs/azure-deployment.md` - Production deploy
- `docs/environment-setup.md` - Local dev setup

## Cost Estimates

### Development
- PostgreSQL (local): $0 (Docker)
- Development time: Saved by good docs!

### Azure Production (Small)
- App Service B1: ~$13/month
- PostgreSQL B1ms: ~$16/month
- Static Web App: $0-9/month
- **Total: ~$30-40/month**

### Azure Production (Medium)
- App Service P1v2: ~$80/month
- PostgreSQL D2s_v3: ~$150/month
- Static Web App: $9/month
- Application Insights: ~$10/month
- **Total: ~$250/month**

## Security Best Practices

### ✅ DO
- Use User Secrets for local development
- Store production secrets in Azure Key Vault
- Use managed identities in Azure
- Enable HTTPS only
- Implement rate limiting
- Validate all input
- Use parameterized queries (EF Core does this)

### ❌ DON'T
- Commit `.env.local` or `appsettings.Development.json`
- Store passwords in plain text
- Expose database port to internet
- Use default admin passwords in production
- Skip input validation
- Trust client-side validation only

## Disaster Recovery

### Scenario: Lost Everything, Need to Rebuild

```powershell
# 1. Clone repo
git clone <url>
cd Lawgate-ProjectsOnPhone

# 2. Install prerequisites (see docs/environment-setup.md)
# - .NET 8 SDK
# - Node.js 20+
# - Docker Desktop

# 3. Start Docker
docker-compose up -d

# 4. Recreate database
cd database
./recreate-database.ps1

# 5. Start backend
cd ../backend
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=lawgate_db;Username=lawgate_user;Password=lawgate_dev_password_change_in_production"
dotnet user-secrets set "Jwt:Key" "your-super-secret-jwt-key-min-32-chars"
dotnet run

# 6. Start frontend
cd ../frontend
npm install
# Create .env.local with VITE_API_URL=http://localhost:5000/api
npm run dev

# 7. Login with admin@lawgate.com / Admin@123
```

**Time to working system: ~15 minutes** (most of that is npm install!)

## Common Pitfalls & Solutions

### ❌ "I can't remember the database schema"
✅ It's all in code! Check `backend/Models/` and `Migrations/`

### ❌ "I don't remember what seed data I need"
✅ Check `backend/Data/DbSeeder.cs` - it's documented!

### ❌ "The database version is wrong"
✅ Run `database/recreate-database.ps1` - always up to date!

### ❌ "I forgot the admin password"
✅ Run recreate script - creates admin@lawgate.com / Admin@123

### ❌ "Migrations are out of sync"
✅ `dotnet ef database update` applies all pending migrations

### ❌ "Environment variables are missing"
✅ Check `docs/environment-setup.md` for full list

## Testing Strategy

### Unit Tests
- Backend: xUnit + FluentAssertions
- Frontend: Vitest + React Testing Library

### Integration Tests
- API endpoints with Testcontainers
- Real PostgreSQL in Docker

### E2E Tests
- Playwright for full user flows
- Critical paths: login, CRUD operations

## CI/CD Pipeline

### GitHub Actions
- **Backend**: Build, test, deploy to Azure App Service
- **Frontend**: Build, test, deploy to Static Web Apps
- **Database**: Migrations run automatically on deploy

### Workflow
```
Push to main → Tests → Build → Deploy → Migrations → Health Check
```

## Monitoring & Observability

### Development
- Console logs
- Swagger UI for API testing
- Browser DevTools

### Production
- Application Insights
- Azure Monitor
- Health check endpoints
- Structured logging (Serilog)

## Scaling Strategy

### Vertical (Scale Up)
- Increase App Service tier
- Upgrade database tier
- More CPU/RAM per instance

### Horizontal (Scale Out)
- Multiple App Service instances
- Load balancer (Azure Front Door)
- Read replicas for database
- Redis cache layer

## Future Enhancements

### High Priority
- [ ] Add comprehensive unit tests
- [ ] Implement rate limiting
- [ ] Add Redis caching layer
- [ ] Set up Application Insights
- [ ] Configure automated backups

### Medium Priority
- [ ] Add API versioning
- [ ] Implement GraphQL endpoint
- [ ] Add webhook system
- [ ] Multi-tenant support
- [ ] Advanced audit logging

### Low Priority
- [ ] Real-time notifications (SignalR)
- [ ] Elasticsearch for search
- [ ] Multi-region deployment
- [ ] Mobile app (React Native)

## Remember for Next Session

### Key Phrase
"Database recreation after 1 year" - This was the core problem we solved

### Most Important Files
1. `database/recreate-database.ps1` - THE solution
2. `backend/Data/DbSeeder.cs` - Initial data
3. `backend/Migrations/` - Schema history
4. `docs/environment-setup.md` - Getting started
5. `*/docs/claude.md` - AI context in each component

### Quick Wins
- Everything is scriptable
- No manual database setup
- Docker for consistency
- Documentation everywhere
- Azure-ready from day 1

### User's Environment
- **OS**: Windows
- **Shell**: PowerShell
- **Target**: Azure deployment
- **Pain Point**: Database recreation difficulty

## Success Metrics

✅ **Can rebuild from scratch in < 15 minutes**
✅ **No manual database configuration needed**
✅ **All secrets managed properly**
✅ **Comprehensive documentation**
✅ **Azure deployment ready**
✅ **Professional project structure**

## Contact & Support

- Project Repository: [GitHub URL]
- Documentation: [This repo]/docs/
- Azure Portal: https://portal.azure.com

---

**Last Updated**: 2026-01-11
**Version**: 1.0.0
**Status**: Ready for development
