# Quickstart: Automated Audit Report Review Tool

**Feature**: 001-audit-review-tool  
**Date**: 2026-01-05

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Visual Studio 2022 (17.12+) or VS Code with C# Dev Kit
- Git
- **Azurite running locally** (Blob + Table Storage emulation)

**Optional (for Azure deployment)**:
- Azure subscription
- Azure CLI (`az`)
- Azure Developer CLI (`azd`)

## Quick Start (Local with Azurite)

### 1. Clone and Navigate

```powershell
git clone <repository-url>
cd DocCoach
git checkout 001-audit-review-tool
```

### 2. Verify Azurite is Running

Azurite should already be running. Verify with:
```powershell
# Check if Azurite is listening
Test-NetConnection -ComputerName 127.0.0.1 -Port 10000
```

If not running, start it:
```powershell
azurite --silent --location ./azurite-data
```

### 3. Build and Run with Aspire

```powershell
cd src/DocCoach.AppHost
dotnet run
```

### 4. Open Aspire Dashboard

The Aspire dashboard opens automatically at: **https://localhost:15000** (or URL shown in terminal)

From the dashboard:
- Click on **web** to open the Blazor application
- View logs, traces, and metrics for all services
- Monitor Azurite storage connections

### 5. Try It Out

1. **Select Role**: Click "Auditor" or "Admin" on the home page
2. **As Auditor**:
   - Go to "Upload Document"
   - Upload a sample PDF from `wwwroot/sample-documents/`
   - Wait for processing (mock AI: 2-3 seconds)
   - View feedback and quality score
3. **As Admin**:
   - Go to "Configure Guidelines"
   - View/edit criteria
   - Upload example documents

## Project Structure Overview

```
src/
├── DocCoach.AppHost/        # Aspire orchestration
│   └── Program.cs              # Service wiring, Azurite config
├── DocCoach.ServiceDefaults/ # Shared Aspire configuration
│   └── Extensions.cs           # OpenTelemetry, health checks
└── DocCoach.Web/            # Blazor Server application
    ├── Program.cs              # Entry point, DI setup
    ├── appsettings.json        # Configuration
    ├── Components/
    │   ├── Pages/              # Routable pages
    │   └── Shared/             # Reusable components
    ├── Models/                 # Domain entities
    ├── Services/
    │   ├── Interfaces/         # Service contracts
    │   ├── Mock/               # Mock AI implementations
    │   └── Azure/              # Real Azure implementations
    └── wwwroot/
        └── sample-documents/   # Demo files
```

## Configuration

### Service Mode

Edit `src/DocCoach.Web/appsettings.json`:

```json
{
  "AI": {
    "UseMock": true    // true = mock AI, false = use Microsoft Foundry
  }
}
```

**Mock Mode** (default):
- Azurite provides real Blob + Table Storage locally
- AI responses are simulated with realistic delays
- Perfect for demos and UI development

**Azure AI Mode**:
- Requires Microsoft Foundry configuration (see below)
- Real AI document analysis
- Storage remains on Azurite or Azure (based on Aspire config)

### Microsoft Foundry Configuration

When `AI.UseMock` is `false`, configure Foundry:

```json
{
  "AI": {
    "UseMock": false,
    "Endpoint": "https://<your-project>.inference.ai.azure.com/",
    "DeploymentName": "gpt-4o"
  }
}
```

**Authentication**: Uses `DefaultAzureCredential`. For local development:
```powershell
az login
```

### Environment Variables (Production)

For Azure deployment, use environment variables:

```
AI__USEMOCK=false
AI__ENDPOINT=https://...
AI__DEPLOYMENTNAME=gpt-4o
```

Aspire handles storage connection strings automatically via Azure resource bindings.

## Local Development with Aspire

### Aspire Dashboard Features

The Aspire dashboard (https://localhost:15000) provides:
- **Resources**: View all running services (web, storage)
- **Console**: Live logs from all services
- **Traces**: Distributed tracing for requests
- **Metrics**: Performance metrics

### Storage Explorer

View Azurite data using Azure Storage Explorer:
1. Open Azure Storage Explorer
2. Connect to "Local & Attached" → "Emulator - Default Ports"
3. Browse blobs and tables

## Common Tasks

### Add a Sample Document

Place PDF/DOCX files in:
```
src/DocCoach.Web/wwwroot/sample-documents/
```

### Modify Mock AI Responses

Edit mock service in:
```
src/DocCoach.Web/Services/Mock/MockReviewService.cs
```

### Change UI Theme

MudBlazor theme is configured in `MainLayout.razor`. Modify `MudThemeProvider` for colors.

### Add New Feedback Category

1. Add to `FeedbackCategory` enum in `Models/FeedbackItem.cs`
2. Update mock data in `MockReviewService.cs`
3. Add color mapping in `FeedbackList.razor`

## Troubleshooting

### Aspire Dashboard Not Opening

```powershell
# Check if port 15000 is in use
netstat -ano | findstr :15000

# Or specify different port
dotnet run -- --dashboard-port 15001
```

### Azurite Connection Failed

Verify Azurite is running on default ports:
- Blob: 10000
- Queue: 10001  
- Table: 10002

```powershell
azurite --blobPort 10000 --queuePort 10001 --tablePort 10002
```

### SSL Certificate Issues

```powershell
dotnet dev-certs https --trust
```

### Azure Authentication Errors (for Foundry)

```powershell
# Clear and re-authenticate
az logout
az login
```

## Deployment to Azure

Using Azure Developer CLI:

```powershell
# Initialize (first time only)
azd init

# Deploy
azd up
```

This provisions:
- Azure Storage Account (Blob + Table)
- Azure Container Apps (Blazor app)
- Microsoft Foundry connection

## Useful Commands

```powershell
# Build all projects
dotnet build

# Run with Aspire (recommended)
cd src/DocCoach.AppHost
dotnet run

# Run web only (without Aspire orchestration)
cd src/DocCoach.Web
dotnet run

# Run with hot reload
dotnet watch run

# Publish for deployment
dotnet publish -c Release
```
