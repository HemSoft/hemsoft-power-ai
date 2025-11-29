// <copyright file="TerminalTools.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace AgentDemo.Console.Tools;

using System.ComponentModel;
using System.Diagnostics;
using System.Text;

/// <summary>
/// Provides unified terminal access for the AI agent.
/// Combines command execution, file operations, and process management in a single tool.
/// </summary>
internal static class TerminalTools
{
    private const int MaxOutputLength = 8192;

    /// <summary>
    /// Executes a terminal command with full shell access.
    /// Supports PowerShell on Windows and bash on Unix-like systems.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="workingDirectory">Optional working directory for command execution.</param>
    /// <param name="timeoutSeconds">Timeout in seconds (default: 30, max: 300).</param>
    /// <returns>Command output including stdout, stderr, and exit code.</returns>
    [Description("Execute terminal commands with full shell access. PowerShell on Windows, bash on Unix. Returns stdout, stderr, and exit code.")]
    public static string Execute(string command, string? workingDirectory = null, int timeoutSeconds = 30)
    {
        System.Console.WriteLine($"[Tool] Execute: {command}");

        if (string.IsNullOrWhiteSpace(command))
        {
            return "Error: Command cannot be empty";
        }

        // Clamp timeout to reasonable bounds
        var timeout = Math.Clamp(timeoutSeconds, 1, 300) * 1000;

        var (shell, shellArgs) = GetShellInfo();
        var fullArgs = $"{shellArgs} \"{EscapeCommand(command)}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = shell,
            Arguments = fullArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = ResolveWorkingDirectory(workingDirectory),
        };

        return RunProcess(startInfo, timeout);
    }

    /// <summary>
    /// Executes multiple commands in sequence, stopping on first failure.
    /// </summary>
    /// <param name="commands">Semicolon-separated list of commands.</param>
    /// <param name="workingDirectory">Optional working directory.</param>
    /// <param name="continueOnError">If true, continues execution even if a command fails.</param>
    /// <returns>Combined output from all commands.</returns>
    [Description("Execute multiple commands in sequence. Separate with semicolons. Set continueOnError=true to run all regardless of failures.")]
    public static string ExecuteBatch(string commands, string? workingDirectory = null, bool continueOnError = false)
    {
        System.Console.WriteLine($"[Tool] ExecuteBatch: {commands}");

        if (string.IsNullOrWhiteSpace(commands))
        {
            return "Error: Commands cannot be empty";
        }

        var commandList = commands.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var results = new StringBuilder();
        var allSucceeded = true;

        foreach (var cmd in commandList)
        {
            results.Append(">>> ").AppendLine(cmd);
            var result = Execute(cmd, workingDirectory);
            results.AppendLine(result);

            var failed = result.Contains("Exit: ", StringComparison.Ordinal) && !result.Contains("Exit: 0", StringComparison.Ordinal);
            if (failed)
            {
                allSucceeded = false;
                if (!continueOnError)
                {
                    results.AppendLine("Batch stopped due to failure. Use continueOnError=true to run all commands.");
                    break;
                }
            }
        }

        results.Append("Batch complete. Success: ").Append(allSucceeded).AppendLine();
        return results.ToString();
    }

    private static (string Shell, string Args) GetShellInfo() =>
        OperatingSystem.IsWindows()
            ? ("pwsh", "-NoProfile -NonInteractive -Command")
            : ("/bin/bash", "-c");

    private static string EscapeCommand(string command) =>
        OperatingSystem.IsWindows()
            ? command.Replace("\"", "`\"", StringComparison.Ordinal)
            : command.Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string ResolveWorkingDirectory(string? workingDirectory) =>
        string.IsNullOrEmpty(workingDirectory) || !Directory.Exists(workingDirectory)
            ? Environment.CurrentDirectory
            : workingDirectory;

    private static string RunProcess(ProcessStartInfo startInfo, int timeoutMs)
    {
        var output = new StringBuilder();
        var error = new StringBuilder();

        try
        {
            using var process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null && output.Length < MaxOutputLength)
                {
                    output.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null && error.Length < MaxOutputLength)
                {
                    error.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var completed = process.WaitForExit(timeoutMs);

            if (!completed)
            {
                process.Kill(entireProcessTree: true);
                return $"Error: Command timed out after {timeoutMs / 1000}s\nPartial output:\n{output}";
            }

            return FormatResult(output.ToString(), error.ToString(), process.ExitCode);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return $"Error: Failed to execute command - {ex.Message}";
        }
    }

    private static string FormatResult(string stdout, string stderr, int exitCode)
    {
        var result = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            result.AppendLine(stdout.TrimEnd());
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            result.Append("[stderr] ").AppendLine(stderr.TrimEnd());
        }

        result.Append("Exit: ").Append(exitCode).AppendLine();

        return result.ToString();
    }
}
