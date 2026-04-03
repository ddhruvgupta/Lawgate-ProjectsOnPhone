using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LegalDocSystem.Infrastructure.Data;
using LegalDocSystem.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
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

    /// Creates a project and returns its ID.
    private async Task<int> CreateProjectAsync(string name = "Test Project", string status = "Intake")
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name, description = "desc", status });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    // ─── Authentication ────────────────────────────────────────────────────

    [Fact]
    public async Task GetProjects_WithoutAuth_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.GetAsync("/api/projects");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProject_WithoutAuth_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.PostAsJsonAsync("/api/projects", new { name = "X", status = "Intake" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProject_WithoutAuth_Returns401()
    {
        var id = await CreateProjectAsync();
        var anonClient = _factory.CreateClient();
        var response = await anonClient.DeleteAsync($"/api/projects/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── GET /api/projects ─────────────────────────────────────────────────

    [Fact]
    public async Task GetProjects_Returns200WithArray()
    {
        var response = await _client.GetAsync("/api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ─── GET /api/projects/{id} ────────────────────────────────────────────

    [Fact]
    public async Task GetProject_WithExistingId_Returns200()
    {
        var id = await CreateProjectAsync("Fetch Me Project");
        var response = await _client.GetAsync($"/api/projects/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProject_WithNonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/projects/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProject_ReturnsCorrectFields()
    {
        var id = await CreateProjectAsync("Field Check Project");
        var response = await _client.GetAsync($"/api/projects/{id}");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("id").GetInt32().Should().Be(id);
        doc.RootElement.GetProperty("name").GetString().Should().Be("Field Check Project");
        doc.RootElement.GetProperty("status").GetString().Should().Be("Intake");
    }

    // ─── POST /api/projects ────────────────────────────────────────────────

    [Fact]
    public async Task CreateProject_WithMinimalFields_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new
        {
            name = "Minimal Project",
            description = "",
            status = "Intake"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("name").GetString().Should().Be("Minimal Project");
    }

    [Fact]
    public async Task CreateProject_WithAllFields_Returns201WithAllFieldsPresent()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new
        {
            name = "Full Project",
            description = "A full project",
            clientName = "ACME Corp",
            caseNumber = "CASE-2026-001",
            status = "Active",
            startDate = "2026-01-01",
            endDate = "2026-12-31"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("clientName").GetString().Should().Be("ACME Corp");
        doc.RootElement.GetProperty("caseNumber").GetString().Should().Be("CASE-2026-001");
        doc.RootElement.GetProperty("startDate").GetString().Should().Be("2026-01-01");
        doc.RootElement.GetProperty("endDate").GetString().Should().Be("2026-12-31");
    }

    [Fact]
    public async Task CreateProject_WithDates_PreservesDatesExactly()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new
        {
            name = "Date Project",
            description = "",
            status = "Intake",
            startDate = "2026-04-03",
            endDate = "2026-10-31"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("startDate").GetString().Should().Be("2026-04-03");
        doc.RootElement.GetProperty("endDate").GetString().Should().Be("2026-10-31");
    }

    [Fact]
    public async Task CreateProject_WithMissingName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new
        {
            description = "No name",
            status = "Intake"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProject_WritesAuditLog()
    {
        await CreateProjectAsync("Audit Log Project");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var auditEntry = db.AuditLogs.FirstOrDefault(a => a.Action == "Project.Created");
        auditEntry.Should().NotBeNull();
    }

    // ─── PUT /api/projects/{id} ────────────────────────────────────────────

    [Fact]
    public async Task UpdateProject_Returns200WithUpdatedFields()
    {
        var id = await CreateProjectAsync("Original Name");

        var response = await _client.PutAsJsonAsync($"/api/projects/{id}", new
        {
            name = "Updated Name",
            description = "Updated desc",
            status = "Active"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("name").GetString().Should().Be("Updated Name");
        doc.RootElement.GetProperty("status").GetString().Should().Be("Active");
    }

    [Fact]
    public async Task UpdateProject_WithNonExistentId_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/projects/999999", new
        {
            name = "Ghost",
            description = "",
            status = "Active"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProject_WithMissingName_Returns400()
    {
        var id = await CreateProjectAsync();

        var response = await _client.PutAsJsonAsync($"/api/projects/{id}", new
        {
            description = "No name",
            status = "Active"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProject_ClearsDatesWhenOmitted()
    {
        // Create with dates
        var createResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            name = "Date Clearer",
            description = "",
            status = "Intake",
            startDate = "2026-01-01"
        });
        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var id = createDoc.RootElement.GetProperty("id").GetInt32();

        // Update without dates
        var updateResponse = await _client.PutAsJsonAsync($"/api/projects/{id}", new
        {
            name = "Date Clearer",
            description = "",
            status = "Active"
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await updateResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("startDate").ValueKind.Should().Be(JsonValueKind.Null);
    }

    // ─── DELETE /api/projects/{id} ─────────────────────────────────────────

    [Fact]
    public async Task DeleteProject_AsOwner_Returns204()
    {
        var id = await CreateProjectAsync("Delete Me");
        var response = await _client.DeleteAsync($"/api/projects/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProject_AfterDeletion_Returns404OnGet()
    {
        var id = await CreateProjectAsync("Gone Project");
        await _client.DeleteAsync($"/api/projects/{id}");

        var getResponse = await _client.GetAsync($"/api/projects/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProject_WithNonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/projects/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProject_AsRegularUser_Returns403()
    {
        // Create project as owner
        var id = await CreateProjectAsync("Protected Project");

        // Switch to a regular User token
        var memberClient = _factory.CreateClient();
        var memberToken = await _factory.GetAuthTokenAsync(memberClient, "member@test.com", "Test@1234");
        memberClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var response = await memberClient.DeleteAsync($"/api/projects/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
