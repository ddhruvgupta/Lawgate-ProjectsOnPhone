using LegalDocSystem.Domain.Common;

namespace LegalDocSystem.Domain.Entities;

/// <summary>
/// Records all user actions for compliance and auditing
/// </summary>
public class AuditLog : BaseEntity
{
    public int CompanyId { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty; // e.g., "Document.Upload", "User.Login"
    public string EntityType { get; set; } = string.Empty; // e.g., "Document", "Project"
    public int? EntityId { get; set; }
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    
    // Navigation properties
    public User User { get; set; } = null!;
}
