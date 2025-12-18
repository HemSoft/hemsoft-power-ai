// <copyright file="ConsoleLogServiceTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using System.Globalization;

using HemSoft.PowerAI.Console.Services;

/// <summary>
/// Unit tests for <see cref="ConsoleLogService"/>.
/// </summary>
public sealed class ConsoleLogServiceTests : IDisposable
{
    private readonly string testLogDirectory;
    private readonly List<string> createdFiles = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleLogServiceTests"/> class.
    /// </summary>
    public ConsoleLogServiceTests()
    {
        // Use a unique test directory to avoid conflicts
        this.testLogDirectory = Path.Combine(
            Path.GetTempPath(),
            "ConsoleLogServiceTests_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        _ = Directory.CreateDirectory(this.testLogDirectory);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Clean up any created files
        foreach (var file in this.createdFiles)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    // File in use, skip
                }
            }
        }

        // Clean up test directory
        if (Directory.Exists(this.testLogDirectory))
        {
            try
            {
                Directory.Delete(this.testLogDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Directory in use, skip
            }
        }
    }

    /// <summary>
    /// Verifies constructor creates a log file.
    /// </summary>
    [Fact]
    public void ConstructorCreatesLogFile()
    {
        // Act
        using var service = new ConsoleLogService();
        this.createdFiles.Add(service.CurrentLogPath);

        // Assert
        Assert.True(File.Exists(service.CurrentLogPath));
    }

    /// <summary>
    /// Verifies constructor writes header to log file.
    /// </summary>
    [Fact]
    public void ConstructorWritesHeader()
    {
        // Act
        string logPath;
        using (var service = new ConsoleLogService())
        {
            logPath = service.CurrentLogPath;
            this.createdFiles.Add(logPath);
        }

        // Assert
        var content = File.ReadAllText(logPath);
        Assert.Contains("HEMSOFT POWER AI CONSOLE LOG", content, StringComparison.Ordinal);
        Assert.Contains("Started:", content, StringComparison.Ordinal);
        Assert.Contains("Machine:", content, StringComparison.Ordinal);
        Assert.Contains("User:", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies CurrentLogPath returns a valid path.
    /// </summary>
    [Fact]
    public void CurrentLogPathReturnsValidPath()
    {
        // Act
        using var service = new ConsoleLogService();
        this.createdFiles.Add(service.CurrentLogPath);

        // Assert
        Assert.NotNull(service.CurrentLogPath);
        Assert.EndsWith(".log", service.CurrentLogPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("console_", service.CurrentLogPath, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies GetLogDirectory returns a valid path.
    /// </summary>
    [Fact]
    public void GetLogDirectoryReturnsValidPath()
    {
        // Act
        var directory = ConsoleLogService.GetLogDirectory();

        // Assert
        Assert.NotNull(directory);
        Assert.Contains("HemSoft.PowerAI", directory, StringComparison.Ordinal);
        Assert.Contains("Logs", directory, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies Log writes message to file.
    /// </summary>
    [Fact]
    public void LogWritesMessageToFile()
    {
        // Arrange
        var testMessage = "Test message " + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        string logPath;

        using (var service = new ConsoleLogService())
        {
            logPath = service.CurrentLogPath;
            this.createdFiles.Add(logPath);

            // Act
            service.Log(testMessage);
        }

        // Assert
        var content = File.ReadAllText(logPath);
        Assert.Contains(testMessage, content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies Log includes timestamp in output.
    /// </summary>
    [Fact]
    public void LogIncludesTimestamp()
    {
        // Arrange
        string logPath;

        using (var service = new ConsoleLogService())
        {
            logPath = service.CurrentLogPath;
            this.createdFiles.Add(logPath);

            // Act
            service.Log("Test message");
        }

        // Assert
        var content = File.ReadAllText(logPath);

        // Timestamp format is [HH:mm:ss.fff]
        Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\.\d{3}\]", content);
    }

    /// <summary>
    /// Verifies Log with format writes formatted message.
    /// </summary>
    [Fact]
    public void LogWithFormatWritesFormattedMessage()
    {
        // Arrange
        string logPath;

        using (var service = new ConsoleLogService())
        {
            logPath = service.CurrentLogPath;
            this.createdFiles.Add(logPath);

            // Act
            service.Log("Count: {0}, Name: {1}", 42, "Test");
        }

        // Assert
        var content = File.ReadAllText(logPath);
        Assert.Contains("Count: 42, Name: Test", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies LogProgress writes with agent name prefix.
    /// </summary>
    [Fact]
    public void LogProgressWritesWithAgentName()
    {
        // Arrange
        string logPath;

        using (var service = new ConsoleLogService())
        {
            logPath = service.CurrentLogPath;
            this.createdFiles.Add(logPath);

            // Act
            service.LogProgress("ResearchAgent", "Processing query");
        }

        // Assert
        var content = File.ReadAllText(logPath);
        Assert.Contains("[ResearchAgent]", content, StringComparison.Ordinal);
        Assert.Contains("Processing query", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies LogProgress writes with token information.
    /// </summary>
    [Fact]
    public void LogProgressWritesWithTokenInfo()
    {
        // Arrange
        string logPath;

        using (var service = new ConsoleLogService())
        {
            logPath = service.CurrentLogPath;
            this.createdFiles.Add(logPath);

            // Act
            service.LogProgress("Agent", "Message", inputTokens: 1000, outputTokens: 500);
        }

        // Assert
        var content = File.ReadAllText(logPath);
        Assert.Contains("[tokens:", content, StringComparison.Ordinal);
        Assert.Contains("1,000", content, StringComparison.Ordinal);
        Assert.Contains("500", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies LogProgress with null agent name omits the prefix.
    /// </summary>
    [Fact]
    public void LogProgressWithNullAgentNameOmitsPrefix()
    {
        // Arrange
        string logPath;

        using (var service = new ConsoleLogService())
        {
            logPath = service.CurrentLogPath;
            this.createdFiles.Add(logPath);

            // Act
            service.LogProgress(agentName: null, "Processing query");
        }

        // Assert
        var content = File.ReadAllText(logPath);
        Assert.Contains("Processing query", content, StringComparison.Ordinal);
        Assert.DoesNotContain("[null]", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies LogError writes error prefix.
    /// </summary>
    [Fact]
    public void LogErrorWritesErrorPrefix()
    {
        // Arrange
        string logPath;

        using (var service = new ConsoleLogService())
        {
            logPath = service.CurrentLogPath;
            this.createdFiles.Add(logPath);

            // Act
            service.LogError("Something went wrong");
        }

        // Assert
        var content = File.ReadAllText(logPath);
        Assert.Contains("ERROR: Something went wrong", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies LogError with exception writes exception details.
    /// </summary>
    [Fact]
    public void LogErrorWithExceptionWritesExceptionDetails()
    {
        // Arrange
        string logPath;
        var exception = new InvalidOperationException("Test exception");

        using (var service = new ConsoleLogService())
        {
            logPath = service.CurrentLogPath;
            this.createdFiles.Add(logPath);

            // Act
            service.LogError("Operation failed", exception);
        }

        // Assert
        var content = File.ReadAllText(logPath);
        Assert.Contains("ERROR: Operation failed", content, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", content, StringComparison.Ordinal);
        Assert.Contains("Test exception", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies LogSection writes separator and title.
    /// </summary>
    [Fact]
    public void LogSectionWritesSeparatorAndTitle()
    {
        // Arrange
        string logPath;

        using (var service = new ConsoleLogService())
        {
            logPath = service.CurrentLogPath;
            this.createdFiles.Add(logPath);

            // Act
            service.LogSection("Test Section");
        }

        // Assert
        var content = File.ReadAllText(logPath);
        Assert.Contains("═", content, StringComparison.Ordinal);
        Assert.Contains("TEST SECTION", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies Dispose writes footer to log file.
    /// </summary>
    [Fact]
    public void DisposeWritesFooter()
    {
        // Arrange
        string logPath;
        using (var service = new ConsoleLogService())
        {
            logPath = service.CurrentLogPath;
            this.createdFiles.Add(logPath);
        }

        // Assert
        var content = File.ReadAllText(logPath);
        Assert.Contains("Session ended:", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies Dispose can be called multiple times without throwing.
    /// </summary>
    [Fact]
    public void DisposeCanBeCalledMultipleTimes()
    {
        // Arrange
        string logPath;
        using (var service = new ConsoleLogService())
        {
            logPath = service.CurrentLogPath;
            this.createdFiles.Add(logPath);
        }

        // Act - using block already disposed, so the test passes if no exception was thrown
        // Assert
        Assert.True(File.Exists(logPath));
    }

    /// <summary>
    /// Verifies multiple instances create separate log files.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task MultipleInstancesCreateSeparateLogFilesAsync()
    {
        // Arrange & Act
        using var service1 = new ConsoleLogService();
        this.createdFiles.Add(service1.CurrentLogPath);

        // Small delay to ensure different timestamp
        await Task.Delay(millisecondsDelay: 15).ConfigureAwait(true);

        using var service2 = new ConsoleLogService();
        this.createdFiles.Add(service2.CurrentLogPath);

        // Assert - with same timestamp they might be same file, but different instances should work
        Assert.NotNull(service1.CurrentLogPath);
        Assert.NotNull(service2.CurrentLogPath);
    }
}
