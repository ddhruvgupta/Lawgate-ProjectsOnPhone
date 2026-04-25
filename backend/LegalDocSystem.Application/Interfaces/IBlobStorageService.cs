using System.IO;
using System.Threading.Tasks;
using LegalDocSystem.Domain.Enums;

namespace LegalDocSystem.Application.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadAsync(Stream content, string fileName, string containerName);
        Task<Stream> DownloadAsync(string fileName, string containerName);
        Task DeleteAsync(string fileName, string containerName);
        string GetSasUri(string fileName, string containerName, StorageAccessPermissions permissions, int expirationMinutes = 60);
        Task<long> GetBlobSizeAsync(string fileName, string containerName);
        Task SetBlobTagsAsync(string fileName, string containerName, IDictionary<string, string> tags);
    }
}
