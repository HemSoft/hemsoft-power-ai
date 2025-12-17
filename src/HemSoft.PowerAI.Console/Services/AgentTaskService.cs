// <copyright file="AgentTaskService.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Services;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

using HemSoft.PowerAI.Common.Models;
using HemSoft.PowerAI.Common.Services;

using Spectre.Console;

/// <summary>
/// Provides high-level operations for submitting agent tasks and tracking results.
/// Acts as a facade over the <see cref="IAgentTaskBroker"/> for the console UI.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "UI service with async Redis operations requires integration testing.")]
internal sealed class AgentTaskService : IAsyncDisposable
{
    private readonly IAgentTaskBroker broker;
    private readonly TimeProvider timeProvider;
    private readonly ConcurrentDictionary<string, AgentTaskResult> completedTasks = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<AgentTaskResult>> pendingTasks = new(StringComparer.Ordinal);
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentTaskService"/> class.
    /// </summary>
    /// <param name="broker">The task broker for Redis communication.</param>
    public AgentTaskService(IAgentTaskBroker broker)
        : this(broker, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentTaskService"/> class.
    /// </summary>
    /// <param name="broker">The task broker for Redis communication.</param>
    /// <param name="timeProvider">Time provider for timestamps.</param>
    public AgentTaskService(IAgentTaskBroker broker, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(broker);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.broker = broker;
        this.timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets a value indicating whether there are any pending tasks.
    /// </summary>
    public bool HasPendingTasks => !this.pendingTasks.IsEmpty;

    /// <summary>
    /// Gets the count of pending tasks.
    /// </summary>
    public int PendingTaskCount => this.pendingTasks.Count;

    /// <summary>
    /// Displays a task result in a formatted panel.
    /// </summary>
    /// <param name="result">The result to display.</param>
    public static void DisplayResult(AgentTaskResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var statusColor = result.Status switch
        {
            AgentTaskStatus.Completed => "green",
            AgentTaskStatus.Failed => "red",
            AgentTaskStatus.Cancelled => "yellow",
            AgentTaskStatus.Pending => "blue",
            AgentTaskStatus.Running => "blue",
            _ => "dim",
        };

        var statusText = "[" + statusColor + "]" + result.Status + "[/]";

        if (result.Status == AgentTaskStatus.Completed && result.Data is not null)
        {
            var text = ExtractTextFromResult(result.Data);
            AnsiConsole.Write(new Panel(Markup.Escape(text))
                .Header("[cyan]Research Complete[/] " + statusText)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan));
        }
        else if (result.Status == AgentTaskStatus.Failed)
        {
            AnsiConsole.Write(new Panel("[red]" + Markup.Escape(result.Error ?? "Unknown error") + "[/]")
                .Header("[red]Task Failed[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Red));
        }
        else
        {
            AnsiConsole.MarkupLine("Task " + result.TaskId[..8] + "... " + statusText);
        }
    }

    /// <summary>
    /// Submits a research task and returns the task ID.
    /// </summary>
    /// <param name="prompt">The research prompt.</param>
    /// <param name="outputPath">Optional file path to write result to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The task ID for tracking.</returns>
    public Task<string> SubmitResearchTaskAsync(
        string prompt,
        string? outputPath = null,
        CancellationToken cancellationToken = default) =>
        this.SubmitTaskAsync("research", prompt, outputPath, cancellationToken);

    /// <summary>
    /// Submits an iterative research task and returns the task ID.
    /// </summary>
    /// <param name="prompt">The research prompt.</param>
    /// <param name="outputPath">Optional file path to write result to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The task ID for tracking.</returns>
    public Task<string> SubmitIterativeResearchTaskAsync(
        string prompt,
        string? outputPath = null,
        CancellationToken cancellationToken = default) =>
        this.SubmitTaskAsync("iterative-research", prompt, outputPath, cancellationToken);

    /// <summary>
    /// Subscribes to progress updates for a task.
    /// </summary>
    /// <param name="taskId">The task ID to monitor.</param>
    /// <param name="onProgress">Callback for each progress update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that runs until cancelled or task completes.</returns>
    public Task SubscribeToProgressAsync(
        string taskId,
        Action<AgentTaskProgress> onProgress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentNullException.ThrowIfNull(onProgress);

        return this.broker.SubscribeToProgressAsync(taskId, onProgress, cancellationToken);
    }

    /// <summary>
    /// Waits for a specific task to complete.
    /// </summary>
    /// <param name="taskId">The task ID to wait for.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The task result, or null if timeout or cancelled.</returns>
    public async Task<AgentTaskResult?> WaitForResultAsync(
        string taskId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        // Check if already completed
        if (this.completedTasks.TryGetValue(taskId, out var completed))
        {
            return completed;
        }

        // Wait for pending task
        if (this.pendingTasks.TryGetValue(taskId, out var tcs))
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                timeoutCts.Token,
                cancellationToken);

            try
            {
                return await tcs.Task.WaitAsync(linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the result of a completed task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <returns>The result if completed, null otherwise.</returns>
    public AgentTaskResult? GetResult(string taskId) =>
        this.completedTasks.TryGetValue(taskId, out var result) ? result : null;

    /// <summary>
    /// Gets all pending task IDs.
    /// </summary>
    /// <returns>Collection of pending task IDs.</returns>
    public IReadOnlyCollection<string> GetPendingTaskIds() => [.. this.pendingTasks.Keys];

    /// <summary>
    /// Gets all completed tasks.
    /// </summary>
    /// <returns>Collection of completed task results.</returns>
    public IReadOnlyCollection<AgentTaskResult> GetCompletedTasks() => [.. this.completedTasks.Values];

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        if (this.disposed)
        {
            return ValueTask.CompletedTask;
        }

        this.disposed = true;

        // Clear pending tasks
        foreach (var tcs in this.pendingTasks.Values)
        {
            _ = tcs.TrySetCanceled();
        }

        this.pendingTasks.Clear();
        return ValueTask.CompletedTask;
    }

    private static string ExtractTextFromResult(JsonDocument data) =>
        data.RootElement.TryGetProperty("text", out var textElement)
            ? textElement.GetString() ?? string.Empty
            : data.RootElement.GetRawText();

    private async Task<string> SubmitTaskAsync(
        string agentType,
        string prompt,
        string? outputPath,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var taskId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var request = new AgentTaskRequest(
            TaskId: taskId,
            AgentType: agentType,
            Prompt: prompt,
            SubmittedAt: this.timeProvider.GetUtcNow(),
            OutputPath: outputPath);

        // Register a pending task before submitting
        var tcs = new TaskCompletionSource<AgentTaskResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = this.pendingTasks.TryAdd(taskId, tcs);

        // Start listening for this specific task's result
        _ = this.ListenForResultAsync(taskId, cancellationToken);

        await this.broker.SubmitTaskAsync(request, cancellationToken).ConfigureAwait(false);

        return taskId;
    }

    private async Task ListenForResultAsync(string taskId, CancellationToken cancellationToken)
    {
        try
        {
            await this.broker.SubscribeToResultAsync(
                taskId,
                result =>
                {
                    _ = this.completedTasks.TryAdd(taskId, result);

                    if (this.pendingTasks.TryRemove(taskId, out var tcs))
                    {
                        _ = tcs.TrySetResult(result);
                    }
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
    }
}
