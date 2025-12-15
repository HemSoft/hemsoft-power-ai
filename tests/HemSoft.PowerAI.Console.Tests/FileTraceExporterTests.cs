// <copyright file="FileTraceExporterTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using System.Diagnostics;
using System.Globalization;

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

        static ActivitySamplingResult SampleAll(ref ActivityCreationOptions<ActivityContext> options)
        {
            _ = options; // Discard to satisfy unused parameter analyzer
            return ActivitySamplingResult.AllData;
        }

        using var listener = new ActivityListener
        {
            ShouldListenTo = static _ => true,
            Sample = SampleAll,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("TestOperation");
        _ = activity?.SetTag("test.key", "test.value");
        activity?.Stop();

        // Act
        using var batch = new Batch<Activity>([activity!], 1);
        var result = exporter.Export(in batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);

        var files = Directory.GetFiles(this.testDirectory, "traces-*.jsonl");
        _ = Assert.Single(files);

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

    /// <summary>
    /// Verifies that the exporter rotates files when date changes.
    /// </summary>
    [Fact]
    public void ExportRotatesFilesWhenDateChanges()
    {
        // Arrange
        var mockTimeProvider = new MockTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        using var exporter = new FileTraceExporter(this.testDirectory, retentionDays: 7, mockTimeProvider);
        using var activitySource = new ActivitySource("Test.Source.Rotate");

        static ActivitySamplingResult SampleAll(ref ActivityCreationOptions<ActivityContext> options)
        {
            _ = options;
            return ActivitySamplingResult.AllData;
        }

        using var listener = new ActivityListener
        {
            ShouldListenTo = static _ => true,
            Sample = SampleAll,
        };
        ActivitySource.AddActivityListener(listener);

        // First export on day 1
        using (var activity1 = activitySource.StartActivity("Day1Operation"))
        {
            activity1?.Stop();
            using var batch1 = new Batch<Activity>([activity1!], 1);
            _ = exporter.Export(in batch1);
        }

        // Change date to next day
        mockTimeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero));

        // Second export on day 2
        using (var activity2 = activitySource.StartActivity("Day2Operation"))
        {
            activity2?.Stop();
            using var batch2 = new Batch<Activity>([activity2!], 1);
            _ = exporter.Export(in batch2);
        }

        // Assert - should have two trace files
        var files = Directory.GetFiles(this.testDirectory, "traces-*.jsonl");
        Assert.Equal(2, files.Length);
    }

    /// <summary>
    /// Verifies that the exporter cleans up old files based on retention.
    /// </summary>
    [Fact]
    public void ExportCleansUpOldFilesBasedOnRetention()
    {
        // Arrange - create old files
        _ = Directory.CreateDirectory(this.testDirectory);
        var oldDate = DateTimeOffset.UtcNow.AddDays(-10);
        var formattedDate = string.Create(
            CultureInfo.InvariantCulture,
            $"traces-{oldDate:yyyy-MM-dd}.jsonl");
        var oldFileName = Path.Combine(this.testDirectory, formattedDate);
        File.WriteAllText(oldFileName, "old trace data");

        // Act - create exporter which should cleanup on construction
        using var exporter = new FileTraceExporter(this.testDirectory, retentionDays: 7);

        // Assert - old file should be deleted
        Assert.False(File.Exists(oldFileName));
    }

    /// <summary>
    /// Verifies that the exporter keeps recent files.
    /// </summary>
    [Fact]
    public void ExportKeepsRecentFiles()
    {
        // Arrange - create a recent file
        _ = Directory.CreateDirectory(this.testDirectory);
        var recentDate = DateTimeOffset.UtcNow.AddDays(-3);
        var formattedDate = string.Create(
            CultureInfo.InvariantCulture,
            $"traces-{recentDate:yyyy-MM-dd}.jsonl");
        var recentFileName = Path.Combine(this.testDirectory, formattedDate);
        File.WriteAllText(recentFileName, "recent trace data");

        // Act - create exporter which should cleanup on construction
        using var exporter = new FileTraceExporter(this.testDirectory, retentionDays: 7);

        // Assert - recent file should be kept
        Assert.True(File.Exists(recentFileName));
    }

    /// <summary>
    /// Verifies that activity events are exported.
    /// </summary>
    [Fact]
    public void ExportIncludesActivityEvents()
    {
        // Arrange
        using var exporter = new FileTraceExporter(this.testDirectory, retentionDays: 7);
        using var activitySource = new ActivitySource("Test.Source.Events");

        static ActivitySamplingResult SampleAll(ref ActivityCreationOptions<ActivityContext> options)
        {
            _ = options;
            return ActivitySamplingResult.AllData;
        }

        using var listener = new ActivityListener
        {
            ShouldListenTo = static _ => true,
            Sample = SampleAll,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("TestWithEvents");
        activity?.AddEvent(new ActivityEvent("TestEvent", tags: new ActivityTagsCollection([new("event.key", "event.value")])));
        activity?.Stop();

        // Act
        using var batch = new Batch<Activity>([activity!], 1);
        var result = exporter.Export(in batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);

        var files = Directory.GetFiles(this.testDirectory, "traces-*.jsonl");
        _ = Assert.Single(files);

        var content = File.ReadAllText(files[0]);
        Assert.Contains("TestEvent", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that exporter handles multiple activities in a batch.
    /// </summary>
    [Fact]
    public void ExportHandlesMultipleActivitiesInBatch()
    {
        // Arrange
        using var exporter = new FileTraceExporter(this.testDirectory, retentionDays: 7);
        using var activitySource = new ActivitySource("Test.Source.Multiple");

        static ActivitySamplingResult SampleAll(ref ActivityCreationOptions<ActivityContext> options)
        {
            _ = options;
            return ActivitySamplingResult.AllData;
        }

        using var listener = new ActivityListener
        {
            ShouldListenTo = static _ => true,
            Sample = SampleAll,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity0 = activitySource.StartActivity("Operation0");
        activity0?.Stop();
        using var activity1 = activitySource.StartActivity("Operation1");
        activity1?.Stop();
        using var activity2 = activitySource.StartActivity("Operation2");
        activity2?.Stop();
        using var activity3 = activitySource.StartActivity("Operation3");
        activity3?.Stop();
        using var activity4 = activitySource.StartActivity("Operation4");
        activity4?.Stop();

        var activities = new[]
        {
            activity0!, activity1!, activity2!, activity3!, activity4!,
        };

        // Act
        using var batch = new Batch<Activity>([.. activities], activities.Length);
        var result = exporter.Export(in batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);

        var files = Directory.GetFiles(this.testDirectory, "traces-*.jsonl");
        _ = Assert.Single(files);

        var lines = File.ReadAllLines(files[0]);
        Assert.Equal(5, lines.Length);
    }

    /// <summary>
    /// Verifies that activity status is exported.
    /// </summary>
    [Fact]
    public void ExportIncludesActivityStatus()
    {
        // Arrange
        using var exporter = new FileTraceExporter(this.testDirectory, retentionDays: 7);
        using var activitySource = new ActivitySource("Test.Source.Status");

        static ActivitySamplingResult SampleAll(ref ActivityCreationOptions<ActivityContext> options)
        {
            _ = options;
            return ActivitySamplingResult.AllData;
        }

        using var listener = new ActivityListener
        {
            ShouldListenTo = static _ => true,
            Sample = SampleAll,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("TestWithStatus");
        _ = activity?.SetStatus(ActivityStatusCode.Error, "Test error description");
        activity?.Stop();

        // Act
        using var batch = new Batch<Activity>([activity!], 1);
        var result = exporter.Export(in batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);

        var files = Directory.GetFiles(this.testDirectory, "traces-*.jsonl");
        _ = Assert.Single(files);

        var content = File.ReadAllText(files[0]);
        Assert.Contains("Error", content, StringComparison.Ordinal);
        Assert.Contains("Test error description", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that exporter uses custom time provider.
    /// </summary>
    [Fact]
    public void ExporterUsesCustomTimeProvider()
    {
        // Arrange
        var customDate = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var mockTimeProvider = new MockTimeProvider(customDate);
        using var exporter = new FileTraceExporter(this.testDirectory, retentionDays: 7, mockTimeProvider);
        using var activitySource = new ActivitySource("Test.Source.TimeProvider");

        static ActivitySamplingResult SampleAll(ref ActivityCreationOptions<ActivityContext> options)
        {
            _ = options;
            return ActivitySamplingResult.AllData;
        }

        using var listener = new ActivityListener
        {
            ShouldListenTo = static _ => true,
            Sample = SampleAll,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("TestOperation");
        activity?.Stop();

        // Act
        using var batch = new Batch<Activity>([activity!], 1);
        _ = exporter.Export(in batch);

        // Assert
        var expectedFileName = Path.Combine(this.testDirectory, "traces-2025-06-15.jsonl");
        Assert.True(File.Exists(expectedFileName));
    }

    /// <summary>
    /// Verifies that exporter handles activity with span kind.
    /// </summary>
    [Fact]
    public void ExportIncludesSpanKind()
    {
        // Arrange
        using var exporter = new FileTraceExporter(this.testDirectory, retentionDays: 7);
        using var activitySource = new ActivitySource("Test.Source.SpanKind");

        static ActivitySamplingResult SampleAll(ref ActivityCreationOptions<ActivityContext> options)
        {
            _ = options;
            return ActivitySamplingResult.AllData;
        }

        using var listener = new ActivityListener
        {
            ShouldListenTo = static _ => true,
            Sample = SampleAll,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("ClientOperation", ActivityKind.Client);
        activity?.Stop();

        // Act
        using var batch = new Batch<Activity>([activity!], 1);
        var result = exporter.Export(in batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);

        var files = Directory.GetFiles(this.testDirectory, "traces-*.jsonl");
        _ = Assert.Single(files);

        var content = File.ReadAllText(files[0]);
        Assert.Contains("Client", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// A mock time provider for testing.
    /// </summary>
    /// <param name="utcNow">The initial UTC time.</param>
    private sealed class MockTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => this.utcNow;

        public void SetUtcNow(DateTimeOffset value) => this.utcNow = value;
    }
}
