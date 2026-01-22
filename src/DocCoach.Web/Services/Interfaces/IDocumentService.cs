using DocCoach.Web.Models;

namespace DocCoach.Web.Services.Interfaces;

/// <summary>
/// Handles document upload, storage, and text extraction operations.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Upload a new document for review.
    /// </summary>
    /// <param name="fileName">Original filename</param>
    /// <param name="contentType">MIME type (application/pdf or docx)</param>
    /// <param name="content">File content stream</param>
    /// <param name="guidelineSetId">Selected guideline set for review</param>
    /// <param name="displayName">Optional display name for the review (defaults to fileName if not specified)</param>
    /// <returns>Created document with Id and initial status</returns>
    Task<Document> UploadAsync(string fileName, string contentType, Stream content, string guidelineSetId, string? displayName = null);

    /// <summary>
    /// Get document by ID.
    /// </summary>
    Task<Document?> GetByIdAsync(string id);

    /// <summary>
    /// Get all documents (for history view).
    /// </summary>
    /// <param name="limit">Max results to return</param>
    Task<IReadOnlyList<Document>> GetAllAsync(int limit = 50);

    /// <summary>
    /// Extract text content from a document.
    /// Updates document status and ExtractedText field.
    /// </summary>
    /// <param name="documentId">Document to process</param>
    /// <returns>Extracted text content</returns>
    Task<string> ExtractTextAsync(string documentId);

    /// <summary>
    /// Delete a document and its stored file.
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// Get download stream for original document.
    /// </summary>
    Task<Stream> GetFileStreamAsync(string documentId);
}
