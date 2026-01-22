# Implementation Plan: Automated Audit Report Review Tool

**Branch**: `001-audit-review-tool` | **Date**: 2026-01-05 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/001-audit-review-tool/spec.md`

## Summary

Build an AI-powered web application that automates the review of audit reports against institutional guidelines. The tool enables auditors to upload documents and receive structured feedback on language clarity, section completeness, and factual support. Administrators can configure guidelines and upload example documents. Built with Blazor Server (.NET 10) using .NET Aspire 13 for orchestration and Microsoft Foundry for AI model hosting, following a mock-first development pattern for demo readiness.

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Orchestration**: .NET Aspire 13 (service discovery, configuration, observability)  
**Primary Dependencies**: Blazor Server, Azure.AI.Inference (Microsoft Foundry), Azure.Storage.Blobs, Azure.Data.Tables, MudBlazor (UI components)  
**Storage**: Azure Blob Storage (documents) + Azure Table Storage (metadata/reviews) / Azurite for local development  
**AI Hosting**: Microsoft Foundry (GPT-4o deployment)  
**Testing**: None required (demo project per constitution)  
**Target Platform**: Web (modern browsers), local development on Windows/macOS/Linux  
**Project Type**: Web application (Blazor Server with Aspire AppHost orchestration)  
**Performance Goals**: Document review feedback within 60 seconds, UI load < 3 seconds  
**Constraints**: Must work locally with Azurite, Microsoft/Azure services only  
**Scale/Scope**: Demo scale (~10 concurrent users), 5 main screens

## Constitution Check

*GATE: All items must pass before implementation.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microsoft & Azure Ecosystem | ✅ PASS | Blazor Server, Microsoft Foundry, Table Storage, Blob Storage, Aspire |
| II. Latest & Preview Versions | ✅ PASS | .NET 10, Aspire 13, Azure.AI.Inference latest preview |
| III. Demo-First, No Testing | ✅ PASS | No test infrastructure planned |
| IV. Mock-to-Real Pattern | ✅ PASS | All services have mock implementations |
| V. Local-First Architecture | ✅ PASS | Azurite emulator for Blob + Table Storage |

## Project Structure

### Documentation (this feature)

```text
specs/001-audit-review-tool/
├── plan.md              # This file
├── research.md          # Technology research and decisions
├── data-model.md        # Entity definitions and relationships
├── quickstart.md        # Local development setup guide
├── contracts/           # Service interface definitions
│   ├── IDocumentService.md
│   ├── IReviewService.md
│   ├── IGuidelineService.md
│   └── IStorageService.md
└── tasks.md             # Implementation tasks (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── DocCoach.AppHost/                # .NET Aspire orchestration
│   ├── DocCoach.AppHost.csproj
│   └── Program.cs                       # Aspire app model (services, resources)
│
├── DocCoach.ServiceDefaults/        # Shared Aspire service configuration
│   ├── DocCoach.ServiceDefaults.csproj
│   └── Extensions.cs                    # OpenTelemetry, health checks, resilience
│
└── DocCoach.Web/                    # Blazor Server application
    ├── DocCoach.Web.csproj
    ├── Program.cs                       # Application entry point, DI configuration
    ├── appsettings.json                 # Configuration (local/Azure switching)
    ├── appsettings.Development.json     # Local development overrides
    │
    ├── Components/                      # Blazor components
    │   ├── Layout/
    │   │   ├── MainLayout.razor         # App shell with navigation
    │   │   └── NavMenu.razor            # Role-aware navigation
    │   ├── Pages/
    │   │   ├── Home.razor               # Role selector landing page
    │   │   ├── Auditor/
    │   │   │   ├── Upload.razor         # Document upload page
    │   │   │   ├── ReviewResults.razor  # Review results display
    │   │   │   └── DocumentHistory.razor # Past reviews list
    │   │   └── Admin/
    │   │       ├── Guidelines.razor     # Guideline configuration
    │   │       └── Examples.razor       # Example document upload
    │   └── Shared/
    │       ├── DocumentUploader.razor   # Reusable upload component
    │       ├── FeedbackList.razor       # Feedback items display
    │       ├── ScoreCard.razor          # Quality score visualization
    │       └── ProgressIndicator.razor  # Processing status display
    │
    ├── Models/                          # Domain entities
    │   ├── Document.cs
    │   ├── Review.cs
    │   ├── FeedbackItem.cs
    │   ├── GuidelineSet.cs
    │   ├── Criterion.cs
    │   └── ExampleDocument.cs
    │
    ├── Services/                        # Business logic
    │   ├── Interfaces/
    │   │   ├── IDocumentService.cs
    │   │   ├── IReviewService.cs
    │   │   ├── IGuidelineService.cs
    │   │   └── IStorageService.cs
    │   ├── Mock/                        # Mock implementations (Phase 1)
    │   │   ├── MockDocumentService.cs
    │   │   ├── MockReviewService.cs
    │   │   ├── MockGuidelineService.cs
    │   │   └── MockStorageService.cs
    │   └── Azure/                       # Real implementations (Phase 3)
    │       ├── AzureDocumentService.cs
    │       ├── AzureReviewService.cs
    │       ├── AzureGuidelineService.cs
    │       └── AzureBlobStorageService.cs
    │
    ├── State/                           # Application state management
    │   ├── AppState.cs                  # Current role, selected guideline
    │   └── ReviewState.cs               # Current review session state
    │
    └── wwwroot/                         # Static assets
        ├── css/
        └── sample-documents/            # Demo PDF/DOCX files
