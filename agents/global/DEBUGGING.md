---
title: "Debugging Guide"
version: "1.0.3"
lastModified: "2025-11-28"
author: "Franz Hemmer"
purpose: "Instructions and best practices for debugging the Agent Demo application."
---

## Debugging Instructions

## General Debugging Principles

### Root Cause Analysis

- When debugging, always try to determine the root cause rather than addressing symptoms
- Debug for as long as needed to identify the root cause and identify a fix
- Revisit your assumptions if unexpected behavior occurs
- Consider the full context: dependencies, configuration, environment, and external services

### Making Changes

- Make code changes only if you have high confidence they can solve the problem
- Test hypotheses systematically rather than making multiple changes at once
- Document your debugging process and findings

## Debugging Techniques

### 1. Console Output Analysis

- Check the console output in the terminal for any errors or issues
- Pay attention to the color-coded logging: INFO (default), WARNING (yellow), ERROR (red), DEBUG (cyan)
- Look for exception stack traces, error messages, and warning indicators
- Review structured event logging for detailed operation tracking

### 2. Inspection and Logging

- Use print statements, logs, or temporary code to inspect program state
- Include descriptive statements or error messages to understand what's happening
- Leverage the existing logging system in `Relias.Assistant.Common` for structured output
- Add temporary debug-level logging to trace execution flow

### 3. Test-Driven Debugging

- Write failing tests that reproduce the issue
- Use tests to isolate the problematic behavior
- Add test statements or functions to test hypotheses
- Ensure tests pass after the fix is implemented

### 4. Incremental Verification

- After making changes, verify with `dotnet build` to check for compilation errors
- Run relevant tests with `dotnet test` to ensure no regressions
- Test one component at a time when possible

## Application-Specific Debugging

### API/OpenRouter Issues

- Verify `OPENROUTER_API_KEY` environment variable is set
- Check API response errors in console output
- Review timeout settings (default 2 minutes)
- Test with a simple prompt to verify connectivity

### Tool Function Issues

- Check that tools are registered correctly in `ChatOptions.Tools`
- Verify tool descriptions are clear for the AI model
- Review tool return values and error handling
- Test tools directly in unit tests before using with AI

### File Tool Issues

- Verify file paths are valid and accessible
- Check for permission issues on file system operations
- Test with known-good directory paths
- Review error messages returned from tool functions

### Configuration Issues

- Check `OPENROUTER_API_KEY` environment variable is set
- Optionally verify `OPENROUTER_BASE_URL` if using custom endpoint
- Review startup error messages for clear guidance

### Console Output Issues

- Check Spectre.Console output formatting
- Review error panels for exception details
- Use status spinners to track long-running operations

## Iterating Over Issues

### Incremental Fixes

1. Identify the specific issue from console output or test failures
2. Address **one issue at a time**
3. Return control to the user to run the app and verify the fix
4. Do not attempt to fix multiple unrelated issues in one iteration

### Running the Application

- Run via `dotnet run --project src/AgentDemo.Console` or use `run.ps1` script
- The application is interactive - type prompts and 'exit' to quit
- Check console output for error panels and status updates

### CI/CD Debugging

- Review GitHub Actions workflow output for build/test failures
- Check for environment-specific issues (dependencies, runtime versions)
- Verify environment variables are set in CI environment

## Common Issues and Solutions

### Build Failures

- Run `dotnet build AgentDemo.sln` to identify compilation errors
- Check for missing using statements or namespace issues
- Verify all project references are correct
- Ensure NuGet packages are restored

### Test Failures

- Run `dotnet test --logger "console;verbosity=minimal"` for focused output
- Review test assertion messages for expected vs actual values
- Check for environment-specific test failures
- Verify mock setups are correct for isolated unit tests

### Runtime Exceptions

- Check for null reference exceptions (uninitialized services or configuration)
- Review exception stack traces to identify the originating component
- Verify dependency injection is configured correctly in `Program.cs`
- Check for unhandled exceptions in async operations

### Performance Issues

- Review telemetry timing data for slow operations
- Check for blocking calls that should be async
- Look for excessive logging or telemetry overhead
- Consider connection pooling and caching for external services

## Tools and Commands

### Build and Test

```bash
# Build the solution
dotnet build AgentDemo.sln

# Run all tests
dotnet test AgentDemo.sln

# Run tests with minimal output
dotnet test --logger "console;verbosity=minimal"

# Run specific test
dotnet test --filter "FullyQualifiedName~Namespace.Class.MethodName"
```

### Docker Debugging

```bash
# Build and run with docker-compose
docker-compose up --build

# View container logs
docker logs <container-name>

# Execute commands in running container
docker exec -it <container-name> /bin/bash
```

### Git for Context

```bash
# See recent changes
git log --oneline -10

# View changes in a file
git log -p <file-path>

# Check current branch status
git status
```

## Prevention and Best Practices

- Write tests before implementing fixes (TDD approach)
- Use descriptive error messages and logging
- Validate configuration early at startup
- Fail fast for critical errors, gracefully degrade for optional features
- Document assumptions and edge cases
- Keep methods small and testable (single concern per method)
- Use dependency injection for testability and isolation
