# Contract: IStorageService

**Feature**: 001-audit-review-tool  
**Date**: 2026-01-05

## Purpose

Low-level file storage abstraction. Used by DocumentService and GuidelineService for actual file persistence.

## Interface Definition

```csharp
public interface IStorageService
{
    /// <summary>
    /// Store a file and return its storage path/URI.
    /// </summary>
    /// <param name="container">Logical container name (e.g., "documents", "examples")</param>
    /// <param name="fileName">Desired filename (will be made unique if needed)</param>
    /// <param name="content">File content stream</param>
    /// <param name="contentType">MIME type</param>
    /// <returns>Storage path/URI for retrieval</returns>
    Task<string> StoreAsync(string container, string fileName, Stream content, string contentType);

    /// <summary>
    /// Retrieve a file by its storage path.
    /// </summary>
    /// <param name="storagePath">Path returned from StoreAsync</param>
    /// <returns>File content stream</returns>
    Task<Stream> RetrieveAsync(string storagePath);

    /// <summary>
    /// Delete a file by its storage path.
    /// </summary>
    Task DeleteAsync(string storagePath);

    /// <summary>
    /// Check if a file exists.
    /// </summary>
    Task<bool> ExistsAsync(string storagePath);

    /// <summary>
    /// Get file metadata without downloading content.
    /// </summary>
    Task<StorageFileInfo?> GetInfoAsync(string storagePath);
}

public record StorageFileInfo(
    string StoragePath,
    string FileName,
    string ContentType,
    long Size,
    DateTimeOffset LastModified
);
```

## Behaviors

### StoreAsync

- Creates container if it doesn't exist
- Generates unique filename if collision detected (appends GUID)
- Returns full path/URI that can be used for retrieval
- Path format depends on implementation:
  - Mock: `{container}/{filename}`
  - Azure: `https://{account}.blob.core.windows.net/{container}/{filename}`

### RetrieveAsync

- Returns seekable stream
- Caller responsible for disposing stream
- Throws `NotFoundException` if file doesn't exist

### Container Names

| Container | Purpose |
|-----------|---------|
| `documents` | Uploaded audit reports |
| `examples` | Example compliant documents |
| `guidelines` | Uploaded guideline source documents |

## Mock Implementation

```csharp
public class MockStorageService : IStorageService
{
    private readonly Dictionary<string, StoredFile> _files = new();
    private readonly string _basePath;

    public MockStorageService(string basePath = "data/files")
    {
        _basePath = basePath;
        Directory.CreateDirectory(basePath);
    }

    public async Task<string> StoreAsync(string container, string fileName, Stream content, string contentType)
    {
        var path = $"{container}/{Guid.NewGuid():N}_{fileName}";
        var fullPath = Path.Combine(_basePath, path);
        
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        
        using var file = File.Create(fullPath);
        await content.CopyToAsync(file);
        
        return path;
    }

    public Task<Stream> RetrieveAsync(string storagePath)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (!File.Exists(fullPath))
            throw new NotFoundException($"File not found: {storagePath}");
        
        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }
    
    // ... other methods
}
```

## Azure Implementation Notes

### Azure Blob Storage

```csharp
public class AzureBlobStorageService : IStorageService
{
    private readonly BlobServiceClient _client;

    public AzureBlobStorageService(string connectionString)
    {
        _client = new BlobServiceClient(connectionString);
    }

    public async Task<string> StoreAsync(string container, string fileName, Stream content, string contentType)
    {
        var containerClient = _client.GetBlobContainerClient(container);
        await containerClient.CreateIfNotExistsAsync();

        var blobName = $"{Guid.NewGuid():N}_{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });

        return blobClient.Uri.ToString();
    }

    // ... other methods using BlobClient
}
```

### Connection String Patterns

- **Azurite (local)**: `UseDevelopmentStorage=true`
- **Azure Storage**: `DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...`
- **Managed Identity**: Use `DefaultAzureCredential` with account URL

## Error Cases

| Scenario | Exception |
|----------|-----------|
| File not found | `NotFoundException` |
| Storage unavailable | `StorageException` |
| Quota exceeded | `StorageException` |
| Invalid container name | `ValidationException` |
| Stream read error | `IOException` |

## Configuration

```json
{
  "Storage": {
    "Provider": "Mock",  // or "AzureBlob"
    "Mock": {
      "BasePath": "data/files"
    },
    "AzureBlob": {
      "ConnectionString": "UseDevelopmentStorage=true"
    }
  }
}
```
