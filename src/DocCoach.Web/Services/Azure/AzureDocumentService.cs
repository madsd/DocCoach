using Azure.Data.Tables;
using DocCoach.Web.Exceptions;
using DocCoach.Web.Models;
using DocCoach.Web.Services.Azure.TableEntities;
using DocCoach.Web.Services.Interfaces;
using DocCoach.Web.Services.TextExtraction;

namespace DocCoach.Web.Services.Azure;

/// <summary>
/// Document service that uses Azure Blob Storage for files and Azure Table Storage for metadata.
/// </summary>
public class AzureDocumentService : IDocumentService
{
    private readonly IStorageService _storageService;
    private readonly TableClient _tableClient;
    private readonly TextExtractorFactory _textExtractorFactory;
    private readonly ILogger<AzureDocumentService> _logger;
    private const string TableName = "documents";

    public AzureDocumentService(
        IStorageService storageService,
        TableServiceClient tableServiceClient,
        TextExtractorFactory textExtractorFactory,
        ILogger<AzureDocumentService> logger)
    {
        _storageService = storageService;
        _tableClient = tableServiceClient.GetTableClient(TableName);
        _textExtractorFactory = textExtractorFactory;
        _logger = logger;
        
        // Ensure table exists
        _tableClient.CreateIfNotExists();
    }

    public async Task<Document> UploadAsync(string fileName, string contentType, Stream content, string guidelineSetId, string? displayName = null)
    {
        _logger.LogInformation("Uploading document: {FileName}, ContentType: {ContentType}", fileName, contentType);
        
        // Store the file in blob storage
        var storagePath = await _storageService.StoreAsync("documents", fileName, content, contentType);

        // Get file size
        var fileInfo = await _storageService.GetInfoAsync(storagePath);
        var fileSize = fileInfo?.Size ?? 0;

        var document = new Document
        {
            FileName = fileName,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName,
            ContentType = contentType,
            FileSize = fileSize,
            StoragePath = storagePath,
            GuidelineSetId = guidelineSetId,
            Status = DocumentStatus.Uploaded
        };

        // Validate the document
        if (!document.IsValid(out var errorMessage))
        {
            await _storageService.DeleteAsync(storagePath);
            throw new ValidationException("Document", errorMessage!);
        }

        // Persist metadata to Table Storage
        var entity = DocumentEntity.FromModel(document);
        await _tableClient.UpsertEntityAsync(entity);

        _logger.LogInformation("Document uploaded successfully: {DocumentId}, Path: {StoragePath}", document.Id, storagePath);

        return document;
    }

    public async Task<Document?> GetByIdAsync(string id)
    {
        var entity = await GetEntityByIdAsync(id);
        if (entity == null) return null;

        var document = entity.ToModel();
        
        // Load extracted text from blob storage if available
        if (!string.IsNullOrEmpty(entity.ExtractedTextBlobPath))
        {
            try
            {
                using var stream = await _storageService.RetrieveAsync(entity.ExtractedTextBlobPath);
                using var reader = new StreamReader(stream);
                document.ExtractedText = await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load extracted text for document {DocumentId}", id);
            }
        }
        
        return document;
    }

    public async Task<IReadOnlyList<Document>> GetAllAsync(int limit = 50)
    {
        var documents = new List<Document>();
        
        await foreach (var entity in _tableClient.QueryAsync<DocumentEntity>(e => e.PartitionKey == "document"))
        {
            documents.Add(entity.ToModel());
            if (documents.Count >= limit) break;
        }
        
        return documents.OrderByDescending(d => d.UploadedAt).ToList();
    }

    public async Task<string> ExtractTextAsync(string documentId)
    {
        var documentEntity = await GetEntityByIdAsync(documentId)
            ?? throw new NotFoundException("Document", documentId);

        var document = documentEntity.ToModel();
        document.Status = DocumentStatus.Extracting;
        await UpdateDocumentAsync(document, documentEntity.ExtractedTextBlobPath);

        _logger.LogInformation("Extracting text from document: {DocumentId}, FileName: {FileName}", 
            documentId, document.FileName);

        try
        {
            // Get the file stream from blob storage
            using var fileStream = await _storageService.RetrieveAsync(document.StoragePath);
            
            // Copy to a MemoryStream so we can seek (some extractors need this)
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            // Extract text using the appropriate extractor
            var extractedText = await _textExtractorFactory.ExtractTextAsync(memoryStream, document.FileName);

            // Store extracted text in blob storage (can be large)
            // StoreAsync returns the actual blob path including any prefix it adds
            using var textStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(extractedText));
            var textBlobPath = await _storageService.StoreAsync("extracted-text", $"{documentId}.txt", textStream, "text/plain");

            document.ExtractedText = extractedText;
            document.Status = DocumentStatus.Completed;
            await UpdateDocumentAsync(document, textBlobPath);

            _logger.LogInformation("Text extraction completed for document: {DocumentId}, Length: {Length} chars", 
                documentId, extractedText.Length);

            return extractedText;
        }
        catch (Exception ex)
        {
            document.Status = DocumentStatus.Failed;
            await UpdateDocumentAsync(document, documentEntity.ExtractedTextBlobPath);
            
            _logger.LogError(ex, "Failed to extract text from document: {DocumentId}", documentId);
            throw new DocumentProcessingException(document.FileName, "TextExtraction", ex.Message, ex);
        }
    }

    public async Task DeleteAsync(string id)
    {
        var document = await GetByIdAsync(id)
            ?? throw new NotFoundException("Document", id);

        // Delete from blob storage
        await _storageService.DeleteAsync(document.StoragePath);
        
        // Delete from table storage
        await _tableClient.DeleteEntityAsync("document", id);
        
        _logger.LogInformation("Document deleted: {DocumentId}", id);
    }

    public async Task<Stream> GetFileStreamAsync(string documentId)
    {
        var document = await GetByIdAsync(documentId)
            ?? throw new NotFoundException("Document", documentId);

        return await _storageService.RetrieveAsync(document.StoragePath);
    }

    private async Task UpdateDocumentAsync(Document document, string? extractedTextBlobPath = null)
    {
        var entity = DocumentEntity.FromModel(document, extractedTextBlobPath);
        await _tableClient.UpsertEntityAsync(entity);
    }

    private async Task<DocumentEntity?> GetEntityByIdAsync(string id)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<DocumentEntity>("document", id);
            return response.Value;
        }
        catch (global::Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
