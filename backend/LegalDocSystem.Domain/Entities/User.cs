using LegalDocSystem.Domain.Common;
using LegalDocSystem.Domain.Enums;

namespace LegalDocSystem.Domain.Entities;

/// <summary>
/// Represents a user within a company
/// </summary>
public class User : BaseEntity
{
    public int CompanyId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? ProfileImageUrl { get; set; }
    
    // For password reset
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }
    
    // Navigation properties
    public Company Company { get; set; } = null!;
    public ICollection<ProjectPermission> ProjectPermissions { get; set; } = new List<ProjectPermission>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
