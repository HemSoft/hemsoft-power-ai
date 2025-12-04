// <copyright file="FileTraceExporterTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using System.Diagnostics;

using HemSoft.PowerAI.Console.Telemetry;

using OpenTelemetry;

using Xunit;

/// <summary>
/// Tests for <see cref="FileTraceExporter"/>.
/// </summary>
public sealed class FileTraceExporterTests : IDisposable
{
    private readonly string testDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileTraceExporterTests"/> class.
    /// </summary>
    public FileTraceExporterTests() =>
        this.testDirectory = Path.Combine(Path.GetTempPath(), $"FileTraceExporterTests_{Guid.NewGuid():N}");

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(this.testDirectory))
        {
            Directory.Delete(this.testDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that the exporter creates the trace directory.
    /// </summary>
    [Fact]
    public void ConstructorCreatesTraceDirectory()
    {
        // Arrange & Act
        using var exporter = new FileTraceExporter(this.testDirectory, retentionDays: 7);

        // Assert
        Assert.True(Directory.Exists(this.testDirectory));
    }

    /// <summary>
    /// Verifies that the exporter writes trace data to a file.
    /// </summary>
    [Fact]
    public void ExportWritesTraceToFile()
    {
        // Arrange
        using var exporter = new FileTraceExporter(this.testDirectory, retentionDays: 7);
        using var activitySource = new ActivitySource("Test.Source");

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("TestOperation");
        activity?.SetTag("test.key", "test.value");
        activity?.Stop();

        // Act
        using var batch = new Batch<Activity>([activity!], 1);
        var result = exporter.Export(in batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);

        var files = Directory.GetFiles(this.testDirectory, "traces-*.jsonl");
        Assert.Single(files);

        var content = File.ReadAllText(files[0]);
        Assert.Contains("TestOperation", content, StringComparison.Ordinal);
        Assert.Contains("test.key", content, StringComparison.Ordinal);
        Assert.Contains("test.value", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the exporter returns success for empty batches.
    /// </summary>
    [Fact]
    public void ExportEmptyBatchReturnsSuccess()
    {
        // Arrange
        using var exporter = new FileTraceExporter(this.testDirectory, retentionDays: 7);

        // Act
        using var batch = new Batch<Activity>([], 0);
        var result = exporter.Export(in batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);
    }
}
