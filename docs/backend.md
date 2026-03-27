# Backend Documentation

## Current Status

**95%+ MVP complete** as of March 2026. All core services, controllers, authentication, Azure Blob Storage integration, multi-tenancy, audit logging, and document versioning are implemented and functional.

| Layer | Status |
|-------|--------|
| Clean Architecture structure | ✅ Complete |
| Domain entities (6 entities) | ✅ Complete |
| Infrastructure services (9) | ✅ Complete |
| API controllers (8) | ✅ Complete |
| JWT authentication + refresh tokens | ✅ Complete |
| Azure Blob Storage integration | ✅ Complete |
| Multi-tenant row-level security | ✅ Complete |
| Audit logging | ✅ Complete |
| Document versioning | ✅ Complete |
| Background services | ✅ Complete |
| Database migrations (5) | ✅ Complete |
| Development seed data | ✅ Complete |
| Unit tests | ⏳ In progress |
| Email verification / password reset | ❌ Not started |
| Rate limiting | ❌ Not started |

---

## Project Structure

```
backend/
├── LegalDocSystem.Domain/         # Pure business entities and enums
│   ├── Entities/                  # Company, User, Project, Document, ProjectPermission, AuditLog
│   ├── Enums/                     # UserRole, ProjectStatus, DocumentType, SubscriptionTier, PermissionLevel
│   └── Common/                    # BaseEntity (Id, CreatedAt, UpdatedAt, IsDeleted)
├── LegalDocSystem.Application/    # Business logic interfaces and DTOs
│   ├── Interfaces/                # IAuthService, IProjectService, IDocumentService, etc.
│   └── DTOs/                      # All request/response DTOs
├── LegalDocSystem.Infrastructure/ # Data access and external service implementations
│   ├── Data/                      # ApplicationDbContext, EntityConfigurations, DbSeeder
│   ├── Services/                  # All 9 service implementations
│   ├── BackgroundServices/        # DocumentCleanupService
│   └── Migrations/                # EF Core migration history (5 migrations)
├── LegalDocSystem.API/            # Web API entry point
│   ├── Controllers/               # 8 controllers
│   ├── Middleware/                 # GlobalExceptionMiddleware
│   ├── Program.cs                 # DI registration, middleware pipeline
│   └── appsettings.json           # Configuration template
├── LegalDocSystem.UnitTests/
└── LegalDocSystem.IntegrationTests/
```

---

## Getting Started

### Prerequisites
- .NET 10 SDK
- Docker Desktop (PostgreSQL runs in Docker)
- Entity Framework Core CLI tools

### First Time Setup
```powershell
# Start database
docker-compose up -d postgres

# Navigate to backend
cd backend

# Restore packages
dotnet restore

# Apply migrations + seed dev data (auto-runs on startup in Development)
dotnet run --project LegalDocSystem.API

# Or apply migrations manually
dotnet ef database update --project LegalDocSystem.Infrastructure --startup-project LegalDocSystem.API
```

### Development
```powershell
# Hot reload
dotnet watch run --project LegalDocSystem.API

# Build entire solution
dotnet build

# Run all tests
dotnet test
```

### Access Points (Development)
| Endpoint | URL |
|----------|-----|
| API Base | http://localhost:5059/api |
| Swagger UI | http://localhost:5059/swagger |
| Health Check | http://localhost:5059/health |

---

## API Endpoints

### Authentication — `POST /api/auth`
| Method | Path | Description | Auth Required |
|--------|------|-------------|---------------|
| POST | `/api/auth/register` | Register new company + owner | No |
| POST | `/api/auth/login` | Login, returns access + refresh token | No |
| POST | `/api/auth/refresh` | Refresh access token | No |
| POST | `/api/auth/validate` | Validate token | No |
| GET | `/api/auth/me` | Get current authenticated user | Yes |

### Projects — `CompanyOwner`, `Admin`, `User`
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/projects` | List all company projects |
| GET | `/api/projects/{id}` | Get project details |
| POST | `/api/projects` | Create project |
| PUT | `/api/projects/{id}` | Update project |
| DELETE | `/api/projects/{id}` | Delete project (Owner/Admin only) |

### Documents
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/documents/upload-url` | Generate Azure SAS upload URL |
| POST | `/api/documents/{id}/confirm` | Confirm upload, create document record |
| GET | `/api/documents/{id}` | Get document details |
| GET | `/api/documents/{id}/download-url` | Generate SAS download URL |
| GET | `/api/documents/project/{projectId}` | List all project documents |
| DELETE | `/api/documents/{id}` | Delete document |

### Users — Admin/Owner only
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/users` | List company users |
| GET | `/api/users/{id}` | Get user details |
| POST | `/api/users` | Create user |
| POST | `/api/users/{id}/toggle-status` | Activate / deactivate user |

### Company
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/companies/me` | Get current user's company |
| PUT | `/api/companies/me` | Update company (Owner only) |

### Audit — Owner/Admin only
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/audit` | Get audit logs (filter by entityType, entityId, page, pageSize) |

### Platform Admin — PlatformAdmin role only
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/admin/companies` | List all companies with stats |

