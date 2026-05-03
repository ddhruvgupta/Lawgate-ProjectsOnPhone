# SDET Spec — Storage Quota Integration

## Overview

This spec covers end-to-end scenarios that span the full stack: upload flow (SAS generation → blob commit → quota update → UI refresh).
Use this file when writing integration tests (`LegalDocSystem.IntegrationTests`) or when doing manual exploratory testing.

## Related source files

| File | Role |
|------|------|
| `backend/LegalDocSystem.Infrastructure/Services/DocumentService.cs` | `GenerateUploadUrlAsync`, `ConfirmUploadAsync` |
| `backend/LegalDocSystem.API/Controllers/DocumentsController.cs` | `POST /api/projects/{id}/documents/upload-url` |
| `backend/LegalDocSystem.Domain/Entities/Company.cs` | `StorageQuotaBytes`, `StorageUsedBytes` |
| `backend/LegalDocSystem.Infrastructure/Services/TierStorageLimits.cs` | Quota values |
| `frontend/src/components/StorageBar.tsx` | Reads `/api/companies/me` after upload |
| `backend/LegalDocSystem.IntegrationTests/` | Integration test project |

## Run
```powershell
dotnet test backend/LegalDocSystem.IntegrationTests --verbosity normal
```

## Upload quota test cases

### TC-SQI-01 — Upload succeeds when quota has headroom
- **Arrange** company with `StorageUsedBytes = 0`, `StorageQuotaBytes = 1 GB`; file size = 100 MB
- **Act** `POST /api/projects/{id}/documents/upload-url` with `ContentLength = 104857600`
- **Assert** `200 OK`; response contains `sasUrl` and `documentId`

### TC-SQI-02 — Upload is blocked when file would exceed quota
- **Arrange** `StorageUsedBytes = 900 MB`, `StorageQuotaBytes = 1 GB`; file size = 200 MB
- **Act** `POST /api/projects/{id}/documents/upload-url`
- **Assert** `400 Bad Request`; error message contains "Storage quota exceeded"

### TC-SQI-03 — Upload is blocked when already at quota (zero headroom)
- **Arrange** `StorageUsedBytes == StorageQuotaBytes`; file size = 1 byte
- **Assert** `400 Bad Request`

### TC-SQI-04 — StorageUsedBytes increases by file size after ConfirmUpload
- **Arrange** pre-upload `StorageUsedBytes = 100 MB`; upload 50 MB
- **Act** confirm upload
- **Assert** `GET /api/companies/me → storageUsedBytes == 150 MB` (after cache expires or forced refresh)

### TC-SQI-05 — StorageUsedBytes decreases after document delete
- **Arrange** two 50 MB documents confirmed; delete one
- **Assert** `storageUsedBytes == 50 MB`

### TC-SQI-06 — Quota boundary: last byte accepted
- **Arrange** `StorageUsedBytes = 1073741823`, `StorageQuotaBytes = 1 GB (1073741824)`; file = 1 byte
- **Assert** `200 OK` — 1-byte file fits exactly

### TC-SQI-07 — File larger than 500 MB is rejected (size limit)
- **Arrange** `ContentLength = 524288001` (500 MB + 1 byte)
- **Assert** `400 Bad Request`; error mentions file size limit

### TC-SQI-08 — Unauthenticated upload returns 401
- **Act** `POST /api/projects/{id}/documents/upload-url` without JWT
- **Assert** `401 Unauthorized`

### TC-SQI-09 — User in a different company cannot upload to this project
- **Arrange** project owned by company A; user authenticated for company B
- **Assert** `403 Forbidden`

## SAS token test cases

### TC-SQI-SAS-01 — SAS URL expires after configured duration
- **Arrange** `UploadOptions.SasExpiryMinutes = 1`
- **Assert** SAS token `se` query param equals approximately `UtcNow + 1 minute`

### TC-SQI-SAS-02 — SAS URL is pre-signed (no credential embedded)
- **Assert** SAS URL does not contain the storage account key

### TC-SQI-SAS-03 — SAS URL grants write-only access (no list, no read)
- **Assert** SAS permissions string contains `w` (create/write); does NOT contain `r` (read) or `l` (list)

## Tier promotion test cases (manual)

### TC-SQI-TIER-01 — Upgrading tier increases quota
- Manually set `SubscriptionTier = Basic` for a company → `StorageQuotaBytes` should be updated to `100 GB`
- Verify `GET /api/companies/me → storageQuotaBytes == 107374182400`

### TC-SQI-TIER-02 — Trial expiry does not delete uploaded files
- After `SubscriptionEndDate` passes, the user's documents should remain accessible; only new uploads may be blocked depending on business logic

## Notes

- The `ContentLength` header on the upload-url request is what `DocumentService.GenerateUploadUrlAsync` uses for the quota check. The actual blob size is validated again during `ConfirmUploadAsync` (to prevent spoofing the `ContentLength`).
- Azure Blob Storage fires an Event Grid event for the malware scan result. The `MalwareScanResultHandler` (background service) sets the document status accordingly — this flow is not covered in unit tests.
