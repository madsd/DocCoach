using System.Text;
using UglyToad.PdfPig;

namespace DocCoach.Web.Services.TextExtraction;

/// <summary>
/// Extracts text content from PDF documents using PdfPig.
/// </summary>
public class PdfTextExtractor : ITextExtractor
{
    public IEnumerable<string> SupportedExtensions => [".pdf"];

    public bool CanExtract(string extension)
    {
        return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public Task<string> ExtractTextAsync(Stream stream, string fileName)
    {
        var extractedText = ExtractText(stream);
        return Task.FromResult(extractedText);
    }

    private static string ExtractText(Stream stream)
    {
        var textBuilder = new StringBuilder();

        // PdfPig requires a seekable stream, so copy to MemoryStream if needed
        Stream workingStream = stream;
        MemoryStream? memoryStream = null;
        
        try
        {
            if (!stream.CanSeek)
            {
                memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                workingStream = memoryStream;
            }

            using var document = PdfDocument.Open(workingStream);
            
            foreach (var page in document.GetPages())
            {
                var pageText = page.Text;
                
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    textBuilder.AppendLine(pageText);
                    textBuilder.AppendLine(); // Add spacing between pages
                }
            }
        }
        finally
        {
            memoryStream?.Dispose();
        }

        return textBuilder.ToString().Trim();
    }
}
