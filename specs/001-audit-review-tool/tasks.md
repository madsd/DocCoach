# Tasks: Automated Audit Report Review Tool

**Input**: Design documents from `/specs/001-audit-review-tool/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Not included - per constitution this is a demo-only project.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Project Scaffolding)

**Purpose**: Aspire project initialization and basic structure

- [X] T001 Create solution structure with `dotnet new aspire -n DocCoach` at repository root
- [X] T002 Configure .NET 10 target framework in all project files
- [X] T003 [P] Add MudBlazor 8.x package to DocCoach.Web/DocCoach.Web.csproj
- [X] T004 [P] Add Azure.Data.Tables package to DocCoach.Web/DocCoach.Web.csproj
- [X] T005 [P] Add Azure.Storage.Blobs package to DocCoach.Web/DocCoach.Web.csproj
- [X] T006 [P] Add PdfPig package to DocCoach.Web/DocCoach.Web.csproj
- [X] T007 [P] Add DocumentFormat.OpenXml package to DocCoach.Web/DocCoach.Web.csproj
- [X] T008 [P] Add Azure.AI.Inference (preview) package to DocCoach.Web/DocCoach.Web.csproj
- [X] T009 Configure MudBlazor services in DocCoach.Web/Program.cs
- [X] T010 Configure MudBlazor CSS and JS in DocCoach.Web/Components/App.razor
- [X] T011 Create base layout with MudThemeProvider in DocCoach.Web/Components/Layout/MainLayout.razor
- [X] T012 Configure Aspire AppHost with Azurite reference in DocCoach.AppHost/Program.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Models & Entities

- [X] T013 [P] Create Document model in DocCoach.Web/Models/Document.cs
- [X] T014 [P] Create Review model in DocCoach.Web/Models/Review.cs
- [X] T015 [P] Create FeedbackItem model in DocCoach.Web/Models/FeedbackItem.cs
- [X] T016 [P] Create GuidelineSet model in DocCoach.Web/Models/GuidelineSet.cs
- [X] T017 [P] Create Criterion model in DocCoach.Web/Models/Criterion.cs
- [X] T018 [P] Create ExampleDocument model in DocCoach.Web/Models/ExampleDocument.cs
- [X] T019 [P] Create DocumentStatus enum in DocCoach.Web/Models/DocumentStatus.cs
- [X] T020 [P] Create FeedbackSeverity enum in DocCoach.Web/Models/FeedbackSeverity.cs
- [X] T021 [P] Create FeedbackCategory enum in DocCoach.Web/Models/FeedbackCategory.cs

### Service Interfaces

- [X] T022 [P] Create IStorageService interface in DocCoach.Web/Services/Interfaces/IStorageService.cs
- [X] T023 [P] Create IDocumentService interface in DocCoach.Web/Services/Interfaces/IDocumentService.cs
- [X] T024 [P] Create IReviewService interface in DocCoach.Web/Services/Interfaces/IReviewService.cs
- [X] T025 [P] Create IGuidelineService interface in DocCoach.Web/Services/Interfaces/IGuidelineService.cs

### Table Storage Infrastructure

- [ ] T026 [P] Create DocumentEntity table adapter in DocCoach.Web/Data/DocumentEntity.cs
- [ ] T027 [P] Create ReviewEntity table adapter in DocCoach.Web/Data/ReviewEntity.cs
- [ ] T028 [P] Create GuidelineSetEntity table adapter in DocCoach.Web/Data/GuidelineSetEntity.cs
- [ ] T029 Create TableStorageContext for table client management in DocCoach.Web/Data/TableStorageContext.cs

### Mock Service Implementations (Demo-First)

- [X] T030 [P] Implement MockStorageService in DocCoach.Web/Services/Mock/MockStorageService.cs
- [X] T031 [P] Implement MockGuidelineService with seed data in DocCoach.Web/Services/Mock/MockGuidelineService.cs
- [X] T032 Implement MockDocumentService in DocCoach.Web/Services/Mock/MockDocumentService.cs
- [X] T033 Implement MockReviewService with sample feedback in DocCoach.Web/Services/Mock/MockReviewService.cs

### State Management

- [X] T034 Create AppState for role and session management in DocCoach.Web/State/AppState.cs
- [X] T035 Register AppState as scoped service in DocCoach.Web/Program.cs

### Custom Exceptions

- [X] T036 [P] Create NotFoundException in DocCoach.Web/Exceptions/NotFoundException.cs
- [X] T037 [P] Create ValidationException in DocCoach.Web/Exceptions/ValidationException.cs
- [X] T038 [P] Create DocumentProcessingException in DocCoach.Web/Exceptions/DocumentProcessingException.cs
- [X] T039 [P] Create ExternalServiceException in DocCoach.Web/Exceptions/ExternalServiceException.cs

### Register Services

- [X] T040 Register mock services with DI in DocCoach.Web/Program.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Upload & Review Document (Priority: P1) 🎯 MVP

**Goal**: As an auditor, I can upload an audit report (PDF or DOCX), select a guideline set, and receive AI-generated feedback with quality scores.

**Independent Test**: 
1. Launch app, select Auditor role
2. Click Upload, select a PDF/DOCX file
3. Choose default guideline set
4. See document process and review results with scores and feedback items

### Implementation for User Story 1

#### Pages & Navigation

- [X] T041 Create Home page with role selector in DocCoach.Web/Components/Pages/Home.razor
- [X] T042 Create navigation menu with role-based visibility in DocCoach.Web/Components/Layout/NavMenu.razor

#### Upload Feature

- [X] T043 [P] [US1] Create FileUploadModel in DocCoach.Web/Models/ViewModels/FileUploadModel.cs
- [X] T044 [US1] Create Upload page in DocCoach.Web/Components/Pages/Auditor/Upload.razor
- [X] T045 [US1] Add file type validation (PDF/DOCX only) in Upload.razor
- [X] T046 [US1] Add file size validation (max 50MB) in Upload.razor
- [X] T047 [US1] Add guideline set selector dropdown in Upload.razor

#### Document Processing

- [X] T048 [US1] Create DocumentProcessor component for text extraction in DocCoach.Web/Components/Shared/DocumentProcessor.razor
- [X] T049 [US1] Add PDF text extraction using PdfPig in DocCoach.Web/Services/TextExtraction/PdfTextExtractor.cs
- [X] T050 [US1] Add DOCX text extraction using OpenXml in DocCoach.Web/Services/TextExtraction/DocxTextExtractor.cs
- [X] T051 [US1] Create ITextExtractor interface in DocCoach.Web/Services/TextExtraction/ITextExtractor.cs
- [X] T052 [US1] Register text extractors in DocCoach.Web/Program.cs

#### Review Display

- [X] T053 [US1] Create ReviewResults page in DocCoach.Web/Components/Pages/Auditor/ReviewResults.razor
- [X] T054 [US1] Create ScoreCard component for overall/category scores in DocCoach.Web/Components/Shared/ScoreCard.razor
- [X] T055 [US1] Create FeedbackList component for issues display in DocCoach.Web/Components/Shared/FeedbackList.razor
- [X] T056 [US1] Create FeedbackItem component with severity icons in DocCoach.Web/Components/Shared/FeedbackItemCard.razor
- [X] T057 [US1] Add category grouping tabs in FeedbackList.razor
- [X] T058 [US1] Add severity filtering in FeedbackList.razor

#### Document History

- [X] T059 [US1] Create DocumentHistory page in DocCoach.Web/Components/Pages/Auditor/DocumentHistory.razor
- [X] T060 [US1] Create DocumentCard component for list items in DocCoach.Web/Components/Shared/DocumentCard.razor
- [X] T061 [US1] Add review status badges in DocumentCard.razor

**Checkpoint**: User Story 1 complete - Upload & Review workflow is functional

---

## Phase 4: User Story 2 - Configure Guidelines (Priority: P2)

**Goal**: As an admin, I can create and manage guideline sets with review rules, and optionally use AI to extract feedback from uploaded guideline documents.

**Independent Test**:
1. Launch app, select Admin role
2. Navigate to Guidelines management
3. Create new guideline set with name/description
4. Add rules manually with category, weight, description
5. Edit and delete rules

### Implementation for User Story 2

#### Admin Navigation

- [ ] T062 [US2] Add Admin section to navigation menu in DocCoach.Web/Components/Layout/NavMenu.razor

#### Guideline Set Management

- [ ] T063 [P] [US2] Create GuidelineSetViewModel in DocCoach.Web/Models/ViewModels/GuidelineSetViewModel.cs
- [ ] T064 [US2] Create Guidelines list page in DocCoach.Web/Components/Pages/Admin/Guidelines.razor
- [ ] T065 [US2] Create GuidelineSetCard component in DocCoach.Web/Components/Shared/GuidelineSetCard.razor
- [ ] T066 [US2] Create GuidelineSetEditor dialog in DocCoach.Web/Components/Dialogs/GuidelineSetEditor.razor
- [ ] T067 [US2] Add create/edit/delete actions for guideline sets in Guidelines.razor
- [ ] T068 [US2] Add default set toggle functionality in GuidelineSetEditor.razor

#### Criteria Management

- [ ] T069 [P] [US2] Create CriterionViewModel in DocCoach.Web/Models/ViewModels/CriterionViewModel.cs
- [ ] T070 [US2] Create GuidelineDetail page in DocCoach.Web/Components/Pages/Admin/GuidelineDetail.razor
- [ ] T071 [US2] Create CriteriaList component in DocCoach.Web/Components/Shared/CriteriaList.razor
- [ ] T072 [US2] Create CriterionEditor dialog in DocCoach.Web/Components/Dialogs/CriterionEditor.razor
- [ ] T073 [US2] Add category selector (Clarity/Completeness/FactualSupport) in CriterionEditor.razor
- [ ] T074 [US2] Add weight slider (1-10) in CriterionEditor.razor
- [ ] T075 [US2] Add reorder criteria functionality in CriteriaList.razor

#### AI Criteria Extraction (Optional Feature)

- [ ] T076 [US2] Create CriteriaExtractor dialog for AI extraction in DocCoach.Web/Components/Dialogs/CriteriaExtractor.razor
- [ ] T077 [US2] Add file upload for guideline document in CriteriaExtractor.razor
- [ ] T078 [US2] Create extracted criteria preview/confirm UI in CriteriaExtractor.razor

**Checkpoint**: User Story 2 complete - Admin can fully manage guidelines and rules

---

## Phase 5: User Story 3 - Quality Score Dashboard (Priority: P3)

**Goal**: As an auditor, I can view a detailed quality score breakdown with progress tracking and score comparison between document versions.

**Independent Test**:
1. Launch app, select Auditor role
2. Review a document (from US1)
3. Navigate to Score Dashboard for that review
4. See category breakdown, trend chart (mock data), comparison view

### Implementation for User Story 3

#### Dashboard Components

- [ ] T079 [P] [US3] Create ScoreBreakdownViewModel in DocCoach.Web/Models/ViewModels/ScoreBreakdownViewModel.cs
- [ ] T080 [US3] Create ScoreDashboard page in DocCoach.Web/Components/Pages/Auditor/ScoreDashboard.razor
- [ ] T081 [US3] Create CategoryScoreChart component using MudBlazor Charts in DocCoach.Web/Components/Shared/CategoryScoreChart.razor
- [ ] T082 [US3] Create ScoreGauge component (circular progress) in DocCoach.Web/Components/Shared/ScoreGauge.razor
- [ ] T083 [US3] Create ScoreTrendChart component in DocCoach.Web/Components/Shared/ScoreTrendChart.razor

#### Score Comparison

- [ ] T084 [P] [US3] Create ReviewComparisonViewModel in DocCoach.Web/Models/ViewModels/ReviewComparisonViewModel.cs
- [ ] T085 [US3] Create ScoreComparison page in DocCoach.Web/Components/Pages/Auditor/ScoreComparison.razor
- [ ] T086 [US3] Create ComparisonChart component using MudBlazor Charts in DocCoach.Web/Components/Shared/ComparisonChart.razor
- [ ] T087 [US3] Add review version selector in ScoreComparison.razor
- [ ] T088 [US3] Add delta indicators (improved/declined) in ComparisonChart.razor

#### Detailed Feedback Navigation

- [ ] T089 [US3] Add click-through from score category to filtered feedback in ScoreDashboard.razor
- [ ] T090 [US3] Add severity summary counts in ScoreDashboard.razor

**Checkpoint**: User Story 3 complete - Score visualization and comparison functional

---

## Phase 6: User Story 4 - Example Documents (Priority: P4)

**Goal**: As an admin, I can upload example compliant documents that serve as reference for reviewers and can be used to enhance AI review accuracy.

**Independent Test**:
1. Launch app, select Admin role
2. Navigate to a guideline set's detail page
3. Upload an example document with description
4. See example listed, download original
5. As reviewer, see example reference during review

### Implementation for User Story 4

#### Example Document Management

- [ ] T091 [P] [US4] Create ExampleDocumentViewModel in DocCoach.Web/Models/ViewModels/ExampleDocumentViewModel.cs
- [ ] T092 [US4] Create ExampleDocuments section in DocCoach.Web/Components/Pages/Admin/GuidelineDetail.razor
- [ ] T093 [US4] Create ExampleDocumentCard component in DocCoach.Web/Components/Shared/ExampleDocumentCard.razor
- [ ] T094 [US4] Create ExampleUploader dialog in DocCoach.Web/Components/Dialogs/ExampleUploader.razor
- [ ] T095 [US4] Add description field for example documents in ExampleUploader.razor
- [ ] T096 [US4] Add download functionality for examples in ExampleDocumentCard.razor

#### Auditor Example Reference

- [ ] T097 [US4] Create ExampleReference panel for review page in DocCoach.Web/Components/Shared/ExampleReference.razor
- [ ] T098 [US4] Add example document list to ReviewResults page in ReviewResults.razor
- [ ] T099 [US4] Add example preview/download in ExampleReference.razor

**Checkpoint**: User Story 4 complete - Example document workflow functional

---

## Phase 7: Azure Integration (Real Services)

**Purpose**: Replace mock services with real Azure implementations

### Storage Service

- [ ] T100 [P] Implement AzureBlobStorageService in DocCoach.Web/Services/Azure/AzureBlobStorageService.cs
- [ ] T101 [P] Implement AzureTableStorageService in DocCoach.Web/Services/Azure/AzureTableStorageService.cs
- [ ] T102 Create service factory for mock/real toggle in DocCoach.Web/Services/ServiceFactory.cs
- [ ] T103 Add storage configuration section to appsettings.json

### Document Service

- [ ] T104 Implement AzureDocumentService in DocCoach.Web/Services/Azure/AzureDocumentService.cs
- [ ] T105 Integrate Table Storage for document metadata in AzureDocumentService.cs
- [ ] T106 Integrate Blob Storage for document files in AzureDocumentService.cs

### Guideline Service

- [ ] T107 Implement AzureGuidelineService in DocCoach.Web/Services/Azure/AzureGuidelineService.cs
- [ ] T108 Implement criteria JSON serialization for Table Storage in AzureGuidelineService.cs

### Review Service with AI

- [ ] T109 Implement AzureReviewService in DocCoach.Web/Services/Azure/AzureReviewService.cs
- [ ] T110 Integrate Microsoft Foundry client in AzureReviewService.cs
- [ ] T111 Build review prompt from guideline criteria in AzureReviewService.cs
- [ ] T112 Parse AI response into FeedbackItems in AzureReviewService.cs
- [ ] T113 Implement score calculation per data-model spec in AzureReviewService.cs

### Service Registration Toggle

- [ ] T114 Add configuration for service provider selection (Mock/Azure) in appsettings.json
- [ ] T115 Update Program.cs to conditionally register mock or Azure services

**Checkpoint**: Azure services ready - can switch from mock to real with config change

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

### Error Handling

- [ ] T116 [P] Create ErrorBoundary component in DocCoach.Web/Components/Shared/ErrorBoundary.razor
- [ ] T117 [P] Create global error page in DocCoach.Web/Components/Pages/Error.razor
- [ ] T118 Add error handling to all service calls in pages

### Loading States

- [ ] T119 [P] Create LoadingOverlay component in DocCoach.Web/Components/Shared/LoadingOverlay.razor
- [ ] T120 Add loading states to all async operations in pages

### Notifications

- [ ] T121 Create notification service in DocCoach.Web/Services/NotificationService.cs
- [ ] T122 Add success/error snackbars to all user actions

### Responsive Design

- [ ] T123 Add mobile-responsive breakpoints to all pages
- [ ] T124 Test and fix MudBlazor component sizing on mobile

### Documentation

- [ ] T125 [P] Update README.md with setup instructions
- [ ] T126 [P] Document configuration options in appsettings.json comments

### Final Validation

- [ ] T127 Run quickstart.md validation scenario end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup) ──────────────────────────────────┐
                                                  │
Phase 2 (Foundational) ───────────────────────────┤
                                                  │
    ┌─────────────────────────────────────────────┤
    │                                             │
    ▼                                             │
Phase 3 (US1: Upload/Review) ─────────────────────┤──► MVP Complete!
    │                                             │
    ├─────────────────────────────────────────────┤
    ▼                                             │
Phase 4 (US2: Guidelines) ────────────────────────┤
    │                                             │
    ├─────────────────────────────────────────────┤
    ▼                                             │
Phase 5 (US3: Score Dashboard) ───────────────────┤
    │                                             │
    ├─────────────────────────────────────────────┤
    ▼                                             │
Phase 6 (US4: Examples) ──────────────────────────┤
    │                                             │
    ├─────────────────────────────────────────────┤
    ▼                                             │
Phase 7 (Azure Integration) ──────────────────────┤
    │                                             │
    ▼                                             │
Phase 8 (Polish) ─────────────────────────────────┘
```

