// <copyright file="ConsoleLogService.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Services;

using System.Globalization;
using System.IO;
using System.Text;

using HemSoft.PowerAI.Console.Extensions;

/// <summary>
/// Provides file-based logging for console output.
/// Captures all console output to a timestamped log file with automatic rotation.
/// </summary>
internal sealed class ConsoleLogService : IDisposable
{
    private const string LogDirectoryName = "Logs";
    private const string LogFileExtension = ".log";
    private const int MaxLogFiles = 10;

    /// <summary>
    /// The maximum size of a single log file in bytes (10 MB).
    /// </summary>
    private const long MaxLogFileSizeBytes = 10 * 1024 * 1024;

    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HemSoft.PowerAI",
        LogDirectoryName);

    private static readonly CompositeFormat TimestampFileFormat = CompositeFormat.Parse("{0:yyyy-MM-dd_HH-mm-ss-fff}");
    private static readonly CompositeFormat TimestampLogFormat = CompositeFormat.Parse("{0:HH:mm:ss.fff}");
    private static readonly CompositeFormat DateTimeMessageFormat = CompositeFormat.Parse("{0:yyyy-MM-dd HH:mm:ss}");

    private readonly StreamWriter logWriter;
    private readonly Lock writeLock = new();
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleLogService"/> class.
    /// </summary>
    public ConsoleLogService()
    {
        _ = Directory.CreateDirectory(LogDirectory);
        CleanupOldLogs();

        var timestamp = string.Format(CultureInfo.InvariantCulture, TimestampFileFormat, TimeProvider.System.GetLocalNow());
        var uniqueId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..8];
        this.CurrentLogPath = Path.Combine(LogDirectory, "console_" + timestamp + "_" + uniqueId + LogFileExtension);

        this.logWriter = new StreamWriter(this.CurrentLogPath, append: true)
        {
            AutoFlush = true,
        };

        this.WriteHeader();
    }

    /// <summary>
    /// Gets the path to the current log file.
    /// </summary>
    public string CurrentLogPath { get; }

    /// <summary>
    /// Gets the path to the log directory.
    /// </summary>
    /// <returns>The absolute path to the log directory.</returns>
    public static string GetLogDirectory() => LogDirectory;

    /// <summary>
    /// Writes a message to the log file.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Log(string message)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        var timestamp = string.Format(CultureInfo.InvariantCulture, TimestampLogFormat, TimeProvider.System.GetLocalNow());
        var logLine = "[" + timestamp + "] " + message;

        lock (this.writeLock)
        {
            this.logWriter.WriteLine(logLine);
        }
    }

    /// <summary>
    /// Writes a formatted message to the log file.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The format arguments.</param>
    public void Log(string format, params object[] args)
    {
        var message = string.Format(CultureInfo.InvariantCulture, format, args);
        this.Log(message);
    }

    /// <summary>
    /// Writes a progress update to the log file with agent context.
    /// </summary>
    /// <param name="agentName">The agent name.</param>
    /// <param name="message">The progress message.</param>
    /// <param name="inputTokens">Optional input token count.</param>
    /// <param name="outputTokens">Optional output token count.</param>
    public void LogProgress(string? agentName, string message, int? inputTokens = null, int? outputTokens = null)
    {
        var tokenInfo = inputTokens.HasValue && outputTokens.HasValue
            ? " [tokens: " + inputTokens.Value.ToInvariant("N0") + "→" + outputTokens.Value.ToInvariant("N0") + "]"
            : string.Empty;

        var agentPrefix = !string.IsNullOrEmpty(agentName) ? "[" + agentName + "] " : string.Empty;
        this.Log(agentPrefix + message + tokenInfo);
    }

    /// <summary>
    /// Writes an error to the log file.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="exception">Optional exception details.</param>
    public void LogError(string message, Exception? exception = null)
    {
        this.Log("ERROR: " + message);
        if (exception is not null)
        {
            this.Log("  Exception: " + exception.GetType().Name + ": " + exception.Message);
            if (exception.StackTrace is not null)
            {
                this.Log("  Stack: " + exception.StackTrace);
            }
        }
    }

    /// <summary>
    /// Writes a section separator to the log file.
    /// </summary>
    /// <param name="title">The section title.</param>
    public void LogSection(string title)
    {
        var separator = new string('═', 60);
        this.Log(separator);
        this.Log(title.ToUpperInvariant());
        this.Log(separator);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        // Write footer before setting disposed flag
        this.WriteFooter();
        this.disposed = true;
        this.logWriter.Dispose();
    }

    private static void CleanupOldLogs()
    {
        try
        {
            var logFiles = Directory.GetFiles(LogDirectory, "*" + LogFileExtension)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            // Remove files beyond max count
            foreach (var file in logFiles.Skip(MaxLogFiles))
            {
                TryDeleteFile(file);
            }

            // Remove files larger than max size
            foreach (var file in logFiles.Where(f => f.Length > MaxLogFileSizeBytes))
            {
                TryDeleteFile(file);
            }
        }
        catch (DirectoryNotFoundException)
        {
            // Directory doesn't exist yet, nothing to clean
        }
    }

    private static void TryDeleteFile(FileInfo file)
    {
        try
        {
            file.Delete();
        }
        catch (IOException)
        {
            // File in use, skip
        }
    }

    private void WriteHeader()
    {
        this.Log(new string('═', 60));
        this.Log("HEMSOFT POWER AI CONSOLE LOG");
        this.Log("Started: " + string.Format(CultureInfo.InvariantCulture, DateTimeMessageFormat, TimeProvider.System.GetLocalNow()));
        this.Log("Machine: " + Environment.MachineName);
        this.Log("User: " + Environment.UserName);
        this.Log(new string('═', 60));
    }

    private void WriteFooter()
    {
        this.Log(new string('═', 60));
        this.Log("Session ended: " + string.Format(CultureInfo.InvariantCulture, DateTimeMessageFormat, TimeProvider.System.GetLocalNow()));
        this.Log(new string('═', 60));
    }
}
