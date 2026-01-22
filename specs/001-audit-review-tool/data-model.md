# Data Model: Automated Audit Report Review Tool

**Feature**: 001-audit-review-tool  
**Date**: 2026-01-05

## Entity Relationship Overview

```
┌─────────────────┐       ┌─────────────────┐
│  GuidelineSet   │───────│    Rules    │
│                 │ 1   * │                 │
└────────┬────────┘       └─────────────────┘
         │ 1
         │
         │ *
┌────────┴────────┐       ┌─────────────────┐
│ ExampleDocument │       │    Document     │
│                 │       │                 │
└─────────────────┘       └────────┬────────┘
                                   │ 1
                                   │
                                   │ 1
                          ┌────────┴────────┐       ┌─────────────────┐
                          │     Review      │───────│  FeedbackItem   │
                          │                 │ 1   * │                 │
                          └─────────────────┘       └─────────────────┘
```

## Entity Definitions

### Document

An uploaded audit report awaiting or having completed review.

| Attribute | Type | Description |
|-----------|------|-------------|
| `Id` | string (GUID) | Unique identifier |
| `FileName` | string | Original uploaded filename |
| `FileSize` | long | Size in bytes |
| `ContentType` | string | MIME type (application/pdf, application/vnd.openxmlformats-officedocument.wordprocessingml.document) |
| `StoragePath` | string | Path/URI to stored file (blob or local) |
| `UploadedAt` | DateTimeOffset | When document was uploaded |
| `UploadedBy` | string | Role that uploaded (for demo: "auditor") |
| `GuidelineSetId` | string | Selected guideline set for review |
| `Status` | DocumentStatus | Processing status enum |
| `ExtractedText` | string? | Cached extracted text content (nullable until processed) |

**DocumentStatus Enum**:
- `Uploaded` - File received, not yet processed
- `Extracting` - Text extraction in progress
- `Reviewing` - AI analysis in progress
- `Completed` - Review finished successfully
- `Failed` - Processing error occurred

---

### Review

The result of AI analysis on a document.

| Attribute | Type | Description |
|-----------|------|-------------|
| `Id` | string (GUID) | Unique identifier |
| `DocumentId` | string | Reference to reviewed document |
| `GuidelineSetId` | string | Guideline set used for this review |
| `CreatedAt` | DateTimeOffset | When review completed |
| `OverallScore` | int | Quality score 0-100 |
| `CategoryScores` | Dictionary<string, int> | Scores per category (clarity, completeness, factualSupport) |
| `FeedbackItems` | List<FeedbackItem> | Collection of feedback (embedded) |
| `ProcessingTimeMs` | long | How long AI analysis took |
| `ModelUsed` | string | AI model identifier (e.g., "gpt-4o") |

---

### FeedbackItem

A single piece of feedback within a review.

| Attribute | Type | Description |
|-----------|------|-------------|
| `Id` | string (GUID) | Unique identifier |
| `Category` | FeedbackCategory | Type of feedback |
| `Severity` | FeedbackSeverity | How critical the issue is |
| `Title` | string | Brief summary (≤50 chars) |
| `Description` | string | Detailed explanation |
| `Suggestion` | string? | Recommended improvement (optional) |
| `Location` | DocumentLocation | Where in document this applies |

**FeedbackCategory Enum**:
- `Clarity` - Language simplicity, readability issues
- `Completeness` - Missing sections, incomplete content
- `FactualSupport` - Claims lacking evidence, unsupported statements

**FeedbackSeverity Enum**:
- `Info` - Minor suggestion, low impact
- `Warning` - Should be addressed, moderate impact
- `Error` - Must be fixed, high impact

**DocumentLocation** (value object):
| Attribute | Type | Description |
|-----------|------|-------------|
| `Page` | int? | Page number (1-based, null if unknown) |
| `Section` | string? | Section identifier (e.g., "3.2") |
| `Excerpt` | string? | Brief text excerpt for context |

---

### GuidelineSet

A configured collection of review criteria.

| Attribute | Type | Description |
|-----------|------|-------------|
| `Id` | string (GUID) | Unique identifier |
| `Name` | string | Display name (e.g., "Supreme Audit Office 2025") |
| `Description` | string? | Optional description |
| `SourceDocumentPath` | string? | Path to uploaded guideline document |
| `CreatedAt` | DateTimeOffset | When created |
| `UpdatedAt` | DateTimeOffset | Last modification |
| `IsActive` | bool | Whether available for selection |
| `IsDefault` | bool | Whether selected by default for new reviews |
| `Criteria` | List<Criterion> | Extracted review rules (embedded) |

---

### Criterion

A single review rule within a guideline set.

| Attribute | Type | Description |
|-----------|------|-------------|
| `Id` | string (GUID) | Unique identifier |
| `Category` | FeedbackCategory | Which feedback category this applies to |
| `Name` | string | Short name (e.g., "Active Voice") |
| `Description` | string | Full description of the rule |
| `Weight` | int | Importance 1-10 (affects score calculation) |
| `Examples` | List<string> | Example violations or good practices |
| `IsEnabled` | bool | Whether to apply this criterion |

---

### ExampleDocument

A reference document demonstrating compliance.

