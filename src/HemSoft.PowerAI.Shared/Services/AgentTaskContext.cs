// <copyright file="AgentTaskContext.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Services;

using HemSoft.PowerAI.Common.Models;

/// <summary>
/// Provides context for the currently executing agent task.
/// Used by tools to report progress back to the client.
/// </summary>
public sealed class AgentTaskContext
{
    private static readonly AsyncLocal<AgentTaskContext?> Current = new();

    private readonly IAgentTaskBroker broker;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentTaskContext"/> class.
    /// </summary>
    /// <param name="taskId">The task ID for progress reporting.</param>
    /// <param name="broker">The broker for publishing progress.</param>
    /// <param name="timeProvider">Time provider for timestamps.</param>
    public AgentTaskContext(string taskId, IAgentTaskBroker broker, TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentNullException.ThrowIfNull(broker);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.TaskId = taskId;
        this.broker = broker;
        this.timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the current task context, or null if not in a task.
    /// </summary>
    public static AgentTaskContext? Instance => Current.Value;

    /// <summary>
    /// Gets the task ID.
    /// </summary>
    public string TaskId { get; }

    /// <summary>
    /// Gets or sets the current agent name for progress reporting.
    /// </summary>
    public string? CurrentAgentName { get; set; }

    /// <summary>
    /// Gets or sets the current model ID for progress reporting.
    /// </summary>
    public string? CurrentModelId { get; set; }

    /// <summary>
    /// Gets or sets the cumulative input tokens for the current agent.
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the cumulative output tokens for the current agent.
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Sets the current task context for the async flow.
    /// </summary>
    /// <param name="context">The context to set.</param>
    /// <returns>A disposable that resets the context when disposed.</returns>
    public static IDisposable SetCurrent(AgentTaskContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var previous = Current.Value;
        Current.Value = context;
        return new ContextScope(previous);
    }

    /// <summary>
    /// Reports progress for the current task.
    /// </summary>
    /// <param name="message">The progress message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public Task ReportProgressAsync(string message, CancellationToken cancellationToken)
    {
        var progress = new AgentTaskProgress(
            TaskId: this.TaskId,
            Message: message,
            Timestamp: this.timeProvider.GetUtcNow(),
            ToolName: null,
            AgentName: this.CurrentAgentName,
            ModelId: this.CurrentModelId,
            InputTokens: this.InputTokens > 0 ? this.InputTokens : null,
            OutputTokens: this.OutputTokens > 0 ? this.OutputTokens : null);

        return this.broker.PublishProgressAsync(progress, cancellationToken);
    }

    /// <summary>
    /// Reports progress for the current task with tool name.
    /// </summary>
    /// <param name="message">The progress message.</param>
    /// <param name="toolName">The tool name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public Task ReportProgressAsync(string message, string toolName, CancellationToken cancellationToken)
    {
        var progress = new AgentTaskProgress(
            TaskId: this.TaskId,
            Message: message,
            Timestamp: this.timeProvider.GetUtcNow(),
            ToolName: toolName,
            AgentName: this.CurrentAgentName,
            ModelId: this.CurrentModelId,
            InputTokens: this.InputTokens > 0 ? this.InputTokens : null,
            OutputTokens: this.OutputTokens > 0 ? this.OutputTokens : null);

        return this.broker.PublishProgressAsync(progress, cancellationToken);
    }

    /// <summary>
    /// Updates token usage from a chat response.
    /// </summary>
    /// <param name="inputTokens">Input tokens from this response.</param>
    /// <param name="outputTokens">Output tokens from this response.</param>
    public void AddTokenUsage(int inputTokens, int outputTokens)
    {
        this.InputTokens += inputTokens;
        this.OutputTokens += outputTokens;
    }

    private sealed class ContextScope(AgentTaskContext? previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }
}
