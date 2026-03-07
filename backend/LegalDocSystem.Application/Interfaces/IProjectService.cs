using LegalDocSystem.Application.DTOs.Projects;

namespace LegalDocSystem.Application.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<ProjectDto>> GetProjectsAsync(int companyId);
    Task<ProjectDto> GetProjectAsync(int id, int companyId);
    Task<ProjectDto> CreateProjectAsync(int companyId, CreateProjectDto dto);
    Task<ProjectDto> UpdateProjectAsync(int id, int companyId, UpdateProjectDto dto);
    Task DeleteProjectAsync(int id, int companyId);
}
