using LegalDocSystem.Domain.Common;
using LegalDocSystem.Domain.Enums;

namespace LegalDocSystem.Domain.Entities;

/// <summary>
/// Represents a legal document with versioning support
/// </summary>
public class Document : BaseEntity
{
    public int ProjectId { get; set; }
    public int UploadedByUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DocumentType DocumentType { get; set; } = DocumentType.Other;
    public string? Description { get; set; }
    public string? Tags { get; set; } // JSON array of tags
    
    // Azure Blob Storage
    public string BlobStoragePath { get; set; } = string.Empty;
    public string BlobContainerName { get; set; } = string.Empty;
    
    // Versioning
    public int Version { get; set; } = 1;
    public int? ParentDocumentId { get; set; } // For versioning
    public bool IsLatestVersion { get; set; } = true;
    
    // Metadata
    public string? ContentHash { get; set; } // For integrity verification
    
    // Navigation properties
    public Project Project { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
    public Document? ParentDocument { get; set; }
    public ICollection<Document> Versions { get; set; } = new List<Document>();
}
