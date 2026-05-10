using LegalDocSystem.Application.DTOs.Auth;
using LegalDocSystem.Application.DTOs.Users;
using LegalDocSystem.Application.Interfaces;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LegalDocSystem.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserService> _logger;
    private readonly string _frontendBaseUrl;

    public UserService(ApplicationDbContext context, IEmailService emailService, IConfiguration configuration, ILogger<UserService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
        _frontendBaseUrl = configuration["App:FrontendBaseUrl"] ?? "http://localhost:5173";
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
                IsActive = u.IsActive,
                IsEmailVerified = u.IsEmailVerified
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

        // Generate a verification token. The raw token goes into the invite email
        // button; only the hash is stored. Clicking the button in the invite email
        // proves the user received it and verifies the address in one step.
        var rawVerificationToken = TokenHelper.GenerateSecureToken();

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
            CreatedBy = createdBy,
            IsEmailVerified = false,
            EmailVerificationToken = TokenHelper.HashToken(rawVerificationToken),
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddDays(7),
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Load company relationship for DTO mapping
        await _context.Entry(user).Reference(u => u.Company).LoadAsync();

        // Send the invitation email. The button in the email points to the
        // verify-email page which marks the user as verified and redirects to /login.
        // This is the only email sent — no separate verification email.
        try
        {
            var verificationUrl = $"{_frontendBaseUrl}/verify-email?token={Uri.EscapeDataString(rawVerificationToken)}";

            await _emailService.SendTeamInviteEmailAsync(
                toEmail: user.Email,
                firstName: user.FirstName,
                invitedByName: createdBy,
                companyName: user.Company.Name,
                verificationUrl: verificationUrl,
                temporaryPassword: dto.Password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send team invitation email to {Email}", user.Email);
        }

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
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified
        };
    }
}
