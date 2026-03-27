using FluentAssertions;
using LegalDocSystem.Application.DTOs.Users;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Domain.Enums;
using LegalDocSystem.Infrastructure.Data;
using LegalDocSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LegalDocSystem.UnitTests.Services;

public class UserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"UserServiceTests_{Guid.NewGuid()}")
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new UserService(_context);
    }

    private Company CreateAndSaveCompany(string email = "company@test.com")
    {
        var company = new Company
        {
            Name = "Test Company",
            Email = email,
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

    private User CreateAndSaveUser(int companyId, string email, UserRole role = UserRole.User)
    {
        var user = new User
        {
            CompanyId = companyId,
            FirstName = "Test",
            LastName = "User",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            Phone = "",
            Role = role,
            IsActive = true,
            CreatedBy = "Test"
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsOnlyUsersForGivenCompanyId()
    {
        // Arrange
        var company1 = CreateAndSaveCompany("company1@test.com");
        var company2 = CreateAndSaveCompany("company2@test.com");

        CreateAndSaveUser(company1.Id, "user1@company1.com");
        CreateAndSaveUser(company1.Id, "user2@company1.com");
        CreateAndSaveUser(company2.Id, "user1@company2.com");

        // Act
        var result = await _sut.GetUsersAsync(company1.Id);

        // Assert
        var users = result.ToList();
        users.Should().HaveCount(2);
        users.Should().AllSatisfy(u => u.CompanyId.Should().Be(company1.Id));
        users.Should().NotContain(u => u.Email == "user1@company2.com");
    }

    [Fact]
    public async Task CreateUserAsync_WhenEmailAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var company = CreateAndSaveCompany();
        CreateAndSaveUser(company.Id, "existing@test.com");

        var dto = new CreateUserDto
        {
            FirstName = "New",
            LastName = "User",
            Email = "existing@test.com",
            Password = "Test@1234",
            Role = UserRole.User
        };

        // Act
        Func<Task> act = async () => await _sut.CreateUserAsync(company.Id, dto, "creator@test.com");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email is already in use");
    }

    [Fact]
    public async Task CreateUserAsync_StoresHashedPasswordNotPlaintext()
    {
        // Arrange
        var company = CreateAndSaveCompany();
        var dto = new CreateUserDto
        {
            FirstName = "New",
            LastName = "User",
            Email = "newuser@test.com",
            Password = "PlainTextPassword123",
            Role = UserRole.User
        };

        // Act
        var result = await _sut.CreateUserAsync(company.Id, dto, "creator@test.com");

        // Assert
        var savedUser = await _context.Users.FirstAsync(u => u.Email == "newuser@test.com");
        savedUser.PasswordHash.Should().NotBe("PlainTextPassword123");
        BCrypt.Net.BCrypt.Verify("PlainTextPassword123", savedUser.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ToggleUserStatusAsync_FlipsIsActiveCorrectly()
    {
        // Arrange
        var company = CreateAndSaveCompany();
        var user = CreateAndSaveUser(company.Id, "toggle@test.com");
        user.IsActive.Should().BeTrue();

        // Act - deactivate
        var deactivated = await _sut.ToggleUserStatusAsync(user.Id);
        deactivated.IsActive.Should().BeFalse();

        // Act - reactivate
        var reactivated = await _sut.ToggleUserStatusAsync(user.Id);
        reactivated.IsActive.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
