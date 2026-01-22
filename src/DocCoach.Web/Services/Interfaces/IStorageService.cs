namespace DocCoach.Web.Services.Interfaces;

/// <summary>
/// File metadata returned from storage operations.
/// </summary>
public record StorageFileInfo(
    string StoragePath,
    string FileName,
    string ContentType,
    long Size,
    DateTimeOffset LastModified
);

/// <summary>
/// Low-level file storage abstraction for documents and examples.
/// </summary>
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