### User Story Dependencies

| Story | Depends On | Can Parallelize With |
|-------|------------|---------------------|
| US1 (Upload/Review) | Phase 2 only | None (first story) |
| US2 (Guidelines) | Phase 2 only | US1 (after Phase 2) |
| US3 (Score Dashboard) | US1 (reviews to display) | US2, US4 |
| US4 (Examples) | US2 (guideline detail page) | US3 |

### Within Each User Story

1. ViewModels before Pages
2. Components before Pages that use them
3. Shared components before specific pages

### Parallel Opportunities by Phase

**Phase 1 (Setup)**:
- T003-T008: All package additions can run in parallel

**Phase 2 (Foundational)**:
- T013-T021: All models can be created in parallel
- T022-T025: All interfaces can be created in parallel
- T026-T028: All table entities can be created in parallel
- T030-T031: MockStorageService and MockGuidelineService in parallel
- T036-T039: All exceptions in parallel

**Phase 3 (US1)**:
- T049-T050: Both text extractors in parallel

**Phase 4 (US2)**:
- T063, T069: ViewModels in parallel

**Phase 5 (US3)**:
- T079, T084: ViewModels in parallel

**Phase 7 (Azure)**:
- T100-T101: Both Azure storage services in parallel

---

## Parallel Example: Phase 2 Foundation

