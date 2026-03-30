# Development Setup

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| Docker Desktop | Latest | Runs all services via Compose |
| .NET SDK | 10 | Build/run backend directly (optional) |
| Node.js | 20+ | Build/run frontend directly (optional) |
| `dotnet-ef` tool | Latest | Run EF Core migrations |

Install the EF Core CLI tool once:
```bash
dotnet tool install --global dotnet-ef
```

---

## Quickest start: Docker Compose

```bash
docker compose up
```

This starts four containers:
- `lawgate-postgres` — PostgreSQL 16 on port 5432
- `lawgate-azurite` — Azure Storage emulator on ports 10000–10002
- `lawgate-backend` — ASP.NET Core API on port 5059 (inside container: 8080)
- `lawgate-frontend` — Vite dev server on port 5173 with hot reload

The backend applies EF Core migrations automatically on startup and runs `DbSeeder` to create demo data if the database is empty.

### Default seeded credentials

**Demo company** (seeded by `DbSeeder`):

| Role | Email | Password |
|------|-------|----------|
| CompanyOwner | admin@demolawfirm.com | Admin@123 |
| User | jane.doe@demolawfirm.com | User@123 |

**Platform admins** (seeded by `SeedPlatformAdminsAsync`):

| Role | Email | Password |
|------|-------|----------|
| PlatformAdmin | admin@lawgate.io | LawgatePlatform@1 |
| PlatformSuperAdmin | superadmin@lawgate.io | LawgateSuperAdmin@1 |

All seeded users have `IsEmailVerified = true` and are pre-activated.

---

## Running services individually

### PostgreSQL + Azurite only

```bash
docker compose up postgres azurite
```

### Backend (locally, without Docker)

```bash
cd backend
dotnet restore
dotnet run --project LegalDocSystem.API
```

The API binds to `http://localhost:5059` by default (configured in `launchSettings.json`).

### Frontend (locally, without Docker)

```bash
cd frontend
npm install
npm run dev
```

Vite serves on `http://localhost:5173`.

---

## Environment Variables

### Backend

The backend reads configuration in priority order: `appsettings.json` < `appsettings.Development.json` < environment variables < User Secrets.

Key settings:

| Key | Example value | Notes |
|-----|--------------|-------|
| `ConnectionStrings__DefaultConnection` | `Host=localhost;Database=lawgate_db;Username=lawgate_user;Password=...` | PostgreSQL connection string |
| `ConnectionStrings__AzureStorage` | Azurite connection string | See `docker-compose.yml` for dev value |
| `Jwt__SecretKey` | `your-secret-key-32-chars-minimum` | Must be at least 32 characters |
| `Jwt__Issuer` | `lawgate-api` | |
| `Jwt__Audience` | `lawgate-client` | |
| `Jwt__ExpiryMinutes` | `60` | |
| `ASPNETCORE_ENVIRONMENT` | `Development` | Enables Swagger, auto-migration, seeder |

For local development without Docker, use .NET User Secrets:

```bash
cd backend
dotnet user-secrets init --project LegalDocSystem.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=lawgate_db;Username=lawgate_user;Password=lawgate_dev_password_change_in_production" --project LegalDocSystem.API
dotnet user-secrets set "Jwt:SecretKey" "dev-secret-key-minimum-32-characters-long" --project LegalDocSystem.API
```

### Frontend

Create `frontend/.env.local` (not committed):

```env
VITE_API_URL=http://localhost:5059/api
```

The Docker Compose setup injects this via the `environment` block in `docker-compose.yml`.

---

## Database Migrations

```bash
cd backend

# Create a new migration
dotnet ef migrations add <MigrationName> \
  --project LegalDocSystem.Infrastructure \
  --startup-project LegalDocSystem.API

# Apply pending migrations
dotnet ef database update \
  --project LegalDocSystem.Infrastructure \
  --startup-project LegalDocSystem.API

# Roll back to a specific migration
dotnet ef database update <MigrationName> \
  --project LegalDocSystem.Infrastructure \
  --startup-project LegalDocSystem.API

# Remove the last unapplied migration
dotnet ef migrations remove \
  --project LegalDocSystem.Infrastructure \
  --startup-project LegalDocSystem.API
```

In Docker Compose, migrations run automatically on backend startup.

---

## Resetting the database

Full wipe (removes Docker volume):

```bash
docker compose down -v
docker compose up
```

The backend re-runs all migrations and seeds fresh data.

Alternatively, use the PowerShell reset script:

```powershell
cd database
./recreate-database.ps1
```

---

## Logging

The backend uses Serilog. Logs are written to the console and to `backend/logs/legaldoc-<date>.txt` (rolling daily). The log level for EF Core SQL queries is `Information` in development.

---

## Connecting to PostgreSQL directly

```bash
docker exec -it lawgate-postgres psql -U lawgate_user -d lawgate_db
```

Useful psql commands:

```sql
\dt                  -- list tables
\d "Users"           -- describe Users table
SELECT * FROM "Companies";
SELECT id, email, "Role" FROM "Users";
\q                   -- quit
```

---

## CORS

The backend allows `http://localhost:5173` and `http://localhost:3000` by default (configured in `appsettings.json` under `Cors:AllowedOrigins`). Add origins as needed for other local ports.

---

## VS Code recommended extensions

```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.csdevkit",
    "ms-azuretools.vscode-docker",
    "dbaeumer.vscode-eslint",
    "esbenp.prettier-vscode",
    "bradlc.vscode-tailwindcss"
  ]
}
```
