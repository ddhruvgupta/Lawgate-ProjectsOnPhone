using LegalDocSystem.Application.DTOs.Documents;
using LegalDocSystem.Application.Interfaces;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Domain.Enums;
using LegalDocSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Azure.Storage.Sas;

namespace LegalDocSystem.Infrastructure.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(
            ApplicationDbContext context,
            IBlobStorageService blobStorageService,
            ILogger<DocumentService> logger)
        {
            _context = context;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        public async Task<UploadUrlResponse> GenerateUploadUrlAsync(int userId, UploadDocumentDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found");

            var project = await _context.Projects.FindAsync(dto.ProjectId);
            if (project == null) throw new KeyNotFoundException("Project not found");

            // Verify user/project company (Simple check for now)
            if (project.CompanyId != user.CompanyId) throw new UnauthorizedAccessException("Cannot upload to this project");

            // Check Company Quota
            var company = await _context.Companies.FindAsync(user.CompanyId);
            if (company == null) throw new KeyNotFoundException("Company not found");

            if (company.StorageUsedBytes + dto.FileSizeBytes > company.StorageQuotaBytes)
            {
                throw new InvalidOperationException("Company storage quota exceeded");
            }

            string containerName = $"company-{user.CompanyId}";
            string blobName = $"{dto.ProjectId}/{Guid.NewGuid()}_{dto.FileName}";

            // Generate Write SAS URL
            string uploadUrl = _blobStorageService.GetSasUri(blobName, containerName, BlobSasPermissions.Create | BlobSasPermissions.Write, 15);

            // Create Pending Document Entity
            var document = new Document
            {
                ProjectId = dto.ProjectId,
                UploadedByUserId = userId,
                FileName = dto.FileName,
                FileExtension = Path.GetExtension(dto.FileName).ToLowerInvariant(),
                FileSizeBytes = dto.FileSizeBytes, // Expected size
                DocumentType = dto.DocumentType,
                Description = dto.Description,
                Tags = dto.Tags,
                BlobContainerName = containerName,
                BlobStoragePath = blobName,
                Status = DocumentStatus.Pending,
                Version = 1,
                IsLatestVersion = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Email
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return new UploadUrlResponse
            {
                DocumentId = document.Id,
                UploadUrl = uploadUrl,
                BlobName = blobName,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };
        }

        public async Task<DocumentDto> ConfirmUploadAsync(int documentId, int userId)
        {
            var document = await _context.Documents
                .Include(d => d.UploadedBy)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null) throw new KeyNotFoundException("Document not found");
            if (document.UploadedByUserId != userId) throw new UnauthorizedAccessException();

            // Verification Layer 3: Check actual blob size in Azure
            long actualSize = await _blobStorageService.GetBlobSizeAsync(document.BlobStoragePath, document.BlobContainerName);
            
            if (actualSize == 0) throw new InvalidOperationException("File not found in storage. Upload may have failed.");
            
            // Check if user cheated on size
            if (actualSize > document.FileSizeBytes)
            {
                // Delete the malicious blob
                await _blobStorageService.DeleteAsync(document.BlobStoragePath, document.BlobContainerName);
                document.Status = DocumentStatus.Failed;
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Uploaded file exceeds the requested size limit. File deleted.");
            }

            // Update status and actual size (if slightly smaller, we accept it)
            document.Status = DocumentStatus.Active;
            document.FileSizeBytes = actualSize;

            // Set Blob Index Tag for Lifecycle Management
            await _blobStorageService.SetBlobTagsAsync(document.BlobStoragePath, document.BlobContainerName, new Dictionary<string, string>
            {
                { "Status", "Confirmed" }
            });
            
            // Update company storage usage
            var company = await _context.Companies.FindAsync(document.UploadedBy.CompanyId);
            if (company != null)
            {
                company.StorageUsedBytes += actualSize;
            }

            await _context.SaveChangesAsync();

            return MapToDto(document, document.UploadedBy.FirstName + " " + document.UploadedBy.LastName);
        }

        public async Task<string> GenerateDownloadUrlAsync(int documentId, int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found");

            var document = await _context.Documents
                .Include(d => d.Project)
                .FirstOrDefaultAsync(d => d.Id == documentId);
            if (document == null) throw new KeyNotFoundException("Document not found");

            if (document.Project.CompanyId != user.CompanyId)
                throw new UnauthorizedAccessException("Access to this document is not allowed");

            return _blobStorageService.GetSasUri(document.BlobStoragePath, document.BlobContainerName, BlobSasPermissions.Read, 10);
        }

        public async Task<DocumentDto> GetDocumentAsync(int documentId, int userId)
        {
             var document = await _context.Documents
                .Include(d => d.UploadedBy)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null) throw new KeyNotFoundException("Document not found");
            
            return MapToDto(document, document.UploadedBy.FirstName + " " + document.UploadedBy.LastName);
        }

        public async Task<IEnumerable<DocumentDto>> GetProjectDocumentsAsync(int projectId, int userId)
        {
            return await _context.Documents
                .Where(d => d.ProjectId == projectId && d.Status == DocumentStatus.Active)
                .Include(d => d.UploadedBy)
                .Select(d => MapToDto(d, d.UploadedBy.FirstName + " " + d.UploadedBy.LastName))
                .ToListAsync();
        }

        public async Task DeleteDocumentAsync(int documentId, int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found");

            var document = await _context.Documents
                .Include(d => d.UploadedBy)
                .Include(d => d.Project)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null) throw new KeyNotFoundException("Document not found");

            if (document.Project.CompanyId != user.CompanyId)
                throw new UnauthorizedAccessException("Access to this document is not allowed");

            // Only the uploader or admins/owners can delete
            var isAdminOrOwner = user.Role == Domain.Enums.UserRole.CompanyOwner || user.Role == Domain.Enums.UserRole.Admin;
            if (document.UploadedByUserId != userId && !isAdminOrOwner)
                throw new UnauthorizedAccessException("Only the uploader or an admin can delete this document");

            await _blobStorageService.DeleteAsync(document.BlobStoragePath, document.BlobContainerName);

            // Update quota
            var company = await _context.Companies.FindAsync(document.UploadedBy.CompanyId);
            if (company != null)
            {
                company.StorageUsedBytes = Math.Max(0, company.StorageUsedBytes - document.FileSizeBytes);
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
        }

        private static DocumentDto MapToDto(Document doc, string uploaderName)
        {
            return new DocumentDto
            {
                Id = doc.Id,
                ProjectId = doc.ProjectId,
                FileName = doc.FileName,
                FileExtension = doc.FileExtension,
                FileSizeBytes = doc.FileSizeBytes,
                Description = doc.Description,
                DocumentType = doc.DocumentType.ToString(),
                Version = doc.Version,
                IsLatestVersion = doc.IsLatestVersion,
                UploadedBy = uploaderName,
                CreatedAt = doc.CreatedAt
            };
        }
    }
}
