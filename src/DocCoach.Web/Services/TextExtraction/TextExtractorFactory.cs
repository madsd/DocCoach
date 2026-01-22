using DocCoach.Web.Exceptions;

namespace DocCoach.Web.Services.TextExtraction;

/// <summary>
/// Composite text extractor that delegates to the appropriate extractor based on file type.
/// </summary>
public class TextExtractorFactory : ITextExtractor
{
    private readonly IEnumerable<ITextExtractor> _extractors;
    
    public TextExtractorFactory(IEnumerable<ITextExtractor> extractors)
    {
        _extractors = extractors;
    }
    
    public IEnumerable<string> SupportedExtensions => 
        _extractors.SelectMany(e => e.SupportedExtensions).Distinct();

    public bool CanExtract(string extension)
    {
        return _extractors.Any(e => e.CanExtract(extension));
    }

    public Task<string> ExtractTextAsync(Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var extractor = _extractors.FirstOrDefault(e => e.CanExtract(extension));
        
        if (extractor == null)
        {
            throw new DocumentProcessingException(
                fileName, 
                "ExtractorSelection",
                $"No text extractor available for file type '{extension}'. " +
                $"Supported types: {string.Join(", ", SupportedExtensions)}");
        }
        
        return extractor.ExtractTextAsync(stream, fileName);
    }
}
