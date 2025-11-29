---
title: "Testing Guidelines"
version: "1.0.3"
lastModified: "2025-11-28"
author: "Franz Hemmer"
purpose: "Principles and practices for test-driven development and coverage improvement."
---

## Testing - Guidelines for test driven development

- **Mandatory Coverage**: Any new logic or bug fix MUST be accompanied by a corresponding unit test.
- **Test-Driven Development**: Prefer writing failing tests first, then implementing code to make them pass.
- **Verify Changes**: Always run the full test suite (`dotnet test`) before considering a task complete.
- **Improve Coverage**: Strive to improve test coverage with each change. See [COVERAGE.md](./COVERAGE.md) for more information.
- **No Test-Only Code**: Never write methods or sections of production code that only serve testing purposes.
- **Coverage Threshold**: The build enforces a minimum 22% line coverage. Tests will fail if coverage drops below this threshold.

## Finding Coverage Percentages

See the full [Test Coverage Analysis Guide](./COVERAGE.md) for detailed instructions on generating and analyzing coverage reports.
