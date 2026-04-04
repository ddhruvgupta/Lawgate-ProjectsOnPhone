using LegalDocSystem.Domain.Enums;

namespace LegalDocSystem.Application.DTOs.Projects;

public class ProjectDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public string? CaseNumber { get; set; }
    public ProjectStatus Status { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Tags { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Extra useful info
    public int DocumentCount { get; set; }
}
