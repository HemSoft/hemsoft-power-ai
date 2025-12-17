// <copyright file="AgentWorkerService.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.AgentWorker;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using HemSoft.PowerAI.Common.Agents;
using HemSoft.PowerAI.Common.Models;
using HemSoft.PowerAI.Common.Services;

/// <summary>
/// Background service that processes agent tasks from the Redis pub/sub channel.
/// Subscribes to the agents:tasks channel and routes to appropriate agents.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Background service requires Redis infrastructure for integration testing.")]
internal sealed partial class AgentWorkerService : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IAgentTaskBroker broker;
    private readonly ILogger<AgentWorkerService> logger;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentWorkerService"/> class.
    /// </summary>
    /// <param name="broker">The task broker for receiving and publishing tasks.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public AgentWorkerService(IAgentTaskBroker broker, ILogger<AgentWorkerService> logger)
        : this(broker, logger, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentWorkerService"/> class.
    /// </summary>
    /// <param name="broker">The task broker for receiving and publishing tasks.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    public AgentWorkerService(
        IAgentTaskBroker broker,
        ILogger<AgentWorkerService> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(broker);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.broker = broker;
        this.logger = logger;
        this.timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogServiceStarting(this.logger);

        try
        {
            await this.broker.SubscribeToTasksAsync(
                this.ProcessTaskAsync,
                stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogServiceStopping(this.logger);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "AgentWorkerService starting. Subscribing to task queue...")]
    private static partial void LogServiceStarting(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "AgentWorkerService stopping due to cancellation.")]
    private static partial void LogServiceStopping(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing task {TaskId} of type {AgentType}")]
    private static partial void LogProcessingTask(ILogger logger, string taskId, string agentType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Task {TaskId} completed successfully")]
    private static partial void LogTaskCompleted(ILogger logger, string taskId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Task {TaskId} was cancelled")]
    private static partial void LogTaskCancelled(ILogger logger, string taskId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Task {TaskId} failed with error: {ErrorMessage}")]
    private static partial void LogTaskFailed(ILogger logger, Exception ex, string taskId, string errorMessage);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Creating ResearchAgent for prompt: {Prompt}")]
    private static partial void LogCreatingResearchAgent(ILogger logger, string prompt);

    private async Task ProcessTaskAsync(AgentTaskRequest request, CancellationToken cancellationToken)
    {
        LogProcessingTask(this.logger, request.TaskId, request.AgentType);

        AgentTaskResult result;

        try
        {
            var data = await this.ExecuteAgentAsync(request, cancellationToken).ConfigureAwait(false);

            result = new AgentTaskResult(
                TaskId: request.TaskId,
                Status: AgentTaskStatus.Completed,
                Data: data,
                Error: null,
                CompletedAt: this.timeProvider.GetUtcNow());

            LogTaskCompleted(this.logger, request.TaskId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            result = new AgentTaskResult(
                TaskId: request.TaskId,
                Status: AgentTaskStatus.Cancelled,
                Data: null,
                Error: "Task was cancelled.",
                CompletedAt: this.timeProvider.GetUtcNow());

            LogTaskCancelled(this.logger, request.TaskId);
        }
        catch (NotSupportedException ex)
        {
            result = new AgentTaskResult(
                TaskId: request.TaskId,
                Status: AgentTaskStatus.Failed,
                Data: null,
                Error: ex.Message,
                CompletedAt: this.timeProvider.GetUtcNow());

            LogTaskFailed(this.logger, ex, request.TaskId, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            result = new AgentTaskResult(
                TaskId: request.TaskId,
                Status: AgentTaskStatus.Failed,
                Data: null,
                Error: ex.Message,
                CompletedAt: this.timeProvider.GetUtcNow());

            LogTaskFailed(this.logger, ex, request.TaskId, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            result = new AgentTaskResult(
                TaskId: request.TaskId,
                Status: AgentTaskStatus.Failed,
                Data: null,
                Error: ex.Message,
                CompletedAt: this.timeProvider.GetUtcNow());

            LogTaskFailed(this.logger, ex, request.TaskId, ex.Message);
        }

        await this.broker.PublishResultAsync(result, cancellationToken).ConfigureAwait(false);
    }

    private Task<JsonDocument?> ExecuteAgentAsync(
        AgentTaskRequest request,
        CancellationToken cancellationToken) =>
        request.AgentType.ToUpperInvariant() switch
        {
            "RESEARCH" => this.ExecuteResearchAgentAsync(request.Prompt, cancellationToken),
            _ => throw new NotSupportedException($"Unknown agent type: {request.AgentType}"),
        };

    private async Task<JsonDocument?> ExecuteResearchAgentAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        LogCreatingResearchAgent(this.logger, prompt);

        var agent = ResearchAgent.Create();
        var response = await agent.RunAsync(prompt, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Wrap the text response in a structured JSON document
        // Phase 9.4 will enhance ResearchAgent to return structured data directly
        var resultData = new
        {
            text = response.Text ?? string.Empty,
            agentType = "research",
            timestamp = this.timeProvider.GetUtcNow(),
        };

        var json = JsonSerializer.Serialize(resultData, SerializerOptions);

        return JsonDocument.Parse(json);
    }
}
