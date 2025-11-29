---
title: "Execution Workflow"
version: "1.0.6"
lastModified: "2025-11-28"
author: "Franz Hemmer"
purpose: "Step-by-step operational workflow for problem solving, research, implementation, and validation."
---

## Workflow - The workflow you should follow

1. **Check Memory**: Search `/agents/memories` for relevant context about the user's request (projects, decisions, concepts, people). Use this context to inform your work.
2. Fetch any URL's provided by the user using available web fetch capabilities.
3. Understand the problem deeply. Carefully read the issue and think critically about what is required. Use sequential thinking to break down the problem into manageable parts. Consider the following:
   - What is the expected behavior?
   - What are the edge cases?
   - What are the potential pitfalls?
   - How does this fit into the larger context of the codebase?
   - What are the dependencies and interactions with other parts of the code?
4. Investigate the codebase. Explore relevant files, search for key functions, and gather context.
5. Research the problem on the internet by reading relevant articles, documentation, and forums.
6. Develop a clear, step-by-step plan. Break down the fix into manageable, incremental steps. Maintain a todo list (prefer your host/tooling's native planning mechanism when available) and surface a simple Markdown checkbox checklist to the user at key milestones or when requested.
7. Implement the fix incrementally. Make small, testable code changes.
8. Debug as needed. Use debugging techniques to isolate and resolve issues.
9. Test frequently. Run tests after each change to verify correctness.
10. Iterate until the root cause is fixed and all tests pass.
11. Reflect and validate comprehensively. After tests pass, think about the original intent, write additional tests to ensure correctness, and remember there are hidden tests that must also pass before the solution is truly complete.
12. **Update Memory**: Record important decisions, patterns, or insights in `/agents/memories` so future conversations can build on this work.

Refer to the detailed sections below for more information on each step.

## 0. Check Memory (Always Start Here)

Before beginning work, search the memory system for relevant context. See `agents/global/MEMORY-INSTRUCTIONS.md` for complete guidance on:

- When and how to search memory
- What to look for (projects, decisions, concepts, patterns)
- Search strategies and queries

## 1. Fetch Provided URLs

- If the user provides a URL, use available web fetch capabilities (subject to host/system policy) to retrieve the content.
- After fetching, review the content returned.
- If you find any additional URLs or links that are relevant, fetch those as well.
- Recursively gather all relevant information by fetching additional links until you have all the information you need.

## 2. Deeply Understand the Problem

Carefully read the issue and think hard about a plan to solve it before coding.

## 3. Codebase Investigation

- Explore relevant files and directories.
- Search for key functions, classes, or variables related to the issue.
- Read and understand relevant code snippets.
- Identify the root cause of the problem.
- Validate and update your understanding continuously as you gather more context.

## 4. Internet Research

- Use available web search or fetch capabilities (subject to host/system policy) to search for relevant information.
- After searching, review the content returned.
- You MUST fetch the contents of the most relevant links to gather information. Do not rely on the summary that you find in the search results.
- As you fetch each link, read the content thoroughly and fetch any additional links that you find within the content that are relevant to the problem.
- Recursively gather all relevant information by fetching links until you have all the information you need.

## 5. Develop a Detailed Plan

- Outline a specific, simple, and verifiable sequence of steps to fix the problem.
- Maintain your plan using the host/system's preferred task tracking (or a Markdown checklist if none exists).
- Check off steps as you complete them; show the updated checklist to the user at meaningful milestones or when they ask. If the user requests conciseness or the task is trivial, summarize progress instead of reposting the full checklist.

## 6. Making Code Changes

- Before editing, always read the relevant file contents or section to ensure complete context.
- Always read 2000 lines of code at a time to ensure you have enough context.
- If a patch is not applied correctly, attempt to reapply it.
- Make small, testable, incremental changes that logically follow from your investigation and plan.
- **Plan for Verification**: Before writing code, decide how you will test it. If no test exists, plan to create one.
- Whenever you detect that a project requires an environment variable (such as an API key or secret), always check if a .env file exists in the project root. If it does not exist, automatically create a .env file with a placeholder for the required variable(s) and inform the user. Do this proactively, without waiting for the user to request it.

## 7. Debugging

- Check for any problems in the code using available diagnostic tools
- Make code changes only if you have high confidence they can solve the problem
- When debugging, try to determine the root cause rather than addressing symptoms
- Debug for as long as needed to identify the root cause and identify a fix
- Use print statements, logs, or temporary code to inspect program state, including descriptive statements or error messages to understand what's happening
- To test hypotheses, you can also add test statements or functions
- Revisit your assumptions if unexpected behavior occurs
- See `agents/global/DEBUGGING.md` for comprehensive debugging guidance

## 8. Build & Validation Workflow (CRITICAL)

This project enforces a **STRICT** build policy. You must follow this workflow for all code changes:

1. **Format Code First**:
    - Run `dotnet format` on the solution before building.
    - This ensures all whitespace, indentation, and style issues are corrected according to `.editorconfig`.
    - The `.editorconfig` rules are not enforced as build errors for whitespace/formatting—only `dotnet format` will fix them.
    - **Always run `dotnet format` before committing code.**

2. **Strict Build Execution**:
    - Run `dotnet build` on the solution (`AgentDemo.sln`).
    - This command is configured to:
        - Run strict Markdown linting (via `npm run lint:md`).
        - Treat all C# warnings as errors (`TreatWarningsAsErrors=true`).
        - Check for outdated NuGet packages.
    - **ALL** issues (compilation errors, markdown linting errors, code style warnings, and outdated packages) must be resolved.
    - **A build with warnings is NOT successful.** You must have 0 errors AND 0 warnings.

3. **Outdated Package Policy**:
    - Run `dotnet list package --outdated --include-prerelease` to check for outdated packages.
    - If any packages are outdated, you **MUST** update them before considering the build successful.
    - Use `dotnet add <project> package <PackageName> --version <Version>` to update packages.
    - After updating packages, rebuild and run tests to verify compatibility.

4. **Incremental Fix Strategy**:
    - If the build fails with many errors (> 20), do **NOT** attempt to fix everything at once.
    - Identify the top 20 highest-priority items:
        1. Compilation Errors (Critical)
        2. Markdown Linting Errors
        3. Code Style/Warning-as-Errors
    - Fix these items in small batches.
    - **Verify Incrementally**: After applying a batch of fixes, run `dotnet build` again immediately to verify progress and ensure no new regressions were introduced.

5. **Failure Analysis (Self-Correction)**:
    - If the build fails, pause and analyze **WHY**.
    - Do not blindly apply fixes. Understand the root cause (e.g., missing dependency, wrong pattern, typo).
    - If the failure indicates a gap in the project instructions, update the relevant instruction file immediately.

6. **Final Validation**:
    - Once the build passes completely (0 errors, 0 warnings):
        - Run all tests to ensure no functional regressions.
        - Verify that documentation (CHANGELOG.md, README.md, etc.) is updated and formatted correctly.

## 9. Test Frequently (Mandatory)

- **Run Tests**: Run `dotnet test` after each significant change to verify correctness.
- **Add Tests**: If you modified logic or added a feature, you **MUST** add or update unit tests to cover that logic.
- **Coverage**: Ensure your changes are covered by tests. Do not rely on manual verification alone.
- **Iterate**: If tests fail, fix the code or the test, then run them again.

## 10. Iterate until the root cause is fixed and all tests pass

## 11. Reflect and Validate Comprehensively

- After tests pass, think about the original intent.
- Write additional tests to ensure correctness if edge cases were missed.
- **Proactive Learning**: If you encountered difficulties or made mistakes during the process, reflect on them. Update `AGENTS.md` or other instruction files to prevent future agents from making the same mistakes.
- **Update Memory**: Record important decisions, patterns, or insights in `/agents/memories` so future conversations can build on this work.

## Task Tracking and Todo Lists

Use the host/system's preferred task tracking mechanism when available (e.g., TodoWrite tool). Fall back to Markdown checklists when no native task tracking is available or when the user explicitly requests a simple checklist.

### Using Native Task Tracking (Preferred)

If your environment provides a task tracking tool (like TodoWrite), use it to:

- Break down complex tasks into manageable steps
- Track progress with status updates (pending, in_progress, completed)
- Provide visibility into your work process
- Help organize multi-step implementations

### Using Markdown Checklists (Fallback)

When native task tracking is unavailable or inappropriate, use this markdown format:

```markdown
- [ ] Step 1: Description of the first step
- [ ] Step 2: Description of the second step
- [ ] Step 3: Description of the third step
```

**Guidelines:**

- Do not use HTML tags or other formatting for checklists
- Always wrap checklists in triple backticks for proper formatting
- Update checklists at meaningful milestones, not after every single step
- When policies conflict, host/system policies take precedence
- Prefer brevity when requested by the user
- Use discretion: don't always append the full checklist if the task is simple or the user prefers conciseness
