using System.Security.Cryptography;
using FluentAssertions;
using LegalDocSystem.Application.DTOs.Auth;
using LegalDocSystem.Application.Interfaces;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Domain.Enums;
using LegalDocSystem.Infrastructure.Data;
using LegalDocSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace LegalDocSystem.UnitTests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"AuthServiceTests_{Guid.NewGuid()}")
            .Options;
        _context = new ApplicationDbContext(options);

        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _jwtTokenService.GenerateToken(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns("fake-jwt-token");
        _jwtTokenService.GenerateRefreshToken().Returns("fake-refresh-token");

        _emailService = Substitute.For<IEmailService>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:ExpiryMinutes", "1440" },
            { "App:FrontendBaseUrl", "http://localhost:5173" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _sut = new AuthService(_context, _jwtTokenService, _emailService, _configuration, NullLogger<AuthService>.Instance);
    }

    // Replicates AuthService.HashToken(token) for setting up test data
    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private Company CreateAndSaveActiveCompany()
    {
        var company = new Company
        {
            Name = "Test Company",
            Email = "company@test.com",
            Phone = "",
            Address = "",
            City = "",
            State = "",
            Country = "",
            PostalCode = "",
            SubscriptionTier = SubscriptionTier.Trial,
            SubscriptionStartDate = DateTime.UtcNow,
            IsActive = true,
            StorageUsedBytes = 0,
            StorageQuotaBytes = 10L * 1024 * 1024 * 1024,
            CreatedBy = "Test"
        };
        _context.Companies.Add(company);
        _context.SaveChanges();
        return company;
    }

    private User CreateAndSaveUser(int companyId, string email, string password, bool isActive = true, bool isEmailVerified = true)
    {
        var user = new User
        {
            CompanyId = companyId,
            FirstName = "Test",
            LastName = "User",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Phone = "",
            Role = UserRole.CompanyOwner,
            IsActive = isActive,
            IsEmailVerified = isEmailVerified,
            CreatedBy = "Test"
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    // ─── Register ──────────────────────────────────────────────────────────

    private static RegisterDto ValidRegisterDto(string email = "newowner@test.com", string companyEmail = "newco@test.com") => new()
    {
        CompanyName = "New Law Firm",
        CompanyEmail = companyEmail,
        FirstName = "Jane",
        LastName = "Smith",
        Email = email,
        Password = "Secure@1234",
    };

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesCompanyInDatabase()
    {
        // Act
        await _sut.RegisterAsync(ValidRegisterDto());

        // Assert
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Email == "newco@test.com");
        company.Should().NotBeNull();
        company!.Name.Should().Be("New Law Firm");
        company.SubscriptionTier.Should().Be(SubscriptionTier.Trial);
        company.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUserWithCompanyOwnerRole()
    {
        // Act
        await _sut.RegisterAsync(ValidRegisterDto());

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newowner@test.com");
        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRole.CompanyOwner);
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_SetsIsEmailVerifiedFalse()
    {
        // Act
        await _sut.RegisterAsync(ValidRegisterDto());

        // Assert
        var user = await _context.Users.FirstAsync(u => u.Email == "newowner@test.com");
        user.IsEmailVerified.Should().BeFalse();
        user.EmailVerificationToken.Should().NotBeNullOrWhiteSpace();
        user.EmailVerificationTokenExpiry.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_StoresHashedPasswordNotPlaintext()
    {
        // Act
        await _sut.RegisterAsync(ValidRegisterDto());

        // Assert
        var user = await _context.Users.FirstAsync(u => u.Email == "newowner@test.com");
        user.PasswordHash.Should().NotBe("Secure@1234");
        BCrypt.Net.BCrypt.Verify("Secure@1234", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsToken()
    {
        // Act
        var result = await _sut.RegisterAsync(ValidRegisterDto());

        // Assert
        result.Token.Should().Be("fake-jwt-token");
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.Email.Should().Be("newowner@test.com");
        result.User.Role.Should().Be("CompanyOwner");
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_SendsVerificationEmail()
    {
        // Act
        await _sut.RegisterAsync(ValidRegisterDto());
        await Task.Delay(200); // allow fire-and-forget Task.Run to complete

        // Assert
        await _emailService.Received(1)
            .SendEmailVerificationAsync("newowner@test.com", "Jane", Arg.Any<string>());
    }

    [Fact]
    public async Task RegisterAsync_WithNullPhone_Succeeds()
    {
        // Arrange
        var dto = ValidRegisterDto();
        dto.Phone = null;
        dto.CompanyPhone = null;

        // Act
        Func<Task> act = async () => await _sut.RegisterAsync(dto);

        // Assert
        await act.Should().NotThrowAsync();
        var user = await _context.Users.FirstAsync(u => u.Email == "newowner@test.com");
        user.Phone.Should().Be(string.Empty);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateCompanyEmail_ThrowsInvalidOperationException()
    {
        // Arrange — seed a company with the same email
        CreateAndSaveActiveCompany(); // Email = "company@test.com"
        var dto = ValidRegisterDto(companyEmail: "company@test.com");

        // Act
        Func<Task> act = async () => await _sut.RegisterAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Company with this email already exists*");
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUserEmail_ThrowsInvalidOperationException()
    {
        // Arrange — seed a user with the same email
        var company = CreateAndSaveActiveCompany();
        CreateAndSaveUser(company.Id, "newowner@test.com", "OldPassword@1234");

        var dto = ValidRegisterDto(); // same user email

        // Act
        Func<Task> act = async () => await _sut.RegisterAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*User with this email already exists*");
    }

    [Fact]
    public async Task RegisterAsync_EmailServiceFailure_DoesNotAbortRegistration()
    {
        // Arrange — email service throws
        _emailService
            .SendEmailVerificationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException(new Exception("SMTP unavailable")));

        // Act — registration should still succeed
        Func<Task> act = async () => await _sut.RegisterAsync(ValidRegisterDto());

        // Assert
        await act.Should().NotThrowAsync();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newowner@test.com");
        user.Should().NotBeNull();
    }

    // ─── Login ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        CreateAndSaveUser(company.Id, "owner@test.com", "Test@1234");

        var loginDto = new LoginDto { Email = "owner@test.com", Password = "Test@1234" };

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("fake-jwt-token");
        result.User.Email.Should().Be("owner@test.com");
        result.User.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_WithUnverifiedEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        CreateAndSaveUser(company.Id, "owner@test.com", "Test@1234", isEmailVerified: false);

        var loginDto = new LoginDto { Email = "owner@test.com", Password = "Test@1234" };

        // Act
        Func<Task> act = async () => await _sut.LoginAsync(loginDto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*not verified*");
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();

        var loginDto = new LoginDto { Email = "nonexistent@test.com", Password = "Test@1234" };

        // Act
        Func<Task> act = async () => await _sut.LoginAsync(loginDto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        CreateAndSaveUser(company.Id, "owner@test.com", "Test@1234");

        var loginDto = new LoginDto { Email = "owner@test.com", Password = "WrongPassword" };

        // Act
        Func<Task> act = async () => await _sut.LoginAsync(loginDto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        CreateAndSaveUser(company.Id, "owner@test.com", "Test@1234", isActive: false);

        var loginDto = new LoginDto { Email = "owner@test.com", Password = "Test@1234" };

        // Act
        Func<Task> act = async () => await _sut.LoginAsync(loginDto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User account is inactive");
    }

    // ─── ForgotPassword ────────────────────────────────────────────────────
    [Fact]
    public async Task ForgotPasswordAsync_WithValidEmail_StoresResetToken()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        var user = CreateAndSaveUser(company.Id, "owner@test.com", "Test@1234");

        // Act
        await _sut.ForgotPasswordAsync("owner@test.com");

        // Assert
        var updated = await _context.Users.FindAsync(user.Id);
        updated!.ResetToken.Should().NotBeNullOrWhiteSpace();
        updated.ResetTokenExpiry.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithValidEmail_SendsPasswordResetEmail()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        CreateAndSaveUser(company.Id, "owner@test.com", "Test@1234");

        // Act
        await _sut.ForgotPasswordAsync("owner@test.com");

        // Assert
        await _emailService.Received(1)
            .SendPasswordResetEmailAsync("owner@test.com", Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithUnknownEmail_DoesNotThrowAndDoesNotSendEmail()
    {
        // Act
        Func<Task> act = async () => await _sut.ForgotPasswordAsync("nobody@unknown.com");

        // Assert
        await act.Should().NotThrowAsync();
        await _emailService.DidNotReceive()
            .SendPasswordResetEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // ─── ResetPassword ─────────────────────────────────────────────────────
    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        const string rawToken = "test-reset-token";
        var user = CreateAndSaveUser(company.Id, "owner@test.com", "Test@1234");
        user.ResetToken = HashToken(rawToken);
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ResetPasswordAsync(rawToken, "NewPassword@1234");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_UpdatesPasswordHash()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        const string rawToken = "test-reset-token";
        var user = CreateAndSaveUser(company.Id, "owner@test.com", "OldPassword@1234");
        user.ResetToken = HashToken(rawToken);
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

        // Act
        await _sut.ResetPasswordAsync(rawToken, "NewPassword@9999");

        // Assert
        var updated = await _context.Users.FindAsync(user.Id);
        BCrypt.Net.BCrypt.Verify("NewPassword@9999", updated!.PasswordHash).Should().BeTrue();
        updated.ResetToken.Should().BeNull();
        updated.ResetTokenExpiry.Should().BeNull();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ClearsRefreshToken()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        const string rawToken = "test-reset-token";
        var user = CreateAndSaveUser(company.Id, "owner@test.com", "Test@1234");
        user.ResetToken = HashToken(rawToken);
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        user.RefreshToken = "some-old-refresh-token";
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        // Act
        await _sut.ResetPasswordAsync(rawToken, "NewPassword@1234");

        // Assert
        var updated = await _context.Users.FindAsync(user.Id);
        updated!.RefreshToken.Should().BeNull();
        updated.RefreshTokenExpiry.Should().BeNull();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        const string rawToken = "expired-reset-token";
        var user = CreateAndSaveUser(company.Id, "owner@test.com", "Test@1234");
        user.ResetToken = HashToken(rawToken);
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(-1); // expired
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ResetPasswordAsync(rawToken, "NewPassword@1234");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ReturnsFalse()
    {
        // Act
        var result = await _sut.ResetPasswordAsync("completely-invalid-token", "NewPassword@1234");

        // Assert
        result.Should().BeFalse();
    }

    // ─── VerifyEmail ───────────────────────────────────────────────────────
    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_ReturnsTrueAndSetsEmailVerified()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        const string rawToken = "test-verify-token";
        var user = CreateAndSaveUser(company.Id, "owner@test.com", "Test@1234");
        user.IsEmailVerified = false;
        user.EmailVerificationToken = HashToken(rawToken);
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.VerifyEmailAsync(rawToken);

        // Assert
        result.Should().BeTrue();
        var updated = await _context.Users.FindAsync(user.Id);
        updated!.IsEmailVerified.Should().BeTrue();
        updated.EmailVerificationToken.Should().BeNull();
        updated.EmailVerificationTokenExpiry.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmailAsync_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        const string rawToken = "expired-verify-token";
        var user = CreateAndSaveUser(company.Id, "owner@test.com", "Test@1234", isEmailVerified: false);
        user.EmailVerificationToken = HashToken(rawToken);
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(-1); // expired
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.VerifyEmailAsync(rawToken);

        // Assert
        result.Should().BeFalse();
        var updated = await _context.Users.FindAsync(user.Id);
        updated!.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidToken_ReturnsFalse()
    {
        // Act
        var result = await _sut.VerifyEmailAsync("completely-invalid-token");

        // Assert
        result.Should().BeFalse();
    }

    // ─── ResendVerificationEmail ──────────────────────────────────────────
    [Fact]
    public async Task ResendVerificationEmailAsync_WithValidEmail_SendsVerificationEmail()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        var user = CreateAndSaveUser(company.Id, "owner@test.com", "Test@1234");
        user.IsEmailVerified = false;
        await _context.SaveChangesAsync();

        // Act
        await _sut.ResendVerificationEmailAsync("owner@test.com");
        await Task.Delay(200); // allow fire-and-forget Task.Run to complete

        // Assert
        await _emailService.Received(1)
            .SendEmailVerificationAsync("owner@test.com", Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_WithAlreadyVerifiedEmail_DoesNotSendEmail()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        var user = CreateAndSaveUser(company.Id, "owner@test.com", "Test@1234");
        user.IsEmailVerified = true;
        await _context.SaveChangesAsync();

        // Act
        await _sut.ResendVerificationEmailAsync("owner@test.com");

        // Assert
        await _emailService.DidNotReceive()
            .SendEmailVerificationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_WithUnknownEmail_DoesNotThrowAndDoesNotSendEmail()
    {
        // Act
        Func<Task> act = async () => await _sut.ResendVerificationEmailAsync("nobody@unknown.com");

        // Assert
        await act.Should().NotThrowAsync();
        await _emailService.DidNotReceive()
            .SendEmailVerificationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // ─── RefreshToken (error paths only — happy path requires a real DB for atomic SQL UPDATE) ──
    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ThrowsUnauthorizedAccessException()
    {
        // No user with this token hash exists
        Func<Task> act = async () => await _sut.RefreshTokenAsync("completely-invalid-token");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid or expired refresh token");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var company = CreateAndSaveActiveCompany();
        const string rawToken = "expired-refresh-token";
        var user = CreateAndSaveUser(company.Id, "owner@test.com", "Test@1234");
        user.RefreshToken = HashToken(rawToken);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1); // expired
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _sut.RefreshTokenAsync(rawToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid or expired refresh token");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
