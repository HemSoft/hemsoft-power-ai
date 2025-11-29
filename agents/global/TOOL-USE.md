---
title: "Tool Usage Guide"
version: "1.0.3"
lastModified: "2025-11-28"
author: "Franz Hemmer"
purpose: "Guidance on using terminal, MCP servers, and external tools for research and execution."
---

## How to use external tools

Note: If guidance in this document conflicts with your host/system policies or available tool set, host/system policies take precedence. Treat the named tools as examples and adapt to equivalent capabilities in your environment.

## Terminal

You have access to powerful tools from the command line.

### Command Execution Guidelines

When constructing multiple commands to execute on the command line:

- Use `;` for PowerShell to chain commands
- Use `&&` for bash to chain commands (ensures each command succeeds before running the next)
- Prefer commands appropriate for the active shell environment
- Follow host/system policies and environment constraints

### Common Terminal Tools

When you want to find out about or use:

- **Azure** - Use the Azure CLI to find out about resources
- **GitHub** - Use the GitHub MCP server or the GitHub CLI command to work with GitHub
- **Git** - Use standard git commands for version control operations
- **.NET** - Use dotnet CLI for building, testing, and running .NET applications

## MCP Tools

Research precedence (see also `agents/global/RULES-AND-STANDARDS.md`):

1. Prefer topic-specific MCP servers (first‑party/official sources):
    - Microsoft topics → Microsoft Docs MCP server
    - Atlassian/Relias docs → Atlassian MCP server
    - Third‑party SDKs/nugets → Context7 MCP server
2. If no relevant MCP server applies, use generic web fetch capabilities to gather authoritative sources on the open web (recursively as needed), subject to host/system policy.

- If you have tools available to you, prefer to use them when they help you, within system policy constraints.
- If you have access to a memory tool, search it to see if it contains relevant information for this project.
