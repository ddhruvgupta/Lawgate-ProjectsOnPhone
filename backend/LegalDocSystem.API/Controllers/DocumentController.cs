using LegalDocSystem.Application.DTOs.Documents;
using LegalDocSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LegalDocSystem.API.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost("upload-url")]
    public async Task<ActionResult<UploadUrlResponse>> GenerateUploadUrl(UploadDocumentDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var response = await _documentService.GenerateUploadUrlAsync(userId, dto);
        return Ok(response);
    }

    [HttpPost("{id}/confirm")]
    public async Task<ActionResult<DocumentDto>> ConfirmUpload(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var document = await _documentService.ConfirmUploadAsync(id, userId);
        return Ok(document);
    }

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
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        await _documentService.DeleteDocumentAsync(id, userId);
        return NoContent();
    }
}
