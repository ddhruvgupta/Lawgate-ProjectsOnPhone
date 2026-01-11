# Claude.ai Long-Term Memory Context

## Project: Lawgate Enterprise Application

### Last Updated: 2026-01-11

---

## Critical Context for Future Sessions

### 🔴 MOST IMPORTANT: Database Recreation Problem
**User's Pain Point**: After 1 year of not working on the project, it's extremely hard to recreate the database.

**Our Solution**:
1. Entity Framework Core migrations (version controlled)
2. Automated PowerShell script: `database/recreate-database.ps1`
3. Docker Compose setup with initialization scripts
4. Comprehensive seeding scripts in `backend/Data/DbSeeder.cs`
5. Detailed documentation in `database/docs/README.md`

### Technology Stack
- **Frontend**: React 18 + Vite + Tailwind CSS
- **Backend**: .NET 8 Web API
- **Database**: PostgreSQL 16
- **Deployment**: Azure (App Service + Azure Database for PostgreSQL)
- **Containerization**: Docker + Docker Compose

### Project Structure Philosophy
- **Monorepo**: All projects in one repository
- **Clear Separation**: Frontend, Backend, Database, Docker
- **Documentation-First**: Every folder has a `docs/` directory
- **Azure-Optimized**: Configurations ready for Azure deployment

### Key Design Decisions

#### Why PostgreSQL over MySQL?
- Better Azure integration
- More advanced features (JSONB, better full-text search)
- Similar pricing on Azure
- Better performance for complex queries
- Stronger community and tooling

#### Why .NET 8?
- Latest LTS version (supported until 2026)
- Excellent performance
- Strong typing and compile-time checks
- Great Azure integration
- Built-in Entity Framework Core

#### Why Tailwind CSS over Bootstrap?
- More modern and customizable
- Smaller bundle size with purging
- Better developer experience
- More flexibility for custom designs

### User's Development Environment
- **OS**: Windows
- **Shell**: PowerShell
- **IDE**: VS Code (assumed)

### Critical Files for Database Recreation
1. `database/recreate-database.ps1` - One-command database reset
2. `backend/Data/DbSeeder.cs` - Seed data definitions
3. `backend/Migrations/` - All schema migrations
4. `docker-compose.yml` - Container orchestration
5. `database/docs/README.md` - Step-by-step recovery guide

### Security Considerations
- JWT authentication in backend
- HTTPS enforced in production
- Azure Key Vault for secrets
- Password hashing with BCrypt
- SQL injection protection via EF Core
- CORS properly configured

### Cost Optimization (Azure)
- **Database**: Start with Burstable B1ms (~$16/month for dev)
- **App Service**: Basic B1 tier (~$13/month for dev)
- **Production**: Scale up to General Purpose tiers
- Use Azure Hybrid Benefit if available

### Common Commands for User

#### Start Everything
```powershell
docker-compose up -d
cd backend && dotnet run &
cd ../frontend && npm run dev
```

#### Recreate Database (THE KEY SOLUTION)
```powershell
cd database
./recreate-database.ps1
```

#### Deploy to Azure
```powershell
# Backend
cd backend
dotnet publish -c Release
az webapp deploy --resource-group lawgate-rg --name lawgate-api --src-path ./bin/Release/net8.0/publish.zip

# Frontend
cd ../frontend
npm run build
az storage blob upload-batch --account-name lawgatestorage --destination '$web' --source ./dist
```

### Future Enhancement Ideas
- GraphQL API layer
- Redis caching
- Azure Service Bus for messaging
- Azure Application Insights for monitoring
- Azure CDN for frontend
- Multi-region deployment
- Automated CI/CD with GitHub Actions

### Testing Strategy
- **Frontend**: Vitest + React Testing Library
- **Backend**: xUnit + FluentAssertions
- **Integration**: Testcontainers for database tests
- **E2E**: Playwright

### When User Returns After Long Break
1. Run `docker-compose up -d` to start PostgreSQL
2. Run `cd database && ./recreate-database.ps1` to setup database
3. Run `cd backend && dotnet restore && dotnet run`
4. Run `cd frontend && npm install && npm run dev`
5. Everything should work! All state is in code, not external setup

### Documentation Locations
- **Project Overview**: `/README.md`
- **Frontend Docs**: `/frontend/docs/`
- **Backend Docs**: `/backend/docs/`
- **Database Docs**: `/database/docs/`
- **Azure Deployment**: `/docs/azure-deployment.md`
- **Claude Context**: This file in each docs folder

### Remember for Next Time
- User prefers practical, working solutions over theoretical
- Database recreation is THE priority concern
- Everything should be scriptable and documented
- Azure deployment is the target platform
- Windows/PowerShell commands are required

### Project Status
- ✅ Project structure created
- ✅ Documentation framework established
- ⏳ Frontend scaffolding (in progress)
- ⏳ Backend API setup (in progress)
- ⏳ Database scripts (in progress)
- ⏳ Docker configuration (in progress)
- ⏳ Azure deployment configs (pending)

---

## For Claude in Future Sessions

When the user opens this project again:
1. Check this file first to understand the context
2. Remember the database recreation priority
3. Use PowerShell commands (Windows environment)
4. Reference the comprehensive docs we created
5. Maintain the established patterns and structure
6. Update this file with new decisions and changes

**Key Phrase**: "Database recreation after 1 year" - This is the core problem we solved.
