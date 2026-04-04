using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Infrastructure.Data;
using LegalDocSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LegalDocSystem.IntegrationTests.Controllers;

[Collection("Integration")]
public class AuthControllerTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;
    private HttpClient _client = null!;

    public AuthControllerTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    // Replicates AuthService.HashToken for setting up test DB state
    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private async Task<User> GetUserFromDbAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Users.FirstAsync(u => u.Email == email);
    }

    private async Task UpdateUserAsync(string email, Action<User> update)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);
        update(user);
        await db.SaveChangesAsync();
    }

    // ─── Register ──────────────────────────────────────────────────────────

    private static object ValidRegisterPayload(string email = "newowner@integration.com", string companyEmail = "newco@integration.com") => new
    {
        companyName = "Integration Law Firm",
        companyEmail,
        firstName = "Jane",
        lastName = "Smith",
        email,
        password = "Secure@1234",
    };

    [Fact]
    public async Task Register_WithValidData_Returns200WithToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterPayload());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("data").GetProperty("token").GetString()
            .Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_WithValidData_PersistsUserToDatabase()
    {
        await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterPayload());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "newowner@integration.com");
        user.Should().NotBeNull();
        user!.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
    }

    [Fact]
    public async Task Register_WithValidData_SetsIsEmailVerifiedFalse()
    {
        await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterPayload());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == "newowner@integration.com");
        user.IsEmailVerified.Should().BeFalse();
        user.EmailVerificationToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_WithValidData_CreatesTrialCompany()
    {
        await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterPayload());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Email == "newco@integration.com");
        company.Should().NotBeNull();
        company!.Name.Should().Be("Integration Law Firm");
        company.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithPhone_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            companyName = "Phone Firm",
            companyEmail = "phonefirm@integration.com",
            firstName = "John",
            lastName = "Doe",
            email = "john.phone@integration.com",
            password = "Secure@1234",
            phone = "+14047175785",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithoutPhone_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            companyName = "No Phone Firm",
            companyEmail = "nophone@integration.com",
            firstName = "John",
            lastName = "Doe",
            email = "nophone@integration.com",
            password = "Secure@1234",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithMissingCompanyName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            companyEmail = "x@test.com",
            firstName = "A",
            lastName = "B",
            email = "x@test.com",
            password = "Secure@1234",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithMissingFirstName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            companyName = "Firm",
            companyEmail = "x@test.com",
            lastName = "B",
            email = "x@test.com",
            password = "Secure@1234",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithMissingLastName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            companyName = "Firm",
            companyEmail = "x@test.com",
            firstName = "A",
            email = "x@test.com",
            password = "Secure@1234",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithMissingEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            companyName = "Firm",
            companyEmail = "x@test.com",
            firstName = "A",
            lastName = "B",
            password = "Secure@1234",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidEmailFormat_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            companyName = "Firm",
            companyEmail = "not-an-email",
            firstName = "A",
            lastName = "B",
            email = "also-not-email",
            password = "Secure@1234",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithPasswordTooShort_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            companyName = "Firm",
            companyEmail = "x@test.com",
            firstName = "A",
            lastName = "B",
            email = "x@test.com",
            password = "short",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithDuplicateUserEmail_Returns400()
    {
        // Arrange — register once
        await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterPayload(
            email: "dup@integration.com", companyEmail: "dup1@integration.com"));

        // Act — second registration with same user email
        var response = await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterPayload(
            email: "dup@integration.com", companyEmail: "dup2@integration.com"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Register_WithDuplicateCompanyEmail_Returns400()
    {
        // Arrange — register once
        await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterPayload(
            email: "owner1@integration.com", companyEmail: "shared@integration.com"));

        // Act — second registration with same company email
        var response = await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterPayload(
            email: "owner2@integration.com", companyEmail: "shared@integration.com"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    // ─── Login ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "owner@test.com",
            password = "Test@1234"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var success = doc.RootElement.GetProperty("success").GetBoolean();
        success.Should().BeTrue();

        var token = doc.RootElement.GetProperty("data").GetProperty("token").GetString();
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "owner@test.com",
            password = "WrongPassword"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody@unknown.com",
            password = "Test@1234"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── RefreshToken ──────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshToken_WithValidToken_Returns200WithNewTokens()
    {
        // Arrange: log in to get a refresh token
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "owner@test.com",
            password = "Test@1234"
        });
        loginResponse.EnsureSuccessStatusCode();

        var loginJson = await loginResponse.Content.ReadAsStringAsync();
        using var loginDoc = JsonDocument.Parse(loginJson);
        var refreshToken = loginDoc.RootElement.GetProperty("data").GetProperty("refreshToken").GetString();
        refreshToken.Should().NotBeNullOrWhiteSpace();

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("data").GetProperty("token").GetString()
            .Should().NotBeNullOrWhiteSpace();
        doc.RootElement.GetProperty("data").GetProperty("refreshToken").GetString()
            .Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_Returns401()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = "completely-invalid-token"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── ForgotPassword ────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_WithValidEmail_Returns200()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = "owner@test.com"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPassword_WithUnknownEmail_StillReturns200()
    {
        // The endpoint silently ignores unknown emails to prevent email enumeration
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = "nobody@unknown.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WithInvalidEmailFormat_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = "not-an-email"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── ResetPassword ─────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_WithValidToken_Returns200()
    {
        // Arrange: inject a known reset token directly into DB
        const string rawToken = "integration-test-reset-token";
        await UpdateUserAsync("owner@test.com", u =>
        {
            u.ResetToken = HashToken(rawToken);
            u.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = rawToken,
            newPassword = "NewIntegration@1234",
            confirmPassword = "NewIntegration@1234"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Restore password so other tests still work
        await UpdateUserAsync("owner@test.com", u =>
        {
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234");
        });
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = "invalid-token",
            newPassword = "NewPassword@1234",
            confirmPassword = "NewPassword@1234"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithMismatchedPasswords_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = "some-token",
            newPassword = "Password@1234",
            confirmPassword = "DifferentPassword@1234"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── VerifyEmail ───────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyEmail_WithValidToken_Returns200()
    {
        // Arrange: inject a known verification token
        const string rawToken = "integration-test-verify-token";
        await UpdateUserAsync("owner@test.com", u =>
        {
            u.IsEmailVerified = false;
            u.EmailVerificationToken = HashToken(rawToken);
            u.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", new
        {
            token = rawToken
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", new
        {
            token = "invalid-verify-token"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── ResendVerification ────────────────────────────────────────────────

    [Fact]
    public async Task ResendVerification_WithValidEmail_Returns200()
    {
        // Ensure user is not yet verified
        await UpdateUserAsync("owner@test.com", u => u.IsEmailVerified = false);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/resend-verification", new
        {
            email = "owner@test.com"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResendVerification_WithUnknownEmail_StillReturns200()
    {
        // Silently ignores unknown emails to prevent email enumeration
        var response = await _client.PostAsJsonAsync("/api/auth/resend-verification", new
        {
            email = "nobody@unknown.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
