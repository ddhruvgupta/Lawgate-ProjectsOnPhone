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
- [ ] Create Role controller
- [x] Add health check endpoint
- [x] Implement authorization policies
- [ ] Add input validation

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
- [ ] Update connection strings in docs
- [ ] Add backup/restore procedures
- [ ] Document all seed data

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
- [ ] Setup backend unit tests (xUnit)
- [ ] Setup frontend unit tests (Vitest)
- [ ] Create sample tests
- [ ] Setup integration tests (optional)
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

## Phase 7: Azure Deployment Preparation

### Azure Resource Setup
- [ ] Create Azure account
- [ ] Install Azure CLI
- [ ] Login to Azure (`az login`)
- [ ] Create resource group
- [ ] Create Azure Key Vault
- [ ] Store secrets in Key Vault

### Database Deployment
- [ ] Create Azure Database for PostgreSQL
- [ ] Configure firewall rules
- [ ] Apply migrations to Azure database
- [ ] Seed production data (without test users!)
- [ ] Test connection from local machine
- [ ] Setup automated backups

### Backend Deployment
- [ ] Create App Service Plan
- [ ] Create Web App
- [ ] Configure managed identity
- [ ] Grant Key Vault access
- [ ] Configure app settings
- [ ] Deploy backend code
- [ ] Test backend in Azure
- [ ] Configure custom domain (optional)
- [ ] Enable HTTPS

### Frontend Deployment
- [ ] Create Static Web App or Blob Storage
- [ ] Build production frontend
- [ ] Deploy frontend
- [ ] Configure environment variables
- [ ] Test frontend in Azure
- [ ] Configure custom domain (optional)
- [ ] Setup CDN (optional)

### Monitoring Setup
- [ ] Create Application Insights
- [ ] Configure backend logging
- [ ] Setup alerts
- [ ] Configure health checks
- [ ] Test monitoring dashboard

## Phase 8: CI/CD Setup

### GitHub Actions
- [ ] Create backend deployment workflow
- [ ] Create frontend deployment workflow
- [ ] Create database migration workflow
- [ ] Add secrets to GitHub repository
- [ ] Test automated deployment
- [ ] Setup staging environment (optional)

### Documentation
- [ ] Update README with deployment instructions
- [ ] Document CI/CD process
- [ ] Create runbook for common operations
- [ ] Document rollback procedures

## Phase 9: Production Launch

### Pre-Launch Checklist
- [ ] Remove test/debug code
- [ ] Update all environment variables
- [ ] Remove development seed data
- [ ] Test all critical user flows
- [ ] Perform security audit
- [ ] Test backup and restore
- [ ] Review and optimize database indexes
- [ ] Load testing (optional)
- [ ] Setup monitoring alerts

### Launch
- [ ] Deploy to production
- [ ] Verify all services running
- [ ] Test production environment
- [ ] Monitor for errors
- [ ] Create first production backup

### Post-Launch
- [ ] Monitor application logs
- [ ] Review performance metrics
- [ ] Gather user feedback
- [ ] Plan next iteration

## Phase 10: Maintenance & Improvement

### Regular Maintenance
- [ ] Setup weekly database backups
- [ ] Review and update dependencies monthly
- [ ] Monitor Azure costs
- [ ] Review security alerts
- [ ] Update documentation as needed

### Continuous Improvement
- [ ] Gather performance metrics
- [ ] Identify bottlenecks
- [ ] Implement optimizations
- [ ] Add new features based on feedback
- [ ] Refactor code as needed

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

**Last Updated**: 2026-03-27

**Target Completion**: TBD

**Recently Completed** (March 2026 — Phase 6 completion):
- Email verification: `IsEmailVerified`, `EmailVerificationToken`, `EmailVerificationTokenExpiry` on User; migration `20260408000000_AddEmailVerification`
- Password reset: `ForgotPasswordAsync`, `ResetPasswordAsync` in AuthService + controller endpoints
- Refresh token: `RefreshTokenAsync` with atomic rotation (SHA-256 hashed, prevents replay attacks)
- `IEmailService` + `ConsoleEmailService` (logs to console + `logs/emails/` files)
- Rate limiting: `FixedWindowLimiter` on auth (10/min) and global (100/min)
- Security headers middleware: CSP, X-Frame-Options, Referrer-Policy, CORP, Permissions-Policy
- Input sanitization middleware: strips HTML/script from all JSON request bodies
- In-memory caching: `IMemoryCache` on `CompanyService` (5-min TTL, cache-busting on update)
- API XML documentation: `GenerateDocumentationFile=true` in .csproj; comments on all 7 controllers; wired into Swagger
- Dark mode: Tailwind `darkMode: 'class'`; `useDarkMode` hook (persists to localStorage, respects OS preference); toggle button in Layout sidebar
- Accessibility: `role="alert"` on error divs; `aria-live="polite"` on toast container; `aria-label` on icon-only buttons; `<html lang="en">` with proper page title and meta description
- Frontend pages: `ForgotPasswordPage`, `ResetPasswordPage`, `VerifyEmailPage`
- Frontend routes: `/forgot-password`, `/reset-password`, `/verify-email`
- Unit tests: 28 tests (was 4) — fixed broken constructor, added tests for all new auth methods
- Integration tests: 15 tests (was 3) — added for refresh token, forgot password, reset password, verify email, resend verification

**Notes**:
- Auth is fully done (login, register, JWT, BCrypt).
- All core CRUD controllers and services are implemented.
- Azure Blob Storage integration is wired up (needs real connection string in secrets).
- Basic frontend structure is done but missing utilities/hooks/packages.
- Backend needs: DbSeeder, error handling middleware, input validation.
- Frontend needs: React Query, Hook Form, Zod, Heroicons, Headless UI, layout components.

**Blockers**:
- None currently.

**Next Steps**:
1. Install missing frontend packages (`react-hook-form`, `zod`, `@tanstack/react-query`, `@headlessui/react`, `@heroicons/react`, `clsx`).
2. Create `src/hooks/` and `src/utils/` directories and base layout component.
3. Create `DbSeeder.cs` to seed default roles and a test CompanyOwner user.
4. Add global error handling middleware to the API.
5. Run end-to-end integration test: register → login → API call with JWT.

---

**Remember**: Take it one phase at a time. The scaffolding is ready, now build something amazing! 🚀
