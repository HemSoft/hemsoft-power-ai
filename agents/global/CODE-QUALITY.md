# Code Quality Instructions

## Code Quality & Standards

This repository enforces strict code quality standards. **A build with warnings is NOT a successful build.**

**NON-NEGOTIABLE: You are strictly prohibited from modifying rules in `.markdownlint.json`, `.editorconfig`, `SonarLint.xml`, or any other configuration file without explicit consent.**

When writing or modifying C# code, you must adhere to the following:

1. **Follow .editorconfig**: The project uses `.editorconfig` to enforce styles like file-scoped namespaces, primary constructors, and expression-bodied members.
2. **SonarQube Analysis**: `SonarAnalyzer.CSharp` is included in the project. Pay attention to warnings, especially regarding **Cognitive Complexity (S3776)**. Refactor methods that exceed the complexity threshold (15).
3. **Security Scanning**: `SecurityCodeScan.VS2019` is included to detect security vulnerabilities in the code. Address any security warnings immediately.
4. **Build Enforcement**: Code style is enforced during build (`<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`).
5. **Auto-Formatting**: Always run `dotnet format` after making changes to ensure your code complies with the project's style guidelines.
6. **Outdated Packages**: Outdated NuGet packages are a build failure. Run `dotnet list package --outdated --include-prerelease` and update all packages before considering the build successful.

## PowerShell Instructions

When generating PowerShell commands or scripts, use ';' (semicolon) as a command separator instead of '&&'. PowerShell does not support '&&' for command chaining.

Example:

- ✅ Correct: `command1; command2; command3`
- ❌ Incorrect: `command1 && command2 && command3`

Always run and test PowerShell scripts after creating or modifying them to verify they work as expected in the current environment.

## Markdown Instructions

This repository uses `markdownlint` to enforce consistent markdown styling. It is integrated into the build process.

1. **Configuration**: The project uses a `.markdownlint.json` file in the root directory to define rules.
2. **Build Integration**: Markdown linting runs automatically during `dotnet build`.
    - **Requirement**: Node.js and npm must be installed.
    - **Setup**: The build will automatically run `npm install` if needed to fetch the linter.
3. **Line Length**: Line length rules (MD013) are disabled to allow for long lines in tables and links.
4. **HTML**: Inline HTML (MD033) is allowed for complex formatting where necessary.
5. **VS Code Extension**: It is recommended to install the `markdownlint` extension for Visual Studio Code to see violations in real-time.
