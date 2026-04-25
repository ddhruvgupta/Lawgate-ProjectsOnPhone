# Database Schema Changelog

## Version 1.0.0 - Initial Release (2026-01-12)

### Entities Created (via EF Core migrations)
All core entities in Clean Architecture structure:

- **Companies** — Multi-tenant root (Name, Email, SubscriptionTier, StorageQuotaBytes, etc.)
- **Users** — CompanyId FK, PasswordHash, Role enum, RefreshToken, LastLoginAt
- **Projects** — CompanyId FK, Status enum, ClientName, CaseNumber, Tags (JSON)
- **Documents** — ProjectId FK, BlobStoragePath, DocumentType enum, versioning (ParentDocumentId, IsLatestVersion)
- **ProjectPermissions** — UserId + ProjectId + PermissionLevel enum
- **AuditLogs** — CompanyId + UserId FK, Action, EntityType/Id, OldValues/NewValues (JSON), IpAddress, UserAgent

### Extensions Enabled
- `uuid-ossp` (UUID generation)
- `pg_trgm` (trigram full-text search)

### Indexes
- Users.Email (unique)
- Users.CompanyId
- Projects.CompanyId
- Documents.ProjectId
- AuditLogs.CompanyId, UserId, CreatedAt

### Seed Data
- Default company: Lawgate Demo
- Default users: CompanyOwner, Admin, User, PlatformAdmin (see docs/backend.md for credentials)

---

## Version 1.1.0 - Add Document Status (2026-01-25)

### Columns Added
- **Documents**: `Status` (enum: Pending / Confirmed)
  - Pending = upload URL generated but not yet confirmed
  - Confirmed = client confirmed file uploaded to Azure Blob Storage

### Migration
`20260125031050_AddDocumentStatus`

---

## Version 1.2.0 - Add Refresh Token to User (2026-03-25)

### Columns Added
- **Users**: `RefreshToken` (string, nullable)
- **Users**: `RefreshTokenExpiry` (DateTime, nullable)

### Notes
Enables stateless JWT refresh flow: client posts expired access token + refresh token to `/api/auth/refresh` to get a new pair without re-authenticating.

### Migration
`20260325021906_AddRefreshTokenToUser`

---

## Version 1.3.0 - Refresh Token Index (2026-03-25)

### Indexes Added
- **Users.RefreshToken** (btree) — speeds up token lookup on refresh endpoint

### Migration
`20260325133233_AddRefreshTokenIndex`

---

## Version 1.4.0 - Update Project Status Enum (2026-03-25)

### Enum Updated
- **ProjectStatus** values changed from `Planning / Active / OnHold / Completed / Cancelled / Archived`
  to `Intake / Active / Discovery / Negotiation / Hearing / OnHold / Settled / Closed / Archived`
  Reflects a full legal matter lifecycle.

### Migration
`20260325234043_UpdateProjectStatusToLegal`

---

## Version 1.5.0 - Project Dates as DateOnly (2026-04-03)

### Columns Modified
- **Projects**: `StartDate` changed from `timestamp with time zone` (nullable) to `date` (nullable)
- **Projects**: `EndDate` changed from `timestamp with time zone` (nullable) to `date` (nullable)

### Notes
PostgreSQL `timestamp with time zone` requires a UTC `DateTimeKind` — dates without times caused a `Cannot write DateTime with Kind=Unspecified` error. `date` columns map cleanly to .NET `DateOnly`, removing timezone ambiguity entirely. Corresponding entity, DTO, and service changes applied simultaneously.

### Migration
`20260403210848_ChangeProjectDatesToDateOnly`

---

## Version 1.6.0 - Email Verification & Password Reset Fields (2026-04-08)

### Columns Added
- **Users**: `IsEmailVerified` (bool, default false) — login is blocked until true
- **Users**: `EmailVerificationToken` (string, nullable) — SHA-256 hashed token sent in verification email
- **Users**: `EmailVerificationTokenExpiry` (DateTime, nullable) — token TTL
- **Users**: `PasswordResetToken` (string, nullable) — SHA-256 hashed token sent in reset email
- **Users**: `PasswordResetTokenExpiry` (DateTime, nullable) — token TTL

### Notes
Enables the full forgot-password / reset-password / verify-email / resend-verification flow.
`DbSeeder` sets `IsEmailVerified = true` on all seed users so development login is unblocked.

### Migration
`20260408000000_AddEmailVerification`

---

### Template for New Entries
```markdown
## Version X.Y.Z - Feature Name (YYYY-MM-DD)

### Tables Added
- **TableName**
  - Column definitions

### Tables Modified
- **TableName**
  - Added columns: 
  - Removed columns:
  - Modified columns:

### Indexes Added
- Index definitions

### Data Migrations
- Description of data changes

### Breaking Changes
- Any breaking changes

### Migration Notes
- Special considerations
- Manual steps required
```

---

## Migration Guidelines

1. **Never modify existing migrations** - Always create new ones
2. **Include rollback logic** when possible
3. **Document breaking changes** prominently
4. **Test migrations** on a copy of production data
5. **Backup database** before applying migrations in production
6. **Version migrations** according to semantic versioning

## Rollback History
- No rollbacks yet (Initial version)
