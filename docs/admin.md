# Platform Admin

## Role Model

Lawgate has two distinct permission surfaces:

1. **Company-level roles** — scoped to a single tenant (law firm)
2. **Platform admin** — cross-tenant access for the Lawgate team (not yet built)

### Company-level roles (`UserRole` enum)

| Role | Value | Description |
|------|-------|-------------|
| `CompanyOwner` | 1 | Created at company registration. Full access to their company's data. Can manage all users and projects. |
| `Admin` | 2 | Elevated privileges within the company. Can manage users and projects but cannot change subscription settings. |
| `User` | 3 | Standard access. Can create and manage projects and documents they have permissions on. |
| `Viewer` | 4 | Read-only access. Cannot create or modify any data. |

The `Role` field is stored as an integer on the `Users` table. It is embedded as a claim in the JWT on login, so controllers can use `[Authorize(Roles = "CompanyOwner,Admin")]` without a database lookup.

### Project-level permissions (`PermissionLevel` enum)

On top of the company-wide role, users can have a `PermissionLevel` on individual projects (stored in `ProjectPermissions`):

| Level | Value |
|-------|-------|
| `None` | 0 |
| `Viewer` | 1 |
| `Commenter` | 2 |
| `Editor` | 3 |
| `Admin` | 4 |

A `CompanyOwner` or `Admin` has implicit access to all projects regardless of `ProjectPermissions` entries.

---

## Platform Admin (planned)

A platform admin (`SuperAdmin`) is a Lawgate internal user who can see all companies and cross-tenant data for support and operations. This is not yet implemented.

### Planned capabilities

- List all companies and their subscription status
- View platform-wide usage statistics (total documents, storage used)
- Activate or deactivate a company
- Impersonate a company for support purposes (audit logged)
- View platform-wide audit trail

### Planned implementation approach

- A separate `IsPlatformAdmin` flag on the `User` entity (or a separate `PlatformAdmins` table)
- Dedicated `[Authorize(Policy = "PlatformAdmin")]` policy in `Program.cs`
- A separate `/api/platform-admin/` controller prefix that is blocked from company-scoped users
- Platform admin users belong to an internal Lawgate company (not a real law firm)

### Access control note

Until platform admin is built, there is no way to view cross-tenant data through the API. Direct database access is required for support operations.

---

## DbSeeder credentials

In development, `DbSeeder` seeds the following accounts when the database is empty. All seeded users have `IsEmailVerified = true`.

**Demo company (`Demo Law Firm`):**

| Email | Password | Role |
|-------|----------|------|
| admin@demolawfirm.com | Admin@123 | CompanyOwner |
| jane.doe@demolawfirm.com | User@123 | User |

**Platform accounts (`Lawgate Platform` company):**

| Email | Password | Role |
|-------|----------|------|
| admin@lawgate.io | LawgatePlatform@1 | PlatformAdmin |
| superadmin@lawgate.io | LawgateSuperAdmin@1 | PlatformSuperAdmin |

Demo company users are seeded by `DbSeeder.SeedAsync` and platform accounts by `DbSeeder.SeedPlatformAdminsAsync`. Both run only when `ASPNETCORE_ENVIRONMENT=Development` and the database is empty.
