using LegalDocSystem.Domain.Common;
using LegalDocSystem.Domain.Enums;

namespace LegalDocSystem.Domain.Entities;

/// <summary>
/// Manages role-based access control at the project level
/// </summary>
public class ProjectPermission : BaseEntity
{
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.Viewer;
    public DateTime? ExpiresAt { get; set; }
    
    // Navigation properties
    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
}
