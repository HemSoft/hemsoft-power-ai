---
title: "Code Standards"
version: "1.0.4"
lastModified: "2025-11-28"
author: "Franz Hemmer"
purpose: "C# coding standards and guidelines for simplicity, clarity, and maintainability."
---

<!-- markdownlint-disable MD025 -->

# Code Standards

## C\#

- Only add comments to code when it is absolutely necessary for clarity.
- Prefer to avoid comments and use the best naming conventions for clarity and easy readability.
- Do not place comments on the same line as code (trailing comments). Place comments on the line above the code they describe.
- Every method should only address one concern.
- Every method should be testable.
- Always use file-scoped namespace declaration.
- Always put using statements after the namespace declaration.
- Sort using statements alphabetically.
- Remove unnecessary using statements.
- Use readonly local variables where possible.
- Use const strings where possible.
- Remove unused variables.
- Investigate unused methods. Are they needed?
- Don't embed class definitions or enumerations with code methods. Bring them into their own classes. Consider using an Enums and Models folder or similar names to put those extractions into.
- If a class grows beyond 200 lines it is time to split out the functionality into multiple functional classes. Try to extract as much into logical components.
- Avoid having more than one class in one code file.
- Do not use fully qualified names. Example: System.Text.Json.Serialization.JsonPropertyName - add a using statement so you can abbreviate the type.
- Avoid use of reflection techniques or any other advanced when possible.
- Keep the code as simple as possible.
- Minimize the number of concerns addressed in each method. Avoid doing more than a few things in a method. Split the method in two or more methods when that happens.
- Group all private methods at the bottom of the class and surround them all with one #region --> Private Methods <--

## Verification

- Always run dotnet build after implementing code changes to verify syntax and compilation.
