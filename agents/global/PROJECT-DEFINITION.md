---
title: "Project Definition"
version: "1.0.3"
lastModified: "2025-11-29"
author: "Franz Hemmer"
purpose: "High-level mission, architecture, extension points, and operational model of Agent Demo."
---

## PROJECT DEFINITION - Agent Demo

Purpose-built reference guide for future automation and contributors. Complements (does not duplicate) existing agent docs. This file focuses on: domain intent, high-level architecture, extension seams, and operational model.

---

## TL;DR - Quick Reference

**What:** Console-based AI agent demo showcasing Microsoft.Extensions.AI with tool-calling capabilities via OpenRouter.

**Core Stack:** .NET 10, Microsoft.Extensions.AI, OpenAI SDK (via OpenRouter), Spectre.Console, Microsoft Graph SDK

**Key Features:** Terminal commands, web search, Outlook mail integration (inbox, read, send, search, delete, move)

**Architecture:** Simple console app with tool registration via AIFunctionFactory

**Extension Pattern:** Add tool function with `[Description]` attribute, register in ChatOptions.Tools

**Testing:** xUnit + Moq; test coverage via coverlet/ReportGenerator; see `agents/global/TESTING.md`

**Required Config:** `OPENROUTER_API_KEY`, `OPENROUTER_BASE_URL`, optional `GRAPH_CLIENT_ID` for mail

---

## 1. Problem Statement and Mission

Agent Demo is a demonstration project showcasing how to build AI agents with Microsoft.Extensions.AI and function-calling capabilities. It serves as a learning resource and template for building agentic applications.

Primary objectives:

- Demonstrate Microsoft.Extensions.AI integration patterns
- Show how to implement tool-calling agents with function invocation
- Provide a simple, testable architecture for AI agent development
- Serve as a reference for OpenRouter/OpenAI API integration

---

## 2. Core Architectural Overview

High-level components:

| Layer | Project | Role |
|-------|---------|------|
| Entry / Runtime | `HemSoft.PowerAI.Console` | Console application with interactive chat loop, tool registration, and AI client setup |
| Tools | `HemSoft.PowerAI.Console/Tools` | File system tools exposed to the AI agent |
| Tests | `HemSoft.PowerAI.Console.Tests` | Unit tests for tool functions |

Execution flow (happy path):

1. Console host starts and validates `OPENROUTER_API_KEY` environment variable
2. OpenAI client created pointing to OpenRouter API
3. Chat client built with function invocation support via `.UseFunctionInvocation()`
4. Tools registered via `AIFunctionFactory.Create()` for file operations
5. Interactive chat loop accepts user input
6. AI model processes requests and invokes tools as needed
7. Responses displayed using Spectre.Console formatting

---

## 3. Logging and Error Handling

The application uses Spectre.Console for user-facing output:

- Informational panels with formatted headers
- Error panels with clear error messages
- Status spinners during API calls

Error handling:

- Configuration errors display clear instructions
- Network errors are caught and displayed gracefully
- Request timeouts (2 minutes) prevent hanging

---

## 4. Configuration and Runtime Model

Configuration via environment variables:

| Variable | Required | Description |
|----------|----------|-------------|
| `OPENROUTER_API_KEY` | Yes | OpenRouter API key for LLM access |
| `OPENROUTER_BASE_URL` | Yes | OpenRouter endpoint URL |
| `GRAPH_CLIENT_ID` | For Mail | Azure app registration client ID |
| `GRAPH_TENANT_ID` | No | Defaults to "consumers" for personal accounts |

Operational characteristics:

- Interactive console application
- Type 'exit' to quit
- Async tool invocation with cancellation support

---

## 5. Extension Points

Add a new tool:

1. Create a static method with `[Description]` attribute in `Tools/` folder
2. Use clear parameter names and descriptions
3. Register via `AIFunctionFactory.Create()` in `ChatOptions.Tools`
4. Add unit tests for the tool function

Example tool pattern:

```csharp
[Description("Description of what the tool does")]
public static string MyTool(
    [Description("Parameter description")] string param)
{
    // Implementation
    return result;
}
```

---

## 6. Error Handling Principles

- Validate configuration early and fail fast with clear messages
- Catch network and timeout errors gracefully
- Display user-friendly error messages via Spectre.Console panels

---

## 7. Testing Strategy (Augments TESTING.md)

Test patterns:

- Unit tests for tool functions in `HemSoft.PowerAI.Console.Tests`
- Test success paths, error paths, and edge cases
- Use xUnit with Moq for mocking when needed

When adding tools:

- Test normal operation with valid inputs
- Test edge cases (empty inputs, invalid paths, etc.)
- Test error handling and exception scenarios

---

## 8. Performance Considerations

- API requests have 2-minute timeout
- Async/await used throughout for non-blocking I/O
- Chat history maintained in memory for conversation context

---

## 9. Security and Data Hygiene

- API key read from environment variable, never hardcoded
- No sensitive data logged to console
- File tools operate on local filesystem only

---

## 10. Assumptions

- Demo/learning project, not production deployment
- Single user, local execution
- OpenRouter provides LLM access
- File operations limited to accessible local paths

---

## 11. Quick Reference - Adding a Tool (Checklist)

- [ ] Static method with `[Description]` attribute
- [ ] Clear parameter descriptions
- [ ] Registered in `ChatOptions.Tools` via `AIFunctionFactory.Create()`
- [ ] Unit tests for success and error paths
- [ ] Friendly error handling

---

## 12. When NOT to Extend

Avoid adding tools that:

- Duplicate existing functionality
- Require persistent state management
- Need external service dependencies (keep it simple for demo purposes)

---

## 13. Glossary (Project-Specific)

- **Tool Function**: A callable operation exposed to the LLM via `AIFunctionFactory`
- **OpenRouter**: API gateway providing access to multiple LLM providers
- **Function Invocation**: Microsoft.Extensions.AI middleware that handles tool calls
- **IChatClient**: Abstract interface for chat completions

---

## 14. Evolution Notes

This project serves as a learning demo for Microsoft.Extensions.AI and agentic patterns. Keep it simple and focused on demonstrating core concepts.

---

## 15. Meta

This document provides orientation for contributors working on the Agent Demo project.

Update cadence: revise when architecture changes significantly.
