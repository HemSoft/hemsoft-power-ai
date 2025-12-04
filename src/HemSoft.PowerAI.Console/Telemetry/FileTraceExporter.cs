// <copyright file="FileTraceExporter.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Telemetry;

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

using OpenTelemetry;

/// <summary>
/// Exports OpenTelemetry traces to JSON files for debugging and analysis.
/// Files are rotated daily and retained for a configurable number of days.
/// </summary>
internal sealed class FileTraceExporter : BaseExporter<Activity>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    private readonly string traceDirectory;
    private readonly int retentionDays;
    private readonly Lock fileLock = new();
    private string currentFilePath;
    private DateTime currentFileDate;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileTraceExporter"/> class.
    /// </summary>
    /// <param name="traceDirectory">Directory to write trace files to.</param>
    /// <param name="retentionDays">Number of days to retain trace files.</param>
    public FileTraceExporter(string traceDirectory, int retentionDays = 7)
    {
        this.traceDirectory = traceDirectory;
        this.retentionDays = retentionDays;
        this.currentFileDate = DateTime.UtcNow.Date;
        this.currentFilePath = this.GetTraceFilePath(this.currentFileDate);

        Directory.CreateDirectory(traceDirectory);
        this.CleanupOldFiles();
    }

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<Activity> batch)
    {
        try
        {
            var lines = new List<string>();

            foreach (var activity in batch)
            {
                var trace = ConvertActivityToTrace(activity);
                var json = JsonSerializer.Serialize(trace, JsonOptions);
                lines.Add(json);
            }

            if (lines.Count == 0)
            {
                return ExportResult.Success;
            }

            lock (this.fileLock)
            {
                this.RotateFileIfNeeded();
                File.AppendAllLines(this.currentFilePath, lines);
            }

            return ExportResult.Success;
        }
        catch (IOException)
        {
            return ExportResult.Failure;
        }
    }

    private static TraceRecord ConvertActivityToTrace(Activity activity)
    {
        var tags = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var tag in activity.Tags)
        {
            tags[tag.Key] = tag.Value;
        }

        var events = activity.Events.Select(e => new TraceEvent
        {
            Name = e.Name,
            Timestamp = e.Timestamp.ToString("o", CultureInfo.InvariantCulture),
            Attributes = e.Tags.ToDictionary(t => t.Key, t => t.Value as object, StringComparer.Ordinal),
        }).ToList();

        return new TraceRecord
        {
            TraceId = activity.TraceId.ToString(),
            SpanId = activity.SpanId.ToString(),
            ParentSpanId = activity.ParentSpanId.ToString(),
            OperationName = activity.OperationName,
            DisplayName = activity.DisplayName,
            StartTime = activity.StartTimeUtc.ToString("o", CultureInfo.InvariantCulture),
            Duration = activity.Duration.TotalMilliseconds,
            Status = activity.Status.ToString(),
            StatusDescription = activity.StatusDescription,
            Kind = activity.Kind.ToString(),
            Source = activity.Source.Name,
            Tags = tags,
            Events = events,
        };
    }

    private string GetTraceFilePath(DateTime date) =>
        Path.Combine(this.traceDirectory, $"traces-{date:yyyy-MM-dd}.jsonl");

    private void RotateFileIfNeeded()
    {
        var today = DateTime.UtcNow.Date;
        if (today != this.currentFileDate)
        {
            this.currentFileDate = today;
            this.currentFilePath = this.GetTraceFilePath(today);
            this.CleanupOldFiles();
        }
    }

    private void CleanupOldFiles()
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.Date.AddDays(-this.retentionDays);
            var files = Directory.GetFiles(this.traceDirectory, "traces-*.jsonl");

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.StartsWith("traces-", StringComparison.Ordinal) &&
                    DateTime.TryParseExact(
                        fileName["traces-".Length..],
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var fileDate) &&
                    fileDate < cutoffDate)
                {
                    File.Delete(file);
                }
            }
        }
        catch (IOException)
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Represents a single trace record for JSON serialization.
    /// </summary>
    private sealed class TraceRecord
    {
        /// <summary>
        /// Gets or sets the trace ID.
        /// </summary>
        public string? TraceId { get; set; }

        /// <summary>
        /// Gets or sets the span ID.
        /// </summary>
        public string? SpanId { get; set; }

        /// <summary>
        /// Gets or sets the parent span ID.
        /// </summary>
        public string? ParentSpanId { get; set; }

        /// <summary>
        /// Gets or sets the operation name.
        /// </summary>
        public string? OperationName { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the start time in ISO 8601 format.
        /// </summary>
        public string? StartTime { get; set; }

        /// <summary>
        /// Gets or sets the duration in milliseconds.
        /// </summary>
        public double Duration { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the status description.
        /// </summary>
        public string? StatusDescription { get; set; }

        /// <summary>
        /// Gets or sets the span kind.
        /// </summary>
        public string? Kind { get; set; }

        /// <summary>
        /// Gets or sets the source name.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the tags/attributes.
        /// </summary>
        public Dictionary<string, object?>? Tags { get; set; }

        /// <summary>
        /// Gets or sets the events.
        /// </summary>
        public List<TraceEvent>? Events { get; set; }
    }

    /// <summary>
    /// Represents a trace event.
    /// </summary>
    private sealed class TraceEvent
    {
        /// <summary>
        /// Gets or sets the event name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public string? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the attributes.
        /// </summary>
        public Dictionary<string, object?>? Attributes { get; set; }
    }
}
