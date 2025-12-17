// <copyright file="RedisAgentTaskBroker.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Services;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using HemSoft.PowerAI.Common.Models;

using StackExchange.Redis;

/// <summary>
/// Redis-based implementation of the agent task broker.
/// Uses Redis pub/sub for real-time task and result delivery.
/// </summary>
/// <remarks>
/// Excluded from code coverage because it requires actual Redis infrastructure.
/// Constructor argument validation is tested; Redis operations require integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Requires Redis infrastructure for integration testing.")]
public sealed class RedisAgentTaskBroker : IAgentTaskBroker, IAsyncDisposable
{
    private const string TasksChannel = "agents:tasks";
    private const string ResultsChannelPrefix = "agents:results:";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly ConnectionMultiplexer connection;
    private readonly ISubscriber subscriber;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisAgentTaskBroker"/> class.
    /// </summary>
    /// <param name="connectionString">Redis connection string.</param>
    public RedisAgentTaskBroker(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        this.connection = ConnectionMultiplexer.Connect(connectionString);
        this.subscriber = this.connection.GetSubscriber();
    }

    /// <inheritdoc/>
    public Task SubmitTaskAsync(AgentTaskRequest request) =>
        this.SubmitTaskAsync(request, CancellationToken.None);

    /// <inheritdoc/>
    public Task SubmitTaskAsync(AgentTaskRequest request, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        return this.SubmitTaskCoreAsync(request);
    }

    /// <inheritdoc/>
    public Task SubscribeToTasksAsync(
        Func<AgentTaskRequest, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        ArgumentNullException.ThrowIfNull(handler);

        return this.SubscribeToTasksCoreAsync(handler, cancellationToken);
    }

    /// <inheritdoc/>
    public Task PublishResultAsync(AgentTaskResult result) =>
        this.PublishResultAsync(result, CancellationToken.None);

    /// <inheritdoc/>
    public Task PublishResultAsync(AgentTaskResult result, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        ArgumentNullException.ThrowIfNull(result);

        return this.PublishResultCoreAsync(result);
    }

    /// <inheritdoc/>
    public Task SubscribeToResultAsync(
        string taskId,
        Action<AgentTaskResult> handler,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentNullException.ThrowIfNull(handler);

        return this.SubscribeToResultCoreAsync(taskId, handler, cancellationToken);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        await this.connection.CloseAsync().ConfigureAwait(false);
        await this.connection.DisposeAsync().ConfigureAwait(false);
    }

    private async Task SubmitTaskCoreAsync(AgentTaskRequest request)
    {
        var json = JsonSerializer.Serialize(request, SerializerOptions);
        _ = await this.subscriber.PublishAsync(RedisChannel.Literal(TasksChannel), json).ConfigureAwait(false);
    }

    private async Task SubscribeToTasksCoreAsync(
        Func<AgentTaskRequest, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();
        var registration = cancellationToken.Register(() => tcs.TrySetResult());
        await using (registration.ConfigureAwait(false))
        {
            var channel = RedisChannel.Literal(TasksChannel);
            var messageQueue = await this.subscriber.SubscribeAsync(channel).ConfigureAwait(false);

            _ = ProcessMessagesAsync(messageQueue, handler, cancellationToken).ConfigureAwait(false);

            await tcs.Task.ConfigureAwait(false);
            await this.subscriber.UnsubscribeAsync(channel).ConfigureAwait(false);
        }

        static async Task ProcessMessagesAsync(
            ChannelMessageQueue queue,
            Func<AgentTaskRequest, CancellationToken, Task> taskHandler,
            CancellationToken ct)
        {
            await foreach (var message in queue.WithCancellation(ct).ConfigureAwait(false))
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                var json = message.Message.ToString();
                var request = JsonSerializer.Deserialize<AgentTaskRequest>(json, SerializerOptions);
                if (request is not null)
                {
                    await taskHandler(request, ct).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task PublishResultCoreAsync(AgentTaskResult result)
    {
        var channel = ResultsChannelPrefix + result.TaskId;
        var json = JsonSerializer.Serialize(result, SerializerOptions);
        _ = await this.subscriber.PublishAsync(RedisChannel.Literal(channel), json).ConfigureAwait(false);
    }

    private async Task SubscribeToResultCoreAsync(
        string taskId,
        Action<AgentTaskResult> handler,
        CancellationToken cancellationToken)
    {
        var channel = ResultsChannelPrefix + taskId;
        var tcs = new TaskCompletionSource();
        var registration = cancellationToken.Register(() => tcs.TrySetResult());
        await using (registration.ConfigureAwait(false))
        {
            var redisChannel = RedisChannel.Literal(channel);
            var messageQueue = await this.subscriber.SubscribeAsync(redisChannel).ConfigureAwait(false);

            _ = ProcessResultMessagesAsync(messageQueue, handler, tcs, cancellationToken).ConfigureAwait(false);

            await tcs.Task.ConfigureAwait(false);
            await this.subscriber.UnsubscribeAsync(redisChannel).ConfigureAwait(false);
        }

        static async Task ProcessResultMessagesAsync(
            ChannelMessageQueue queue,
            Action<AgentTaskResult> resultHandler,
            TaskCompletionSource completionSource,
            CancellationToken ct)
        {
            await foreach (var message in queue.WithCancellation(ct).ConfigureAwait(false))
            {
                var json = message.Message.ToString();
                var result = JsonSerializer.Deserialize<AgentTaskResult>(json, SerializerOptions);
                if (result is not null)
                {
                    resultHandler(result);
                    completionSource.TrySetResult();
                    break;
                }
            }
        }
    }
}
