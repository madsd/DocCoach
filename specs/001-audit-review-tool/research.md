# Research: Automated Audit Report Review Tool

**Feature**: 001-audit-review-tool  
**Date**: 2026-01-05

## Technology Selection

### Frontend Framework: Blazor Server

**Selected**: Blazor Server (.NET 10) with .NET Aspire 13 orchestration

**Why Blazor Server over Blazor WebAssembly**:
- Server-side execution simplifies Azure service integration (no CORS, direct SDK usage)
- Faster initial load (no large WASM download)
- Better debugging experience during development
- Real-time SignalR connection suits progress indicator requirements
- For demo purposes, the always-connected model is acceptable

**Why .NET Aspire 13**:
- Simplified local development with orchestrated services
- Built-in Azurite integration for Blob and Table Storage emulation
- Aspire dashboard provides observability out-of-the-box
- Seamless Azure deployment via `azd` tooling
- Service discovery eliminates hardcoded connection strings

**Why not ASP.NET Core MVC/Razor Pages**:
- Blazor provides richer interactivity without JavaScript
- Component model better suits the multi-page application structure
- Modern approach aligns with "latest versions" constitution principle

### UI Component Library: MudBlazor

**Selected**: MudBlazor v8.x (latest)

**Rationale**:
- Material Design aesthetic (professional, public-sector appropriate)
- Comprehensive component set: file upload, progress indicators, data tables, cards
- Active development and .NET 10 support
- MIT licensed, no Azure billing
- Excellent documentation and examples

**Key Components to Use**:
- `MudFileUpload` - Document upload with drag-drop
- `MudProgressLinear` / `MudProgressCircular` - Processing indicators
- `MudCard` - Feedback item display
- `MudRating` or custom gauge - Quality score visualization
- `MudNavMenu` - Role-based navigation
- `MudChip` - Feedback category tags

### AI Service: Microsoft Foundry

**Selected**: Microsoft Foundry with GPT-4o deployment

**Why Microsoft Foundry (formerly Azure AI Foundry)**:
- Unified AI model hosting platform within Azure ecosystem
- Supports GPT-4o and other foundation models
- Enterprise-grade security and compliance
- Managed scaling and quota management
- Integrated with Azure Identity for seamless authentication

**Model Choice**:
- GPT-4o (omni) for multimodal capability (can process document images if needed)
- Large context window (128K tokens) handles full audit reports
- Fast inference suitable for 60-second target

**Integration Pattern**:
```
Document → Extract Text → Build Prompt with Guidelines → Foundry GPT-4o → Parse Structured Response
```

**Prompt Engineering Approach**:
- System prompt contains guideline criteria
- User prompt contains document text
- Request JSON-structured response for reliable parsing
- Include few-shot examples from uploaded example documents

**SDK**: `Azure.AI.Inference` (latest preview) - unified inference SDK for Foundry-hosted models

### Document Processing

**Option A (Selected): Text Extraction + OpenAI**
- Use library to extract text from PDF/DOCX
- Send text to Azure OpenAI for analysis
- Simpler architecture, fewer Azure dependencies

**Libraries**:
- PDF: `PdfPig` (MIT, pure .NET, good text extraction)
- DOCX: `DocumentFormat.OpenXml` (Microsoft official, OOXML parsing)

**Option B (Deferred): Azure AI Document Intelligence**
- Better for scanned documents (OCR)
- More complex setup
- Consider if demo documents are scanned PDFs

### Storage Strategy

**Documents (Files)**:
- **Production**: Azure Blob Storage
- **Local**: Azurite Blob emulator (assumes running)

**Metadata (Reviews, Guidelines)**:
- **Production**: Azure Table Storage
- **Local**: Azurite Table emulator (assumes running)

**Why Azure Table Storage over Cosmos DB**:
- Simpler data model sufficient for demo entities
- Azurite provides complete local emulation (no separate emulator needed)
- Cost-effective for demo scale
- Native support in Aspire via `AddAzureTableService()`
- Partition key / row key pattern maps well to entity relationships

