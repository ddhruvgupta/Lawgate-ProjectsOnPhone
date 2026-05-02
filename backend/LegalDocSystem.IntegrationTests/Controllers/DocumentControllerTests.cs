using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LegalDocSystem.Application.Interfaces;
using LegalDocSystem.Domain.Enums;
using LegalDocSystem.Infrastructure.Data;
using LegalDocSystem.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LegalDocSystem.IntegrationTests.Controllers;

/// <summary>
/// Fake blob storage used in integration tests — avoids needing a real Azure connection.
/// Returns predictable SAS-like URLs and simulates a single stored blob.
/// </summary>
internal sealed class FakeBlobStorageService : IBlobStorageService
{
    private readonly Dictionary<string, long> _blobs = new();

    /// <summary>Pre-seed a blob so ConfirmUpload succeeds in tests.</summary>
    public void SeedBlob(string path, long size) => _blobs[path] = size;

    public Task<string> UploadAsync(Stream content, string fileName, string containerName)
    {
        _blobs[fileName] = content.Length;
        return Task.FromResult($"https://fake.blob/{containerName}/{fileName}");
    }

    public Task<Stream> DownloadAsync(string fileName, string containerName)
    {
        var size = _blobs.GetValueOrDefault(fileName, 0L);
        return Task.FromResult<Stream>(new MemoryStream(new byte[checked((int)size)]));
    }

    public Task DeleteAsync(string fileName, string containerName)
    {
        _blobs.Remove(fileName);
        return Task.CompletedTask;
    }

    public string GetSasUri(string fileName, string containerName, StorageAccessPermissions permissions, int expirationMinutes = 60)
        => $"https://fake.blob/{containerName}/{fileName}?sas=fake&exp={expirationMinutes}";

    public Task<long> GetBlobSizeAsync(string fileName, string containerName)
    {
        var size = _blobs.GetValueOrDefault(fileName, 0L);
        return Task.FromResult(size);
    }

    public Task SetBlobTagsAsync(string fileName, string containerName, IDictionary<string, string> tags)
        => Task.CompletedTask;
}

