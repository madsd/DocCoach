# DocCoach

AI-powered document review assistant for documents. Upload documents, configure review guidelines, and receive intelligent feedback on compliance, clarity, and completeness.

## Features

- **Document Upload & Review** - Upload PDF/DOCX files and receive AI-powered feedback
- **Configurable Guidelines** - Create custom review criteria for different document types
- **Quality Scoring** - Get overall scores with category breakdowns (clarity, completeness, compliance)
- **Multiple Analyzers** - Combines AI analysis with static checks (readability, passive voice, sentence length)
- **Example Documents** - Upload reference documents to guide AI feedback

## Architecture

```
┌───────────────────────────────────────────────────────────────┐
│                       DocCoach.Web                            │
│                    (Blazor Server App)                        │
|                                                               |
|                  • MudBlazor UI                               |
|                  • Review Analyzers                           |
|                  • Text Extraction                            |
└───────────────────────────────────────────────────────────────┘
                                 │
                    ┌────────────┴────────────┐
                    ▼                         ▼
┌─────────────────────────────┐   ┌─────────────────────────────┐
│      Azure AI Services      │   │        Azure Storage        │
│     (Microsoft Foundry)     │   │   (Local Storage Emulator)  │
│                             │   │                             │
│  • Content Analysis         │   │  • Blob Storage (documents) │
│  • Guideline Extraction     │   │  • Table Storage (metadata) │
│  • Feedback Generation      │   │                             │
└─────────────────────────────┘   └─────────────────────────────┘

```

### Tech Stack

- **.NET 10** with **Aspire** for orchestration
- **Blazor Server** with **MudBlazor** components
- **Azure Blob Storage** - Document file storage
- **Azure Table Storage** - Metadata persistence
- **Azure AI Services** - Microsoft Foundry for intelligent analysis

### Project Structure

```
src/
├── DocCoach.AppHost/          # Aspire orchestrator
├── DocCoach.ServiceDefaults/  # Shared service configuration
└── DocCoach.Web/              # Main Blazor application
    ├── Components/            # Razor components (Pages, Dialogs, Shared)
    ├── Models/                # Domain models
    ├── Services/
    │   ├── Analyzers/         # Review analyzers (AI + Static)
    │   ├── Azure/             # Azure service implementations
    │   ├── Interfaces/        # Service contracts
    │   └── TextExtraction/    # PDF/DOCX text extraction
    └── State/                 # Application state management
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (local storage emulator)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (for Azure deployment)

## Running Locally

### 1. Start Azurite

Start the Azure Storage emulator before running the application:

```bash
azurite --silent --location ./AzuriteConfig --blobHost 127.0.0.1 --queueHost 127.0.0.1 --tableHost 127.0.0.1
```

### 2. Configure Azure AI (Optional)

For AI-powered analysis, create `src/DocCoach.Web/appsettings.Development.json`:

```json
{
  "AzureAI": {
    "Endpoint": "https://your-resource.services.ai.azure.com/api/projects/your-project",
    "ModelDeployment": "gpt-4o",
    "ApiKey": "your-api-key"
  }
}
```

> **Note:** Without AI configuration, the app still works with rule-based analyzers (readability, passive voice, sentence length).

### 3. Run the Application

```bash
cd src
aspire run
```

The application will be available at `https://localhost:7109`.

The Aspire dashboard provides monitoring at the URL shown in the terminal output.

## Deploying to Azure

DocCoach uses Azure Developer CLI (azd) for deployment.

### Prerequisites

- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) - authenticated with `az login`
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)

### Infrastructure

The deployment creates:

| Resource | Purpose |
|----------|---------|
| App Service (Linux) | Hosts the Blazor application |
| Storage Account | Blob + Table storage for documents and metadata |
| Microsoft Foundry | GPT-4o model for document analysis |
| Application Insights | Monitoring and diagnostics |
| Log Analytics | Centralized logging |

All resources use **Managed Identity** for authentication - no secrets in configuration.

### Deploy

```bash
# Initialize (first time only)
azd init

# Set the Azure location (required)
azd env set AZURE_LOCATION <azure location>

# Provision infrastructure and deploy
azd up
```

You'll be prompted for:
- **Environment name** (e.g., `doccoach-staging`, `doccoach-production`)
- **Azure subscription**
- **Azure region** (default: `swedencentral`)

### Environment Variables

The following are automatically configured during deployment:

| Variable | Description |
|----------|-------------|
| `Azure__StorageAccountName` | Storage account for documents |
| `AzureAI__Endpoint` | Azure AI Services endpoint |
| `AzureAI__ModelDeployment` | GPT model deployment name |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application monitoring |

### Redeploy Application Only

After initial setup, deploy code changes without reprovisioning:

```bash
azd deploy
```

### Tear Down

Remove all Azure resources:

```bash
azd down
```

## Development

### Adding a New Analyzer

1. Create a class implementing `IReviewAnalyzer` in `Services/Analyzers/`
2. Register in `AnalyzerServiceCollectionExtensions.cs`
3. The analyzer automatically appears in criterion type options

### Adding a New Page

1. Create `.razor` file in `Components/Pages/`
2. Add `@page "/route"` directive
3. Use `@attribute [StreamRendering]` for async loading patterns

### Hot Reload

Most Blazor component changes hot reload automatically. Restart Aspire for:
- Changes to `AppHost.cs`
- Changes to `Program.cs`
- Service registration changes

## License

[MIT](LICENSE)
