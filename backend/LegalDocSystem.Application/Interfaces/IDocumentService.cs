using LegalDocSystem.Application.DTOs.Documents;
using System.IO;
using System.Threading.Tasks;

namespace LegalDocSystem.Application.Interfaces
{
    public interface IDocumentService
    {
        Task<UploadUrlResponse> GenerateUploadUrlAsync(int userId, UploadDocumentDto dto);
        Task<DocumentDto> ConfirmUploadAsync(int documentId, int userId);
        Task<string> GenerateDownloadUrlAsync(int documentId, int userId);
        Task<DocumentDto> GetDocumentAsync(int documentId, int userId);
        Task<IEnumerable<DocumentDto>> GetProjectDocumentsAsync(int projectId, int userId);
        Task DeleteDocumentAsync(int documentId, int userId);
    }
}
