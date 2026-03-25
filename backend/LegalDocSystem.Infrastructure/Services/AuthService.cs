using System.Security.Cryptography;
using LegalDocSystem.Application.DTOs.Auth;
using LegalDocSystem.Application.Interfaces;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Domain.Enums;
using LegalDocSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LegalDocSystem.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly int _jwtExpiryMinutes;

    public AuthService(
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
        _jwtExpiryMinutes = int.Parse(configuration["Jwt:ExpiryMinutes"] ?? "1440");
    }

    private static string HashRefreshToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    public async Task<TokenResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            // Check if company email already exists
            var existingCompany = await _context.Companies
                .FirstOrDefaultAsync(c => c.Email == registerDto.CompanyEmail);

            if (existingCompany != null)
            {
                throw new InvalidOperationException("Company with this email already exists");
            }

            // Check if user email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            // Create company
            var company = new Company
            {
                Name = registerDto.CompanyName,
                Email = registerDto.CompanyEmail,
                Phone = registerDto.CompanyPhone,
                Address = "",
                City = "",
                State = "",
                Country = "",
                PostalCode = "",
                SubscriptionTier = SubscriptionTier.Trial,
                SubscriptionStartDate = DateTime.UtcNow,
                SubscriptionEndDate = DateTime.UtcNow.AddDays(14), // 14-day trial
                IsActive = true,
                StorageUsedBytes = 0,
                StorageQuotaBytes = 10L * 1024 * 1024 * 1024, // 10 GB for trial
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            // Create user as company owner
            var user = new User
            {
                CompanyId = company.Id,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                Phone = registerDto.Phone,
                Role = UserRole.CompanyOwner,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New company registered: {CompanyName} with owner: {Email}",
                company.Name, user.Email);

            // Generate tokens
            var token = _jwtTokenService.GenerateToken(
                user.Id,
                user.Email,
                user.Role.ToString(),
                user.CompanyId);

            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            user.RefreshToken = HashRefreshToken(refreshToken);
            user.RefreshTokenExpiry = refreshTokenExpiry;
            await _context.SaveChangesAsync();

            return new TokenResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpiryMinutes),
                User = new UserDto
                {
                    Id = user.Id,
                    CompanyId = user.CompanyId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role.ToString(),
                    CompanyName = company.Name,
                    IsActive = user.IsActive
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            throw;
        }
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto loginDto)
    {
        try
        {
            // Find user by email
            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("User account is inactive");
            }

            // Check if company is active
            if (!user.Company.IsActive)
            {
                throw new UnauthorizedAccessException("Company account is inactive");
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;

            // Generate tokens
            var token = _jwtTokenService.GenerateToken(
                user.Id,
                user.Email,
                user.Role.ToString(),
                user.CompanyId);

            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            user.RefreshToken = HashRefreshToken(refreshToken);
            user.RefreshTokenExpiry = refreshTokenExpiry;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User logged in: {Email}", user.Email);

            return new TokenResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpiryMinutes),
                User = new UserDto
                {
                    Id = user.Id,
                    CompanyId = user.CompanyId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role.ToString(),
                    CompanyName = user.Company.Name,
                    IsActive = user.IsActive
                }
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            throw;
        }
    }

    public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);

        var user = await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.RefreshToken == tokenHash);

        if (user == null || user.RefreshTokenExpiry == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        if (!user.IsActive || !user.Company.IsActive)
            throw new UnauthorizedAccessException("User or company account is inactive");

        var newAccessToken = _jwtTokenService.GenerateToken(user.Id, user.Email, user.Role.ToString(), user.CompanyId);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenHash = HashRefreshToken(newRefreshToken);
        var newRefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        // Atomic rotation: only update if the stored hash still matches (prevents replay attacks)
        var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE ""Users""
               SET ""RefreshToken"" = {newRefreshTokenHash},
                   ""RefreshTokenExpiry"" = {newRefreshTokenExpiry}
             WHERE ""Id"" = {user.Id}
               AND ""RefreshToken"" = {tokenHash}");

        if (rowsAffected == 0)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        _logger.LogInformation("Refresh token rotated for user: {Email}", user.Email);

        return new TokenResponseDto
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpiryMinutes),
            User = new UserDto
            {
                Id = user.Id,
                CompanyId = user.CompanyId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role.ToString(),
                CompanyName = user.Company.Name,
                IsActive = user.IsActive
            }
        };
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        return await Task.FromResult(_jwtTokenService.ValidateToken(token));
    }
}