```

**Structure Decision**: Single Blazor Server project. Blazor Server provides unified frontend/backend in one deployment, server-side rendering for fast initial load, and simplified state management. This is appropriate for a demo application where deployment simplicity and rapid iteration are priorities.

## Implementation Phases

### Phase 1: Operational Shell (Mock Services)

Create fully navigable UI with mock data. All user journeys work but return hardcoded/simulated responses.

**Deliverables**:
- Project scaffolding with .NET 10 Blazor Server + Aspire 13 AppHost
- All pages navigable with role switching
- Mock services returning realistic sample data
- MudBlazor UI components for polished appearance
- Aspire dashboard for local observability

### Phase 2: Service Interfaces

Define and document all service contracts. Ensure mock implementations satisfy these contracts.

**Deliverables**:
- Interface definitions for all services
- Mock implementations conforming to interfaces
- Dependency injection configuration for service switching
- Aspire resource wiring (storage, AI connections)

### Phase 3: Azure Integration

Replace mocks with real Azure service implementations one at a time.

**Integration Order**:
1. Azure Blob Storage via Azurite (document upload/storage) - already available locally
2. Azure Table Storage via Azurite (metadata persistence) - already available locally
3. Microsoft Foundry (document review analysis with GPT-4o)

**Deliverables**:
- Azure service implementations using Aspire resource bindings
- Configuration-based switching between mock/real AI
- DefaultAzureCredential integration for Foundry

## Key Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Orchestration | .NET Aspire 13 | Service discovery, local dev experience, observability dashboard, Azure deployment ready |
| UI Framework | MudBlazor | Material Design components, excellent Blazor integration, active community |
| State Management | Scoped services + cascading parameters | Simple, built-in Blazor patterns sufficient for demo |
| AI Service | Microsoft Foundry (GPT-4o) | Azure-hosted model inference, managed scaling, enterprise security |
| Metadata Storage | Azure Table Storage | Simple key-value, Azurite compatible, cost-effective for demo scale |
| Storage Pattern | Interface-based with DI + Aspire | Enables mock/real switching via Aspire resource configuration |
| Configuration | Aspire + appsettings.json | Connection strings injected by Aspire, environment-aware |

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Microsoft Foundry quota/latency | Mock service always available, realistic delays simulated |
| Large document processing | Client-side size validation, chunked processing for analysis |
| Demo without Azure subscription | Azurite provides full Blob + Table Storage locally; mock AI for offline |
| Aspire complexity | ServiceDefaults project provides reusable patterns; AppHost is minimal |

## Complexity Tracking

> No constitution violations requiring justification.

| Item | Status |
|------|--------|
| Single project (no unnecessary separation) | ✅ |
| No test infrastructure | ✅ |
| Microsoft/Azure only | ✅ |
| Mock implementations for all services | ✅ |