### Test
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/test/database` | Database health check |

---

## Domain Entities

| Entity | Key Relationships |
|--------|------------------|
| **Company** | Root tenant; has many Users, Projects |
| **User** | Belongs to Company; has Role enum; has many ProjectPermissions |
| **Project** | Belongs to Company; has Status enum; has many Documents, ProjectPermissions |
| **Document** | Belongs to Project; uploaded by User; supports versioning (ParentDocumentId, IsLatestVersion) |
| **ProjectPermission** | Many-to-many bridge between User and Project with PermissionLevel |
| **AuditLog** | Logs every user action with OldValues/NewValues (JSON), IpAddress, UserAgent |

### Enums
- `UserRole`: CompanyOwner, Admin, User, Viewer, PlatformAdmin, PlatformSuperAdmin
- `ProjectStatus`: Intake, InProgress, Legal, Completed, Closed
- `DocumentType`: Contract, Brief, Motion, Pleading, Exhibit, Amendment, Correspondence, Other
- `SubscriptionTier`: Trial (14 days / 10 GB), Basic, Professional, Enterprise
- `PermissionLevel`: Viewer, Editor, Manager

---

## Infrastructure Services

All registered as **scoped** services in DI:

| Service | Responsibility |
|---------|---------------|
| `JwtTokenService` | Create/validate JWT tokens, extract claims |
| `AuthService` | Register, login, refresh token logic |
| `AzureBlobStorageService` | Generate SAS upload/download URLs, delete blobs |
| `DocumentService` | Document CRUD, confirm upload, versioning |
| `CompanyService` | Company CRUD |
| `UserService` | User CRUD, activate/deactivate |
| `ProjectService` | Project CRUD with multi-tenant CompanyId filter |
| `AuditService` | Log actions with full context (IP, user agent, old/new values) |
| `PlatformAdminService` | Cross-company admin operations |

### Background Services
- `DocumentCleanupService` — Hosted service; periodically cleans up unconfirmed (pending) document records

---

## Database Migrations

```powershell
# Create new migration
dotnet ef migrations add <MigrationName> \
  --project LegalDocSystem.Infrastructure \
  --startup-project LegalDocSystem.API

# Apply migrations
dotnet ef database update \
  --project LegalDocSystem.Infrastructure \
  --startup-project LegalDocSystem.API

# Remove last migration (if not applied)
dotnet ef migrations remove \
  --project LegalDocSystem.Infrastructure \
  --startup-project LegalDocSystem.API

# Generate SQL script
dotnet ef migrations script \
  --project LegalDocSystem.Infrastructure \
  --startup-project LegalDocSystem.API \
  --output migration.sql
```

### Applied Migrations

| # | Migration | Date |
|---|-----------|------|
| 1 | `InitialCreate` — Full schema: Companies, Users, Projects, Documents, ProjectPermissions, AuditLogs | 2026-01-12 |
| 2 | `AddDocumentStatus` — Added DocumentStatus enum column | 2026-01-25 |
| 3 | `AddRefreshTokenToUser` — Added RefreshToken, RefreshTokenExpiry to Users | 2026-03-25 |
| 4 | `AddRefreshTokenIndex` — Index on RefreshToken column | 2026-03-25 |
| 5 | `UpdateProjectStatusToLegal` — Updated ProjectStatus enum values | 2026-03-25 |

---

## Configuration

### Connection String (Development)
```
Host=localhost;Port=5432;Database=lawgate_db;Username=lawgate_user;Password=lawgate_dev_password_change_in_production
```

### JWT Settings (`appsettings.json`)
```json
{
  "Jwt": {
    "Key": "your-secret-key-min-32-characters",
    "Issuer": "lawgate-api",
    "Audience": "lawgate-client",
    "ExpiryMinutes": 1440
  }
}
```

JWT tokens expire after **24 hours**. Refresh tokens are stored in the database (hashed).

### Azure Storage (Local Dev — Azurite)
```json
{
  "AzureStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "legal-documents"
  }
}
```

⚠️ **Never commit secrets.** Use [.NET User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets) for local development and Azure Key Vault in production.

---

## Security Features

- **Passwords**: BCrypt with cost factor 11
- **Tokens**: JWT (HS256), 24-hour access token + refresh token
- **Multi-tenancy**: Every EF Core query is filtered by `CompanyId` — users can only see their company's data
- **RBAC**: Role checked at controller level (`[Authorize(Roles = "...")]`)
- **Project-level permissions**: `ProjectPermission` entity for fine-grained access (Viewer/Editor/Manager)
- **Audit trail**: All mutating actions logged with before/after values, IP, user agent
- **Global exception handling**: Consistent error response format, no stack traces in production

---

## Default Dev Users (Seed Data)

| Email | Password | Role |
|-------|----------|------|
| `owner@lawgate.com` | `Admin@123` | CompanyOwner |
| `admin@lawgate.com` | `Admin@123` | Admin |
| `user@lawgate.com`  | `Admin@123` | User |
| `platform@lawgate.com` | `Admin@123` | PlatformAdmin |

---

## Build Commands

```powershell
# Full solution build
cd backend
dotnet build

# Run API (applies migrations on startup in Development)
cd LegalDocSystem.API
dotnet run

# Run with hot reload
dotnet watch run

# Run via Docker
docker-compose up backend
```

---

## NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.0 | PostgreSQL driver |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.1 | EF Core migrations |
| `BCrypt.Net-Next` | 4.0.3 | Password hashing |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.1 | JWT middleware |
| `Serilog.AspNetCore` | 10.0.0 | Structured logging |
| `Swashbuckle.AspNetCore` | 10.1.0 | Swagger/OpenAPI |

---

*Last updated: 2026-03-27*
