# Test Results - Legal Document Management System

**Last Updated:** March 30, 2026
**Test Status:** ✅ ALL TESTS PASSED

---

## Summary

| Suite | Tests | Passed | Failed | Status |
|-------|-------|--------|--------|--------|
| Backend Unit (xUnit + EF InMemory) | 29 | 29 | 0 | ✅ |
| Backend Integration (xUnit + Testcontainers) | 29 | 29 | 0 | ✅ |
| Frontend (Vitest + RTL) | 43 | 43 | 0 | ✅ |
| **TOTAL** | **101** | **101** | **0** | **✅** |

---

## Backend Unit Tests (29)

```
dotnet test LegalDocSystem.UnitTests
Passed!  - Failed: 0, Passed: 29, Skipped: 0, Total: 29, Duration: 8s
```

### AuthService (20 tests)

| Test | Status |
|------|--------|
| `LoginAsync_WithValidCredentials_ReturnsToken` | ✅ |
| `LoginAsync_WithUnknownEmail_ThrowsUnauthorizedAccessException` | ✅ |
| `LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException` | ✅ |
| `LoginAsync_WithInactiveUser_ThrowsUnauthorizedAccessException` | ✅ |
| `LoginAsync_WithUnverifiedEmail_ThrowsUnauthorizedAccessException` | ✅ |
| `RefreshTokenAsync_WithInvalidToken_ThrowsUnauthorizedAccessException` | ✅ |
| `RefreshTokenAsync_WithExpiredToken_ThrowsUnauthorizedAccessException` | ✅ |
| `ForgotPasswordAsync_WithValidEmail_StoresResetToken` | ✅ |
| `ForgotPasswordAsync_WithValidEmail_SendsPasswordResetEmail` | ✅ |
| `ForgotPasswordAsync_WithUnknownEmail_DoesNotThrowAndDoesNotSendEmail` | ✅ |
| `ResetPasswordAsync_WithValidToken_ReturnsTrue` | ✅ |
| `ResetPasswordAsync_WithValidToken_UpdatesPasswordHash` | ✅ |
| `ResetPasswordAsync_WithValidToken_ClearsRefreshToken` | ✅ |
| `ResetPasswordAsync_WithExpiredToken_ReturnsFalse` | ✅ |
| `ResetPasswordAsync_WithInvalidToken_ReturnsFalse` | ✅ |
| `VerifyEmailAsync_WithValidToken_ReturnsTrueAndSetsEmailVerified` | ✅ |
| `VerifyEmailAsync_WithExpiredToken_ReturnsFalse` | ✅ |
| `VerifyEmailAsync_WithInvalidToken_ReturnsFalse` | ✅ |
| `ResendVerificationEmailAsync_WithValidEmail_SendsVerificationEmail` | ✅ |
| `ResendVerificationEmailAsync_WithAlreadyVerifiedEmail_DoesNotSendEmail` | ✅ |
| `ResendVerificationEmailAsync_WithUnknownEmail_DoesNotThrowAndDoesNotSendEmail` | ✅ |

### ProjectService (4 tests)

| Test | Status |
|------|--------|
| `CreateProjectAsync_SetsCompanyIdAndCreatedByCorrectly` | ✅ |
| `GetProjectsAsync_ReturnsOnlyProjectsForGivenCompanyId` | ✅ |
| `UpdateProjectAsync_WithMissingProject_ThrowsKeyNotFoundException` | ✅ |
| `DeleteProjectAsync_WithMissingProject_ThrowsKeyNotFoundException` | ✅ |

### UserService (4 tests)

| Test | Status |
|------|--------|
| `GetUsersAsync_ReturnsOnlyUsersForGivenCompanyId` | ✅ |
| `CreateUserAsync_WhenEmailAlreadyExists_ThrowsInvalidOperationException` | ✅ |
| `CreateUserAsync_StoresHashedPasswordNotPlaintext` | ✅ |
| `ToggleUserStatusAsync_FlipsIsActiveCorrectly` | ✅ |

---

## Backend Integration Tests (29)

Requires Docker. `TestWebAppFactory` spins up a real `postgres:16-alpine` container via Testcontainers, runs EF Core migrations, and boots the full ASP.NET Core application.

```
dotnet test LegalDocSystem.IntegrationTests
Passed!  - Failed: 0, Passed: 29, Skipped: 0, Total: 29, Duration: 9s
```

### AuthController (17 tests)