[Collection("Integration")]
public class DocumentControllerTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;
    private HttpClient _client = null!;
    private FakeBlobStorageService _fakeBlob = null!;

    public DocumentControllerTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        // Replace IBlobStorageService with a fake for this test run
        _fakeBlob = new FakeBlobStorageService();
        _factory.Services.GetRequiredService<IServiceScopeFactory>(); // warm-up

        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var existing = services.SingleOrDefault(d => d.ServiceType == typeof(IBlobStorageService));
                if (existing != null) services.Remove(existing);
                services.AddScoped<IBlobStorageService>(_ => _fakeBlob);
            });
        }).CreateClient();

        var token = await _factory.GetAuthTokenAsync(_client, "owner@test.com", "Test@1234");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private async Task<int> GetProjectIdAsync()
    {
        var response = await _client.GetAsync("/api/projects");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var projects = doc.RootElement.EnumerateArray().ToList();

        if (projects.Count > 0)
            return projects[0].GetProperty("id").GetInt32();

        // Create one if none exist
        var create = await _client.PostAsJsonAsync("/api/projects", new
        {
            name = "Doc Test Project",
            description = "for doc upload tests",
            status = "Intake"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadAsStringAsync();
        using var d2 = JsonDocument.Parse(created);
        return d2.RootElement.GetProperty("id").GetInt32();
    }

    private static object UploadUrlPayload(int projectId, string fileName = "brief.pdf", long size = 2048) => new
    {
        projectId,
        fileName,
        fileSizeBytes = size,
        documentType = 2, // Brief
        description = "Integration test doc",
        contentType = "application/pdf"
    };

    // ── POST /api/documents/upload-url ────────────────────────────────────

    [Fact]
    public async Task GenerateUploadUrl_WithoutAuth_Returns401()
    {
        var anon = _factory.CreateClient();
        var projectId = await GetProjectIdAsync();
        var response = await anon.PostAsJsonAsync("/api/documents/upload-url", UploadUrlPayload(projectId));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateUploadUrl_ValidRequest_Returns200WithSasUrl()
    {
        var projectId = await GetProjectIdAsync();

        var response = await _client.PostAsJsonAsync("/api/documents/upload-url", UploadUrlPayload(projectId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("uploadUrl").GetString().Should().StartWith("https://fake.blob/");
        doc.RootElement.GetProperty("documentId").GetInt32().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("blobName").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateUploadUrl_NonExistentProject_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/api/documents/upload-url", UploadUrlPayload(99999));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateUploadUrl_MissingRequiredFields_Returns400()
    {
        // Missing fileName and fileSizeBytes
        var response = await _client.PostAsJsonAsync("/api/documents/upload-url", new
        {
            projectId = 1,
            documentType = 2
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/documents/{id}/confirm ─────────────────────────────────

    [Fact]
    public async Task ConfirmUpload_WithoutAuth_Returns401()
    {
        var anon = _factory.CreateClient();
        var response = await anon.PostAsync("/api/documents/1/confirm", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConfirmUpload_WhenBlobSeeded_Returns200WithActiveDocument()
    {
        var projectId = await GetProjectIdAsync();

        // Step 1: get upload URL (creates pending document)
        var uploadResp = await _client.PostAsJsonAsync("/api/documents/upload-url", UploadUrlPayload(projectId, size: 1024));
        uploadResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadBody = await uploadResp.Content.ReadAsStringAsync();
        using var uploadDoc = JsonDocument.Parse(uploadBody);
        var documentId = uploadDoc.RootElement.GetProperty("documentId").GetInt32();
        var blobName = uploadDoc.RootElement.GetProperty("blobName").GetString()!;

        // Simulate a successful blob PUT: seed the fake blob with the right path and size
        _fakeBlob.SeedBlob(blobName, 1024);

        // Step 2: confirm
        var confirmResp = await _client.PostAsync($"/api/documents/{documentId}/confirm", null);
        confirmResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await confirmResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("id").GetInt32().Should().Be(documentId);
        doc.RootElement.GetProperty("fileName").GetString().Should().Be("brief.pdf");
    }

    [Fact]
    public async Task ConfirmUpload_WhenBlobNotUploaded_Returns400()
    {
        var projectId = await GetProjectIdAsync();

        var uploadResp = await _client.PostAsJsonAsync("/api/documents/upload-url", UploadUrlPayload(projectId));
        uploadResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await uploadResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var documentId = doc.RootElement.GetProperty("documentId").GetInt32();

        // Do NOT seed the blob → size returns 0
        var confirmResp = await _client.PostAsync($"/api/documents/{documentId}/confirm", null);
        confirmResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmUpload_NonExistentDocument_Returns404()
    {
        var response = await _client.PostAsync("/api/documents/99999/confirm", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/documents/{id}/download-url ─────────────────────────────

    [Fact]
    public async Task GetDownloadUrl_WithoutAuth_Returns401()
    {
        var anon = _factory.CreateClient();
        var response = await anon.GetAsync("/api/documents/1/download-url");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDownloadUrl_ForOwnedDocument_Returns200WithUrl()
    {
        // Create + confirm a document first
        var projectId = await GetProjectIdAsync();
        var uploadResp = await _client.PostAsJsonAsync("/api/documents/upload-url", UploadUrlPayload(projectId, size: 512));
        var uploadBody = await uploadResp.Content.ReadAsStringAsync();
        using var uploadDoc = JsonDocument.Parse(uploadBody);
        var documentId = uploadDoc.RootElement.GetProperty("documentId").GetInt32();
        var blobName = uploadDoc.RootElement.GetProperty("blobName").GetString()!;
        _fakeBlob.SeedBlob(blobName, 512);
        await _client.PostAsync($"/api/documents/{documentId}/confirm", null);

        var response = await _client.GetAsync($"/api/documents/{documentId}/download-url");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("downloadUrl").GetString().Should().StartWith("https://fake.blob/");
    }

    [Fact]
    public async Task GetDownloadUrl_NonExistentDocument_Returns404()
    {
        var response = await _client.GetAsync("/api/documents/99999/download-url");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/documents/project/{projectId} ───────────────────────────

    [Fact]
    public async Task GetProjectDocuments_WithoutAuth_Returns401()
    {
        var anon = _factory.CreateClient();
        var response = await anon.GetAsync("/api/documents/project/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjectDocuments_EmptyProject_Returns200WithEmptyArray()
    {
        var projectId = await GetProjectIdAsync();
        var response = await _client.GetAsync($"/api/documents/project/{projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetProjectDocuments_AfterConfirmedUpload_ReturnsDocument()
    {
        var projectId = await GetProjectIdAsync();

        var uploadResp = await _client.PostAsJsonAsync("/api/documents/upload-url", UploadUrlPayload(projectId, "motion.docx", 2048));
        var uploadBody = await uploadResp.Content.ReadAsStringAsync();
        using var uploadDoc = JsonDocument.Parse(uploadBody);
        var documentId = uploadDoc.RootElement.GetProperty("documentId").GetInt32();
        var blobName = uploadDoc.RootElement.GetProperty("blobName").GetString()!;
        _fakeBlob.SeedBlob(blobName, 2048);
        await _client.PostAsync($"/api/documents/{documentId}/confirm", null);

        var response = await _client.GetAsync($"/api/documents/project/{projectId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var listDoc = JsonDocument.Parse(body);
        var docs = listDoc.RootElement.EnumerateArray().ToList();
        docs.Should().Contain(d => d.GetProperty("id").GetInt32() == documentId);
    }

    // ── DELETE /api/documents/{id} ────────────────────────────────────────

    [Fact]
    public async Task DeleteDocument_WithoutAuth_Returns401()
    {
        var anon = _factory.CreateClient();
        var response = await anon.DeleteAsync("/api/documents/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteDocument_AsOwner_Returns204()
    {
        var projectId = await GetProjectIdAsync();
        var uploadResp = await _client.PostAsJsonAsync("/api/documents/upload-url", UploadUrlPayload(projectId, size: 256));
        var uploadBody = await uploadResp.Content.ReadAsStringAsync();
        using var uploadDoc = JsonDocument.Parse(uploadBody);
        var documentId = uploadDoc.RootElement.GetProperty("documentId").GetInt32();
        var blobName = uploadDoc.RootElement.GetProperty("blobName").GetString()!;
        _fakeBlob.SeedBlob(blobName, 256);
        await _client.PostAsync($"/api/documents/{documentId}/confirm", null);

        var deleteResp = await _client.DeleteAsync($"/api/documents/{documentId}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteDocument_AsRegularUser_Returns403()
    {
        // Get a token for the regular User role member
        var memberClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var existing = services.SingleOrDefault(d => d.ServiceType == typeof(IBlobStorageService));
                if (existing != null) services.Remove(existing);
                services.AddScoped<IBlobStorageService>(_ => _fakeBlob);
            });
        }).CreateClient();
        var memberToken = await _factory.GetAuthTokenAsync(memberClient, "member@test.com", "Test@1234");
        memberClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var projectId = await GetProjectIdAsync();
        var uploadResp = await _client.PostAsJsonAsync("/api/documents/upload-url", UploadUrlPayload(projectId, size: 256));
        var uploadBody = await uploadResp.Content.ReadAsStringAsync();
        using var uploadDoc = JsonDocument.Parse(uploadBody);
        var documentId = uploadDoc.RootElement.GetProperty("documentId").GetInt32();
        var blobName = uploadDoc.RootElement.GetProperty("blobName").GetString()!;
        _fakeBlob.SeedBlob(blobName, 256);
        await _client.PostAsync($"/api/documents/{documentId}/confirm", null);

        var deleteResp = await memberClient.DeleteAsync($"/api/documents/{documentId}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        memberClient.Dispose();
    }

    [Fact]
    public async Task DeleteDocument_NonExistentDocument_Returns404()
    {
        var response = await _client.DeleteAsync("/api/documents/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
