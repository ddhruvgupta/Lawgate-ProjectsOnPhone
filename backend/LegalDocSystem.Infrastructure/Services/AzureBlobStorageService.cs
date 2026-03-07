using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using LegalDocSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LegalDocSystem.Infrastructure.Services
{
    public class AzureBlobStorageService : IBlobStorageService
    {
        private readonly string _connectionString;

        public AzureBlobStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureStorage") 
                ?? throw new ArgumentNullException("AzureStorage connection string is missing");
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

        public string GetSasUri(string fileName, string containerName, BlobSasPermissions permissions, int expirationMinutes = 60)
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

            sasBuilder.SetPermissions(permissions);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
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
