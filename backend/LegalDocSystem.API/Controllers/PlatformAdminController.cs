using LegalDocSystem.Application.DTOs.Admin;
using LegalDocSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegalDocSystem.API.Controllers;

/// <summary>Platform-level administration endpoints. Restricted to PlatformAdmin and PlatformSuperAdmin roles.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "PlatformAdmin,PlatformSuperAdmin")]
public class PlatformAdminController : ControllerBase
{
    private readonly IPlatformAdminService _adminService;

    public PlatformAdminController(IPlatformAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Returns all customer companies with aggregate stats.
    /// Accessible by PlatformAdmin and PlatformSuperAdmin.
    /// </summary>
    [HttpGet("companies")]
    public async Task<ActionResult<IEnumerable<CompanyOverviewDto>>> GetCompanies()
    {
        var companies = await _adminService.GetAllCompaniesAsync();
        return Ok(companies);
    }

    /// <summary>
    /// Returns full detail for one company including its users and projects (no documents).
    /// Accessible by PlatformAdmin and PlatformSuperAdmin.
    /// </summary>
    [HttpGet("companies/{id:int}")]
    public async Task<ActionResult<CompanyDetailDto>> GetCompany(int id)
    {
        var company = await _adminService.GetCompanyDetailAsync(id);
        return Ok(company);
    }

    /// <summary>
    /// Returns all documents for a company. PlatformSuperAdmin only.
    /// </summary>
    [HttpGet("companies/{id:int}/documents")]
    [Authorize(Roles = "PlatformSuperAdmin")]
    public async Task<ActionResult<IEnumerable<CompanyDocumentDto>>> GetCompanyDocuments(int id)
    {
        var documents = await _adminService.GetCompanyDocumentsAsync(id);
        return Ok(documents);
    }
}
