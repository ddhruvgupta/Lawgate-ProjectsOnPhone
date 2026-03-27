using LegalDocSystem.Application.DTOs.Audit;

namespace LegalDocSystem.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        int companyId,
        int userId,
        string action,
        string entityType,
        int? entityId = null,
        string? description = null,
        string? oldValues = null);

    Task<(IEnumerable<AuditLogDto> Items, int TotalCount)> GetLogsAsync(
        int companyId,
        string? entityType = null,
        int? entityId = null,
        int page = 1,
        int pageSize = 50);
}
