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
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly int _jwtExpiryMinutes;
    private readonly string _frontendBaseUrl;

    public AuthService(
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _logger = logger;
        _jwtExpiryMinutes = int.Parse(configuration["Jwt:ExpiryMinutes"] ?? "1440");
        _frontendBaseUrl = configuration["App:FrontendBaseUrl"] ?? "http://localhost:5173";
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    // Keep backward-compat alias used by refresh token logic
    private static string HashRefreshToken(string token) => HashToken(token);

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').Replace("=", ""); // URL-safe
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
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New company registered: {CompanyName} with owner: {Email}",
                company.Name, user.Email);

            // Generate email verification token
            var rawVerificationToken = GenerateSecureToken();
            user.EmailVerificationToken = HashToken(rawVerificationToken);
            user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);

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

            // Send verification email (non-blocking — failure should not abort registration)
            try
            {
                var verificationLink = $"{_frontendBaseUrl}/verify-email?token={Uri.EscapeDataString(rawVerificationToken)}";
                await _emailService.SendEmailVerificationAsync(user.Email, user.FirstName, verificationLink);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send verification email to {Email}", user.Email);
            }

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

    public async Task ForgotPasswordAsync(string email)
    {
        // Always return success to prevent email enumeration attacks
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !user.IsActive)
        {
            _logger.LogInformation("ForgotPassword requested for unknown/inactive email: {Email}", email);
            return;
        }

        var rawToken = GenerateSecureToken();
        user.ResetToken = HashToken(rawToken);
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

        try
        {
            var resetLink = $"{_frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.FirstName, resetLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
        }
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var tokenHash = HashToken(token);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.ResetToken == tokenHash);

        if (user == null || user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow)
        {
            _logger.LogWarning("Invalid or expired password reset token");
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        // Invalidate all existing refresh tokens on password reset
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset successful for user: {Email}", user.Email);
        return true;
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var tokenHash = HashToken(token);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == tokenHash);

        if (user == null || user.EmailVerificationTokenExpiry == null
            || user.EmailVerificationTokenExpiry < DateTime.UtcNow)
        {
            _logger.LogWarning("Invalid or expired email verification token");
            return false;
        }

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Email verified for user: {Email}", user.Email);
        return true;
    }

    public async Task ResendVerificationEmailAsync(string email)
    {
        // Always succeed silently to prevent email enumeration
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !user.IsActive || user.IsEmailVerified)
            return;

        var rawToken = GenerateSecureToken();
        user.EmailVerificationToken = HashToken(rawToken);
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await _context.SaveChangesAsync();

        try
        {
            var verificationLink = $"{_frontendBaseUrl}/verify-email?token={Uri.EscapeDataString(rawToken)}";
            await _emailService.SendEmailVerificationAsync(user.Email, user.FirstName, verificationLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend verification email to {Email}", email);
        }
    }
}
