# SDET Spec — CompanyService

## Overview

`CompanyService` provides two operations:
1. `GetCompanyAsync(id)` — fetches and caches the company DTO (5-min in-memory cache)
2. `UpdateCompanyAsync(id, dto)` — updates profile fields and invalidates the cache

The DTO exposed to the frontend includes `StorageUsedBytes`, `StorageQuotaBytes`, and `SubscriptionTier` (as a string).
The API endpoint is `GET /api/companies/me` — reads `CompanyId` from the JWT claim.

## Source files

| File | Purpose |
|------|---------|
| `backend/LegalDocSystem.Infrastructure/Services/CompanyService.cs` | Class under test |
| `backend/LegalDocSystem.API/Controllers/CompanyController.cs` | API entry point |
| `backend/LegalDocSystem.Application/DTOs/Companies/CompanyDto.cs` | Response shape |
| `backend/LegalDocSystem.UnitTests/Services/CompanyServiceTests.cs` | Automated tests |

## Run
```powershell
dotnet test backend/LegalDocSystem.UnitTests --filter "FullyQualifiedName~CompanyServiceTests"
```

## Test cases

### TC-CS-01 — GetCompanyAsync returns correct storage bytes
- **Arrange** company with `StorageUsedBytes = 500_000_000`, `StorageQuotaBytes = 1 GB`
- **Assert** DTO fields match exactly
- **File** `GetCompanyAsync_ReturnsCorrectStorageBytes`

### TC-CS-02 — GetCompanyAsync maps SubscriptionTier enum to string
- **Arrange** `tier = Professional`
- **Assert** `dto.SubscriptionTier == "Professional"`
- **File** `GetCompanyAsync_ReturnsSubscriptionTierAsString`

### TC-CS-03 — All four tiers map to the correct string (parametric)
- **Tiers** Trial→"Trial", Basic→"Basic", Professional→"Professional", Enterprise→"Enterprise"
- **File** `GetCompanyAsync_MapsAllTiersToString` (`[Theory]`)

### TC-CS-04 — GetCompanyAsync returns SubscriptionEndDate
- **Arrange** company with `SubscriptionEndDate = UtcNow + 14 days`
- **Assert** DTO `SubscriptionEndDate` is within 1 second of source value
- **File** `GetCompanyAsync_ReturnsSubscriptionEndDate`

### TC-CS-05 — GetCompanyAsync throws KeyNotFoundException for unknown ID
- **Act** `GetCompanyAsync(99999)`
- **Assert** throws `KeyNotFoundException("*Company not found*")`
- **File** `GetCompanyAsync_WhenCompanyNotFound_ThrowsKeyNotFoundException`

### TC-CS-06 — Second call returns cached DTO (stale after DB mutation)
- **Arrange** call once to warm cache; mutate DB directly; call again
- **Assert** second result matches first (cache served; DB change invisible)
- **File** `GetCompanyAsync_SecondCall_ReturnsCachedDto`
- **Rationale** ensures the 5-min cache works — a storage update in DB isn't immediately visible

### TC-CS-07 — UpdateCompanyAsync invalidates cache so next read is fresh
- **Arrange** warm cache; call UpdateCompanyAsync; call GetCompanyAsync again
- **Assert** result reflects the updated name/city
- **File** `UpdateCompanyAsync_InvalidatesCacheSoNextReadIsFresh`

### TC-CS-08 — UpdateCompanyAsync throws KeyNotFoundException for unknown ID
- **File** `UpdateCompanyAsync_WhenCompanyNotFound_ThrowsKeyNotFoundException`

### TC-CS-09 — DTO contains all expected fields
- **Assert** Id, Name, Email, Phone, SubscriptionTier, StorageUsedBytes, StorageQuotaBytes are all populated
- **File** `GetCompanyAsync_DtoContainsAllExpectedFields`

## API endpoint test cases (integration / manual)

### TC-CS-API-01 — Unauthenticated request returns 401
- `GET /api/companies/me` without Authorization header → `401 Unauthorized`

### TC-CS-API-02 — Authenticated request returns 200 with storage fields
- `GET /api/companies/me` with valid JWT → `200 OK`; body contains `storageUsedBytes`, `storageQuotaBytes`, `subscriptionTier`

### TC-CS-API-03 — Token missing CompanyId claim returns 400
- Craft a JWT without the `CompanyId` claim → `400 Bad Request` ("Invalid token: CompanyId missing")

### TC-CS-API-04 — PUT /api/companies/me by non-owner returns 403
- Authenticate as `Admin` or `User` role → `PUT /api/companies/me` → `403 Forbidden`

### TC-CS-API-05 — PUT /api/companies/me by CompanyOwner returns 200
- Authenticate as `CompanyOwner` → `PUT` with valid DTO → `200 OK`; response reflects new values

## Notes

- `StorageUsedBytes` is incremented by `DocumentService.ConfirmUploadAsync` and decremented by `DeleteDocumentAsync`. `CompanyService` does **not** own that counter — it only reads and maps it.
- Cache duration is 5 minutes. After a successful upload, the sidebar storage bar may show slightly stale data until the cache expires or the component re-fetches (React Query staleTime = 5 min).
