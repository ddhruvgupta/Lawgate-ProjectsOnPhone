using LegalDocSystem.Application.DTOs.Auth;
using LegalDocSystem.Application.DTOs.Users;
using LegalDocSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegalDocSystem.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
        {
            return BadRequest("Invalid token: CompanyId missing");
        }

        var users = await _userService.GetUsersAsync(companyId);
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
         if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
        {
            return BadRequest("Invalid token: CompanyId missing");
        }

        var user = await _userService.GetUserAsync(id);

        // Ensure user belongs to the same company
        if (user.CompanyId != companyId)
        {
            return Forbid();
        }

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto dto)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
        {
            return BadRequest("Invalid token: CompanyId missing");
        }

        // Only allow creation for current company
        var user = await _userService.CreateUserAsync(companyId, dto);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPost("{id}/toggle-status")]
    public async Task<ActionResult<UserDto>> ToggleUserStatus(int id)
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
        {
            return BadRequest("Invalid token: CompanyId missing");
        }

        var user = await _userService.GetUserAsync(id);
        if (user.CompanyId != companyId)
        {
            return Forbid();
        }

        var updatedUser = await _userService.ToggleUserStatusAsync(id);
        return Ok(updatedUser);
    }
}
