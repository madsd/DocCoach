<!--
SYNC IMPACT REPORT
==================
Version change: 1.0.0 → 1.1.0
Modified principles:
  - I. Microsoft & Azure Ecosystem: Updated AI to Microsoft Foundry
  - II. Latest & Preview Versions: Updated to .NET 10, Aspire 13
  - V. Local-First: Updated to use Azurite for all storage (assumes running)
Modified sections:
  - Technology Stack: .NET 10, Aspire 13, Microsoft Foundry, Azure Table Storage
Removed: Cosmos DB (replaced with Table Storage)
Templates requiring updates: None (tech-agnostic)
Follow-up TODOs: None
-->

# DocCoach Constitution

## Core Principles

### I. Microsoft & Azure Ecosystem (NON-NEGOTIABLE)

All technology choices MUST use Microsoft and Azure services exclusively:
- **Frontend**: Blazor (Server or WebAssembly) or ASP.NET Core MVC/Razor Pages
- **Backend**: ASP.NET Core (.NET 10 or latest)
- **Orchestration**: .NET Aspire 13 for local development and service composition
- **AI Services**: Microsoft Foundry (formerly Azure AI Foundry) for model hosting
- **Cloud Services**: Azure-only (App Service, Container Apps, Storage, Table Storage, etc.)
- **IDE**: Visual Studio or VS Code with C#/Azure extensions

**Rationale**: Ensures consistent tooling, unified support channels, and seamless Azure integration for public sector deployment scenarios.

### II. Latest & Preview Versions

Development MUST target the latest stable or preview versions of frameworks and services:
- Use .NET 10 for all backend/Blazor projects
- Use .NET Aspire 13 for orchestration and local development
- Enable preview features in Azure services when beneficial
- Update dependencies proactively; do not pin to outdated versions
- Document any preview features used for future migration awareness

**Rationale**: Demo applications benefit from showcasing cutting-edge capabilities; production stability concerns do not apply.

### III. Demo-First, No Testing

This project is for demonstration purposes only. Testing is explicitly NOT required:
- No unit tests, integration tests, or contract tests are mandatory
- No TDD workflow enforced
- Manual verification of functionality is acceptable
- Focus developer time on visible features and UX polish

**Rationale**: Time investment in test infrastructure yields no return for a demo; prioritize demonstrable functionality over code coverage.

### IV. Mock-to-Real Implementation Pattern

Development MUST follow an iterative mock-first approach:
1. **Phase 1 - Operational Shell**: Create a fully navigable UI with mock AI services
2. **Phase 2 - Service Interfaces**: Define clean interfaces for all external dependencies
3. **Phase 3 - Gradual Integration**: Replace mock AI with real Microsoft Foundry implementation
4. Storage uses Azurite locally from the start (no mock needed)
5. Keep mock AI implementations available for offline demo scenarios

**Rationale**: Enables rapid prototyping, parallel frontend/backend development, and ensures the app remains demo-ready at every stage.

### V. Local-First with Cloud-Ready Architecture

The application MUST run locally while supporting Azure cloud services:
- Local development uses Azurite for Blob and Table Storage (assumes Azurite already running)
- Mock AI service provides offline capability when Microsoft Foundry unavailable
- Configuration MUST support environment-based AI service switching (mock vs. Foundry)
- Use Azure Identity with DefaultAzureCredential for seamless local-to-cloud auth
- Aspire orchestrates local services and provides observability dashboard

**Rationale**: Enables demos without Azure subscription while maintaining production-like architecture for credibility.

## Technology Stack

### Required Technologies

| Layer | Technology | Version |
|-------|------------|---------|
| Runtime | .NET | 10.0 (latest) |
| Orchestration | .NET Aspire | 13.x (latest) |
| Frontend | Blazor Server or Blazor WebAssembly | Latest |
| Backend API | ASP.NET Core Minimal APIs or Controllers | Latest |
| AI/ML | Microsoft Foundry | Latest preview |
| Document Processing | PdfPig, DocumentFormat.OpenXml | Latest |
| File Storage | Azure Blob Storage / Azurite (local) | Latest |
| Metadata Storage | Azure Table Storage / Azurite (local) | Latest |
| Configuration | Aspire + appsettings.json | Latest |
| Authentication | Microsoft Entra ID / MSAL (optional) | Latest |

### Prohibited Technologies

- Non-Microsoft web frameworks (React, Angular, Vue as primary)
- Non-Azure cloud services (AWS, GCP, Firebase)
- Non-.NET backend languages for core services
- Third-party AI services outside Microsoft Foundry ecosystem

## Development Workflow

### Iteration Cycle

1. **Define**: Capture feature requirements in spec.md
2. **Mock**: Implement with mock AI services (storage uses real Azurite)
3. **Integrate**: Replace mock AI with Microsoft Foundry
4. **Polish**: Refine UI/UX for demo impact

### Quality Gates (Lightweight)

- Code compiles without errors
- Application runs locally via Aspire AppHost
- Key user journeys are manually walkable
- Aspire dashboard shows healthy services
- No visible console errors in browser

### Documentation Requirements

- README.md with local setup instructions (assumes Azurite running)
- Environment variable documentation for Microsoft Foundry configuration
- Brief inline comments for complex AI prompt engineering

## Governance

This constitution supersedes all other development practices for the DocCoach project:
- All implementation decisions MUST verify compliance with these principles
- Deviations require explicit justification documented in the relevant spec or plan
- Technology additions outside Microsoft/Azure ecosystem are NOT permitted
- Constitution amendments require version increment and migration notes

**Version**: 1.1.0 | **Ratified**: 2026-01-05 | **Last Amended**: 2026-01-05
