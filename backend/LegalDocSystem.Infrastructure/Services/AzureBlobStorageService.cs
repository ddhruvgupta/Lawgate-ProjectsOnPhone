using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using LegalDocSystem.Application.Interfaces;
using LegalDocSystem.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace LegalDocSystem.Infrastructure.Services
{
    public class AzureBlobStorageService : IBlobStorageService
    {
        private readonly string _connectionString;
        // Optional: rewrite internal blob hostname with a public-facing one so browsers can
        // reach Azurite (or a private endpoint) directly. Set Storage:PublicBlobEndpoint in
        // docker-compose / appsettings for dev; leave empty in production.
        private readonly string? _publicBlobEndpoint;

        public AzureBlobStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BlobStorage")
                ?? throw new ArgumentNullException("BlobStorage connection string is missing");
            _publicBlobEndpoint = configuration["Storage:PublicBlobEndpoint"];
        }

        public async Task<string> UploadAsync(Stream content, string fileName, string containerName)
        {
            var containerClient = new BlobContainerClient(_connectionString, containerName);
            await containerClient.CreateIfNotExistsAsync();
            
            // Set access policy to private by default
            await containerClient.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.None);

            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(content, overwrite: true);

            return blobClient.Uri.ToString();
        }

        public async Task<Stream> DownloadAsync(string fileName, string containerName)
        {
            var containerClient = new BlobContainerClient(_connectionString, containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"Blob {fileName} not found in container {containerName}");
            }

            var downloadResult = await blobClient.DownloadAsync();
            return downloadResult.Value.Content;
        }

        public async Task DeleteAsync(string fileName, string containerName)
        {
            var containerClient = new BlobContainerClient(_connectionString, containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task EnsureContainerExistsAsync(string containerName)
        {
            var containerClient = new BlobContainerClient(_connectionString, containerName);
            await containerClient.CreateIfNotExistsAsync();
        }

        public string GetSasUri(string fileName, string containerName, StorageAccessPermissions permissions, int expirationMinutes = 60)
        {
            var containerClient = new BlobContainerClient(_connectionString, containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!blobClient.CanGenerateSasUri)
            {
                return string.Empty;
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = fileName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
            };

            sasBuilder.SetPermissions(MapPermissions(permissions));

            var sasUri = blobClient.GenerateSasUri(sasBuilder).ToString();

            // Rewrite the internal endpoint (e.g. http://azurite:10000) with the public-facing
            // one so the browser can reach it. No-op when Storage:PublicBlobEndpoint is not set.
            if (!string.IsNullOrEmpty(_publicBlobEndpoint))
            {
                var internalOrigin = blobClient.Uri.GetLeftPart(UriPartial.Authority);
                sasUri = sasUri.Replace(internalOrigin, _publicBlobEndpoint.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
            }

            return sasUri;
        }

        private static BlobSasPermissions MapPermissions(StorageAccessPermissions permissions)
        {
            BlobSasPermissions result = 0;
            if (permissions.HasFlag(StorageAccessPermissions.Read))   result |= BlobSasPermissions.Read;
            if (permissions.HasFlag(StorageAccessPermissions.Write))  result |= BlobSasPermissions.Write;
            if (permissions.HasFlag(StorageAccessPermissions.Create)) result |= BlobSasPermissions.Create;
            if (permissions.HasFlag(StorageAccessPermissions.Delete)) result |= BlobSasPermissions.Delete;
            return result;
        }

        public async Task<long> GetBlobSizeAsync(string fileName, string containerName)
        {
            var containerClient = new BlobContainerClient(_connectionString, containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
            {
                return 0;
            }

            var properties = await blobClient.GetPropertiesAsync();
            return properties.Value.ContentLength;
        }

        public async Task SetBlobTagsAsync(string fileName, string containerName, IDictionary<string, string> tags)
        {
            var containerClient = new BlobContainerClient(_connectionString, containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.SetTagsAsync(tags);
        }
    }
}
