# Azure Deployment Plan

## Overview

Deploy the DocCoach Blazor Server application to Azure using:
- **Azure Developer CLI (azd)** - for deployment orchestration
- **Bicep** - for Infrastructure as Code (IaC)
- **Azure App Service** - for hosting the Blazor Server app

## Configuration

| Setting | Value |
|---------|-------|
| **Region** | Sweden Central (`swedencentral`) |
| **Environment** | Staging (initial) |
| **Authentication** | Managed Identity (all services) |
| **Custom Domain** | None (use App Service default URL) |
| **AI Foundry** | New instance |

## Current Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Local Development                            │
├─────────────────────────────────────────────────────────────────┤
│  Aspire AppHost                                                  │
│  ├── DocCoach.Web (Blazor Server)                            │
│  ├── Azurite (local emulator)                                   │
│  │   ├── Blob Storage (documents)                               │
│  │   └── Table Storage (metadata)                               │
│  └── Azure AI Foundry (external)                                │
└─────────────────────────────────────────────────────────────────┘
```

## Target Azure Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│              Azure Resource Group (Sweden Central)               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                    Azure App Service                      │   │
│  │  ┌────────────────────────────────────────────────────┐  │   │
│  │  │         DocCoach.Web (Blazor Server)            │  │   │
│  │  │  • .NET 10                                         │  │   │
│  │  │  • System-assigned Managed Identity                │  │   │
│  │  │  • Linux App Service Plan (B1/S1)                  │  │   │
│  │  └────────────────────────────────────────────────────┘  │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│            ┌─────────────────┼─────────────────┐                 │
│            │ Managed Identity│                 │                 │
│            ▼                 ▼                 ▼                 │
│  ┌────────────────────┐  ┌────────────────────┐                 │
│  │  Azure Storage     │  │  Azure AI Foundry  │                 │
│  │  Account           │  │  Hub + Project     │                 │
│  │  ├── Blob (docs)   │  │  └── GPT-4o        │                 │
│  │  └── Table (meta)  │  │     deployment     │                 │
│  │                    │  │                    │                 │
│  │  RBAC Roles:       │  │  RBAC Roles:       │                 │
│  │  • Storage Blob    │  │  • Cognitive       │                 │
│  │    Data Contrib.   │  │    Services User   │                 │
│  │  • Storage Table   │  │                    │                 │
│  │    Data Contrib.   │  │                    │                 │
│  └────────────────────┘  └────────────────────┘                 │
│                                                                  │
│  ┌────────────────────┐  ┌────────────────────┐                 │
│  │  Log Analytics     │  │  Application       │                 │
│  │  Workspace         │  │  Insights          │                 │
│  └────────────────────┘  └────────────────────┘                 │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Azure Resources Required

| Resource | Purpose | SKU/Tier |
|----------|---------|----------|
| Resource Group | Container for all resources | - |
| App Service Plan | Compute for web app | B1 (staging) / S1 (prod) |
| App Service | Blazor Server application | Linux, .NET 10 |
| Azure Storage Account | Blob + Table storage | Standard LRS |
| Microsoft Foundry Hub | AI services management | - |
| Microsoft Foundry Project | Model deployments | - |
| Log Analytics Workspace | Logging/monitoring | Per GB |
| Application Insights | APM | - |

## Implementation Tasks

### Phase 1: Initialize azd Project
- [ ] Initialize azd project structure
- [ ] Create azure.yaml manifest
- [ ] Set up staging environment configuration

### Phase 2: Bicep Infrastructure
- [ ] Create main.bicep orchestrator
- [ ] Create modules:
  - [ ] app-service.bicep (Plan + Web App)
  - [ ] storage-account.bicep
  - [ ] ai-foundry.bicep (Hub + Project + OpenAI)
  - [ ] monitoring.bicep (Log Analytics + App Insights)
- [ ] Configure managed identity and RBAC roles
- [ ] Wire up all connections via managed identity

### Phase 3: Application Configuration
- [ ] Update Program.cs for Azure configuration (managed identity)
- [ ] Configure connection via environment variables
- [ ] Add health check endpoints for App Service
- [ ] Test locally with Azure resources

### Phase 4: Deploy & Validate
- [ ] Deploy infrastructure with `azd provision`
- [ ] Deploy application with `azd deploy`
- [ ] Validate storage connectivity
- [ ] Validate AI service connectivity
- [ ] Test end-to-end functionality

### Phase 5: CI/CD Pipeline (Future)
- [ ] Create GitHub Actions workflow
- [ ] Configure automated deployments
- [ ] Set up production environment

## File Structure

```
DocCoach/
├── azure.yaml                    # azd project manifest
├── .azure/                       # azd environment configs
│   └── staging/
│       └── .env
├── infra/                        # Bicep IaC
│   ├── main.bicep               # Main orchestrator
│   ├── main.parameters.json     # Parameter values
│   └── modules/
│       ├── app-service.bicep    # App Service Plan + Web App
│       ├── storage-account.bicep
│       ├── ai-foundry.bicep     # Hub + Project + OpenAI
│       └── monitoring.bicep     # Log Analytics + App Insights
└── src/
    └── DocCoach.Web/
        └── (existing code)
