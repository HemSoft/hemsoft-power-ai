---
title: "AGENTS.md"
version: "1.0.19"
lastModified: "2025-12-16"
author: "Franz Hemmer"
purpose: "Instructions for AI, LLM's, tools and agents."
---

## AGENTS.md -- Instructions for AI LLM's, Agents and Tools

## Follow instructions contextually according to your task at hand

## Policy precedence and tool-agnostic use

- These repository instructions are guidance. If any instruction here conflicts with your host's system policies, platform rules, compliance constraints, or safety guardrails, those host/system policies take precedence.
- Treat specific tool names and agent capabilities referenced here as examples. Use the closest equivalent tools and capabilities available in your runtime environment.
- When adapting due to a policy conflict, proceed with the spirit of these instructions, note 1–2 brief assumptions if needed, and avoid blocking unless the conflict is truly blocking.

## Critical Constraints

- **Configuration Files**: You are **strictly prohibited** from modifying rules in `.markdownlint.json`, `.editorconfig`, `SonarLint.xml`, or any other configuration file to resolve warnings or errors without explicit user consent. You must fix the underlying code or content issues.
- **Markdown Lint Exemptions**: You are **strictly prohibited** from using `<!-- markdownlint-disable ... -->` comments to suppress markdown linting warnings. You must fix the content to comply with the rules.
- **Versioning**: For any file containing version metadata (e.g., YAML frontmatter with `version` and `lastModified`), you **MUST** increment the version number and update the `lastModified` date whenever you modify the file's content.
- **Hardcoded URIs**: You are **strictly prohibited** from using `#pragma warning disable S1075` to suppress warnings about hardcoded URIs. You must properly configure URIs in configuration files or environment variables.
- **StyleCop Exemptions**: You are **strictly prohibited** from adding exemptions for StyleCop rules (e.g., `dotnet_diagnostic.SAxxxx.severity = none`) in `.editorconfig`. This includes but is not limited to `SA1633` (File Headers) and `SA1101` (Prefix local calls with this). You must fix the code to comply with the rules.
- **No Suppressions or Exclusions**: You are **strictly prohibited** from suppressing, disabling, or excluding ANY analyzer rule, warning, or error via any mechanism including but not limited to: `#pragma warning disable`, `[SuppressMessage]`, `[ExcludeFromCodeCoverage]`, `.editorconfig` severity changes, or any other suppression technique. You must fix the actual code to resolve the issue.
- **No Coverage Exclusions**: You are **strictly prohibited** from adding files to `ExcludeByFile` in test project coverage settings to bypass coverage thresholds. You **MUST** write proper unit tests for all new code. The only files that may be excluded are entry points with external dependencies that cannot be reasonably unit tested (e.g., Program.cs). If you add new code, you must add tests - not exclusions.
- **Tool Consolidation**: When adding new AI agent tool functionality, you **MUST** first check if an existing tool can be extended with a new `mode` parameter rather than creating a separate tool. Follow the "fewer tools, more power" principle from [MCP-GUIDELINES.md](./agents/global/MCP-GUIDELINES.md). For example, spam registry operations belong in the Mail tool as `spamlist`, `spamadd`, `spamcheck` modes—not as separate tools.

## CRITICAL: Strict Build Policy

