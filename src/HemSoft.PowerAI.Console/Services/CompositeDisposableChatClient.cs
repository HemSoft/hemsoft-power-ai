// <copyright file="CompositeDisposableChatClient.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Services;

using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.AI;

using OpenAI;

/// <summary>
/// A chat client wrapper that ensures proper disposal of both the built client and its inner client.
/// </summary>
/// <remarks>
/// When using the ChatClientBuilder pattern with .AsIChatClient().AsBuilder().Build(),
/// the resulting client does not automatically dispose the inner client. This wrapper
/// ensures both are disposed correctly, satisfying IDisposableAnalyzers requirements.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Chat client wrapper requires external API")]
internal sealed class CompositeDisposableChatClient : IChatClient
{
    private readonly IChatClient builtClient;
    private readonly IDisposable innerClientDisposable;
    private readonly IDisposable? loggingClientDisposable;
    private bool disposed;

    private CompositeDisposableChatClient(
        IChatClient builtClient,
        IDisposable innerClientDisposable,
        IDisposable? loggingClientDisposable = null)
    {
        this.builtClient = builtClient;
        this.innerClientDisposable = innerClientDisposable;
        this.loggingClientDisposable = loggingClientDisposable;
    }

    /// <summary>
    /// Creates a chat client with function invocation support from an OpenAI client.
    /// </summary>
    /// <param name="openAiClient">The OpenAI client.</param>
    /// <param name="modelId">The model ID to use.</param>
    /// <param name="enableLogging">Whether to enable request/response logging.</param>
    /// <returns>A composite chat client that properly disposes all resources.</returns>
    public static CompositeDisposableChatClient CreateWithFunctionInvocation(
        OpenAIClient openAiClient,
        string modelId,
        bool enableLogging = false) =>
        enableLogging
            ? CreateWithLogging(openAiClient, modelId)
            : CreateWithoutLogging(openAiClient, modelId);

    /// <summary>
    /// Gets a response from the chat client.
    /// </summary>
    /// <param name="messages">The chat messages.</param>
    /// <param name="options">The chat options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The chat response.</returns>
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        this.builtClient.GetResponseAsync(messages, options, cancellationToken);

    /// <summary>
    /// Gets a streaming response from the chat client.
    /// </summary>
    /// <param name="messages">The chat messages.</param>
    /// <param name="options">The chat options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The streaming chat response updates.</returns>
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        this.builtClient.GetStreamingResponseAsync(messages, options, cancellationToken);

    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <typeparam name="TService">The type of service.</typeparam>
    /// <param name="key">The optional key.</param>
    /// <returns>The service instance or null.</returns>
    public TService? GetService<TService>(object? key = null)
        where TService : class =>
        this.builtClient.GetService<TService>(key);

    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <param name="serviceType">The type of service.</param>
    /// <param name="serviceKey">The optional key.</param>
    /// <returns>The service instance or null.</returns>
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        this.builtClient.GetService(serviceType, serviceKey);

    /// <summary>
    /// Disposes both the built client and the inner client.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.builtClient.Dispose();
        this.loggingClientDisposable?.Dispose();
        this.innerClientDisposable.Dispose();
        this.disposed = true;
    }

    private static CompositeDisposableChatClient CreateWithLogging(OpenAIClient openAiClient, string modelId)
    {
        var innerClient = openAiClient.GetChatClient(modelId).AsIChatClient();
        LoggingChatClient? loggingClient = null;
        IChatClient? wrappedClient = null;
        var success = false;
        try
        {
            loggingClient = new LoggingChatClient(innerClient);
            wrappedClient = loggingClient.AsBuilder().UseFunctionInvocation().Build();
            var result = new CompositeDisposableChatClient(wrappedClient, innerClient, loggingClient);
            success = true;
            return result;
        }
        finally
        {
            if (!success)
            {
                wrappedClient?.Dispose();
                loggingClient?.Dispose();
                innerClient.Dispose();
            }
        }
    }

    private static CompositeDisposableChatClient CreateWithoutLogging(OpenAIClient openAiClient, string modelId)
    {
        var innerClient = openAiClient.GetChatClient(modelId).AsIChatClient();
        IChatClient? wrappedClient = null;
        var success = false;
        try
        {
            wrappedClient = innerClient.AsBuilder().UseFunctionInvocation().Build();
            var result = new CompositeDisposableChatClient(wrappedClient, innerClient, loggingClientDisposable: null);
            success = true;
            return result;
        }
        finally
        {
            if (!success)
            {
                wrappedClient?.Dispose();
                innerClient.Dispose();
            }
        }
    }
}