| Test | Status |
|------|--------|
| `Login_WithValidCredentials_Returns200WithToken` | ✅ |
| `Login_WithWrongPassword_Returns401` | ✅ |
| `Login_WithUnknownEmail_Returns401` | ✅ |
| `RefreshToken_WithValidToken_Returns200WithNewTokens` | ✅ |
| `RefreshToken_WithInvalidToken_Returns401` | ✅ |
| `ForgotPassword_WithValidEmail_Returns200` | ✅ |
| `ForgotPassword_WithUnknownEmail_StillReturns200` | ✅ |
| `ForgotPassword_WithInvalidEmailFormat_Returns400` | ✅ |
| `ResetPassword_WithValidToken_Returns200` | ✅ |
| `ResetPassword_WithInvalidToken_Returns400` | ✅ |
| `ResetPassword_WithMismatchedPasswords_Returns400` | ✅ |
| `VerifyEmail_WithValidToken_Returns200` | ✅ |
| `VerifyEmail_WithInvalidToken_Returns400` | ✅ |
| `ResendVerification_WithValidEmail_Returns200` | ✅ |
| `ResendVerification_WithUnknownEmail_StillReturns200` | ✅ |

### ProjectController, UserController, PlatformAdminController (14 tests)

| Test | Status |
|------|--------|
| `GetProjects_Returns200WithList` | ✅ |
| `GetProject_WithExistingId_Returns200` | ✅ |
| `GetProject_WithNonExistentId_Returns404` | ✅ |
| `CreateProject_Returns201WithCreatedProject` | ✅ |
| `UpdateProject_Returns200WithUpdatedProject` | ✅ |
| `DeleteProject_Returns204` | ✅ |
| `GetUsers_Returns200WithList` | ✅ |
| `GetUser_Returns200` | ✅ |
| `CreateUser_Returns201` | ✅ |
| `ToggleUserStatus_Returns200` | ✅ |
| `GetCompanies_AsSuperAdmin_Returns200` | ✅ |
| `GetCompany_AsSuperAdmin_Returns200WithCompanyOverview` | ✅ |
| `GetCompanies_AsCompanyOwner_Returns403` | ✅ |
| `GetCompany_AsCompanyOwner_Returns403` | ✅ |

---

## Frontend Tests (43)

```
cd frontend && npm test
Test Files  4 passed (4)
Tests       43 passed (43)
Duration    5.47s
```

| File | Tests | Status |
|------|-------|--------|
| `src/utils/cn.test.ts` | 8 | ✅ |
| `src/utils/formatters.test.ts` | 12 | ✅ |
| `src/hooks/usePermissions.test.ts` | 17 | ✅ |
| `src/components/RoleGuard.test.tsx` | 6 | ✅ |

---

## Build Verification

```
dotnet build LegalDocSystem.sln -c Debug
Build succeeded.  0 Warning(s)  0 Error(s)
```

| Project | Status |
|---------|--------|
| LegalDocSystem.Domain | ✅ |
| LegalDocSystem.Application | ✅ |
| LegalDocSystem.Infrastructure | ✅ |
| LegalDocSystem.API | ✅ |
| LegalDocSystem.UnitTests | ✅ |
| LegalDocSystem.IntegrationTests | ✅ |

---

## Fixes Applied (March 30, 2026)

| Item | Fix |
|------|-----|
| `GlobalExceptionMiddleware` build failure (static method + bare `throw`) | Made method non-static; replaced `throw` with `return Task.CompletedTask` |
| `POST /projects` returning `status: "0"` | Changed `ProjectDto.Status` from `string` to `ProjectStatus` enum |
| Integration tests failing — `Jwt:SecretKey not configured` | Injected JWT settings in `TestWebAppFactory` |
| Integration tests failing — `Documents table does not exist` | Removed `DocumentCleanupService` from test host |
| Integration tests returning 429 Too Many Requests | Rate limiter uses unlimited permits when `ASPNETCORE_ENVIRONMENT=Testing` |
| Migration `AddEmailVerification` not applied in tests | Created missing `.Designer.cs` file |
| Seeded users blocked by email verification at login | Added `IsEmailVerified = true` to all seeded users in `DbSeeder` and `TestWebAppFactory` |

---

## Security Fixes Applied (March 29, 2026)

| Item | Status |
|------|--------|
| Email verification enforced at login | ✅ |
| DTO validation attributes (`[Required]`, `[EmailAddress]`, `[MinLength]`) | ✅ |
| TestController (unauthenticated seed endpoint) removed | ✅ |
| `IsEmailVerified` propagated to `UserDto` and all construction sites | ✅ |
| JWT secret removed from `appsettings.json`; startup validates presence | ✅ |

---

## Seeded Credentials (Development)

All seeded users have `IsEmailVerified = true`.

| Email | Password | Role |
|-------|----------|------|
| admin@demolawfirm.com | Admin@123 | CompanyOwner |
| jane.doe@demolawfirm.com | User@123 | User |
| admin@lawgate.io | LawgatePlatform@1 | PlatformAdmin |
| superadmin@lawgate.io | LawgateSuperAdmin@1 | PlatformSuperAdmin |

---

**Environment:** Development (Local Docker)
**Test Runner:** xUnit + FluentAssertions + Testcontainers / Vitest + RTL
**Branch:** dgupta/auth-updates
