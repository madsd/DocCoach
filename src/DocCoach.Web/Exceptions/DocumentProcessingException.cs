namespace DocCoach.Web.Exceptions;

/// <summary>
/// Thrown when document processing (text extraction) fails.
/// </summary>
public class DocumentProcessingException : Exception
{
    public string DocumentId { get; }
    public string ProcessingStage { get; }

    public DocumentProcessingException(string documentId, string processingStage, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        DocumentId = documentId;
        ProcessingStage = processingStage;
    }

    public DocumentProcessingException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
        DocumentId = string.Empty;
        ProcessingStage = "Unknown";
    }
}
