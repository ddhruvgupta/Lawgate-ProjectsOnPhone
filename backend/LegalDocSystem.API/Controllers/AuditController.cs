using LegalDocSystem.Application.DTOs.Audit;
using LegalDocSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegalDocSystem.API.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Get audit logs for the authenticated company.
    /// Supports filtering by entityType and entityId, with pagination.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "CompanyOwner,Admin")]
    public async Task<ActionResult<object>> GetLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] int? entityId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
            return BadRequest("Invalid token: CompanyId missing");

        pageSize = Math.Clamp(pageSize, 1, 200);

        var (items, totalCount) = await _auditService.GetLogsAsync(companyId, entityType, entityId, page, pageSize);

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        });
    }
}
