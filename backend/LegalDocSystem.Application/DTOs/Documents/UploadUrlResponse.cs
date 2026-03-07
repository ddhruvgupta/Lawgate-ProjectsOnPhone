namespace LegalDocSystem.Application.DTOs.Documents;

public class UploadUrlResponse
{
    public int DocumentId { get; set; }
    public string UploadUrl { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
