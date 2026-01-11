# Database Schema Changelog

## Version 1.0.0 - Initial Release (2026-01-11)

### Tables Created
- **Users**
  - Id (PK, GUID)
  - Email (unique, indexed)
  - PasswordHash
  - FirstName, LastName
  - IsActive, IsEmailVerified
  - CreatedAt, UpdatedAt
  - RoleId (FK to Roles)

- **Roles**
  - Id (PK, GUID)
  - Name (unique)
  - Description
  - Permissions (JSON)
  - CreatedAt, UpdatedAt

- **AuditLogs**
  - Id (PK, GUID)
  - UserId (FK to Users, nullable)
  - Action
  - EntityName
  - EntityId
  - OldValues (JSON)
  - NewValues (JSON)
  - Timestamp
  - IpAddress
  - UserAgent

### Indexes
- Users.Email (unique, btree)
- Users.RoleId (btree)
- AuditLogs.UserId (btree)
- AuditLogs.Timestamp (btree)
- AuditLogs.EntityName (btree)

### Seed Data
- Admin role with full permissions
- User role with read permissions
- Guest role with limited permissions

---

## Future Migrations

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
