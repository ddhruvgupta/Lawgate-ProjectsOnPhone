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
            migrationBuilder.Sql(
                @"CREATE INDEX CONCURRENTLY ""ix_users_refreshtoken""
                  ON ""Users"" (""RefreshToken"")
                  WHERE ""RefreshToken"" IS NOT NULL;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                @"CREATE INDEX CONCURRENTLY ""ix_documents_project_status""
                  ON ""Documents"" (""ProjectId"", ""Status"");",
                suppressTransaction: true);

            migrationBuilder.Sql(
                @"CREATE INDEX CONCURRENTLY ""ix_auditlogs_company_entity""
                  ON ""AuditLogs"" (""CompanyId"", ""EntityType"");",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DROP INDEX CONCURRENTLY IF EXISTS ""ix_users_refreshtoken"";",
                suppressTransaction: true);

            migrationBuilder.Sql(
                @"DROP INDEX CONCURRENTLY IF EXISTS ""ix_documents_project_status"";",
                suppressTransaction: true);

            migrationBuilder.Sql(
                @"DROP INDEX CONCURRENTLY IF EXISTS ""ix_auditlogs_company_entity"";",
                suppressTransaction: true);
        }
    }
}
