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
            migrationBuilder.CreateIndex(
                name: "ix_users_refreshtoken",
                table: "Users",
                column: "RefreshToken",
                filter: "\"RefreshToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_documents_project_status",
                table: "Documents",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_auditlogs_company_entity",
                table: "AuditLogs",
                columns: new[] { "CompanyId", "EntityType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_refreshtoken",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "ix_documents_project_status",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "ix_auditlogs_company_entity",
                table: "AuditLogs");
        }
    }
}
