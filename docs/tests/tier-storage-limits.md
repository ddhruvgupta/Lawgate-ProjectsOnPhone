# SDET Spec — TierStorageLimits

## Overview

`TierStorageLimits` is a bound-options class (`IOptions<TierStorageLimits>`) that holds per-tier storage quota values in GB and exposes a `ForTier(SubscriptionTier)` helper that returns the quota in bytes.

## Source files

| File | Purpose |
|------|---------|
| `backend/LegalDocSystem.Infrastructure/Services/TierStorageLimits.cs` | Class under test |
| `backend/LegalDocSystem.UnitTests/Services/TierStorageLimitsTests.cs` | Automated tests |
| `backend/LegalDocSystem.API/appsettings.json` | Config section `TierStorageLimits` |
| `backend/LegalDocSystem.API/Program.cs` | `Configure<TierStorageLimits>` registration |

## Run
```powershell
dotnet test backend/LegalDocSystem.UnitTests --filter "FullyQualifiedName~TierStorageLimitsTests"
```

## Test cases

### TC-TSL-01 — Default Trial quota is 1 GB
- **Arrange** `new TierStorageLimits()` (default property initialisers)
- **Act** `ForTier(SubscriptionTier.Trial)`
- **Assert** `== 1 * 1024 * 1024 * 1024`
- **File** `TierStorageLimitsTests.ForTier_Trial_Returns1GbByDefault`

### TC-TSL-02 — Default Basic quota is 100 GB
- **Act** `ForTier(SubscriptionTier.Basic)`
- **Assert** `== 100L * 1024 * 1024 * 1024`

### TC-TSL-03 — Default Professional quota is 500 GB
- **Act** `ForTier(SubscriptionTier.Professional)`
- **Assert** `== 500L * 1024 * 1024 * 1024`

### TC-TSL-04 — Default Enterprise quota is 2 TB (2048 GB)
- **Act** `ForTier(SubscriptionTier.Enterprise)`
- **Assert** `== 2048L * 1024 * 1024 * 1024`

### TC-TSL-05 — Custom TrialGb is respected
- **Arrange** `new TierStorageLimits { TrialGb = 5 }`
- **Assert** `ForTier(Trial) == 5L * 1024^3`

### TC-TSL-06 — Custom EnterpriseGb is respected
- **Arrange** `new TierStorageLimits { EnterpriseGb = 4096 }`
- **Assert** `ForTier(Enterprise) == 4096L * 1024^3`

### TC-TSL-07 — Each tier is strictly larger than the previous
- **Assert** `Trial < Basic < Professional < Enterprise`

### TC-TSL-08 — Unknown enum value falls back to Trial quota
- **Arrange** `(SubscriptionTier)99`
- **Assert** result equals `ForTier(Trial)`
- **Rationale** prevents a future new enum value from silently granting unlimited access

### TC-TSL-09 — No integer overflow for very large EnterpriseGb
- **Arrange** `EnterpriseGb = int.MaxValue / 1024`
- **Assert** does not throw; result > 0

## Edge cases to watch

- `TrialGb = 0` — ForTier would return 0, meaning uploads always fail. Config validation should enforce `> 0`.
- Negative GB values — same concern; config schema validation (not yet implemented) should reject them.
- `ForTier` is synchronous and allocation-free — it must not throw under concurrent load.

## Config parity check (manual)

Verify that `appsettings.json → TierStorageLimits` matches the defaults in the C# class:

| Key | appsettings.json value | C# default |
|-----|----------------------|-----------|
| TrialGb | 1 | 1 |
| BasicGb | 100 | 100 |
| ProfessionalGb | 500 | 500 |
| EnterpriseGb | 2048 | 2048 |
