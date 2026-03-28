using LegalDocSystem.Application.DTOs.Auth;
using LegalDocSystem.Application.DTOs.Users;
using LegalDocSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LegalDocSystem.API.Controllers;

/// <summary>Manage users within a company.</summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    public UserController(IUserService userService, IAuditService auditService)
    {
        _userService = userService;
        _auditService = auditService;
    }

    /// <summary>Lists all active users in the caller's company.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
            return BadRequest("Invalid token: CompanyId missing");

        var users = await _userService.GetUsersAsync(companyId);
        return Ok(users);
    }

    /// <summary>Returns a single user by ID (must belong to the caller's company).</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
            return BadRequest("Invalid token: CompanyId missing");

        var user = await _userService.GetUserAsync(id);
        if (user.CompanyId != companyId)
            return Forbid();

        return Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = "CompanyOwner,Admin")]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto dto)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
            return BadRequest("Invalid token: CompanyId missing");

        var actorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var actorEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

        var user = await _userService.CreateUserAsync(companyId, dto, actorEmail);

        await _auditService.LogAsync(
            companyId, actorId,
            action: "User.Created",
            entityType: "User",
            entityId: user.Id,
            description: $"Added team member {user.FirstName} {user.LastName} ({user.Email}) with role {user.Role}");

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPost("{id}/toggle-status")]
    [Authorize(Roles = "CompanyOwner,Admin")]
    public async Task<ActionResult<UserDto>> ToggleUserStatus(int id)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
            return BadRequest("Invalid token: CompanyId missing");

        var actorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var user = await _userService.GetUserAsync(id);
        if (user.CompanyId != companyId)
            return Forbid();

        var updatedUser = await _userService.ToggleUserStatusAsync(id);

        var action = updatedUser.IsActive ? "User.Activated" : "User.Deactivated";
        var verb = updatedUser.IsActive ? "Activated" : "Deactivated";

        await _auditService.LogAsync(
            companyId, actorId,
            action: action,
            entityType: "User",
            entityId: id,
            description: $"{verb} team member {updatedUser.FirstName} {updatedUser.LastName} ({updatedUser.Email})");

        return Ok(updatedUser);
    }
}
