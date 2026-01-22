using DocCoach.Web.Components;
using DocCoach.Web.Services.Azure;
using DocCoach.Web.Services.Interfaces;
using DocCoach.Web.Services.TextExtraction;
using DocCoach.Web.State;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Configure Azure Storage clients
// Uses Azurite for local dev, Managed Identity for Azure
var storageAccountName = builder.Configuration["Azure:StorageAccountName"];
if (string.IsNullOrEmpty(storageAccountName))
{
    // Local development with Azurite
    builder.Services.AddSingleton(_ => new BlobServiceClient("UseDevelopmentStorage=true"));
    builder.Services.AddSingleton(_ => new TableServiceClient("UseDevelopmentStorage=true"));
}
else
{
    // Azure: Use Managed Identity
    var credential = new DefaultAzureCredential();
    var blobUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
    var tableUri = new Uri($"https://{storageAccountName}.table.core.windows.net");
    builder.Services.AddSingleton(_ => new BlobServiceClient(blobUri, credential));
    builder.Services.AddSingleton(_ => new TableServiceClient(tableUri, credential));
}

// Add MudBlazor services
builder.Services.AddMudServices();

// Register application state (scoped per user/circuit)
builder.Services.AddScoped<AppState>();

// Register text extractors
builder.Services.AddSingleton<ITextExtractor, PdfTextExtractor>();
builder.Services.AddSingleton<ITextExtractor, DocxTextExtractor>();
builder.Services.AddSingleton<TextExtractorFactory>();

// Register application services - all using Azure Storage (Azurite for local dev)
builder.Services.AddSingleton<IStorageService, AzureBlobStorageService>();
builder.Services.AddSingleton<IGuidelineService, AzureGuidelineService>();
builder.Services.AddSingleton<IDocumentService, AzureDocumentService>();
builder.Services.AddSingleton<IAIConfigurationService, AzureAIConfigurationService>();

// Configure Azure AI service for document review
builder.Services.Configure<AzureAIOptions>(builder.Configuration.GetSection(AzureAIOptions.SectionName));
builder.Services.AddSingleton<IReviewService, AzureAIReviewService>();

// Register review analyzers (for static and AI-based analysis)
builder.Services.AddReviewAnalyzers();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// API endpoint to serve original documents for embedded viewing
app.MapGet("/api/documents/{id}/file", async (string id, IDocumentService documentService) =>
{
    var document = await documentService.GetByIdAsync(id);
    if (document is null)
        return Results.NotFound();
    
    var stream = await documentService.GetFileStreamAsync(id);
    // Use inline disposition so browser displays instead of downloading
    return Results.File(stream, document.ContentType, fileDownloadName: null, enableRangeProcessing: true);
}).WithName("GetDocumentFile");

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
