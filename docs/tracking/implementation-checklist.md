# Implementation Checklist

Use this checklist to track your progress in implementing the full application.

## Phase 1: Initial Setup (Start Here!)

### Prerequisites Installation
- [x] Install .NET 10 SDK
- [x] Install Node.js 24+
- [x] Install Docker Desktop
- [x] Install PostgreSQL tools (optional)
- [x] Install Entity Framework Core tools globally
- [x] Install Azure CLI (for deployment later)

### Project Initialization
- [x] Review `SETUP-COMPLETE.md`
- [x] Read `docs/claude-main-context.md`
- [x] Initialize Git repository
- [x] Create GitHub repository (optional)
- [x] Review all documentation structure

## Phase 2: Frontend Setup

### Create React Project
- [x] Run `npm create vite@latest` in frontend folder
- [x] Choose React + TypeScript template
- [x] Install base dependencies (`npm install`)

### Install Frontend Packages
- [x] Install Tailwind CSS and configure
- [x] Install React Router (`react-router-dom`)
- [x] Install Axios (`axios`)
- [x] Install React Hook Form + Zod
- [x] Install Headless UI components
- [x] Install Heroicons
- [x] Install React Query
- [x] Install clsx utility

### Configure Frontend
- [x] Setup Tailwind CSS (tailwind.config.js)
- [x] Create `.env.local` from `.env.example`
- [x] Configure Vite (vite.config.ts)
- [x] Setup TypeScript (tsconfig.json)
- [x] Configure ESLint and Prettier

### Create Frontend Structure
- [x] Create `src/components/` directory
- [x] Create `src/pages/` directory
- [x] Create `src/services/` directory
- [x] Create `src/hooks/` directory
- [x] Create `src/contexts/` directory
- [x] Create `src/utils/` directory
- [x] Create `src/types/` directory

### Build Core Frontend Components
- [x] Create API service layer (`services/api.ts`)
- [x] Create Auth context (`contexts/AuthContext.tsx`)
- [x] Create Protected Route component
- [x] Setup React Router in `App.tsx`
- [x] Create basic layout components
- [x] Create login page
- [x] Create registration page
- [x] Create dashboard page
- [x] Create projects page
- [x] Create project detail page
- [x] Create team management page
- [x] Create activity log page
- [x] Create platform admin page

## Phase 3: Backend Setup

### Create .NET Project
- [x] Run `dotnet new webapi` in backend folder
- [x] Install required NuGet packages
- [x] Setup project structure (Controllers, Models, Data, Services)

### Database Configuration
- [x] Create `ApplicationDbContext.cs`
- [x] Configure connection strings
- [x] Setup User Secrets for development
- [x] Create entity models (User, Role, AuditLog)
- [x] Create entity configurations

### Create Initial Migration
- [x] Run `dotnet ef migrations add InitialCreate`
- [x] Review generated migration
- [x] Create `DbSeeder.cs` for initial data
- [x] Test database creation

### Implement Authentication
- [x] Setup JWT configuration
- [x] Create authentication service
- [x] Implement password hashing (BCrypt)
- [x] Create auth controllers (login, register)
- [x] Add JWT middleware

### Build API Endpoints
- [x] Create User controller
- [x] Create Company controller
- [x] Create Project controller
- [x] Create Document controller
- [x] Create Audit controller
- [x] Create Platform Admin controller
- [x] Create Role controller
- [x] Add health check endpoint
- [x] Implement authorization policies
- [x] Add input validation (DTO validation attributes: `[Required]`, `[EmailAddress]`, `[MinLength]`, `[Phone]` on all auth DTOs)

### Configure API
- [x] Setup Swagger/OpenAPI
- [x] Configure CORS
- [x] Add error handling middleware
- [x] Setup logging (Serilog)
- [ ] Add API versioning (optional)

## Phase 4: Database Setup

### Local Development
- [x] Start PostgreSQL via Docker (`docker-compose up -d postgres`)
- [x] Test connection to database
- [x] Run `database/recreate-database.ps1`
- [x] Verify migrations applied
- [x] Verify seed data created
- [x] Test with default users

### Documentation
- [x] Document current schema in `database/docs/schema-changelog.md`
- [x] Update connection strings in docs
- [x] Add backup/restore procedures
- [x] Document all seed data

## Phase 5: Integration & Testing

### Local Development Environment
- [x] Test frontend connects to backend
- [x] Test user registration flow
- [x] Test user login flow
- [x] Test protected routes
- [x] Test API calls with authentication
- [x] Test error handling

