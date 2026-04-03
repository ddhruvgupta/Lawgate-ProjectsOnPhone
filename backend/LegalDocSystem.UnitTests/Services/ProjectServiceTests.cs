using FluentAssertions;
using LegalDocSystem.Application.DTOs.Projects;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Domain.Enums;
using LegalDocSystem.Infrastructure.Data;
using LegalDocSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LegalDocSystem.UnitTests.Services;

public class ProjectServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"ProjectServiceTests_{Guid.NewGuid()}")
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new ProjectService(_context);
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

    private Project CreateAndSaveProject(int companyId, string name = "Test Project",
        ProjectStatus status = ProjectStatus.Active)
    {
        var project = new Project
        {
            CompanyId = companyId,
            Name = name,
            Description = "A test project",
            Status = status,
            CreatedBy = "test@test.com",
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        _context.SaveChanges();
        return project;
    }

    // ─── GetProjectsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetProjectsAsync_WithNoProjects_ReturnsEmptyList()
    {
        var company = CreateAndSaveCompany();

        var result = await _sut.GetProjectsAsync(company.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectsAsync_ReturnsOnlyProjectsForGivenCompanyId()
    {
        var company1 = CreateAndSaveCompany("company1@test.com");
        var company2 = CreateAndSaveCompany("company2@test.com");
        CreateAndSaveProject(company1.Id, "Project A");
        CreateAndSaveProject(company1.Id, "Project B");
        CreateAndSaveProject(company2.Id, "Project C");

        var result = await _sut.GetProjectsAsync(company1.Id);

        var projects = result.ToList();
        projects.Should().HaveCount(2);
        projects.Should().AllSatisfy(p => p.CompanyId.Should().Be(company1.Id));
        projects.Should().NotContain(p => p.Name == "Project C");
    }

    [Fact]
    public async Task GetProjectsAsync_ReturnsProjectsOrderedByCreatedAtDescending()
    {
        var company = CreateAndSaveCompany();

        _context.Projects.AddRange(
            new Project { CompanyId = company.Id, Name = "Oldest", Description = "", CreatedBy = "t", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Project { CompanyId = company.Id, Name = "Newest", Description = "", CreatedBy = "t", CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Project { CompanyId = company.Id, Name = "Middle", Description = "", CreatedBy = "t", CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
        await _context.SaveChangesAsync();

        var result = (await _sut.GetProjectsAsync(company.Id)).ToList();

        result.Should().HaveCount(3);
        result.Select(p => p.CreatedAt).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetProjectsAsync_ReturnsCorrectDocumentCount()
    {
        var company = CreateAndSaveCompany();
        var project = CreateAndSaveProject(company.Id);

        _context.Documents.AddRange(
            new Document { ProjectId = project.Id, UploadedByUserId = 1, FileName = "a.pdf", FileExtension = ".pdf", FileSizeBytes = 100, BlobStoragePath = "x", BlobContainerName = "docs", CreatedBy = "t" },
            new Document { ProjectId = project.Id, UploadedByUserId = 1, FileName = "b.pdf", FileExtension = ".pdf", FileSizeBytes = 100, BlobStoragePath = "y", BlobContainerName = "docs", CreatedBy = "t" }
        );
        await _context.SaveChangesAsync();

        var result = (await _sut.GetProjectsAsync(company.Id)).ToList();

        result.Single().DocumentCount.Should().Be(2);
    }

    // ─── GetProjectAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetProjectAsync_WithValidIdAndCompanyId_ReturnsProject()
    {
        var company = CreateAndSaveCompany();
        var project = CreateAndSaveProject(company.Id, "My Project");

        var result = await _sut.GetProjectAsync(project.Id, company.Id);

        result.Id.Should().Be(project.Id);
        result.Name.Should().Be("My Project");
        result.CompanyId.Should().Be(company.Id);
    }

    [Fact]
    public async Task GetProjectAsync_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        var company = CreateAndSaveCompany();

        Func<Task> act = async () => await _sut.GetProjectAsync(99999, company.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Project not found");
    }

    [Fact]
    public async Task GetProjectAsync_WithWrongCompanyId_ThrowsKeyNotFoundException()
    {
        var company1 = CreateAndSaveCompany("c1@test.com");
        var company2 = CreateAndSaveCompany("c2@test.com");
        var project = CreateAndSaveProject(company1.Id);

        // Attempt to fetch company1's project using company2's ID
        Func<Task> act = async () => await _sut.GetProjectAsync(project.Id, company2.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Project not found");
    }

    // ─── CreateProjectAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateProjectAsync_SetsCompanyIdAndCreatedByCorrectly()
    {
        var company = CreateAndSaveCompany();
        var dto = new CreateProjectDto { Name = "New Project", Description = "Desc", Status = ProjectStatus.Intake };

        var result = await _sut.CreateProjectAsync(company.Id, dto, "creator@test.com");

        result.CompanyId.Should().Be(company.Id);
        var saved = await _context.Projects.FirstAsync(p => p.Id == result.Id);
        saved.CreatedBy.Should().Be("creator@test.com");
    }

    [Fact]
    public async Task CreateProjectAsync_WithAllFields_PersistsAllFields()
    {
        var company = CreateAndSaveCompany();
        var dto = new CreateProjectDto
        {
            Name = "Full Project",
            Description = "Full description",
            ClientName = "ACME Corp",
            CaseNumber = "CASE-001",
            Status = ProjectStatus.Discovery,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            Tags = "[\"contract\",\"urgent\"]"
        };

        var result = await _sut.CreateProjectAsync(company.Id, dto, "user@test.com");

        var saved = await _context.Projects.FirstAsync(p => p.Id == result.Id);
        saved.Name.Should().Be("Full Project");
        saved.ClientName.Should().Be("ACME Corp");
        saved.CaseNumber.Should().Be("CASE-001");
        saved.Status.Should().Be(ProjectStatus.Discovery);
        saved.StartDate.Should().Be(new DateOnly(2026, 1, 1));
        saved.EndDate.Should().Be(new DateOnly(2026, 12, 31));
        saved.Tags.Should().Be("[\"contract\",\"urgent\"]");
    }

    [Fact]
    public async Task CreateProjectAsync_WithNullDates_SucceedsWithNullDates()
    {
        var company = CreateAndSaveCompany();
        var dto = new CreateProjectDto { Name = "No Dates", Description = "", Status = ProjectStatus.Intake };

        var result = await _sut.CreateProjectAsync(company.Id, dto, "user@test.com");

        var saved = await _context.Projects.FirstAsync(p => p.Id == result.Id);
        saved.StartDate.Should().BeNull();
        saved.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task CreateProjectAsync_ReturnsCorrectStatus()
    {
        var company = CreateAndSaveCompany();
        var dto = new CreateProjectDto { Name = "P", Description = "", Status = ProjectStatus.Negotiation };

        var result = await _sut.CreateProjectAsync(company.Id, dto, "user@test.com");

        result.Status.Should().Be(ProjectStatus.Negotiation);
    }

    // ─── UpdateProjectAsync ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateProjectAsync_UpdatesAllFields()
    {
        var company = CreateAndSaveCompany();
        var project = CreateAndSaveProject(company.Id, "Original Name");

        var dto = new UpdateProjectDto
        {
            Name = "Updated Name",
            Description = "Updated Desc",
            ClientName = "New Client",
            CaseNumber = "NEW-001",
            Status = ProjectStatus.Settled,
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2026, 9, 30)
        };

        var result = await _sut.UpdateProjectAsync(project.Id, company.Id, dto, "updater@test.com");

        result.Name.Should().Be("Updated Name");
        result.Status.Should().Be(ProjectStatus.Settled);

        var saved = await _context.Projects.FirstAsync(p => p.Id == project.Id);
        saved.Name.Should().Be("Updated Name");
        saved.Description.Should().Be("Updated Desc");
        saved.ClientName.Should().Be("New Client");
        saved.CaseNumber.Should().Be("NEW-001");
        saved.StartDate.Should().Be(new DateOnly(2026, 3, 1));
        saved.EndDate.Should().Be(new DateOnly(2026, 9, 30));
        saved.UpdatedBy.Should().Be("updater@test.com");
        saved.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateProjectAsync_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        var company = CreateAndSaveCompany();
        var dto = new UpdateProjectDto { Name = "X", Description = "", Status = ProjectStatus.Active };

        Func<Task> act = async () => await _sut.UpdateProjectAsync(99999, company.Id, dto, "user@test.com");

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Project not found");
    }

    [Fact]
    public async Task UpdateProjectAsync_WithWrongCompanyId_ThrowsKeyNotFoundException()
    {
        var company1 = CreateAndSaveCompany("c1@test.com");
        var company2 = CreateAndSaveCompany("c2@test.com");
        var project = CreateAndSaveProject(company1.Id);
        var dto = new UpdateProjectDto { Name = "Hack", Description = "", Status = ProjectStatus.Active };

        Func<Task> act = async () => await _sut.UpdateProjectAsync(project.Id, company2.Id, dto, "hacker@test.com");

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Project not found");
    }

    // ─── DeleteProjectAsync ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteProjectAsync_RemovesProjectFromDatabase()
    {
        var company = CreateAndSaveCompany();
        var project = CreateAndSaveProject(company.Id);

        await _sut.DeleteProjectAsync(project.Id, company.Id);

        var deleted = await _context.Projects.FindAsync(project.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProjectAsync_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        var company = CreateAndSaveCompany();

        Func<Task> act = async () => await _sut.DeleteProjectAsync(99999, company.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Project not found");
    }

    [Fact]
    public async Task DeleteProjectAsync_WithWrongCompanyId_ThrowsKeyNotFoundException()
    {
        var company1 = CreateAndSaveCompany("c1@test.com");
        var company2 = CreateAndSaveCompany("c2@test.com");
        var project = CreateAndSaveProject(company1.Id);

        Func<Task> act = async () => await _sut.DeleteProjectAsync(project.Id, company2.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Project not found");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
