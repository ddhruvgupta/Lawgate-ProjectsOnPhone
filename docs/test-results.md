# Test Results - Legal Document Management System

**Last Updated:** April 4, 2026
**Test Status:** ✅ ALL TESTS PASSED

---

## Summary

| Suite | Tests | Passed | Failed | Status |
|-------|-------|--------|--------|--------|
| Backend Unit (xUnit + EF InMemory) | 52 | 52 | 0 | ✅ |
| Backend Integration (xUnit + Testcontainers) | 57 | 57 | 0 | ✅ |
| Frontend (Vitest + RTL) | 43 | 43 | 0 | ✅ |
| **TOTAL** | **152** | **152** | **0** | **✅** |

---

## Backend Unit Tests (52)

```
dotnet test LegalDocSystem.UnitTests
Passed!  - Failed: 0, Passed: 52, Skipped: 0, Total: 52
```

### AuthService (31 tests)

**Register (10)**

| Test | Status |
|------|--------|
| `RegisterAsync_CreatesCompanyWithCorrectName` | ✅ |
| `RegisterAsync_CreatesUserWithCompanyOwnerRole` | ✅ |
| `RegisterAsync_SetsIsEmailVerifiedFalse` | ✅ |
| `RegisterAsync_StoresHashedPassword` | ✅ |
| `RegisterAsync_ReturnsToken` | ✅ |
| `RegisterAsync_SendsVerificationEmail` | ✅ |
| `RegisterAsync_WithNullPhone_Succeeds` | ✅ |
| `RegisterAsync_WithDuplicateCompanyEmail_ThrowsInvalidOperationException` | ✅ |
| `RegisterAsync_WithDuplicateUserEmail_ThrowsInvalidOperationException` | ✅ |
| `RegisterAsync_WhenEmailServiceFails_StillCompletesRegistration` | ✅ |

**Login (5)**

| Test | Status |
|------|--------|
| `LoginAsync_WithValidCredentials_ReturnsToken` | ✅ |
| `LoginAsync_WithUnknownEmail_ThrowsUnauthorizedAccessException` | ✅ |
| `LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException` | ✅ |
| `LoginAsync_WithInactiveUser_ThrowsUnauthorizedAccessException` | ✅ |
| `LoginAsync_WithUnverifiedEmail_ThrowsUnauthorizedAccessException` | ✅ |

**Refresh Token (2)**

| Test | Status |
|------|--------|
| `RefreshTokenAsync_WithInvalidToken_ThrowsUnauthorizedAccessException` | ✅ |
| `RefreshTokenAsync_WithExpiredToken_ThrowsUnauthorizedAccessException` | ✅ |

**Forgot / Reset Password (8)**

| Test | Status |
|------|--------|
| `ForgotPasswordAsync_WithValidEmail_StoresResetToken` | ✅ |
| `ForgotPasswordAsync_WithValidEmail_SendsPasswordResetEmail` | ✅ |
| `ForgotPasswordAsync_WithUnknownEmail_DoesNotThrowAndDoesNotSendEmail` | ✅ |
| `ResetPasswordAsync_WithValidToken_ReturnsTrue` | ✅ |
| `ResetPasswordAsync_WithValidToken_UpdatesPasswordHash` | ✅ |
| `ResetPasswordAsync_WithValidToken_ClearsRefreshToken` | ✅ |
| `ResetPasswordAsync_WithExpiredToken_ReturnsFalse` | ✅ |
| `ResetPasswordAsync_WithInvalidToken_ReturnsFalse` | ✅ |

**Email Verification / Resend (6)**

| Test | Status |
|------|--------|
| `VerifyEmailAsync_WithValidToken_ReturnsTrueAndSetsEmailVerified` | ✅ |
| `VerifyEmailAsync_WithExpiredToken_ReturnsFalse` | ✅ |
| `VerifyEmailAsync_WithInvalidToken_ReturnsFalse` | ✅ |
| `ResendVerificationEmailAsync_WithValidEmail_SendsVerificationEmail` | ✅ |
| `ResendVerificationEmailAsync_WithAlreadyVerifiedEmail_DoesNotSendEmail` | ✅ |
| `ResendVerificationEmailAsync_WithUnknownEmail_DoesNotThrowAndDoesNotSendEmail` | ✅ |

### ProjectService (17 tests)

| Test | Status |
|------|--------|
| `GetProjectsAsync_WithNoProjects_ReturnsEmptyList` | ✅ |
| `GetProjectsAsync_ReturnsOnlyProjectsForGivenCompanyId` | ✅ |
| `GetProjectsAsync_ReturnsProjectsOrderedByCreatedAtDescending` | ✅ |
| `GetProjectsAsync_ReturnsCorrectDocumentCount` | ✅ |
| `GetProjectAsync_WithValidIdAndCompanyId_ReturnsProject` | ✅ |
| `GetProjectAsync_WithNonExistentId_ThrowsKeyNotFoundException` | ✅ |
| `GetProjectAsync_WithWrongCompanyId_ThrowsKeyNotFoundException` | ✅ |
| `CreateProjectAsync_SetsCompanyIdAndCreatedByCorrectly` | ✅ |
| `CreateProjectAsync_WithAllFields_PersistsAllFields` | ✅ |
| `CreateProjectAsync_WithNullDates_SucceedsWithNullDates` | ✅ |
| `CreateProjectAsync_ReturnsCorrectStatus` | ✅ |
| `UpdateProjectAsync_UpdatesAllFields` | ✅ |
| `UpdateProjectAsync_WithNonExistentId_ThrowsKeyNotFoundException` | ✅ |
| `UpdateProjectAsync_WithWrongCompanyId_ThrowsKeyNotFoundException` | ✅ |
| `DeleteProjectAsync_RemovesProjectFromDatabase` | ✅ |
| `DeleteProjectAsync_WithNonExistentId_ThrowsKeyNotFoundException` | ✅ |
| `DeleteProjectAsync_WithWrongCompanyId_ThrowsKeyNotFoundException` | ✅ |

