using LegalDocSystem.Application.DTOs.Documents;
using LegalDocSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LegalDocSystem.API.Controllers;

/// <summary>Upload, download, and manage legal documents stored in Azure Blob Storage.</summary>
[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IAuditService _auditService;

    public DocumentController(IDocumentService documentService, IAuditService auditService)
    {
        _documentService = documentService;
        _auditService = auditService;
    }

    /// <summary>Generates a pre-signed SAS URL for direct client upload to Azure Blob Storage.</summary>
    [HttpPost("upload-url")]
    public async Task<ActionResult<UploadUrlResponse>> GenerateUploadUrl(UploadDocumentDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var response = await _documentService.GenerateUploadUrlAsync(userId, dto);
        return Ok(response);
    }

    /// <summary>Confirms that a previously generated upload has completed successfully.</summary>
    [HttpPost("{id}/confirm")]
    public async Task<ActionResult<DocumentDto>> ConfirmUpload(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var companyIdClaim = User.FindFirst("CompanyId");
        int.TryParse(companyIdClaim?.Value, out int companyId);

        var document = await _documentService.ConfirmUploadAsync(id, userId);

        await _auditService.LogAsync(
            companyId, userId,
            action: "Document.Uploaded",
            entityType: "Document",
            entityId: document.Id,
            description: $"Uploaded document '{document.FileName}' to project #{document.ProjectId}");

        return Ok(document);
    }

    /// <summary>Returns a short-lived SAS download URL for the specified document.</summary>
    [HttpGet("{id}/download-url")]
    public async Task<ActionResult<object>> GetDownloadUrl(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var url = await _documentService.GenerateDownloadUrlAsync(id, userId);
        return Ok(new { downloadUrl = url });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentDto>> GetDocument(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var document = await _documentService.GetDocumentAsync(id, userId);
        return Ok(document);
    }

    [HttpGet("project/{projectId}")]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetProjectDocuments(int projectId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var documents = await _documentService.GetProjectDocumentsAsync(projectId, userId);
        return Ok(documents);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "CompanyOwner,Admin")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var companyIdClaim = User.FindFirst("CompanyId");
        int.TryParse(companyIdClaim?.Value, out int companyId);

        // Fetch metadata before deletion for the audit log
        var document = await _documentService.GetDocumentAsync(id, userId);
        var fileName = document.FileName;
        var projectId = document.ProjectId;

        await _documentService.DeleteDocumentAsync(id, userId);

        await _auditService.LogAsync(
            companyId, userId,
            action: "Document.Deleted",
            entityType: "Document",
            entityId: id,
            description: $"Deleted document '{fileName}' from project #{projectId}");

        return NoContent();
    }
}
