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

    private Project CreateAndSaveProject(int companyId, string name = "Test Project")
    {
        var project = new Project
        {
            CompanyId = companyId,
            Name = name,
            Description = "A test project",
            Status = ProjectStatus.Active,
            CreatedBy = "test@test.com"
        };
        _context.Projects.Add(project);
        _context.SaveChanges();
        return project;
    }

    [Fact]
    public async Task GetProjectsAsync_ReturnsOnlyProjectsForGivenCompanyId()
    {
        // Arrange
        var company1 = CreateAndSaveCompany("company1@test.com");
        var company2 = CreateAndSaveCompany("company2@test.com");

        CreateAndSaveProject(company1.Id, "Project A");
        CreateAndSaveProject(company1.Id, "Project B");
        CreateAndSaveProject(company2.Id, "Project C");

        // Act
        var result = await _sut.GetProjectsAsync(company1.Id);

        // Assert
        var projects = result.ToList();
        projects.Should().HaveCount(2);
        projects.Should().AllSatisfy(p => p.CompanyId.Should().Be(company1.Id));
        projects.Should().NotContain(p => p.Name == "Project C");
    }

    [Fact]
    public async Task CreateProjectAsync_SetsCompanyIdAndCreatedByCorrectly()
    {
        // Arrange
        var company = CreateAndSaveCompany();
        var dto = new CreateProjectDto
        {
            Name = "New Project",
            Description = "A new test project",
            Status = ProjectStatus.Intake
        };
        const string createdBy = "creator@test.com";

        // Act
        var result = await _sut.CreateProjectAsync(company.Id, dto, createdBy);

        // Assert
        result.CompanyId.Should().Be(company.Id);
        result.Name.Should().Be("New Project");

        var savedProject = await _context.Projects.FirstAsync(p => p.Id == result.Id);
        savedProject.CompanyId.Should().Be(company.Id);
        savedProject.CreatedBy.Should().Be(createdBy);
    }

    [Fact]
    public async Task UpdateProjectAsync_WithMissingProject_ThrowsKeyNotFoundException()
    {
        // Arrange
        var company = CreateAndSaveCompany();
        const int nonExistentId = 99999;
        var dto = new UpdateProjectDto
        {
            Name = "Updated Name",
            Description = "Updated Desc",
            Status = ProjectStatus.Active
        };

        // Act
        Func<Task> act = async () => await _sut.UpdateProjectAsync(nonExistentId, company.Id, dto, "user@test.com");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Project not found");
    }

    [Fact]
    public async Task DeleteProjectAsync_WithMissingProject_ThrowsKeyNotFoundException()
    {
        // Arrange
        var company = CreateAndSaveCompany();
        const int nonExistentId = 99999;

        // Act
        Func<Task> act = async () => await _sut.DeleteProjectAsync(nonExistentId, company.Id);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Project not found");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
