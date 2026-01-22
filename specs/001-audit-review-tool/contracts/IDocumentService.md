# Contract: IDocumentService

**Feature**: 001-audit-review-tool  
**Date**: 2026-01-05

## Purpose

Handles document upload, storage, and text extraction operations.

## Interface Definition

```csharp
public interface IDocumentService
{
    /// <summary>
    /// Upload a new document for review.
    /// </summary>
    /// <param name="fileName">Original filename</param>
    /// <param name="contentType">MIME type (application/pdf or docx)</param>
    /// <param name="content">File content stream</param>
    /// <param name="guidelineSetId">Selected guideline set for review</param>
    /// <returns>Created document with Id and initial status</returns>
    Task<Document> UploadAsync(string fileName, string contentType, Stream content, string guidelineSetId);

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
    /// Get download URL or stream for original document.
    /// </summary>
    Task<Stream> GetFileStreamAsync(string documentId);
}
```

## Behaviors

### UploadAsync

- Validates file size (max 50MB)
- Validates content type (PDF or DOCX only)
- Stores file to configured storage (blob or local)
- Creates Document record with `Status = Uploaded`
- Returns immediately (does not wait for extraction)

### ExtractTextAsync

- Retrieves file from storage
- Uses appropriate extractor based on content type:
  - PDF: PdfPig library
  - DOCX: DocumentFormat.OpenXml
- Updates `Document.Status` to `Extracting` then `Completed` (or `Failed`)
- Caches extracted text in `Document.ExtractedText`
- Throws `DocumentProcessingException` on failure

### Error Cases

| Scenario | Exception | Status |
|----------|-----------|--------|
| File too large (>50MB) | `ValidationException` | N/A (rejected before create) |
| Invalid content type | `ValidationException` | N/A |
| Document not found | `NotFoundException` | N/A |
| Extraction failure | `DocumentProcessingException` | `Failed` |
| Storage unavailable | `StorageException` | `Failed` |

## Mock Implementation Notes

- Store files in memory dictionary
- Use sample extracted text from pre-defined content
- Simulate 500ms delay for upload, 1s for extraction
- Return consistent IDs for demo reproducibility