**You MUST follow the strict build and validation workflow defined in [WORKFLOW.md](./agents/global/WORKFLOW.md#8-build--validation-workflow-critical).**

- The build is configured to treat **ALL warnings as errors**.
- Markdown linting is **mandatory** and runs as part of the build.
- **Outdated NuGet packages are a build failure**. You must update all packages before considering the build successful.
- You must fix issues incrementally and verify frequently.
- **DO NOT** ignore build warnings; they are errors in this project.
- **A build with warnings is NOT a successful build.** You must resolve all warnings.

## CRITICAL: The Golden Rule of Changes (Build & Test)

**You are strictly prohibited from considering a task complete until you have successfully executed the following loop:**

1. **Make Changes**: Implement your code, configuration, or documentation changes.
2. **Build**: Run `dotnet build` immediately. You **MUST** resolve all errors and warnings (including Markdown linting).
3. **Test**: Run `dotnet test`.
    - If you modified logic, you **MUST** add or update unit tests to cover that logic.
    - **ALL** tests must pass.
4. **Verify**: Ensure that your changes actually fixed the issue or implemented the feature as requested.

**NEVER** submit code or end your turn without verifying that the project builds and tests pass.

## CRITICAL: Proactive Learning & Self-Correction

**You MUST learn from your mistakes.**

- If a build fails or tests fail, do not just fix it and move on. **Analyze WHY it failed.**
- Did you miss a dependency? Did you misunderstand a pattern?
- If the failure reveals a gap in your knowledge or these instructions, **YOU MUST UPDATE** the relevant instruction file (e.g., `AGENTS.md`, `WORKFLOW.md`, `CODE-STANDARDS.md`) or add a memory entry to prevent future agents from making the same mistake.
- **Do not repeat the same error twice.**

## CRITICAL: Proactively Load Referenced Instructions into Context

**You MUST read the instruction files listed below into your context window at the start of any task.** These files contain essential guidance that will:

- Provide coding standards, testing requirements, and security practices
- Define workflows and debugging procedures specific to this repository
- Prevent common mistakes and ensure consistency with project standards
- Save time by answering questions before you need to ask them

**When to load these files:**

- **At the beginning of any new conversation or task** - Load all relevant files proactively
- **When starting any implementation work** - Read CODE-STANDARDS.md, TESTING.md, and WORKFLOW.md at minimum
- **When debugging or troubleshooting** - Read DEBUGGING.md and TOOL-USE.md
- **When working with tests or coverage** - Read TESTING.md and COVERAGE.md
- **When making commits or managing branches** - Read GIT-WORKFLOW.md
- **When handling security-sensitive code** - Read SECURITY.md
- **When uncertain about project architecture** - Read PROJECT-DEFINITION.md

**Do not wait to be explicitly told to read these files.** They are your primary reference documentation and should be in your context for nearly all operations in this repository.

Read all the instruction files below and understand and follow them:

## Global Instructions

| File | Purpose |
|------|---------|
| [ARCHITECTURE.md](./agents/global/ARCHITECTURE.md) | System architecture, component design, and event-driven patterns |
| [CODE-QUALITY.md](./agents/global/CODE-QUALITY.md) | Code quality standards and build enforcement |
| [CODE-STANDARDS.md](./agents/global/CODE-STANDARDS.md) | C# coding standards to adhere to |
| [COVERAGE.md](./agents/global/COVERAGE.md) | Test coverage analysis and threshold enforcement |
| [DEBUGGING.md](./agents/global/DEBUGGING.md) | Comprehensive debugging tips and procedures for diagnosing issues |
| [GIT-WORKFLOW.md](./agents/global/GIT-WORKFLOW.md) | Version control standards, branching strategy, and commit conventions |
| [MCP-GUIDELINES.md](./agents/global/MCP-GUIDELINES.md) | MCP tool implementation patterns and best practices |
| [MEMORY-INSTRUCTIONS.md](./agents/global/MEMORY-INSTRUCTIONS.md) | Persistent memory implementation using version-controlled knowledge graphs |
| [PROJECT-DEFINITION.md](./agents/global/PROJECT-DEFINITION.md) | High-level project mission, architecture, and extension points |
| [RULES-AND-STANDARDS.md](./agents/global/RULES-AND-STANDARDS.md) | General standards and guardrails to follow |
| [SECURITY.md](./agents/global/SECURITY.md) | Security practices, secret management, and data protection |
| [TESTING.md](./agents/global/TESTING.md) | Guidance for writing and running tests |
| [TOOL-USE.md](./agents/global/TOOL-USE.md) | Guidance on using the terminal, MCP servers, and external tools |
| [WORKFLOW.md](./agents/global/WORKFLOW.md) | Step-by-step operational workflow for problem solving and implementation |

## Research & Reference Material

| File | Purpose |
|------|---------|
| [REFERENCE-MATERIAL.md](./research-material/REFERENCE-MATERIAL.md) | MS Agent Framework patterns, A2A protocol, multi-agent orchestration examples |
| [PLAN.md](./PLAN.md) | Current migration plan and implementation status |
