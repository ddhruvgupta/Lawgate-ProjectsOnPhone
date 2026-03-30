# Lawgate

A multi-tenant SaaS application for law firms to manage legal cases, contracts, and documents. Built for Indian law firms with a focus on data isolation, audit compliance, and Azure deployment.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 19, Vite 7, TypeScript, Tailwind CSS v4, React Router v7, React Query, React Hook Form + Zod, Axios |
| Backend | ASP.NET Core 10, Entity Framework Core 10, JWT Bearer auth, Serilog, Swagger |
| Database | PostgreSQL 16 |
| Storage | Azure Blob Storage (Azurite for local dev) |
| Containerization | Docker Compose |
| Deployment | Azure App Service + Azure Database for PostgreSQL |

---

## Quick Start

**Prerequisites:** Docker Desktop (everything else runs in containers)

```bash
docker compose up
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:5173 |
| Backend API | http://localhost:5059 |
| Swagger UI | http://localhost:5059/swagger |
| Health check | http://localhost:5059/health |

The backend auto-applies EF Core migrations and seeds demo data on first run (development only).

### Default credentials (seeded by DbSeeder)

| Role | Email | Password |
|------|-------|----------|
| CompanyOwner | admin@demolawfirm.com | Admin@123 |
| User | jane.doe@demolawfirm.com | User@123 |
| PlatformAdmin | admin@lawgate.io | LawgatePlatform@1 |
| PlatformSuperAdmin | superadmin@lawgate.io | LawgateSuperAdmin@1 |

---

## Project Structure

```
.
├── frontend/          # React 19 + Vite + TypeScript
├── backend/           # .NET 10 Clean Architecture Web API
│   ├── LegalDocSystem.API/          # Controllers, middleware, Program.cs
│   ├── LegalDocSystem.Application/  # Services, DTOs, interfaces
│   ├── LegalDocSystem.Domain/       # Entities, enums
│   └── LegalDocSystem.Infrastructure/ # EF Core, migrations, Azure Blob, background services
├── database/          # PostgreSQL init scripts and recreate tooling
├── docker-compose.yml # Full local stack (postgres, azurite, backend, frontend)
└── docs/              # Project-wide documentation
```

---

## Feature Status

### Built
- JWT authentication (register, login, refresh token, validate, /me)
- Multi-tenant architecture: every entity is scoped to a `CompanyId`
- Clean Architecture: Domain / Application / Infrastructure / API layers
- Company, User, Project, Document entities with EF Core migrations
- CompanyService, UserService, ProjectService, DocumentService
- Azure Blob Storage integration with SAS token generation (chunked upload support)
- DocumentCleanupService background job
- GlobalExceptionMiddleware returning consistent JSON error responses
- DbSeeder: seeds demo company, owner user, standard user, and a sample project on startup
- Frontend: Login, Register, Dashboard pages; ProtectedRoute; AuthContext; Layout
- Frontend utilities: `cn` (clsx wrapper), `formatters`, `useApi` hook
- Docker Compose stack: PostgreSQL, Azurite, backend, frontend with hot reload

### Not Yet Built
- Role controller / RBAC enforcement at project level
- Document upload UI (frontend)
- Project and user management UI pages
- Platform admin views
- Email verification / password reset
- Full-text search
- CI/CD pipelines
- Production Azure deployment

---

## Documentation

| Doc | Contents |
|-----|----------|
| [docs/architecture.md](./docs/architecture.md) | System design, layers, data model, entity relationships |
| [docs/api.md](./docs/api.md) | All API endpoints, request/response shapes, auth |
| [docs/development.md](./docs/development.md) | Local dev setup, Docker, env vars, migrations |
| [docs/testing.md](./docs/testing.md) | Test structure, how to run tests, what's covered |
| [docs/deployment.md](./docs/deployment.md) | Azure deployment, production Dockerfile, CI/CD |
| [docs/admin.md](./docs/admin.md) | Platform admin system and roles |

---

## License

Proprietary — All rights reserved
