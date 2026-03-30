# Testing

## Overview

| Suite | Framework | Location | Tests |
|-------|-----------|----------|-------|
| Backend unit tests | xUnit + EF Core InMemory | `backend/LegalDocSystem.UnitTests/` | 29 |
| Backend integration tests | xUnit + Testcontainers | `backend/LegalDocSystem.IntegrationTests/` | 29 |
| Frontend tests | Vitest + React Testing Library | `frontend/src/` | 43 |

All 101 tests pass as of the initial implementation. Run them with:

```bash
# Backend (from backend/)
dotnet test LegalDocSystem.UnitTests
dotnet test LegalDocSystem.IntegrationTests   # requires Docker

# Frontend (from frontend/)
npm test
```

---

## Backend Unit Tests

**Project:** `LegalDocSystem.UnitTests`
**Dependencies:** xUnit, EF Core InMemory provider, BCrypt.Net

Unit tests use EF Core's `UseInMemoryDatabase` provider — no Docker or running PostgreSQL needed.

### What is covered

| Test class | Tests |
|------------|-------|
| `AuthServiceTests` | Login with valid credentials; wrong password; unknown email; unverified email; inactive user; refresh token (valid, invalid, expired); forgot password (sends email, unknown email no-op); reset password (valid, expired, invalid tokens); email verification (valid, expired, invalid tokens); resend verification |
| `ProjectServiceTests` | Create project (sets companyId and createdBy); list projects scoped to company; update missing project throws; delete missing project throws |
| `UserServiceTests` | List users scoped to company; duplicate email throws; password stored as hash not plaintext; toggle active status |

### Run

```bash
cd backend
dotnet test LegalDocSystem.UnitTests
```

---

## Backend Integration Tests

**Project:** `LegalDocSystem.IntegrationTests`
**Dependencies:** xUnit, Testcontainers.PostgreSql, Microsoft.AspNetCore.Mvc.Testing, FluentAssertions

Integration tests spin up a real PostgreSQL 16 container via Testcontainers and boot the full ASP.NET Core application with `WebApplicationFactory`. Docker must be running.

**`TestWebAppFactory`** handles the full setup:
- Starts a `postgres:16-alpine` Testcontainers instance
- Replaces the production `DbContext` with one pointed at the test container
- Injects JWT settings so the app boots in `Testing` mode
- Removes `DocumentCleanupService` (background job that runs before migrations in tests)
- Runs EF Core migrations and seeds minimal test data (one company, one CompanyOwner, one PlatformSuperAdmin)
- Rate limiter is configured with effectively unlimited permits when `ASPNETCORE_ENVIRONMENT=Testing`

### What is covered

| Test class | Tests |
|------------|-------|
| `AuthControllerTests` | Login (valid, wrong password, unknown email); refresh token (valid, invalid); forgot password (valid email, unknown email, invalid format); reset password (valid token, invalid token, mismatched passwords); verify email (valid, invalid token); resend verification (valid, unknown email) |
| `ProjectControllerTests` | List projects; get by id; get non-existent (404); create; update; delete |
| `UserControllerTests` | List users; get user; create user (invite); toggle active status |
| `PlatformAdminControllerTests` | List companies as SuperAdmin; get company as SuperAdmin; access as CompanyOwner returns 403 |

### Test credentials (injected by `TestWebAppFactory`)

| Email | Password | Role |
|-------|----------|------|
| `owner@test.com` | `Test@1234` | CompanyOwner |
| `superadmin@lawgate.com` | `Admin@1234` | PlatformSuperAdmin |

### Run

Docker must be running (Testcontainers pulls `postgres:16-alpine` automatically):

```bash
cd backend
dotnet test LegalDocSystem.IntegrationTests
```

---

## Frontend Tests

**Framework:** Vitest + React Testing Library
**Setup:** `frontend/src/test/setup.ts`

### What is covered

| File | Tests |
|------|-------|
| `src/utils/cn.test.ts` | `cn` merges class names; handles conditional classes; handles falsy values (8 tests) |
| `src/utils/formatters.test.ts` | `formatDate` formats ISO dates; `formatFileSize` bytes to human-readable; edge cases (0, null, large values) (12 tests) |
| `src/hooks/usePermissions.test.ts` | `usePermissions` returns correct booleans for every `UserRole` value; unauthenticated user returns false for all (17 tests) |
| `src/components/RoleGuard.test.tsx` | Renders children when role matches; renders fallback when unmatched; handles array of allowed roles (6 tests) |

### Run

```bash
cd frontend
npm test              # single pass (exits with code)
npm run test:watch    # watch mode
npm run test:coverage # with V8 coverage report
```

---

## CI

Both test suites run in GitHub Actions. Integration tests require Docker — use `ubuntu-latest` which has Docker pre-installed.

```yaml
- name: Run backend tests
  run: dotnet test backend/ --no-restore

- name: Run frontend tests
  run: npm test
  working-directory: frontend
```
