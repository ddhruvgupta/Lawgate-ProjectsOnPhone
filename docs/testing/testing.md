# Testing

## Overview

| Suite | Framework | Location | Tests |
|-------|-----------|----------|-------|
| Backend unit tests | xUnit + EF Core InMemory | `backend/LegalDocSystem.UnitTests/` | 52 |
| Backend integration tests | xUnit + Testcontainers | `backend/LegalDocSystem.IntegrationTests/` | 57 |
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
| `AuthServiceTests` | Register (company created, user created, role, email unverified, hashed password, token returned, email sent, null phone, duplicate company email, duplicate user email); login (valid, wrong password, unknown, unverified, inactive); refresh token (invalid, expired); forgot/reset password; email verification; resend verification (31 tests) |
| `ProjectServiceTests` | GetProjects (empty, company isolation, descending order, document count); GetProject (valid, 404, wrong company 404); CreateProject (all fields, DateOnly dates, null dates, status); UpdateProject (all fields + audit, 404, wrong company 404); DeleteProject (removes from DB, 404, wrong company 404) (17 tests) |
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
| `AuthControllerTests` | Register (200+token, persists user, email unverified, trial company, with/without phone, missing fields 400, invalid email 400, short password 400, duplicate emails 400); login; refresh token; forgot/reset password; verify email; resend verification (29 tests) |
| `ProjectControllerTests` | Auth (401 without token on all mutations); get list; get by id; get 404; get correct fields; create minimal/full/with-dates; create missing name 400; create writes audit log; update 200; update 404; update missing name 400; update clears dates; delete 204; delete then 404; delete 404; delete as User 403 (20 tests) |
| `UserControllerTests` | List; get; create; toggle active (4 tests) |
| `PlatformAdminControllerTests` | SuperAdmin can list/get companies; CompanyOwner gets 403 (4 tests) |

### Test credentials

| Email | Password | Role |
|-------|----------|------|
| `owner@test.com` | `Test@1234` | CompanyOwner |
| `member@test.com` | `Test@1234` | User |
| `superadmin@lawgate.com` | `Admin@1234` | PlatformSuperAdmin |

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
