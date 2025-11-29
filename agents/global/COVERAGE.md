---
title: "Test Coverage Analysis Guide"
version: "1.0.4"
lastModified: "2025-11-28"
author: "Franz Hemmer"
purpose: "How to generate, analyze, and interpret code coverage using Coverlet and ReportGenerator."
---

This guide explains how to analyze test coverage for the Agent Demo project.

## Coverage Threshold Enforcement

The project enforces a **minimum 90% line coverage**. Tests will fail if coverage drops below this threshold.

This is configured in the test project via Coverlet:

```xml
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <Threshold>90</Threshold>
  <ThresholdType>line</ThresholdType>
  <ThresholdStat>total</ThresholdStat>
</PropertyGroup>
```

To adjust the threshold, update the `Threshold` value in `AgentDemo.Console.Tests.csproj`.

## Overview

The project uses xUnit for testing with Coverlet for coverage analysis:

- `tests/AgentDemo.Console.Tests/` - Unit tests for all components
- `coverlet.collector` package for coverage collection

## Quick Start

### 1. Run Tests with Coverage

```bash
dotnet test AgentDemo.sln --collect:"XPlat Code Coverage" --results-directory:TestResults
```

### 2. Generate HTML Report

```bash
# Install ReportGenerator (one-time setup)
dotnet tool install --global dotnet-reportgenerator-globaltool

# Generate HTML coverage report
reportgenerator -reports:"TestResults\**\coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:"Html"
```

### 3. View Results

Open `CoverageReport\index.html` in your browser to see detailed coverage information.

## Project Structure

### Test Projects

- **AgentDemo.Console.Tests**: Main unit test project
  - Uses xUnit testing framework
  - Includes Moq for mocking
  - Has coverlet.collector for built-in coverage

### Source Projects Covered

- **AgentDemo.Console**: Console application with AI agent and tools

## Coverage Report Types

### Cobertura XML (Default)

- Generated automatically with `--collect:"XPlat Code Coverage"`
- Located in `TestResults/*/coverage.cobertura.xml`
- Can be converted to HTML using ReportGenerator

### HTML Reports

- Human-readable coverage reports
- Show line-by-line coverage
- Include summary statistics

## Common Commands Reference

### Test Execution

```bash
# Run all tests
dotnet test AgentDemo.sln

# Run tests with minimal logging
dotnet test AgentDemo.sln --logger "console;verbosity=minimal"

# Run specific test project
dotnet test tests/AgentDemo.Console.Tests/AgentDemo.Console.Tests.csproj
```

### Coverage Analysis

```bash
# Basic coverage with coverlet
dotnet test --collect:"XPlat Code Coverage"

# Coverage with specific output directory
dotnet test --collect:"XPlat Code Coverage" --results-directory:TestResults
```

### Report Generation

```bash
# HTML report with ReportGenerator
reportgenerator -reports:"TestResults\**\coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:"Html"

# Multiple report types
reportgenerator -reports:"TestResults\**\coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:"Html;JsonSummary;Badges"
```

## Troubleshooting

### Coverage Not Showing

- Verify test projects have `<IsTestProject>true</IsTestProject>` in their .csproj
- Check that coverlet.collector package is referenced in test projects
- Ensure source projects are referenced by test projects

### Missing Reports

- Check TestResults directory exists and contains coverage files
- Verify ReportGenerator is installed: `reportgenerator --help`
- Ensure file paths use correct separators for your OS

## Extracting Coverage Percentages

From Cobertura XML files, the `line-rate` and `branch-rate` attributes show coverage:

- `line-rate="0.21"` = 21% line coverage
- `branch-rate="0.14"` = 14% branch coverage

From HTML reports, open `CoverageReport/index.html` - percentages are displayed on the summary page.
