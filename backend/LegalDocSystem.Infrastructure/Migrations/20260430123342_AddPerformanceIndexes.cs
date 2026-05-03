using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalDocSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CONCURRENTLY avoids an exclusive table lock during index build.
            // suppressTransaction: true is required because CONCURRENTLY cannot run inside a transaction.

            // Filtered index: speeds up refresh-token lookups while ignoring logged-out users.
            migrationBuilder.Sql(
                """CREATE INDEX CONCURRENTLY IF NOT EXISTS "ix_users_refreshtoken" ON "Users" ("RefreshToken") WHERE "RefreshToken" IS NOT NULL;""",
                suppressTransaction: true);

            // Composite index: covers the (ProjectId, Status) filter on the documents listing query.
            migrationBuilder.Sql(
                """CREATE INDEX CONCURRENTLY IF NOT EXISTS "ix_documents_project_status" ON "Documents" ("ProjectId", "Status");""",
                suppressTransaction: true);

            // Composite index: covers the audit-log filter AND the ORDER BY CreatedAt DESC so
            // PostgreSQL can satisfy both the WHERE clause and the sort from a single index scan.
            migrationBuilder.Sql(
                """CREATE INDEX CONCURRENTLY IF NOT EXISTS "ix_auditlogs_company_entity" ON "AuditLogs" ("CompanyId", "EntityType", "CreatedAt" DESC);""",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX CONCURRENTLY IF EXISTS "ix_users_refreshtoken";""", suppressTransaction: true);
            migrationBuilder.Sql("""DROP INDEX CONCURRENTLY IF EXISTS "ix_documents_project_status";""", suppressTransaction: true);
            migrationBuilder.Sql("""DROP INDEX CONCURRENTLY IF EXISTS "ix_auditlogs_company_entity";""", suppressTransaction: true);
        }
    }
}
