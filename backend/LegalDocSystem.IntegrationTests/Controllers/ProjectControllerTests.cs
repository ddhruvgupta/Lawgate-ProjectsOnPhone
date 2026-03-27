using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LegalDocSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LegalDocSystem.IntegrationTests.Controllers;

[Collection("Integration")]
public class ProjectControllerTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;
    private HttpClient _client = null!;

    public ProjectControllerTests(TestWebAppFactory factory)
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
    public async Task GetProjects_Returns200WithList()
    {
        // Act
        var response = await _client.GetAsync("/api/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task CreateProject_Returns201WithCreatedProject()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/projects", new
        {
            name = "Integration Test Project",
            description = "Created by integration test",
            status = "Intake"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var name = doc.RootElement.GetProperty("name").GetString();
        name.Should().Be("Integration Test Project");
    }

    [Fact]
    public async Task GetProject_WithExistingId_Returns200()
    {
        // Arrange: create a project first
        var createResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            name = "Fetch Me Project",
            description = "For fetch test",
            status = "Intake"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var projectId = createDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await _client.GetAsync($"/api/projects/{projectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProject_WithNonExistentId_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/projects/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProject_Returns200WithUpdatedProject()
    {
        // Arrange: create a project
        var createResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            name = "Update Me Project",
            description = "Original description",
            status = "Intake"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var projectId = createDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/projects/{projectId}", new
        {
            name = "Updated Project Name",
            description = "Updated description",
            status = "Active"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var name = doc.RootElement.GetProperty("name").GetString();
        name.Should().Be("Updated Project Name");
    }

    [Fact]
    public async Task DeleteProject_Returns204()
    {
        // Arrange: create a project
        var createResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            name = "Delete Me Project",
            description = "Will be deleted",
            status = "Intake"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var projectId = createDoc.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await _client.DeleteAsync($"/api/projects/{projectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