### UserService (4 tests)

| Test | Status |
|------|--------|
| `GetUsersAsync_ReturnsOnlyUsersForGivenCompanyId` | ✅ |
| `CreateUserAsync_WhenEmailAlreadyExists_ThrowsInvalidOperationException` | ✅ |
| `CreateUserAsync_StoresHashedPasswordNotPlaintext` | ✅ |
| `ToggleUserStatusAsync_FlipsIsActiveCorrectly` | ✅ |

---

## Backend Integration Tests (57)

Requires Docker. `TestWebAppFactory` spins up a real `postgres:16-alpine` container via Testcontainers, runs EF Core migrations, and boots the full ASP.NET Core application.

```
dotnet test LegalDocSystem.IntegrationTests
Passed!  - Failed: 0, Passed: 57, Skipped: 0, Total: 57
```

### AuthController (29 tests)

**Register (14)**

| Test | Status |
|------|--------|
| `Register_WithValidPayload_Returns200WithToken` | ✅ |
| `Register_PersistsUserInDatabase` | ✅ |
| `Register_SetsEmailUnverified` | ✅ |
| `Register_CreatesTrialCompany` | ✅ |
| `Register_WithPhone_Returns200` | ✅ |
| `Register_WithoutPhone_Returns200` | ✅ |
| `Register_WithMissingName_Returns400` | ✅ |
| `Register_WithMissingEmail_Returns400` | ✅ |
| `Register_WithMissingPassword_Returns400` | ✅ |
| `Register_WithInvalidEmailFormat_Returns400` | ✅ |
| `Register_WithShortPassword_Returns400` | ✅ |
| `Register_WithDuplicateUserEmail_Returns400` | ✅ |
| `Register_WithDuplicateCompanyEmail_Returns400` | ✅ |
| `Register_WithMissingCompanyName_Returns400` | ✅ |

**Login / Refresh / Auth flows (15)**

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

### ProjectController (20 tests)

| Test | Status |
|------|--------|
| `GetProjects_WithoutAuth_Returns401` | ✅ |
| `CreateProject_WithoutAuth_Returns401` | ✅ |
| `DeleteProject_WithoutAuth_Returns401` | ✅ |
| `GetProjects_Returns200WithArray` | ✅ |
| `GetProject_WithExistingId_Returns200` | ✅ |
| `GetProject_WithNonExistentId_Returns404` | ✅ |
| `GetProject_ReturnsCorrectFields` | ✅ |
| `CreateProject_WithMinimalFields_Returns201` | ✅ |
| `CreateProject_WithAllFields_Returns201WithAllFieldsPresent` | ✅ |
| `CreateProject_WithDates_PreservesDatesExactly` | ✅ |
| `CreateProject_WithMissingName_Returns400` | ✅ |
| `CreateProject_WritesAuditLog` | ✅ |
| `UpdateProject_Returns200WithUpdatedFields` | ✅ |
| `UpdateProject_WithNonExistentId_Returns404` | ✅ |
| `UpdateProject_WithMissingName_Returns400` | ✅ |
| `UpdateProject_ClearsDatesWhenOmitted` | ✅ |
| `DeleteProject_AsOwner_Returns204` | ✅ |
| `DeleteProject_AfterDeletion_Returns404OnGet` | ✅ |
| `DeleteProject_WithNonExistentId_Returns404` | ✅ |
| `DeleteProject_AsRegularUser_Returns403` | ✅ |

### UserController (4 tests)

| Test | Status |
|------|--------|
| `GetUsers_Returns200WithList` | ✅ |
| `GetUser_Returns200` | ✅ |
| `CreateUser_Returns201` | ✅ |
| `ToggleUserStatus_Returns200` | ✅ |

### PlatformAdminController (4 tests)

| Test | Status |
|------|--------|
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

## Seeded Credentials (Development)

All seeded users have `IsEmailVerified = true`.

| Email | Password | Role |
|-------|----------|------|
| `admin@demolawfirm.com` | `Admin@123` | CompanyOwner |
| `jane.doe@demolawfirm.com` | `User@123` | User |
| `admin@lawgate.io` | `LawgatePlatform@1` | PlatformAdmin |
| `superadmin@lawgate.io` | `LawgateSuperAdmin@1` | PlatformSuperAdmin |

**Integration test users (seeded by `TestWebAppFactory`):**

| Email | Password | Role |
|-------|----------|------|
| `owner@test.com` | `Test@1234` | CompanyOwner |
| `member@test.com` | `Test@1234` | User |
| `superadmin@lawgate.com` | `Admin@1234` | PlatformSuperAdmin |

---

**Environment:** Development (Local Docker)
**Test Runner:** xUnit + FluentAssertions + Testcontainers / Vitest + RTL
**Branch:** main
