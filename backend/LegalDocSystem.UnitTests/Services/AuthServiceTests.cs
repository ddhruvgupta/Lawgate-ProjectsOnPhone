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

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:ExpiryMinutes", "1440" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _sut = new AuthService(_context, _jwtTokenService, _configuration, NullLogger<AuthService>.Instance);
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

    private User CreateAndSaveUser(int companyId, string email, string password, bool isActive = true)
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
            CreatedBy = "Test"
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

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

    public void Dispose()
    {
        _context.Dispose();
    }
}
