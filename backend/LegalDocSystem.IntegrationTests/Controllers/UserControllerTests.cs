using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LegalDocSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LegalDocSystem.IntegrationTests.Controllers;

[Collection("Integration")]
public class UserControllerTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;
    private HttpClient _client = null!;

    public UserControllerTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient();
        var token = await _factory.GetAuthTokenAsync(_client, "owner@test.com", "Test@1234");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetUsers_Returns200WithCurrentUser()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task CreateUser_Returns201WithNewUser()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            firstName = "New",
            lastName = "Member",
            email = $"newmember_{Guid.NewGuid()}@test.com",
            password = "Member@1234",
            role = "User"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("firstName").GetString().Should().Be("New");
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange: create a user first
        var email = $"duplicate_{Guid.NewGuid()}@test.com";
        var firstResponse = await _client.PostAsJsonAsync("/api/users", new
        {
            firstName = "First",
            lastName = "User",
            email = email,
            password = "Member@1234",
            role = "User"
        });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act: try creating another user with the same email
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            firstName = "Duplicate",
            lastName = "User",
            email = email,
            password = "Member@1234",
            role = "User"
        });

        // Assert: the middleware returns 400 for InvalidOperationException
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ToggleUserStatus_Returns200()
    {
        // Arrange: create a user to toggle
        var createResponse = await _client.PostAsJsonAsync("/api/users", new
        {
            firstName = "Toggle",
            lastName = "Me",
            email = $"toggle_{Guid.NewGuid()}@test.com",
            password = "Toggle@1234",
            role = "User"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var userId = createDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await _client.PostAsync($"/api/users/{userId}/toggle-status", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
