using LegalDocSystem.Application.DTOs.Auth;
using LegalDocSystem.Application.DTOs.Users;
using LegalDocSystem.Application.Interfaces;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LegalDocSystem.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync(int companyId)
    {
        return await _context.Users
            .Where(u => u.CompanyId == companyId)
            .Select(u => new UserDto
            {
                Id = u.Id,
                CompanyId = u.CompanyId,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Phone = u.Phone,
                Role = u.Role.ToString(),
                CompanyName = u.Company.Name,
                IsActive = u.IsActive
            })
            .ToListAsync();
    }

    public async Task<UserDto> GetUserAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) throw new KeyNotFoundException("User not found");

        return MapToDto(user);
    }

    public async Task<UserDto> CreateUserAsync(int companyId, CreateUserDto dto, string createdBy)
    {
        // Check if email exists
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            throw new InvalidOperationException("Email is already in use");
        }

        var user = new User
        {
            CompanyId = companyId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone ?? string.Empty,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Load company relationship for DTO mapping
        await _context.Entry(user).Reference(u => u.Company).LoadAsync();

        return MapToDto(user);
    }

    public async Task<UserDto> ToggleUserStatusAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) throw new KeyNotFoundException("User not found");

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();

        return MapToDto(user);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            CompanyId = user.CompanyId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            CompanyName = user.Company?.Name ?? string.Empty,
            IsActive = user.IsActive
        };
    }
}
