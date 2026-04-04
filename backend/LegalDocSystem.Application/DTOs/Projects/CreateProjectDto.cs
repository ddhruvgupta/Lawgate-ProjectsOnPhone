using System.ComponentModel.DataAnnotations;
using LegalDocSystem.Domain.Enums;

namespace LegalDocSystem.Application.DTOs.Projects;

public class CreateProjectDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ClientName { get; set; }

    [MaxLength(50)]
    public string? CaseNumber { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Tags { get; set; }
    
    public ProjectStatus Status { get; set; } = ProjectStatus.Intake;
}
