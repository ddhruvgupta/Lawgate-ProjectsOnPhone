using LegalDocSystem.Application.DTOs.Projects;
using LegalDocSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegalDocSystem.API.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
        {
            return BadRequest("Invalid token: CompanyId missing");
        }

        var projects = await _projectService.GetProjectsAsync(companyId);
        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectDto>> GetProject(int id)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
        {
            return BadRequest("Invalid token: CompanyId missing");
        }

        var project = await _projectService.GetProjectAsync(id, companyId);
        return Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateProject(CreateProjectDto dto)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
        {
            return BadRequest("Invalid token: CompanyId missing");
        }

        var project = await _projectService.CreateProjectAsync(companyId, dto);
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProjectDto>> UpdateProject(int id, UpdateProjectDto dto)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
        {
            return BadRequest("Invalid token: CompanyId missing");
        }

        var project = await _projectService.UpdateProjectAsync(id, companyId, dto);
        return Ok(project);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
        {
            return BadRequest("Invalid token: CompanyId missing");
        }

        await _projectService.DeleteProjectAsync(id, companyId);
        return NoContent();
    }
}
