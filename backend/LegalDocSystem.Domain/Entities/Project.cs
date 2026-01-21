using LegalDocSystem.Domain.Common;
using LegalDocSystem.Domain.Enums;

namespace LegalDocSystem.Domain.Entities;

/// <summary>
/// Represents a legal case or contract project
/// </summary>
public class Project : BaseEntity
{
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public string? CaseNumber { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Tags { get; set; } // JSON array of tags
    
    // Navigation properties
    public Company Company { get; set; } = null!;
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<ProjectPermission> ProjectPermissions { get; set; } = new List<ProjectPermission>();
}
