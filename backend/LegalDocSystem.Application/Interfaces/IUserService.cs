using LegalDocSystem.Application.DTOs.Auth;
using LegalDocSystem.Application.DTOs.Users;

namespace LegalDocSystem.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetUsersAsync(int companyId);
    Task<UserDto> GetUserAsync(int id);
    Task<UserDto> CreateUserAsync(int companyId, CreateUserDto dto);
    Task<UserDto> ToggleUserStatusAsync(int id);
}
