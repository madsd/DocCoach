using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DocCoach.Web.Services.Interfaces;

namespace DocCoach.Web.Services.Azure;

/// <summary>
/// Azure Blob Storage implementation of IStorageService.
/// Works with Azurite for local development and Azure Blob Storage in production.
/// </summary>
public class AzureBlobStorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string> StoreAsync(string container, string fileName, Stream content, string contentType)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        await containerClient.CreateIfNotExistsAsync();

        // Generate unique blob name to avoid collisions
        var uniqueName = $"{Guid.NewGuid():N}_{fileName}";
        var blobClient = containerClient.GetBlobClient(uniqueName);

        // Reset stream position if possible
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            }
        };

        await blobClient.UploadAsync(content, options);

        var storagePath = $"{container}/{uniqueName}";
        _logger.LogInformation("Stored blob: {StoragePath}, ContentType: {ContentType}", storagePath, contentType);

        return storagePath;
    }

    public async Task<Stream> RetrieveAsync(string storagePath)
    {
        var (containerName, blobName) = ParseStoragePath(storagePath);
        
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var response = await blobClient.DownloadStreamingAsync();
        
        // Copy to MemoryStream so caller can seek
        var memoryStream = new MemoryStream();
        await response.Value.Content.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        
        _logger.LogDebug("Retrieved blob: {StoragePath}", storagePath);
        
        return memoryStream;
    }

    public async Task DeleteAsync(string storagePath)
    {
        var (containerName, blobName) = ParseStoragePath(storagePath);
        
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync();
        
        _logger.LogInformation("Deleted blob: {StoragePath}", storagePath);
    }

    public async Task<bool> ExistsAsync(string storagePath)
    {
        var (containerName, blobName) = ParseStoragePath(storagePath);
        
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        return await blobClient.ExistsAsync();
    }

    public async Task<StorageFileInfo?> GetInfoAsync(string storagePath)
    {
        var (containerName, blobName) = ParseStoragePath(storagePath);
        
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
        {
            return null;
        }

        var properties = await blobClient.GetPropertiesAsync();
        
        // Extract original filename from the unique blob name (format: guid_filename)
        var originalFileName = blobName;
        var underscoreIndex = blobName.IndexOf('_');
        if (underscoreIndex > 0 && underscoreIndex < blobName.Length - 1)
        {
            originalFileName = blobName[(underscoreIndex + 1)..];
        }

        return new StorageFileInfo(
            StoragePath: storagePath,
            FileName: originalFileName,
            ContentType: properties.Value.ContentType,
            Size: properties.Value.ContentLength,
            LastModified: properties.Value.LastModified
        );
    }

    private static (string containerName, string blobName) ParseStoragePath(string storagePath)
    {
        var parts = storagePath.Split('/', 2);
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid storage path format: {storagePath}. Expected 'container/blobname'");
        }
        return (parts[0], parts[1]);
    }
}
