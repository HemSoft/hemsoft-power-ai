---
title: "Testing Guidelines"
version: "1.0.5"
lastModified: "2025-11-30"
author: "Franz Hemmer"
purpose: "Principles and practices for test-driven development and coverage improvement."
---

## Testing - Guidelines for test driven development

- **Mandatory Coverage**: Any new logic or bug fix MUST be accompanied by a corresponding unit test.
- **Test-Driven Development**: Prefer writing failing tests first, then implementing code to make them pass.
- **Verify Changes**: Always run the full test suite (`dotnet test`) before considering a task complete.
- **Improve Coverage**: Strive to improve test coverage with each change. See [COVERAGE.md](./COVERAGE.md) for more information.
- **No Test-Only Code**: Never write methods or sections of production code that only serve testing purposes.
- **Coverage Threshold**: The build enforces a minimum 90% line coverage. Tests will fail if coverage drops below this threshold.
- **No Coverage Exclusions**: You are **strictly prohibited** from adding files to `ExcludeByFile` in test project coverage settings to bypass coverage thresholds. You **MUST** write proper unit tests for all new code. If you add new code, you must add tests - not exclusions.

## Current Coverage Exclusions (Technical Debt)

The following files are currently excluded from coverage due to heavy external dependencies (Graph API, console I/O, HTTP clients). These represent technical debt that should be addressed by introducing abstractions and mocking.

| File | Reason for Exclusion | Improvement Strategy |
|------|---------------------|---------------------|
| `Program.cs` | Entry point with DI setup, console loops | Extract testable orchestration logic |
| `WebSearchTools.cs` | HTTP client calls to external APIs | Inject `IHttpClientFactory`, mock responses |
| `OutlookMailTools.cs` | Microsoft Graph API calls | Inject `IGraphServiceClient` interface, mock |
| `SpamFilterAgent.cs` | Console I/O, Graph API, AI chat client | Extract business logic, inject dependencies |
| `SpamFilterTools.cs` | Graph API operations | Abstract Graph client, add interface |
| `SpamCleanupAgent.cs` | Graph API, device code auth flow | Abstract auth and Graph client |
| `SpamReviewAgent.cs` | Console prompts (Spectre.Console) | Extract logic from UI, test via reflection |
| `SpamScanAgent.cs` | AI client, console progress | Abstract AI client, extract scan logic |
| `SpamScanTools.cs` | Combines multiple external dependencies | Decompose into smaller testable units |

**Goal**: Incrementally reduce this list by introducing proper abstractions (interfaces) and using mocking frameworks like `Moq` or `NSubstitute`.

## Finding Coverage Percentages

See the full [Test Coverage Analysis Guide](./COVERAGE.md) for detailed instructions on generating and analyzing coverage reports.
