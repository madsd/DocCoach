namespace DocCoach.Web.Services.TextExtraction;

/// <summary>
/// Interface for extracting text content from documents.
/// </summary>
public interface ITextExtractor
{
    /// <summary>
    /// Gets the file extensions this extractor supports.
    /// </summary>
    IEnumerable<string> SupportedExtensions { get; }
    
    /// <summary>
    /// Determines if this extractor can handle the given file extension.
    /// </summary>
    /// <param name="extension">File extension including the dot (e.g., ".pdf")</param>
    bool CanExtract(string extension);
    
    /// <summary>
    /// Extracts text content from a document stream.
    /// </summary>
    /// <param name="stream">Document content stream</param>
    /// <param name="fileName">Original filename for logging/context</param>
    /// <returns>Extracted text content</returns>
    Task<string> ExtractTextAsync(Stream stream, string fileName);
}
