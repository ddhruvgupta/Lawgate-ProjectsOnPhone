using LegalDocSystem.Application.DTOs.Audit;
using LegalDocSystem.Application.Interfaces;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LegalDocSystem.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        int companyId,
        int userId,
        string action,
        string entityType,
        int? entityId = null,
        string? description = null,
        string? oldValues = null)
    {
        var log = new AuditLog
        {
            CompanyId = companyId,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            NewValues = description,
            OldValues = oldValues,
            IpAddress = string.Empty,
            UserAgent = string.Empty,
            CreatedBy = userId.ToString(),
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<(IEnumerable<AuditLogDto> Items, int TotalCount)> GetLogsAsync(
        int companyId,
        string? entityType = null,
        int? entityId = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.AuditLogs
            .Include(al => al.User)
            .Where(al => al.CompanyId == companyId);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(al => al.EntityType == entityType);

        if (entityId.HasValue)
            query = query.Where(al => al.EntityId == entityId);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(al => al.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(al => new AuditLogDto
            {
                Id = al.Id,
                Action = al.Action,
                EntityType = al.EntityType,
                EntityId = al.EntityId,
                Description = al.NewValues ?? al.Action,
                OldValues = al.OldValues,
                UserName = al.User.FirstName + " " + al.User.LastName,
                UserEmail = al.User.Email,
                CreatedAt = al.CreatedAt,
            })
            .ToListAsync();

        return (items, total);
    }
}