**Why not JSON files for local**:
- Azurite is already running per requirements
- Table Storage provides consistent API between local and cloud
- Better represents real deployment architecture
- Aspire wires connection strings automatically

### State Management

**Approach**: Scoped services + Cascading Parameters

For a Blazor Server demo application:
- `AppState` service (scoped): Current role, selected user
- `ReviewState` service (scoped): Current review session, uploaded document
- Cascading parameters for deeply nested components

No need for complex state libraries (Fluxor, etc.) given demo scope.

## Architecture Decisions

### Service Layer Pattern

```
Pages/Components → Services (Interfaces) → Implementations (Mock or Azure)
                                        ↓
                              Registered via DI based on configuration
                                        ↓
                              Aspire injects connection strings/endpoints
```

**Aspire AppHost Configuration** (`Program.cs`):
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Storage resources (Azurite locally, Azure in cloud)
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();  // Uses Azurite
var blobs = storage.AddBlobs("blobs");
var tables = storage.AddTables("tables");

// AI service (mock locally, Foundry in cloud)
var aiEndpoint = builder.AddParameter("foundry-endpoint", secret: false);

// Web application
builder.AddProject<Projects.DocCoach_Web>("web")
    .WithReference(blobs)
    .WithReference(tables)
    .WithEnvironment("AI__Endpoint", aiEndpoint);

builder.Build().Run();
```

**Configuration Switch** (`appsettings.json`):
```json
{
  "ServiceMode": "Mock",  // or "Azure" for real Foundry AI
  "AI": {
    "Endpoint": "https://<your-foundry>.inference.ai.azure.com/",
    "DeploymentName": "gpt-4o",
    "UseMock": true
  }
}
```

### Mock Service Design

Mock services should:
1. Return realistic data (not "Lorem ipsum")
2. Simulate realistic delays (1-3 seconds for AI operations)
3. Support multiple scenarios (success, partial issues, many issues)
4. Use sample audit report content that matches Supreme Audit Office domain

**Sample Mock Review Response**:
```json
{
  "qualityScore": 72,
  "categoryScores": {
    "clarity": 85,
    "completeness": 65,
    "factualSupport": 68
  },
  "feedbackItems": [
    {
      "category": "completeness",
      "severity": "warning",
      "description": "Section 3.2 'Methodology' lacks detail on sampling approach",
      "location": { "page": 5, "section": "3.2" }
    }
  ]
}
```

### Error Handling Strategy

For a demo application:
- Display user-friendly error messages in UI (MudSnackbar)
- Log errors to console (browser dev tools visible in demos)
- Graceful degradation: if Azure fails, offer retry or mock fallback
- No complex retry/circuit breaker patterns needed

## Dependencies Summary

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Aspire.Hosting` | 13.x | AppHost orchestration |
| `Aspire.Hosting.Azure.Storage` | 13.x | Blob + Table Storage resources |
| `Aspire.Azure.Storage.Blobs` | 13.x | Blob client integration |
| `Aspire.Azure.Data.Tables` | 13.x | Table client integration |
| `MudBlazor` | 8.x | UI components |
| `Azure.AI.Inference` | 1.x (preview) | Microsoft Foundry inference SDK |
| `Azure.Identity` | 1.x | DefaultAzureCredential |
| `PdfPig` | 0.1.x | PDF text extraction |
| `DocumentFormat.OpenXml` | 3.x | DOCX processing |

### Development Tools

- .NET 10 SDK
- Visual Studio 2022 (17.12+) or VS Code with C# Dev Kit
- Azure CLI (for deployment)
- Azurite (already running per requirements)
- Azure Developer CLI (`azd`) for deployment

## Open Questions Resolved

| Question | Resolution |
|----------|------------|
| Blazor Server vs WASM? | Server - simpler Azure integration |
| How to handle scanned PDFs? | Defer OCR; assume text-based PDFs for demo |
| Real-time progress? | SignalR built into Blazor Server; use for streaming feedback |
| Multi-language UI? | English only for demo; architecture supports localization |
| Local storage emulation? | Azurite for both Blob and Table Storage (already running) |
| AI local fallback? | Mock service with simulated delays and realistic responses |