| Attribute | Type | Description |
|-----------|------|-------------|
| `Id` | string (GUID) | Unique identifier |
| `GuidelineSetId` | string | Parent guideline set |
| `FileName` | string | Original filename |
| `StoragePath` | string | Path/URI to stored file |
| `UploadedAt` | DateTimeOffset | When uploaded |
| `Description` | string? | What this example demonstrates |
| `ExtractedText` | string? | Cached extracted text |

---

## Application State Entities

### AppState (Runtime Only)

Session state for the current user.

| Attribute | Type | Description |
|-----------|------|-------------|
| `CurrentRole` | UserRole | Selected role (Auditor/Admin) |
| `SelectedGuidelineSetId` | string? | Currently selected guidelines |
| `Theme` | string | UI theme preference |

**UserRole Enum**:
- `Auditor` - Can upload documents, view reviews
- `Admin` - Can configure guidelines, upload examples

### ReviewSession (Runtime Only)

State for an active review workflow.

| Attribute | Type | Description |
|-----------|------|-------------|
| `CurrentDocumentId` | string? | Document being processed |
| `ProcessingStatus` | DocumentStatus | Current status |
| `ProgressPercent` | int | 0-100 progress indicator |
| `CurrentReview` | Review? | Completed review result |

---

## Storage Mapping

### Local Development (Azurite)

Azurite emulates both Blob and Table Storage locally. Connection string: `UseDevelopmentStorage=true`

**Blob Containers**:
- `documents` - Uploaded audit reports
- `examples` - Example compliant documents  
- `guidelines` - Uploaded guideline source documents

**Table Storage Tables**:
- `documents` - Document metadata
- `reviews` - Review results with embedded feedback
- `guidelines` - GuidelineSet with embedded Criteria

### Azure Mode

| Entity | Storage | Table/Container | Partition Key | Row Key |
|--------|---------|-----------------|---------------|----------|
| Document (file) | Blob Storage | `documents` | N/A | N/A |
| Document (metadata) | Table Storage | `documents` | `"doc"` | `Id` |
| Review | Table Storage | `reviews` | `DocumentId` | `Id` |
| GuidelineSet | Table Storage | `guidelines` | `"guideline"` | `Id` |
| ExampleDocument (file) | Blob Storage | `examples` | N/A | N/A |
| ExampleDocument (metadata) | Table Storage | `guidelines` | `GuidelineSetId` | `Id` |

**Table Storage Design Notes**:
- Complex properties (FeedbackItems, Criteria, CategoryScores) serialized as JSON strings
- Partition keys designed for common query patterns
- Reviews partitioned by DocumentId for efficient document-scoped queries
- Guidelines use static partition key (few items, simple queries)

---

## Validation Rules

### Document
- `FileName`: Required, max 255 characters
- `FileSize`: Max 50MB (52,428,800 bytes)
- `ContentType`: Must be PDF or DOCX MIME type

### GuidelineSet
- `Name`: Required, max 100 characters, unique
- `Criteria`: At least 1 criterion required when active

### Criterion
- `Name`: Required, max 50 characters
- `Weight`: 1-10 inclusive
- `Description`: Required, max 500 characters

### FeedbackItem
- `Title`: Required, max 100 characters
- `Description`: Required, max 1000 characters
- `Severity`: Required

---

## Sample Data

### Sample GuidelineSet (Mock)

```json
{
  "id": "sao-2025",
  "name": "Supreme Audit Office Guidelines 2025",
  "description": "Standard review criteria for SAO audit reports",
  "isActive": true,
  "isDefault": true,
  "criteria": [
    {
      "id": "crit-001",
      "category": "Clarity",
      "name": "Simple Language",
      "description": "Reports must use clear, simple language accessible to non-experts",
      "weight": 8,
      "isEnabled": true
    },
    {
      "id": "crit-002",
      "category": "Completeness",
      "name": "Executive Summary",
      "description": "Reports must include an executive summary with key findings",
      "weight": 9,
      "isEnabled": true
    },
    {
      "id": "crit-003",
      "category": "FactualSupport",
      "name": "Evidence Citation",
      "description": "All findings must reference supporting evidence or data sources",
      "weight": 10,
      "isEnabled": true
    }
  ]
}
```

### Sample Review (Mock)

```json
{
  "id": "rev-001",
  "documentId": "doc-001",
  "guidelineSetId": "sao-2025",
  "overallScore": 72,
  "categoryScores": {
    "Clarity": 85,
    "Completeness": 65,
    "FactualSupport": 68
  },
  "feedbackItems": [
    {
      "id": "fb-001",
      "category": "Completeness",
      "severity": "Warning",
      "title": "Methodology section incomplete",
      "description": "Section 3.2 'Methodology' lacks detail on the sampling approach used for data collection.",
      "suggestion": "Add a paragraph describing sample size, selection criteria, and any limitations.",
      "location": { "page": 5, "section": "3.2" }
    },
    {
      "id": "fb-002",
      "category": "FactualSupport",
      "severity": "Error",
      "title": "Unsupported claim",
      "description": "The statement 'costs increased significantly' on page 8 lacks quantitative data.",
      "suggestion": "Include specific figures or percentage changes with source references.",
      "location": { "page": 8, "section": "4.1", "excerpt": "costs increased significantly" }
    }
  ]
}
```
