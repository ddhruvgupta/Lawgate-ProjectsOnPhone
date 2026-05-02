using FluentAssertions;
using LegalDocSystem.Application.DTOs.Documents;
using LegalDocSystem.Application.Interfaces;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Domain.Enums;
using LegalDocSystem.Infrastructure.Data;
using LegalDocSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace LegalDocSystem.UnitTests.Services;

public class DocumentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IBlobStorageService _blob;
    private readonly DocumentService _sut;

    // ── Shared seed IDs ───────────────────────────────────────────────────
    private readonly int _companyId;
    private readonly int _ownerId;
    private readonly int _projectId;

    public DocumentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"DocServiceTests_{Guid.NewGuid()}")
            .Options;
        _context = new ApplicationDbContext(options);

        _blob = Substitute.For<IBlobStorageService>();
        _sut = new DocumentService(_context, _blob, NullLogger<DocumentService>.Instance);

        // Seed: company + owner + project
        var company = new Company
        {
            Name = "Test Firm",
            Email = "firm@test.com",
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
            CreatedBy = "seed"
        };
        _context.Companies.Add(company);
        _context.SaveChanges();
        _companyId = company.Id;

        var owner = new User
        {
            CompanyId = _companyId,
            FirstName = "Jane",
            LastName = "Owner",
            Email = "jane@test.com",
            PasswordHash = "hash",
            Phone = "",
            Role = UserRole.CompanyOwner,
            IsActive = true,
            IsEmailVerified = true,
            CreatedBy = "seed"
        };
        _context.Users.Add(owner);
        _context.SaveChanges();
        _ownerId = owner.Id;

        var project = new Project
        {
            CompanyId = _companyId,
            Name = "Test Project",
            Description = "",
            Status = ProjectStatus.Active,
            CreatedBy = "seed"
        };
        _context.Projects.Add(project);
        _context.SaveChanges();
        _projectId = project.Id;
    }

    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────

    private UploadDocumentDto ValidUploadDto(string fileName = "contract.pdf", long sizeBytes = 1024) => new()
    {
        ProjectId = _projectId,
        FileName = fileName,
        FileSizeBytes = sizeBytes,
        DocumentType = DocumentType.Contract,
        Description = "Test doc",
        ContentType = "application/pdf"
    };

    private Document SeedPendingDocument(long sizeBytes = 1024)
    {
        _blob.GetSasUri(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<StorageAccessPermissions>(), Arg.Any<int>())
             .Returns("https://blob.example/upload?sas=token");

        var doc = new Document
        {
            ProjectId = _projectId,
            UploadedByUserId = _ownerId,
            FileName = "contract.pdf",
            FileExtension = ".pdf",
            FileSizeBytes = sizeBytes,
            DocumentType = DocumentType.Contract,
            BlobContainerName = $"company-{_companyId}",
            BlobStoragePath = $"{_projectId}/abc_contract.pdf",
            Status = DocumentStatus.Pending,
            Version = 1,
            IsLatestVersion = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "jane@test.com"
        };
        _context.Documents.Add(doc);
        _context.SaveChanges();
        return doc;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GenerateUploadUrlAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateUploadUrlAsync_ValidRequest_ReturnsSasUrlAndCreatesDocument()
    {
        const string expectedSas = "https://blob.example/upload?sas=token";
        _blob.GetSasUri(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<StorageAccessPermissions>(), Arg.Any<int>())
             .Returns(expectedSas);

        var result = await _sut.GenerateUploadUrlAsync(_ownerId, ValidUploadDto());

        result.UploadUrl.Should().Be(expectedSas);
        result.DocumentId.Should().BeGreaterThan(0);
        result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(10));

        var saved = await _context.Documents.FindAsync(result.DocumentId);
        saved.Should().NotBeNull();
        saved!.Status.Should().Be(DocumentStatus.Pending);
        saved.ProjectId.Should().Be(_projectId);
        saved.FileName.Should().Be("contract.pdf");
        saved.FileExtension.Should().Be(".pdf");
    }

    [Fact]
    public async Task GenerateUploadUrlAsync_ContainerNameIsCompanyScoped()
    {
        _blob.GetSasUri(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<StorageAccessPermissions>(), Arg.Any<int>())
             .Returns("https://blob.example/sas");

        await _sut.GenerateUploadUrlAsync(_ownerId, ValidUploadDto());

        _blob.Received(1).GetSasUri(
            Arg.Any<string>(),
            $"company-{_companyId}",
            Arg.Any<StorageAccessPermissions>(),
            15);
    }

    [Fact]
    public async Task GenerateUploadUrlAsync_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.GenerateUploadUrlAsync(99999, ValidUploadDto());
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*User not found*");
    }

    [Fact]
    public async Task GenerateUploadUrlAsync_WhenProjectNotFound_ThrowsKeyNotFoundException()
    {
        var dto = ValidUploadDto();
        dto.ProjectId = 99999;

        var act = async () => await _sut.GenerateUploadUrlAsync(_ownerId, dto);
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Project not found*");
    }

    [Fact]
    public async Task GenerateUploadUrlAsync_WhenProjectBelongsToDifferentCompany_ThrowsUnauthorized()
    {
        // Create a project owned by a different company
        var otherCompany = new Company
        {
            Name = "Other Firm", Email = "other@test.com", Phone = "", Address = "", City = "",
            State = "", Country = "", PostalCode = "", SubscriptionTier = SubscriptionTier.Trial,
            SubscriptionStartDate = DateTime.UtcNow, IsActive = true, StorageUsedBytes = 0,
            StorageQuotaBytes = 1024 * 1024, CreatedBy = "seed"
        };
        _context.Companies.Add(otherCompany);
        await _context.SaveChangesAsync();

        var otherProject = new Project
        {
            CompanyId = otherCompany.Id, Name = "Other Project", Description = "",
            Status = ProjectStatus.Active, CreatedBy = "seed"
        };
        _context.Projects.Add(otherProject);
        await _context.SaveChangesAsync();

        var dto = ValidUploadDto();
        dto.ProjectId = otherProject.Id;

        var act = async () => await _sut.GenerateUploadUrlAsync(_ownerId, dto);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GenerateUploadUrlAsync_WhenQuotaExceeded_ThrowsInvalidOperation()
    {
        // Set company quota to exactly 1 byte less than requested size
        var company = await _context.Companies.FindAsync(_companyId);
        company!.StorageUsedBytes = company.StorageQuotaBytes - 100;
        await _context.SaveChangesAsync();

        var dto = ValidUploadDto(sizeBytes: 200);

        var act = async () => await _sut.GenerateUploadUrlAsync(_ownerId, dto);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*quota*");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ConfirmUploadAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConfirmUploadAsync_WhenBlobExists_SetsStatusActiveAndUpdatesQuota()
    {
        const long actualSize = 900L;
        var doc = SeedPendingDocument(sizeBytes: 1024);
        _blob.GetBlobSizeAsync(doc.BlobStoragePath, doc.BlobContainerName).Returns(actualSize);

        var result = await _sut.ConfirmUploadAsync(doc.Id, _ownerId);

        result.Should().NotBeNull();
        result.FileName.Should().Be("contract.pdf");

        var updated = await _context.Documents.FindAsync(doc.Id);
        updated!.Status.Should().Be(DocumentStatus.Active);
        updated.FileSizeBytes.Should().Be(actualSize);

        var company = await _context.Companies.FindAsync(_companyId);
        company!.StorageUsedBytes.Should().Be(actualSize);
    }

    [Fact]
    public async Task ConfirmUploadAsync_WhenBlobSizeIsZero_ThrowsInvalidOperation()
    {
        var doc = SeedPendingDocument();
        _blob.GetBlobSizeAsync(doc.BlobStoragePath, doc.BlobContainerName).Returns(0L);

        var act = async () => await _sut.ConfirmUploadAsync(doc.Id, _ownerId);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found in storage*");
    }

    [Fact]
    public async Task ConfirmUploadAsync_WhenUploadedSizeExceedsDeclaration_DeletesBlobAndThrows()
    {
        const long declaredSize = 500L;
        const long actualSize = 5000L; // larger than declared
        var doc = SeedPendingDocument(sizeBytes: declaredSize);
        _blob.GetBlobSizeAsync(doc.BlobStoragePath, doc.BlobContainerName).Returns(actualSize);

        var act = async () => await _sut.ConfirmUploadAsync(doc.Id, _ownerId);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*exceeds the requested size*");

        await _blob.Received(1).DeleteAsync(doc.BlobStoragePath, doc.BlobContainerName);

        var updated = await _context.Documents.FindAsync(doc.Id);
        updated!.Status.Should().Be(DocumentStatus.Failed);
    }

    [Fact]
    public async Task ConfirmUploadAsync_WhenDocumentNotFound_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.ConfirmUploadAsync(99999, _ownerId);
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Document not found*");
    }

    [Fact]
    public async Task ConfirmUploadAsync_WhenCalledByDifferentUser_ThrowsUnauthorized()
    {
        // Create a second user in same company
        var otherUser = new User
        {
            CompanyId = _companyId, FirstName = "Bob", LastName = "Member",
            Email = "bob@test.com", PasswordHash = "hash", Phone = "",
            Role = UserRole.User, IsActive = true, IsEmailVerified = true, CreatedBy = "seed"
        };
        _context.Users.Add(otherUser);
        await _context.SaveChangesAsync();

        var doc = SeedPendingDocument();
        _blob.GetBlobSizeAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(500L);

        var act = async () => await _sut.ConfirmUploadAsync(doc.Id, otherUser.Id);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ConfirmUploadAsync_SetsBlobConfirmedTag()
    {
        var doc = SeedPendingDocument(sizeBytes: 1024);
        _blob.GetBlobSizeAsync(doc.BlobStoragePath, doc.BlobContainerName).Returns(1024L);

        await _sut.ConfirmUploadAsync(doc.Id, _ownerId);

        await _blob.Received(1).SetBlobTagsAsync(
            doc.BlobStoragePath,
            doc.BlobContainerName,
            Arg.Is<IDictionary<string, string>>(d => d.ContainsKey("Status") && d["Status"] == "Confirmed"));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GenerateDownloadUrlAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateDownloadUrlAsync_ValidRequest_ReturnsReadSasUrl()
    {
        const string expectedUrl = "https://blob.example/contract.pdf?sas=read";
        var doc = SeedPendingDocument();
        doc.Status = DocumentStatus.Active;
        await _context.SaveChangesAsync();

        _blob.GetSasUri(doc.BlobStoragePath, doc.BlobContainerName, StorageAccessPermissions.Read, 10)
             .Returns(expectedUrl);

        var result = await _sut.GenerateDownloadUrlAsync(doc.Id, _ownerId);
        result.Should().Be(expectedUrl);
    }

    [Fact]
    public async Task GenerateDownloadUrlAsync_WhenDocumentBelongsToDifferentCompany_ThrowsUnauthorized()
    {
        // Create another company and user
        var otherCompany = new Company
        {
            Name = "Other Firm", Email = "other2@test.com", Phone = "", Address = "", City = "",
            State = "", Country = "", PostalCode = "", SubscriptionTier = SubscriptionTier.Trial,
            SubscriptionStartDate = DateTime.UtcNow, IsActive = true,
            StorageUsedBytes = 0, StorageQuotaBytes = 1024 * 1024, CreatedBy = "seed"
        };
        _context.Companies.Add(otherCompany);
        var otherUser = new User
        {
            CompanyId = otherCompany.Id, FirstName = "Eve", LastName = "Other",
            Email = "eve@other.com", PasswordHash = "hash", Phone = "",
            Role = UserRole.User, IsActive = true, IsEmailVerified = true, CreatedBy = "seed"
        };
        _context.Users.Add(otherUser);
        await _context.SaveChangesAsync();

        var doc = SeedPendingDocument();

        var act = async () => await _sut.GenerateDownloadUrlAsync(doc.Id, otherUser.Id);
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*not allowed*");
    }

    [Fact]
    public async Task GenerateDownloadUrlAsync_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        var doc = SeedPendingDocument();
        var act = async () => await _sut.GenerateDownloadUrlAsync(doc.Id, 99999);
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*User not found*");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetProjectDocumentsAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetProjectDocumentsAsync_ReturnsOnlyActiveDocuments()
    {
        var active = SeedPendingDocument();
        active.Status = DocumentStatus.Active;
        var pending = SeedPendingDocument();
        pending.Status = DocumentStatus.Pending;
        await _context.SaveChangesAsync();

        var results = await _sut.GetProjectDocumentsAsync(_projectId, _ownerId);

        results.Should().HaveCount(1);
        results.First().Id.Should().Be(active.Id);
    }

    [Fact]
    public async Task GetProjectDocumentsAsync_EmptyProject_ReturnsEmptyList()
    {
        var results = await _sut.GetProjectDocumentsAsync(_projectId, _ownerId);
        results.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DeleteDocumentAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteDocumentAsync_Owner_DeletesBlobAndRemovesDocument()
    {
        var doc = SeedPendingDocument(sizeBytes: 500);
        doc.Status = DocumentStatus.Active;

        var company = await _context.Companies.FindAsync(_companyId);
        company!.StorageUsedBytes = 500;
        await _context.SaveChangesAsync();

        await _sut.DeleteDocumentAsync(doc.Id, _ownerId);

        await _blob.Received(1).DeleteAsync(doc.BlobStoragePath, doc.BlobContainerName);

        var removed = await _context.Documents.FindAsync(doc.Id);
        removed.Should().BeNull();

        var updatedCompany = await _context.Companies.FindAsync(_companyId);
        updatedCompany!.StorageUsedBytes.Should().Be(0);
    }

    [Fact]
    public async Task DeleteDocumentAsync_RegularUserNotUploader_ThrowsUnauthorized()
    {
        var regularUser = new User
        {
            CompanyId = _companyId, FirstName = "Regular", LastName = "User",
            Email = "regular@test.com", PasswordHash = "hash", Phone = "",
            Role = UserRole.User, IsActive = true, IsEmailVerified = true, CreatedBy = "seed"
        };
        _context.Users.Add(regularUser);
        await _context.SaveChangesAsync();

        var doc = SeedPendingDocument();
        doc.Status = DocumentStatus.Active;
        await _context.SaveChangesAsync();

        // doc.UploadedByUserId == _ownerId, but caller is regularUser (different user, Role=User)
        var act = async () => await _sut.DeleteDocumentAsync(doc.Id, regularUser.Id);
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*uploader*");
    }

    [Fact]
    public async Task DeleteDocumentAsync_WhenDocumentNotFound_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.DeleteDocumentAsync(99999, _ownerId);
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Document not found*");
    }

    [Fact]
    public async Task DeleteDocumentAsync_WhenDocumentBelongsToDifferentCompany_ThrowsUnauthorized()
    {
        var otherCompany = new Company
        {
            Name = "Other Firm2", Email = "other3@test.com", Phone = "", Address = "", City = "",
            State = "", Country = "", PostalCode = "", SubscriptionTier = SubscriptionTier.Trial,
            SubscriptionStartDate = DateTime.UtcNow, IsActive = true,
            StorageUsedBytes = 0, StorageQuotaBytes = 1024 * 1024, CreatedBy = "seed"
        };
        _context.Companies.Add(otherCompany);

        var outsider = new User
        {
            CompanyId = otherCompany.Id, FirstName = "Out", LastName = "Sider",
            Email = "out@other.com", PasswordHash = "hash", Phone = "",
            Role = UserRole.CompanyOwner, IsActive = true, IsEmailVerified = true, CreatedBy = "seed"
        };
        _context.Users.Add(outsider);
        await _context.SaveChangesAsync();

        var doc = SeedPendingDocument();

        var act = async () => await _sut.DeleteDocumentAsync(doc.Id, outsider.Id);
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*not allowed*");
    }

    [Fact]
    public async Task DeleteDocumentAsync_StorageUsedBytes_NeverGoesNegative()
    {
        var doc = SeedPendingDocument(sizeBytes: 9999);
        doc.Status = DocumentStatus.Active;
        // StorageUsedBytes is already 0 (below fileSizeBytes)
        await _context.SaveChangesAsync();

        await _sut.DeleteDocumentAsync(doc.Id, _ownerId);

        var company = await _context.Companies.FindAsync(_companyId);
        company!.StorageUsedBytes.Should().Be(0);
    }
}
