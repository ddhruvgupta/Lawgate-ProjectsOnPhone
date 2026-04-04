# Testing

## Overview

| Suite | Framework | Location | Tests |
|-------|-----------|----------|-------|
| Backend unit tests | xUnit + EF Core InMemory | `backend/LegalDocSystem.UnitTests/` | 29 |
| Backend integration tests | xUnit + Testcontainers | `backend/LegalDocSystem.IntegrationTests/` | 29 |
| Frontend tests | Vitest + React Testing Library | `frontend/src/` | 43 |

---

## Backend Unit Tests

**Project:** `LegalDocSystem.UnitTests`

Unit tests use EF Core's `UseInMemoryDatabase` — no Docker or running PostgreSQL needed.

### Run

```bash
cd backend
dotnet test LegalDocSystem.UnitTests
```

### What is covered

| Test class | Tests |
|------------|-------|
| `AuthServiceTests` | Login (valid, wrong password, unknown, unverified, inactive); refresh token; forgot/reset password; email verification; resend verification (20 tests) |
| `ProjectServiceTests` | Create; list scoped to company; update/delete missing project throws (4 tests) |
| `UserServiceTests` | List scoped to company; duplicate email; hashed password; toggle active (4 tests) |

---

## Backend Integration Tests

**Project:** `LegalDocSystem.IntegrationTests`

Integration tests boot the full ASP.NET Core app using `WebApplicationFactory` against a real PostgreSQL container spun up by Testcontainers. **Docker must be running.**

### `TestWebAppFactory` setup

The factory handles all wiring automatically:
- Starts `postgres:16-alpine` via Testcontainers
- Replaces production `DbContext` with the test container connection
- Injects JWT config (`Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience`) so the app boots in testing mode
- Removes `DocumentCleanupService` (background job that queries DB before migrations run)
- Configures rate limiter with effectively unlimited permits in `Testing` environment
- Runs all EF Core migrations and seeds minimal test data

### Run

```bash
cd backend
dotnet test LegalDocSystem.IntegrationTests
```

### What is covered

| Test class | Tests |
|------------|-------|
| `AuthControllerTests` | Login; refresh token; forgot/reset password; verify email; resend verification (15 tests) |
| `ProjectControllerTests` | List; get by id; 404 for missing; create; update; delete (6 tests) |
| `UserControllerTests` | List; get; create; toggle active (4 tests) |
| `PlatformAdminControllerTests` | SuperAdmin can list/get companies; CompanyOwner gets 403 (4 tests) |

### Test credentials

| Email | Password | Role |
|-------|----------|------|
| owner@test.com | Test@1234 | CompanyOwner |
| superadmin@lawgate.com | Admin@1234 | PlatformSuperAdmin |

---

## Frontend Tests

**Framework:** Vitest + React Testing Library
**Setup file:** `frontend/src/test/setup.ts`

### Run

```bash
cd frontend
npm test              # single pass (exits with code)
npm run test:watch    # watch mode
npm run test:coverage # with V8 coverage report
```

### What is covered

| File | Tests |
|------|-------|
| `src/utils/cn.test.ts` | `cn` merges and de-duplicates class names; handles falsy values (8 tests) |
| `src/utils/formatters.test.ts` | `formatDate` ISO formatting; `formatFileSize` human-readable bytes; edge cases (12 tests) |
| `src/hooks/usePermissions.test.ts` | Correct permission booleans for every `UserRole`; unauthenticated returns false (17 tests) |
| `src/components/RoleGuard.test.tsx` | Renders children when role matches; renders fallback otherwise; array of roles (6 tests) |

---

## CI

Both backend suites and the frontend suite run in GitHub Actions. Integration tests need Docker — `ubuntu-latest` has it pre-installed.

```yaml
- name: Run backend tests
  run: dotnet test backend/ --no-restore

- name: Run frontend tests
  run: npm test
  working-directory: frontend
```
