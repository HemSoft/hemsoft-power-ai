// <copyright file="IAgentTaskBroker.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Services;

using HemSoft.PowerAI.Common.Models;

/// <summary>
/// Defines operations for submitting and consuming agent tasks via a message broker.
/// </summary>
public interface IAgentTaskBroker
{
    /// <summary>
    /// Submits a new task to the broker.
    /// </summary>
    /// <param name="request">The task request to submit.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubmitTaskAsync(AgentTaskRequest request);

    /// <summary>
    /// Submits a new task to the broker with cancellation support.
    /// </summary>
    /// <param name="request">The task request to submit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubmitTaskAsync(AgentTaskRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes to incoming tasks from the broker.
    /// </summary>
    /// <param name="handler">Handler invoked for each received task.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the subscription lifetime.</returns>
    Task SubscribeToTasksAsync(Func<AgentTaskRequest, CancellationToken, Task> handler, CancellationToken cancellationToken);

    /// <summary>
    /// Publishes a task result to the results channel.
    /// </summary>
    /// <param name="result">The task result to publish.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishResultAsync(AgentTaskResult result);

    /// <summary>
    /// Publishes a task result to the results channel with cancellation support.
    /// </summary>
    /// <param name="result">The task result to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishResultAsync(AgentTaskResult result, CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes to results for a specific task.
    /// </summary>
    /// <param name="taskId">The task ID to listen for.</param>
    /// <param name="handler">Handler invoked when the result is received.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the result is received or cancelled.</returns>
    Task SubscribeToResultAsync(string taskId, Action<AgentTaskResult> handler, CancellationToken cancellationToken);
}