```bash
# Launch all model creation together:
T013 → Document.cs
T014 → Review.cs
T015 → FeedbackItem.cs
T016 → GuidelineSet.cs
T017 → Criterion.cs
T018 → ExampleDocument.cs
T019 → DocumentStatus.cs
T020 → FeedbackSeverity.cs
T021 → FeedbackCategory.cs

# Then launch all interfaces together:
T022 → IStorageService.cs
T023 → IDocumentService.cs
T024 → IReviewService.cs
T025 → IGuidelineService.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T012)
2. Complete Phase 2: Foundational (T013-T040)
3. Complete Phase 3: User Story 1 (T041-T061)
4. **STOP and VALIDATE**: Upload a document, see mock review results
5. Demo/deploy if ready - core value delivered!

### Incremental Delivery

| Milestone | Tasks | Deliverable |
|-----------|-------|-------------|
| **M1: MVP** | T001-T061 | Upload & review with mock AI |
| **M2: Admin** | +T062-T078 | Guideline management |
| **M3: Insights** | +T079-T090 | Score dashboard & comparison |
| **M4: Examples** | +T091-T099 | Reference documents |
| **M5: Production** | +T100-T115 | Real Azure services |
| **M6: Polish** | +T116-T127 | Error handling, UX polish |

### Demo-First Approach (Per Constitution)

All user stories work with mock services first:
- MockStorageService stores files in memory
- MockDocumentService returns sample documents
- MockReviewService returns pre-defined feedback with realistic scores
- MockGuidelineService seeds with "Supreme Audit Office Guidelines 2025"

Phase 7 (Azure Integration) can be deferred or skipped entirely for demo purposes.

---

## Notes

- **[P]** tasks = different files, no dependencies on incomplete tasks
- **[Story]** label maps task to specific user story for traceability
- Each user story should be independently completable with mock services
- No test tasks included per constitution (demo project)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Mock services provide realistic demo experience without Azure dependencies
