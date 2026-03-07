using LegalDocSystem.Application.DTOs.Companies;
using LegalDocSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LegalDocSystem.API.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompanyController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CompanyDto>> GetMyCompany()
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
        {
            return BadRequest("Invalid token: CompanyId missing");
        }

        var company = await _companyService.GetCompanyAsync(companyId);
        return Ok(company);
    }

    [HttpPut("me")]
    public async Task<ActionResult<CompanyDto>> UpdateMyCompany(UpdateCompanyDto dto)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
        {
            return BadRequest("Invalid token: CompanyId missing");
        }

        // Optional: Check if user is CompanyOwner
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role != "CompanyOwner")
        {
            return Forbid();
        }

        var company = await _companyService.UpdateCompanyAsync(companyId, dto);
        return Ok(company);
    }
}
