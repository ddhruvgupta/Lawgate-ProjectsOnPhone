using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LegalDocSystem.IntegrationTests.Infrastructure;
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
}
