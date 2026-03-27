using LegalDocSystem.Application.DTOs.Projects;
using LegalDocSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LegalDocSystem.API.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IAuditService _auditService;

    public ProjectController(IProjectService projectService, IAuditService auditService)
    {
        _projectService = projectService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
            return BadRequest("Invalid token: CompanyId missing");

        var projects = await _projectService.GetProjectsAsync(companyId);
        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectDto>> GetProject(int id)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
            return BadRequest("Invalid token: CompanyId missing");

        var project = await _projectService.GetProjectAsync(id, companyId);
        return Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateProject(CreateProjectDto dto)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
            return BadRequest("Invalid token: CompanyId missing");

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

        var project = await _projectService.CreateProjectAsync(companyId, dto, userEmail);

        await _auditService.LogAsync(
            companyId, userId,
            action: "Project.Created",
            entityType: "Project",
            entityId: project.Id,
            description: $"Created project '{project.Name}'" +
                         (project.ClientName != null ? $" for client {project.ClientName}" : "") +
                         (project.CaseNumber != null ? $" (case {project.CaseNumber})" : ""));

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProjectDto>> UpdateProject(int id, UpdateProjectDto dto)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
            return BadRequest("Invalid token: CompanyId missing");

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

        var project = await _projectService.UpdateProjectAsync(id, companyId, dto, userEmail);

        await _auditService.LogAsync(
            companyId, userId,
            action: "Project.Updated",
            entityType: "Project",
            entityId: project.Id,
            description: $"Updated project '{project.Name}' — status: {project.Status}");

        return Ok(project);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "CompanyOwner,Admin")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
            return BadRequest("Invalid token: CompanyId missing");

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // Fetch name before deleting for the audit description
        var project = await _projectService.GetProjectAsync(id, companyId);
        var projectName = project.Name;

        await _projectService.DeleteProjectAsync(id, companyId);

        await _auditService.LogAsync(
            companyId, userId,
            action: "Project.Deleted",
            entityType: "Project",
            entityId: id,
            description: $"Deleted project '{projectName}'");

        return NoContent();
    }
}
