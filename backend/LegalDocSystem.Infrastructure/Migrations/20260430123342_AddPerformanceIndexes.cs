using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalDocSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        // CONCURRENTLY avoids an exclusive table lock during index build.
        // EF Core must not wrap these statements in a transaction.
        protected override bool SuppressTransaction => true;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Filtered index: speeds up refresh-token lookups while ignoring logged-out users.
            migrationBuilder.Sql(
                """CREATE INDEX CONCURRENTLY IF NOT EXISTS "ix_users_refreshtoken" ON "Users" ("RefreshToken") WHERE "RefreshToken" IS NOT NULL;""");

            // Composite index: covers the (ProjectId, Status) filter on the documents listing query.
            migrationBuilder.Sql(
                """CREATE INDEX CONCURRENTLY IF NOT EXISTS "ix_documents_project_status" ON "Documents" ("ProjectId", "Status");""");

            // Composite index: covers the audit-log filter AND the ORDER BY CreatedAt DESC so
            // PostgreSQL can satisfy both the WHERE clause and the sort from a single index scan.
            migrationBuilder.Sql(
                """CREATE INDEX CONCURRENTLY IF NOT EXISTS "ix_auditlogs_company_entity" ON "AuditLogs" ("CompanyId", "EntityType", "CreatedAt" DESC);""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX CONCURRENTLY IF EXISTS "ix_users_refreshtoken";""");
            migrationBuilder.Sql("""DROP INDEX CONCURRENTLY IF EXISTS "ix_documents_project_status";""");
            migrationBuilder.Sql("""DROP INDEX CONCURRENTLY IF EXISTS "ix_auditlogs_company_entity";""");
        }
    }
}
