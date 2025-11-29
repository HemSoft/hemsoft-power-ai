# MCP Tool Guidelines

## Overview

This project exposes AI agent tools using `Microsoft.Extensions.AI` with `AIFunctionFactory.Create()`. Tools are static methods decorated with `[Description]` attributes that the AI model can invoke.

## Design Philosophy

**Fewer tools, more power.** Each tool should be flexible and capable rather than narrow and single-purpose. Aim to reduce tool count while increasing tool capability.

- Prefer one versatile tool over multiple specialized ones
- Use parameters to control behavior instead of creating separate tools
- Combine related operations into a single tool when logical
- Every new tool must justify its existence - can an existing tool be extended instead?

## Tool Implementation Pattern

```csharp
[Description("Brief description of what the tool does")]
public static ReturnType ToolName(ParamType paramName)
{
    // Log invocation for observability
    Console.WriteLine($"[Tool] ToolName: {paramName}");

    // Implementation
}
```

## Requirements

1. **Static Methods**: Tools must be `public static` methods in a static class
2. **Description Attribute**: Every tool requires `[Description("...")]` for the AI to understand its purpose
3. **Simple Return Types**: Return primitives, strings, or arrays - avoid complex objects
4. **Error Handling**: Return error messages as strings rather than throwing exceptions
5. **Logging**: Log tool invocations with `[Tool] MethodName: params` format

## Tool Registration

Register tools in `Program.cs`:

```csharp
var tools = new ChatOptions
{
    Tools =
    [
        AIFunctionFactory.Create(ToolClass.MethodName),
    ],
};
```

## Best Practices

- Keep tools focused on a single responsibility
- Limit output size (e.g., `Take(100)` for file listings)
- Provide clear, actionable error messages
- Use descriptive parameter names - the AI sees them