### Docker Integration
- [x] Build backend Docker image
- [x] Build frontend Docker image
- [x] Test complete Docker Compose setup
- [x] Verify hot reload works
- [x] Test container networking

### Testing Setup
- [x] Setup backend unit tests (xUnit) — 52 tests, all passing
- [x] Setup frontend unit tests (Vitest) — 43 tests, all passing
- [x] Create sample tests
- [x] Setup integration tests — 57 tests, all passing (xUnit + Testcontainers)
- [ ] Setup E2E tests with Playwright (optional)

## Phase 6: Advanced Features ✅ COMPLETE

### Backend Enhancements
- [x] Implement refresh tokens
- [x] Implement audit logging
- [x] Add email verification (backend service + frontend pages + routes)
- [x] Add password reset functionality (forgot-password + reset-password endpoints)
- [x] Implement rate limiting (auth: 10/min, global: 100/min)
- [x] Add request/response compression
- [x] Add caching layer (IMemoryCache on CompanyService — 5-min TTL with cache invalidation on update)
- [x] Add API documentation (XML doc comments on all controllers; wired into Swagger)

### Frontend Enhancements
- [x] Implement toast notifications
- [x] Add form validation feedback
- [x] Add loading states (LoadingSkeleton, CardSkeleton components)
- [x] Add error boundaries (ErrorBoundary component wrapping all protected routes)
- [x] Create reusable UI components library (ErrorBoundary, LoadingSkeleton, RoleGuard, ProjectStatusBadge, ToastContainer)
- [x] Add dark mode toggle (class-based via Tailwind `darkMode: 'class'`; persists to localStorage; respects OS preference)
- [x] Implement responsive design (Tailwind responsive utilities across all pages)
- [x] Add accessibility features (role=alert on errors, aria-live on toasts, aria-label on icon buttons, semantic HTML5, lang attribute)

### Security Hardening
- [x] Review and fix security vulnerabilities (OWASP Top 10 review completed)
- [x] Implement CSRF protection (JWT Bearer-only auth prevents cross-origin forgery; CSRF posture documented in SecurityHeadersMiddleware)
- [x] Add rate limiting on API (FixedWindowLimiter on auth + global endpoints)
- [x] Setup security headers (X-Content-Type-Options, X-Frame-Options, CSP, Referrer-Policy, CORP, Permissions-Policy)
- [x] Implement input sanitization (InputSanitizationMiddleware strips HTML/script tags from all JSON request bodies)
- [x] Add SQL injection protection (EF Core parameterized queries throughout)
- [x] Review and restrict CORS origins (explicit AllowedOrigins list in appsettings.json; no wildcard)

### Email Infrastructure
- [x] Azure Communication Services resource deployed (data residency: India — lawgate-prod-acs)
- [x] Email Communication Service + Azure Managed Domain provisioned
- [x] AcsEmailService implemented using Azure.Communication.Email SDK
- [x] ACS connection string + sender domain stored in dotnet user-secrets (dev) and Key Vault (prod)
- [x] Production: AcsEmailService registered; Development: ConsoleEmailService registered (logs to file)

### ✅ All Production-Blocking Gaps Resolved (March 2026)

- [x] **Email verification enforced at login** — `LoginAsync` blocks login and throws `UnauthorizedAccessException` for unverified users; frontend shows resend-verification link on login error.
- [x] **DTO validation attributes added** — `LoginDto`, `RegisterDto`, `RefreshTokenRequest` all have `[Required]`, `[EmailAddress]`, `[MinLength]`, `[Phone]` annotations; `ModelState.IsValid` now catches bad input.
- [x] **TestController removed** — Unauthenticated seed endpoint `api/test/seed-company` deleted.
- [x] **`UserDto` includes `IsEmailVerified`** — Returned in every token response; frontend shows a warning banner on dashboard and a resend link on login when unverified.
- [x] **JWT secret removed from appsettings.json** — `SecretKey` is now empty in `appsettings.json`; populated via user-secrets/Key Vault; startup throws `InvalidOperationException` if missing.
- [x] **SmtpEmailService dead code removed** — File deleted; `AcsEmailService` is the sole email implementation.
- [x] **SmtpEmailService dead code removed** — File deleted; `AcsEmailService` (prod) and `ConsoleEmailService` (dev) are the sole implementations.

## Phase 7: Azure Deployment Preparation

