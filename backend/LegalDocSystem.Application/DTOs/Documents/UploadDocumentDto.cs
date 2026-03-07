using System.ComponentModel.DataAnnotations;
using LegalDocSystem.Domain.Enums;

namespace LegalDocSystem.Application.DTOs.Documents;

public class UploadDocumentDto
{
    [Required]
    public int ProjectId { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DocumentType DocumentType { get; set; } = DocumentType.Other;

    [Required]
    public long FileSizeBytes { get; set; }

    [Required]
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    public string? Tags { get; set; } // JSON array or comma separated
}