```

## Configuration Strategy

### Environment Variables for App Service

| Variable | Source | Description |
|----------|--------|-------------|
| `Azure__StorageAccountName` | Bicep output | Storage account name for managed identity |
| `AzureAI__Endpoint` | Bicep output | AI Foundry project endpoint URL |
| `AzureAI__ModelDeployment` | Config | Model name (gpt-4o) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Bicep output | APM connection |

### Managed Identity RBAC Roles

| Resource | Role | Purpose |
|----------|------|---------|
| Storage Account | Storage Blob Data Contributor | Read/write documents |
| Storage Account | Storage Table Data Contributor | Read/write metadata |
| Azure OpenAI | Cognitive Services OpenAI User | Call GPT-4o API |

## Code Changes Required

### 1. Update Storage Client Configuration (Managed Identity)

```csharp
// Current (Azurite-only)
const string azuriteConnectionString = "UseDevelopmentStorage=true";
builder.Services.AddSingleton(_ => new BlobServiceClient(azuriteConnectionString));

// Updated (supports both local and Azure with Managed Identity)
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
```

### 2. Update AI Service Configuration (Managed Identity)

```csharp
// Use DefaultAzureCredential for Azure OpenAI
// The AzureAIOptions.ApiKey will be null, triggering managed identity auth
```

### 3. Add Health Check Endpoints

```csharp
builder.Services.AddHealthChecks();
app.MapHealthChecks("/health");
```

## azd Commands

```bash
# Initialize the staging environment (first time)
azd env new staging

# Set the Azure location (required)
azd env set AZURE_LOCATION swedencentral

# Login to Azure (if not already logged in)
azd auth login

# Provision infrastructure + deploy app
azd up

# Or run separately:
# azd provision  # Create infrastructure only
# azd deploy     # Deploy application only

# View deployed resources
azd show

# Tear down resources
azd down
```

## Cost Estimate (Staging Environment)

| Resource | Estimated Monthly Cost |
|----------|----------------------|
| App Service Plan (B1) | ~$13 |
| Storage Account (Standard LRS) | ~$1-5 |
| Log Analytics | ~$2-5 |
| Application Insights | Free tier |
| Azure OpenAI (GPT-4o) | ~$10-50 (based on tokens) |
| AI Foundry Hub | Free (pay for compute) |
| **Total** | **~$30-75/month** |

## Security Considerations

1. **Managed Identity** - No secrets in code or config
2. **RBAC** - Least privilege access for each resource
3. **HTTPS Only** - Enforced by App Service
4. **TLS 1.2+** - Minimum TLS version

## Next Steps

1. ✅ Create deployment plan
2. ✅ Create azd project structure (`azure.yaml`)
3. ✅ Create Bicep infrastructure modules
4. ✅ Update application code for managed identity
5. Deploy to staging with `azd up`
6. Validate and test
