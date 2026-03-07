using LegalDocSystem.Domain.Enums;

namespace LegalDocSystem.Application.DTOs.Documents;

public class DocumentDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Description { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsLatestVersion { get; set; }
    public string UploadedBy { get; set; } = string.Empty; // User Name
    public DateTime CreatedAt { get; set; }
    public string? DownloadUrl { get; set; } // Generated SAS URL if needed
}
