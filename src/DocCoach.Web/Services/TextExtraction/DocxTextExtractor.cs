using System.Text;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocCoach.Web.Services.TextExtraction;

/// <summary>
/// Extracts text content from DOCX documents using Open XML SDK.
/// </summary>
public class DocxTextExtractor : ITextExtractor
{
    public IEnumerable<string> SupportedExtensions => [".docx"];

    public bool CanExtract(string extension)
    {
        return extension.Equals(".docx", StringComparison.OrdinalIgnoreCase);
    }

    public Task<string> ExtractTextAsync(Stream stream, string fileName)
    {
        var extractedText = ExtractText(stream);
        return Task.FromResult(extractedText);
    }

    private static string ExtractText(Stream stream)
    {
        var textBuilder = new StringBuilder();

        // OpenXml requires a seekable stream, so copy to MemoryStream if needed
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

            using var document = WordprocessingDocument.Open(workingStream, false);
            
            var body = document.MainDocumentPart?.Document?.Body;
            if (body == null)
            {
                return string.Empty;
            }

            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                var paragraphText = GetParagraphText(paragraph);
                if (!string.IsNullOrWhiteSpace(paragraphText))
                {
                    textBuilder.AppendLine(paragraphText);
                }
            }

            // Also extract text from tables
            foreach (var table in body.Descendants<Table>())
            {
                foreach (var row in table.Descendants<TableRow>())
                {
                    var rowTexts = new List<string>();
                    foreach (var cell in row.Descendants<TableCell>())
                    {
                        var cellText = GetCellText(cell);
                        if (!string.IsNullOrWhiteSpace(cellText))
                        {
                            rowTexts.Add(cellText.Trim());
                        }
                    }
                    if (rowTexts.Count > 0)
                    {
                        textBuilder.AppendLine(string.Join(" | ", rowTexts));
                    }
                }
                textBuilder.AppendLine(); // Add spacing after tables
            }
        }
        finally
        {
            memoryStream?.Dispose();
        }

        return textBuilder.ToString().Trim();
    }

    private static string GetParagraphText(Paragraph paragraph)
    {
        var textBuilder = new StringBuilder();
        
        foreach (var run in paragraph.Descendants<Run>())
        {
            foreach (var text in run.Descendants<Text>())
            {
                textBuilder.Append(text.Text);
            }
        }

        return textBuilder.ToString();
    }

    private static string GetCellText(TableCell cell)
    {
        var textBuilder = new StringBuilder();
        
        foreach (var paragraph in cell.Descendants<Paragraph>())
        {
            var paragraphText = GetParagraphText(paragraph);
            if (!string.IsNullOrWhiteSpace(paragraphText))
            {
                if (textBuilder.Length > 0)
                {
                    textBuilder.Append(' ');
                }
                textBuilder.Append(paragraphText);
            }
        }

        return textBuilder.ToString();
    }
}
