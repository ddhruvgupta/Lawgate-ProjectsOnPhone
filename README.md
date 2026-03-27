# Lawgate — Legal Document Management System

A multi-tenant SaaS application for Indian law firms to manage legal cases, contracts, and documents.  
Built with React, .NET Clean Architecture, PostgreSQL, and Azure.

---

## 🚦 Current Status — March 2026

### ✅ Completed
| Area | Details |
|------|---------|
| **Clean Architecture** | Domain / Application / Infrastructure / API layers |
| **Authentication** | JWT login & register, BCrypt, refresh-ready |
| **Core Domain** | Company, User, Project, Document entities + EF Core migrations |
| **Service Layer** | CompanyService, ProjectService, UserService, DocumentService |
| **API Controllers** | Auth, Company, Project, User, Document (tenant-scoped) |
| **Azure Blob Storage** | AzureBlobStorageService with SAS token generation |
| **Frontend Shell** | React + Vite + Tailwind, Login/Register/Dashboard pages, Auth context |
| **Database** | PostgreSQL via Docker, schema migrations, recreate script |

### 🔨 Currently Working On (Phase 4 → 5)
1. **Install missing frontend packages** — `react-hook-form`, `zod`, `@tanstack/react-query`, `@headlessui/react`, `@heroicons/react`, `clsx`
2. **`DbSeeder.cs`** — seed default roles and a test `CompanyOwner` account so the app is usable without manual SQL
3. **Global error-handling middleware** — return clean JSON errors from the API instead of stack traces
4. **Frontend layout components** — `Navbar`, `Sidebar`, base `Layout` wrapper
5. **End-to-end integration test** — verify full register → login → dashboard → API call flow works locally

### 📋 Open GitHub Issues
- **21 open issues** across Phase 1 (MVP) and Phase 2 (advanced features)
- View them: `gh issue list --state open`
- Next up on the board: `#8` RBAC, `#18–20` Frontend UI pages

---

## Project Structure
```
.
├── frontend/          # React 19 + Vite + Tailwind CSS + TypeScript
├── backend/           # .NET 10 Clean Architecture Web API
│   ├── LegalDocSystem.API/          # Controllers, middleware
│   ├── LegalDocSystem.Application/  # Services, DTOs, interfaces
│   ├── LegalDocSystem.Domain/       # Entities, enums, value objects
│   └── LegalDocSystem.Infrastructure/ # EF Core, Azure, background jobs
├── database/          # PostgreSQL init scripts & recreate tooling
├── docker/            # Container configs
└── docs/              # Project-wide documentation
```

## Quick Start

### Prerequisites
- Node.js 20+ and npm
- .NET 10 SDK
- Docker Desktop

### Getting Up and Running
```powershell
# 1. Start PostgreSQL (via Docker)
docker-compose up -d postgres

# 2. Backend
cd backend
dotnet restore
dotnet ef database update --project LegalDocSystem.Infrastructure --startup-project LegalDocSystem.API
dotnet run --project LegalDocSystem.API

# 3. Frontend
cd ../frontend
npm install
npm run dev
```

### Database Recreation (After a Long Break)
```powershell
cd database
./recreate-database.ps1
```

### Default Dev Credentials
After running the recreate script:
- **Admin**: `admin@lawgate.com` / `Admin@123`
- **User**: `user@lawgate.com` / `User@123`

---

## Technology Stack
| Layer | Tech |
|-------|------|
| Frontend | React 19, Vite, Tailwind CSS v4, TypeScript, React Router v7, Axios |
| Backend | .NET 10, Entity Framework Core, JWT Auth, Swagger, Serilog |
| Database | PostgreSQL 16 |
| Storage | Azure Blob Storage |
| Deployment | Azure App Service + Azure Database for PostgreSQL |
| DevOps | Docker Compose, GitHub Actions (planned) |

## Documentation

All documentation is consolidated in [`/docs/`](./docs/) — see the [Documentation Index](./docs/index.md) for a full list.

| Quick links | |
|-------------|---|
| [Quick Start](./docs/quick-start.md) | Get running in minutes |
| [Quick Reference](./docs/quick-reference.md) | Common commands cheat sheet |
| [Backend](./docs/backend.md) | API, controllers, services, migrations |
| [Frontend](./docs/frontend.md) | Pages, components, routing |
| [Database](./docs/database.md) | Schema, migrations, recreation |
| [Architecture](./docs/architecture.md) | System design and decisions |
| [Deployment](./docs/deployment.md) | Azure deployment guide |
| [Implementation Checklist](./docs/implementation-checklist.md) | Progress tracker (Phase 6) |

## Environment Variables
- Copy `frontend/.env.local` from `.env.example` — set `VITE_API_URL`
- Configure `backend/appsettings.Development.json` — connection string + JWT key + Azure Blob

## License
Proprietary — All rights reserved
