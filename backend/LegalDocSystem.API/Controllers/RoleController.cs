using LegalDocSystem.Application.DTOs.Common;
using LegalDocSystem.Application.DTOs.Roles;
using LegalDocSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegalDocSystem.API.Controllers;

/// <summary>Exposes the available user roles so clients can populate role-assignment dropdowns.</summary>
[ApiController]
[Route("api/roles")]
[Authorize]
public class RoleController : ControllerBase
{
    private static readonly IReadOnlyList<RoleDto> _allRoles = new[]
    {
        new RoleDto((int)UserRole.CompanyOwner,      "CompanyOwner",      "Full access to all company data, billing, and user management.", IsPlatformRole: false),
        new RoleDto((int)UserRole.Admin,             "Admin",             "Elevated privileges — manage users and projects, cannot change billing.", IsPlatformRole: false),
        new RoleDto((int)UserRole.User,              "User",              "Standard access — create and manage their own projects and documents.", IsPlatformRole: false),
        new RoleDto((int)UserRole.Viewer,            "Viewer",            "Read-only access to projects and documents they are added to.", IsPlatformRole: false),
        new RoleDto((int)UserRole.PlatformAdmin,     "PlatformAdmin",     "Lawgate internal — can view all customer companies, users and projects, but not documents.", IsPlatformRole: true),
        new RoleDto((int)UserRole.PlatformSuperAdmin,"PlatformSuperAdmin","Lawgate internal — full visibility including customer documents.", IsPlatformRole: true),
    };

    /// <summary>
    /// Returns the roles that can be assigned to users within a company.
    /// Platform-internal roles (PlatformAdmin, PlatformSuperAdmin) are excluded.
    /// </summary>
    [HttpGet]
    public ActionResult<ApiResponse<IEnumerable<RoleDto>>> GetCompanyRoles()
    {
        var companyRoles = _allRoles.Where(r => !r.IsPlatformRole);
        return Ok(ApiResponse<IEnumerable<RoleDto>>.SuccessResponse(companyRoles));
    }

    /// <summary>
    /// Returns all roles including platform-internal ones.
    /// Restricted to PlatformAdmin and PlatformSuperAdmin.
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "PlatformAdmin,PlatformSuperAdmin")]
    public ActionResult<ApiResponse<IEnumerable<RoleDto>>> GetAllRoles()
    {
        return Ok(ApiResponse<IEnumerable<RoleDto>>.SuccessResponse(_allRoles));
    }
}