### Infrastructure as Code (Bicep) ✅ COMPLETE
- [x] Create Azure Bicep templates — all modules exist in `infra/`
- [x] App Service Plan + Web App (`infra/modules/app-service.bicep`) — Linux B1, .NET 10, system-assigned managed identity, health check at `/health`, HTTPS-only
- [x] Azure Database for PostgreSQL Flexible Server (`infra/modules/database.bicep`)
- [x] Azure Blob Storage account (`infra/modules/storage.bicep`) — `legal-documents` container
- [x] Azure Key Vault (`infra/modules/keyvault.bicep`) — stores DB connection string, JWT secret, ACS connection string, SAS key via RBAC
- [x] Application Insights + Log Analytics Workspace (`infra/modules/monitoring.bicep`)
- [x] Azure Static Web App (`infra/modules/static-web-app.bicep`) — Free tier, GitHub Actions deployment
- [x] Azure Communication Services (`infra/modules/communication.bicep`) — existing resource referenced in `main.bicep` via `existing` keyword
- [x] Parameter file (`infra/main.bicepparam`) — non-sensitive params committed; secrets passed at deploy time
- [x] Full deployment script (`infra/scripts/deploy.ps1`) — runs what-if, Bicep deploy, firewall rule, EF migrations, backend zip-deploy, frontend SWA deploy in one command

### CI/CD Pipelines ✅ COMPLETE
- [x] GitHub Actions CI workflow (`.github/workflows/ci.yml`) — runs on every PR/push to main: backend unit tests (52), integration tests (57), frontend type-check + lint + unit tests (43) + production build
- [x] GitHub Actions backend deploy (`.github/workflows/deploy-backend.yml`) — triggered on push to `main` when `backend/` changes; builds, publishes linux-x64, runs EF migrations, zip-deploys to App Service, verifies `/health`
- [x] GitHub Actions frontend deploy (`.github/workflows/deploy-frontend.yml`) — triggered on push to `main` when `frontend/` changes; builds with production env vars, deploys to Static Web App

### Required GitHub Secrets (user action needed)
Add these in **GitHub → Settings → Secrets and variables → Actions → New repository secret**:

| Secret | How to obtain |
|---|---|
| `AZURE_CREDENTIALS` | `az ad sp create-for-rbac --name lawgate-github --role contributor --scopes /subscriptions/<id>/resourceGroups/<rg> --sdk-auth` |
| `AZURE_WEBAPP_NAME` | Output of `az webapp list --rg <rg> --query '[0].name' -o tsv` after Bicep deploy |
| `AZURE_API_URL` | Bicep output `apiUrl` (e.g. `https://lawgate-prod-api-abc123.azurewebsites.net`) |
| `AZURE_DATABASE_CONNECTION_STRING` | For migrations: `Host=<fqdn>;Database=lawgate_db;Username=lawgate_admin;Password=<pw>;SslMode=Require` |
| `JWT_SECRET_KEY` | Same value used during Bicep deploy |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | `az staticwebapp secrets list --name <swa-name> --query 'properties.apiKey' -o tsv` |
| `VITE_API_URL` | Backend API URL + `/api` (e.g. `https://lawgate-prod-api-abc123.azurewebsites.net/api`) |

### Actual Azure Deployment (user action needed)
- [ ] Run `az login` and select the correct subscription
- [ ] Create resource group: `az group create --name project-management --location eastus`
- [ ] Run `.\infra\scripts\deploy.ps1 -ResourceGroup project-management -Location eastus`
- [ ] Verify backend health: `curl https://<app-name>.azurewebsites.net/health`
- [ ] Verify frontend loads at the Static Web App URL
- [ ] Add GitHub Actions secrets (table above)
- [ ] Trigger a deployment via `git push main` and confirm Actions run green

### Post-Deployment Verification (user action needed)
- [ ] Register a new user and complete email verification flow end-to-end
- [ ] Create a project and upload a document
- [ ] Check Application Insights for any errors
- [ ] Confirm logs appear in Log Analytics Workspace

## Phase 8: Production Launch

### Pre-Launch Checklist
- [x] No test/debug code in codebase — `TestController` removed in Phase 6
- [x] JWT secret not in source code — empty in `appsettings.json`, required at startup
- [x] Development seed data is dev-only — `DbSeeder` only runs in `Development` environment
- [x] OWASP Top 10 review completed — see Phase 6 Security Hardening
- [x] Rate limiting configured — auth: 10/min, global: 100/min
- [x] Security headers middleware active — CSP, X-Frame-Options, etc.
- [ ] Custom domain configured (optional — add `CNAME` to your DNS after deployment)
- [x] Review and optimize database indexes — `AddPerformanceIndexes` migration added (ix_documents_project_status, ix_auditlogs_company_entity, ix_users_refreshtoken)
- [ ] Load test the `/api/documents/upload-url` and `/api/auth/login` endpoints before launch

