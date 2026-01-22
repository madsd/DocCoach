# Copilot Instructions

## Project Overview

DocCoach is a .NET 10 Blazor Server application for AI-assisted document review. It allows users to upload audit documents (PDF/DOCX), configure review guidelines with criteria, and receive AI-powered feedback.

## Tech Stack

- **.NET 10** with **Aspire 13** for orchestration
- **Blazor Server** with **MudBlazor** UI components
- **Azure Blob Storage** (Azurite for local dev) for document storage
- **Azure Table Storage** for metadata persistence
- **Azure OpenAI** for AI-based document analysis

## Project Structure

```
src/
├── DocCoach.AppHost/     # Aspire orchestrator
├── DocCoach.ServiceDefaults/  # Shared service configuration
└── DocCoach.Web/         # Main Blazor web application
    ├── Components/          # Razor components
    │   ├── Pages/          # Page components (Auditor/, Guidelines/)
    │   ├── Shared/         # Reusable components
    │   └── Dialogs/        # Modal dialogs
    ├── Models/             # Domain models
    ├── Services/           # Business logic & integrations
    │   ├── Azure/          # Azure service implementations
    │   ├── Interfaces/     # Service contracts
    │   └── TextExtraction/ # PDF/DOCX text extraction
    └── State/              # Application state management
```

## Key Concepts

### Review Dimensions & Criterion Types
- **ReviewDimension**: High-level categories (Clarity, Accuracy, Completeness, Compliance, Style)
- **CriterionType**: Specific checks within dimensions (e.g., PassiveVoice, ReadabilityScore, SentenceLength)
- **EvaluationMode**: How criteria are evaluated (AIBased, RuleBased, Hybrid)

### Analyzers
Review analyzers implement `IReviewAnalyzer` and provide:
- `SupportedCriterionTypes` - Which criterion types they can evaluate
- `AnalyzeAsync()` - Analysis logic returning `FeedbackItem` results

### Document Flow
1. Upload document → Extract text → Store in blob
2. Select guideline set with criteria
3. Run review → Each criterion evaluated by appropriate analyzer
4. View results with feedback highlighting in document viewer

## Coding Conventions

- Use `MudBlazor` components for UI (MudCard, MudChip, MudButton, etc.)
- Follow existing patterns for new components
- Services are registered as singletons in `Program.cs`
- Use extension methods like `GetDisplayName()` for enum display values

## Running the Application

```bash
cd src/DocCoach.AppHost
aspire run
```

The web app runs at `https://localhost:7109`.

## Aspire Integration

This project uses .NET Aspire for orchestration. Key points:
- Changes to `AppHost.cs` require restart
- Use Aspire MCP tools to check resource status and debug issues
- Hot reload works for most Blazor component changes
- Use `aspire run` to start (will prompt to stop existing instance)

## Testing Changes

1. Make incremental changes
2. Build: `dotnet build src/DocCoach.Web`
3. Hot reload should pick up Razor changes
4. For `Program.cs` or service changes, restart Aspire
5. Use Aspire MCP tools for diagnostics if issues arise
6. Validate functionality in the web UI using the Playwright MCP server if needed

## Common Tasks

### Adding a New Analyzer
1. Create class implementing `IReviewAnalyzer` in `Services/Analyzers/`
2. Register in `AnalyzerServiceExtensions.cs`
3. Analyzer will automatically appear in criterion type dropdown

### Adding a New Page
1. Create `.razor` file in `Components/Pages/`
2. Add `@page "/route"` directive
3. Use `@attribute [StreamRendering]` for async loading

### Modifying Models
- Models in `Models/` folder
- Update corresponding ViewModels if needed
- Consider Table Storage schema implications

## Important Notes

- Avoid persistent containers during development
- The Aspire workload is obsolete - don't use it
- Prefer official docs: https://aspire.dev, https://learn.microsoft.com/dotnet/aspire
