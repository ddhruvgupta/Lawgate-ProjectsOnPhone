using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LegalDocSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LegalDocSystem.IntegrationTests.Controllers;

[Collection("Integration")]
public class PlatformAdminControllerTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;
    private HttpClient _ownerClient = null!;
    private HttpClient _superAdminClient = null!;

    public PlatformAdminControllerTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        _ownerClient = _factory.CreateClient();
        var ownerToken = await _factory.GetAuthTokenAsync(_ownerClient, "owner@test.com", "Test@1234");
        _ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        _superAdminClient = _factory.CreateClient();
        var superAdminToken = await _factory.GetAuthTokenAsync(_superAdminClient, "superadmin@lawgate.com", "Admin@1234");
        _superAdminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", superAdminToken);
    }

    public Task DisposeAsync()
    {
        _ownerClient.Dispose();
        _superAdminClient.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetCompanies_AsCompanyOwner_Returns403()
    {
        // Act
        var response = await _ownerClient.GetAsync("/api/admin/companies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCompanies_AsSuperAdmin_Returns200()
    {
        // Act
        var response = await _superAdminClient.GetAsync("/api/admin/companies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetCompany_AsSuperAdmin_Returns200WithCompanyOverview()
    {
        // Arrange: get the list to find the test company ID
        var listResponse = await _superAdminClient.GetAsync("/api/admin/companies");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listJson = await listResponse.Content.ReadAsStringAsync();
        using var listDoc = JsonDocument.Parse(listJson);
        var companies = listDoc.RootElement.EnumerateArray().ToList();
        companies.Should().NotBeEmpty();

        var companyId = companies.First().GetProperty("id").GetInt32();

        // Act
        var response = await _superAdminClient.GetAsync($"/api/admin/companies/{companyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("id", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("users", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("projects", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetCompany_AsCompanyOwner_Returns403()
    {
        // Act
        var response = await _ownerClient.GetAsync("/api/admin/companies/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
