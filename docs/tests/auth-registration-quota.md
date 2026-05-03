# SDET Spec — AuthService Registration Quota Assignment

## Overview

When a company registers, `AuthService.RegisterAsync` must create the company row with:
- `SubscriptionTier = Trial`
- `StorageUsedBytes = 0`
- `StorageQuotaBytes = TierStorageLimits.ForTier(Trial)` (driven by config, not hardcoded)
- `SubscriptionEndDate` set to `UtcNow + 14 days`

## Source files

| File | Purpose |
|------|---------|
| `backend/LegalDocSystem.Infrastructure/Services/AuthService.cs` | Class under test |
| `backend/LegalDocSystem.Infrastructure/Services/TierStorageLimits.cs` | Quota source |
| `backend/LegalDocSystem.UnitTests/Services/AuthServiceTests.cs` | Automated tests |
| `backend/LegalDocSystem.API/Controllers/AuthController.cs` | API entry point |

## Run
```powershell
dotnet test backend/LegalDocSystem.UnitTests --filter "FullyQualifiedName~AuthServiceTests"
```

## Test cases

### TC-ARQ-01 — New company gets Trial quota from TierStorageLimits
- **Arrange** `RegisterAsync(validDto)` with default `TierStorageLimits` (TrialGb = 1)
- **Assert** persisted `StorageQuotaBytes == 1 * 1024^3`
- **File** `RegisterAsync_NewCompany_GetsTrialQuotaFromTierStorageLimits`

### TC-ARQ-02 — Custom TrialGb config is used instead of a magic number
- **Arrange** `TierStorageLimits { TrialGb = 5 }` injected into `AuthService`
- **Assert** persisted `StorageQuotaBytes == 5 * 1024^3`
- **File** `RegisterAsync_WithCustomTrialQuota_UsesConfiguredValue`
- **Rationale** proves the service does NOT have a hardcoded `10L * 1024 * 1024 * 1024`

### TC-ARQ-03 — StorageUsedBytes is 0 on new registration
- **Assert** `StorageUsedBytes == 0`
- **File** `RegisterAsync_NewCompany_StorageUsedBytesIsZero`

### TC-ARQ-04 — SubscriptionTier is Trial on new registration
- **Assert** `SubscriptionTier == SubscriptionTier.Trial`
- **File** `RegisterAsync_NewCompany_SubscriptionTierIsTrial`

### TC-ARQ-05 — SubscriptionEndDate is ~14 days in the future
- **Assert** `SubscriptionEndDate` is within ±1 day of `UtcNow + 14 days`
- **File** `RegisterAsync_NewCompany_SubscriptionEndsIn14Days`

### TC-ARQ-06 — Duplicate company email is rejected (pre-existing)
- **Arrange** seed a company with the same email first
- **Assert** throws `InvalidOperationException("*Company with this email already exists*")`

### TC-ARQ-07 — Duplicate user email is rejected
- **Arrange** seed a user with the same email in any company
- **Assert** throws `InvalidOperationException("*User with this email already exists*")`

### TC-ARQ-08 — Email service failure does not abort registration
- **Arrange** mock `IEmailService` to throw
- **Assert** registration succeeds; company and user rows exist in DB

## Pre-conditions

- `IOptions<TierStorageLimits>` must be registered in DI (`Program.cs`)
- `appsettings.json → TierStorageLimits → TrialGb` controls the real quota value

## Post-conditions (after register)

| Field | Expected |
|-------|---------|
| `Company.SubscriptionTier` | `Trial` |
| `Company.StorageUsedBytes` | `0` |
| `Company.StorageQuotaBytes` | `TrialGb × 1024³` |
| `Company.IsActive` | `true` |
| `User.Role` | `CompanyOwner` |
| `User.IsEmailVerified` | `false` |