### Database Index Review
The following queries will be expensive at scale without indexes. Run after first deployment:
```sql
-- Documents: lookup by project + status (used in list queries)
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_documents_project_status
  ON "Documents" ("ProjectId", "Status");

-- AuditLogs: company + entity type queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_auditlogs_company_entity
  ON "AuditLogs" ("CompanyId", "EntityType");

-- RefreshTokens: lookup by token hash (login/refresh flow)
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_users_refreshtoken
  ON "Users" ("RefreshToken") WHERE "RefreshToken" IS NOT NULL;
```
Add these as an EF Core migration if you decide to make them permanent.

### Launch
- [ ] Run full deployment (Phase 7 steps above)
- [ ] Smoke-test all critical user flows in production
- [ ] Create first production database backup
- [ ] Monitor Application Insights for 24 hours post-launch

## Phase 9: Maintenance & Improvement

### Regular Maintenance
- [ ] Setup Azure Database automated backups (enable in Azure Portal → PostgreSQL → Backup)
- [ ] Review and update NuGet/npm dependencies monthly (`dotnet outdated`, `npm outdated`)
- [ ] Monitor Azure costs monthly via Cost Management
- [ ] Review security advisories — two **existing** vulnerabilities flagged at build time:
  - `MailKit 4.15.1` — moderate severity ([GHSA-9j88-vvj5-vhgr](https://github.com/advisories/GHSA-9j88-vvj5-vhgr)) — upgrade when fixed
  - `System.Security.Cryptography.Xml 9.0.0` — high severity ([GHSA-37gx-xxp4-5rgx](https://github.com/advisories/GHSA-37gx-xxp4-5rgx)) — upgrade when .NET 10 patch available
- [ ] Update documentation as architecture evolves

### Continuous Improvement
- [ ] Add missing features from open GitHub Issues (see issue list)
- [ ] Implement document versioning (`GET /api/documents/{id}/versions`) — Issue #12
- [ ] Implement project-level permission grant/revoke endpoints — Issue #8
- [ ] Document upload UI on `ProjectDetailPage` (currently placeholder) — Issue #20
- [ ] Implement document tagging endpoints — Issue #10

## Phase 10: Search (Future)

### Azure AI Search — full-text document search scoped by company (Issue #38)

#### Infrastructure
- [ ] Add `infra/modules/search.bicep` — Azure AI Search resource (Free tier for MVP, Basic for prod SLA)
- [ ] Wire into `infra/main.bicep`; store endpoint + admin key in Key Vault
- [ ] Configure indexer: storage account data source → index with field mappings (`companyId`, `projectId`, `fileName`, `documentType`, `tags`, `content`, `uploadedAt`, `status`)
- [ ] Grant indexer read access to blob storage via managed identity (preferred over key-based)

#### Backend
- [ ] Add `ISearchService` interface to `LegalDocSystem.Application/Interfaces/`
- [ ] Add `AzureSearchService` to `LegalDocSystem.Infrastructure/Services/` using `Azure.Search.Documents` NuGet (Infrastructure-only — Application layer stays SDK-free)
- [ ] Add `SearchDocumentsQuery` + MediatR handler in Application layer
- [ ] Add `GET /api/documents/search?q={query}&projectId={id}` to `DocumentsController`
- [ ] Mandatory server-side filter: `companyId eq {user.CompanyId}` — never trust client to scope their own tenant

#### Frontend
- [ ] Search bar on Projects page and Project Detail page
- [ ] `useDocumentSearch` debounced custom hook (React Query)
- [ ] Results list with document name, type, project, and content snippet highlighting

#### Design Notes
- Tenant isolation is enforced by `companyId` filter at the API layer — not by container-level access in Search
- Azure AI Search's enrichment pipeline handles content extraction from PDF/Word/etc blobs automatically
- One indexer for all containers; `companyId` stored as a filterable field on every indexed document


### Technical Debt
- [x] Fix `IBlobStorageService` interface — `GetSasUri` parameter now uses `StorageAccessPermissions` enum (defined in `LegalDocSystem.Domain.Enums`). Removed `Azure.Storage.Sas.BlobSasPermissions` from Application layer entirely. `AzureBlobStorageService` maps the enum to Azure's type internally. `Azure.Storage.Blobs` NuGet package removed from `LegalDocSystem.Application.csproj`. Config key renamed from `AzureStorage` → `BlobStorage` in both `appsettings.json` files. See `docs/architecture/storage-provider-decision.md` for context.

## Important Reminders

### Security
- ❌ Never commit secrets to Git
- ✅ Use Azure Key Vault for production
- ✅ Use User Secrets for development
- ✅ Rotate secrets regularly
- ✅ Keep dependencies updated

### Documentation
- ✅ Update `schema-changelog.md` with every migration
- ✅ Update component docs when adding features
- ✅ Keep `claude.md` files current for AI context
- ✅ Document all major decisions

### Database
- ✅ Always create migrations for schema changes
- ✅ Test migrations on copy of production first
- ✅ Backup before applying migrations in production
- ✅ Never manually modify production database
- ✅ Keep `DbSeeder.cs` updated

### Testing
- ✅ Write tests for critical functionality
- ✅ Run tests before deployment
- ✅ Test in staging environment
- ✅ Have rollback plan ready

## Progress Tracking

Use this section to track your progress:

**Current Phase**: Phase 7 (Azure Deployment Preparation)

**Started**: 2025-01-20

## Progress Tracking

**Current Phase**: Phase 7 — Azure Deployment (infrastructure code complete; user must run `deploy.ps1`)

**Last Updated**: 2026-04-25

**Recently Completed** (April 2026):
- Cloud-provider independence: `StorageAccessPermissions` enum replaces `BlobSasPermissions` in Application layer; `Azure.Storage.Blobs` removed from `Application.csproj`; config key `AzureStorage` → `BlobStorage`
- Fixed `app-service.bicep`: App Setting key renamed from `AzureStorage__ConnectionString` → `ConnectionStrings__BlobStorage`
- Fixed `deploy.ps1`: SWA deployment token retrieved via `az staticwebapp secrets list` (not Bicep outputs)
- GitHub Actions workflows created: `ci.yml`, `deploy-backend.yml`, `deploy-frontend.yml`
- GitHub Issues #9, #22, #23 closed (audit logging, unit tests, integration tests — all complete)

**Recently Completed** (April 2026 — earlier):
- Project `StartDate`/`EndDate` changed from `timestamp with time zone` → `date` (DateOnly); migration `20260403210848_ChangeProjectDatesToDateOnly`
- Unit tests expanded: 52 total — full coverage for RegisterAsync, ProjectService
- Integration tests expanded: 57 total — full coverage for register endpoint, ProjectController
- All 3 Copilot PR #33 feedback items resolved

**Recently Completed** (March 2026 — Phase 6):
- Email verification: `IsEmailVerified`, `EmailVerificationToken`, `EmailVerificationTokenExpiry` on User; migration `20260408000000_AddEmailVerification`
- Password reset: `ForgotPasswordAsync`, `ResetPasswordAsync` in AuthService + controller endpoints
- Refresh token: `RefreshTokenAsync` with atomic rotation (SHA-256 hashed, prevents replay attacks)
- `IEmailService` + `ConsoleEmailService` (logs to console + `logs/emails/` files); `AcsEmailService` for Production
- Rate limiting: `FixedWindowLimiter` on auth (10/min) and global (100/min)
- Security headers middleware: CSP, X-Frame-Options, Referrer-Policy, CORP, Permissions-Policy
- Input sanitization middleware: strips HTML/script from all JSON request bodies
- In-memory caching: `IMemoryCache` on `CompanyService` (5-min TTL, cache-busting on update)
- API XML documentation: `GenerateDocumentationFile=true` in .csproj; comments on all 7 controllers; wired into Swagger
- Dark mode: Tailwind `darkMode: 'class'`; `useDarkMode` hook (persists to localStorage, respects OS preference)
- Accessibility: `role="alert"` on error divs; `aria-live="polite"` on toast container; `aria-label` on icon-only buttons
- Frontend pages: `ForgotPasswordPage`, `ResetPasswordPage`, `VerifyEmailPage`
- Frontend routes: `/forgot-password`, `/reset-password`, `/verify-email`
- Azure IaC: full Bicep modules for App Service, PostgreSQL, Key Vault, Storage, ACS, Static Web App, Monitoring

**Blockers**:
- None currently.

**Next Steps** (Phase 7):
1. Provision Azure resources using `infra/main.bicep` (see `infra/README.md`)
2. Store ACS connection string + sender domain in Key Vault → verify real emails send in Production
3. Apply EF Core migrations to Azure PostgreSQL
4. Deploy backend to Azure App Service; configure managed identity + Key Vault references
5. Deploy frontend to Azure Static Web Apps; retrieve deployment token via `az staticwebapp secrets list`
6. Configure GitHub Actions CI/CD workflows (Phase 8)

---

**Remember**: Take it one phase at a time. The scaffolding is ready, now build something amazing! 🚀
