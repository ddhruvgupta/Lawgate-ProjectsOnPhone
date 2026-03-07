using LegalDocSystem.Application.DTOs.Projects;
using LegalDocSystem.Application.Interfaces;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LegalDocSystem.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;

    public ProjectService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProjectDto>> GetProjectsAsync(int companyId)
    {
        return await _context.Projects
            .Where(p => p.CompanyId == companyId)
            .Select(p => new ProjectDto
            {
                Id = p.Id,
                CompanyId = p.CompanyId,
                Name = p.Name,
                Description = p.Description,
                ClientName = p.ClientName,
                CaseNumber = p.CaseNumber,
                Status = p.Status.ToString(),
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Tags = p.Tags,
                CreatedAt = p.CreatedAt,
                DocumentCount = p.Documents.Count
            })
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<ProjectDto> GetProjectAsync(int id, int companyId)
    {
        var project = await _context.Projects
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

        if (project == null) throw new KeyNotFoundException("Project not found");

        return MapToDto(project);
    }

    public async Task<ProjectDto> CreateProjectAsync(int companyId, CreateProjectDto dto)
    {
        var project = new Project
        {
            CompanyId = companyId,
            Name = dto.Name,
            Description = dto.Description,
            ClientName = dto.ClientName,
            CaseNumber = dto.CaseNumber,
            Status = dto.Status,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Tags = dto.Tags,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System" // Or current user from context if passed
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return MapToDto(project);
    }

    public async Task<ProjectDto> UpdateProjectAsync(int id, int companyId, UpdateProjectDto dto)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

        if (project == null) throw new KeyNotFoundException("Project not found");

        project.Name = dto.Name;
        project.Description = dto.Description;
        project.ClientName = dto.ClientName;
        project.CaseNumber = dto.CaseNumber;
        project.Status = dto.Status;
        project.StartDate = dto.StartDate;
        project.EndDate = dto.EndDate;
        project.Tags = dto.Tags;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(project);
    }

    public async Task DeleteProjectAsync(int id, int companyId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

        if (project == null) throw new KeyNotFoundException("Project not found");

        // Ideally soft delete. For now, we perform hard delete but could fail if documents exist (FK constraint)
        // Let's implement soft delete approach or delete check
        // Check if documents exist
        bool hasDocuments = await _context.Documents.AnyAsync(d => d.ProjectId == id);
        if (hasDocuments)
        {
            throw new InvalidOperationException("Cannot delete project with existing documents. Delete documents first.");
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
    }

    private static ProjectDto MapToDto(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            CompanyId = project.CompanyId,
            Name = project.Name,
            Description = project.Description,
            ClientName = project.ClientName,
            CaseNumber = project.CaseNumber,
            Status = project.Status.ToString(),
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Tags = project.Tags,
            CreatedAt = project.CreatedAt,
            DocumentCount = project.Documents?.Count ?? 0
        };
    }
}
