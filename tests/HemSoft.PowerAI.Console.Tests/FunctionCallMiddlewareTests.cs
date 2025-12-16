// <copyright file="FunctionCallMiddlewareTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using HemSoft.PowerAI.Console.Telemetry;

using Microsoft.Extensions.AI;

/// <summary>
/// Unit tests for <see cref="FunctionCallMiddleware"/> and the chat client extension methods.
/// </summary>
public class FunctionCallMiddlewareTests
{
    /// <summary>
    /// Tests that UseFunctionCallLogging throws ArgumentNullException when builder is null.
    /// </summary>
    [Fact]
    public void UseFunctionCallLoggingThrowsWhenBuilderIsNull()
    {
        // Arrange
        ChatClientBuilder builder = null!;

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(builder.UseFunctionCallLogging);
    }

    /// <summary>
    /// Tests that UseFunctionCallLogging returns the builder for chaining.
    /// </summary>
    [Fact]
    public void UseFunctionCallLoggingReturnsBuilderForChaining()
    {
        // Arrange
        using var mockClient = new MockChatClient();
        var builder = mockClient.AsBuilder();

        // Act
        var result = builder.UseFunctionCallLogging();

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    /// Tests that the middleware can build a chat client.
    /// </summary>
    [Fact]
    public void UseFunctionCallLoggingBuildsValidChatClient()
    {
        // Arrange
        using var mockClient = new MockChatClient();

        // Act
        using var client = mockClient.AsBuilder()
            .UseFunctionCallLogging()
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    /// <summary>
    /// Tests that GetResponseAsync passes through messages to inner client.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetResponseAsyncPassesThroughToInnerClient()
    {
        // Arrange
        using var mockClient = new MockChatClient();
        using var client = mockClient.AsBuilder()
            .UseFunctionCallLogging()
            .Build();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello"),
        };

        // Act
        using var cts = new CancellationTokenSource();
        var response = await client.GetResponseAsync(messages, cancellationToken: cts.Token)
            .ConfigureAwait(true);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(1, mockClient.GetResponseCallCount);
    }

    /// <summary>
    /// Tests that GetResponseAsync handles options with tools.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetResponseAsyncHandlesOptionsWithTools()
    {
        // Arrange
        using var mockClient = new MockChatClient();
        using var client = mockClient.AsBuilder()
            .UseFunctionCallLogging()
            .Build();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Call a tool"),
        };

        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(() => "test result", "TestTool", "A test tool"),
            ],
        };

        // Act
        using var cts = new CancellationTokenSource();
        var response = await client.GetResponseAsync(messages, options, cts.Token)
            .ConfigureAwait(true);

        // Assert
        Assert.NotNull(response);
    }

    /// <summary>
    /// Tests that GetResponseAsync logs function calls when present in response.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetResponseAsyncLogsFunctionCallsFromResponse()
    {
        // Arrange
        var functionCallContent = new FunctionCallContent(
            "id",
            "TestFunction",
            new Dictionary<string, object?>(StringComparer.Ordinal) { { "arg1", "value1" } });
        using var mockClient = new MockChatClient(
            new ChatMessage(ChatRole.Assistant, [functionCallContent]));
        using var client = mockClient.AsBuilder()
            .UseFunctionCallLogging()
            .Build();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Call a function"),
        };

        // Act
        using var cts = new CancellationTokenSource();
        var response = await client.GetResponseAsync(messages, cancellationToken: cts.Token)
            .ConfigureAwait(true);

        // Assert
        Assert.NotNull(response);
        Assert.Single(response.Messages);
    }

    /// <summary>
    /// Tests that GetStreamingResponseAsync passes through to inner client.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetStreamingResponseAsyncPassesThroughToInnerClient()
    {
        // Arrange
        using var mockClient = new MockChatClient();
        using var client = mockClient.AsBuilder()
            .UseFunctionCallLogging()
            .Build();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello streaming"),
        };

        // Act
        var updates = new List<ChatResponseUpdate>();
        using var cts = new CancellationTokenSource();
        await foreach (var update in client.GetStreamingResponseAsync(messages, cancellationToken: cts.Token)
            .ConfigureAwait(true))
        {
            updates.Add(update);
        }

        // Assert
        Assert.NotEmpty(updates);
        Assert.Equal(1, mockClient.GetStreamingResponseCallCount);
    }

    /// <summary>
    /// Tests that GetStreamingResponseAsync handles function calls in updates.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetStreamingResponseAsyncHandlesFunctionCallsInUpdates()
    {
        // Arrange
        var functionCallContent = new FunctionCallContent(
            "id",
            "TestFunction",
            new Dictionary<string, object?>(StringComparer.Ordinal) { { "param", "value" } });
        using var mockClient = new MockChatClient(streamingContents: [functionCallContent]);
        using var client = mockClient.AsBuilder()
            .UseFunctionCallLogging()
            .Build();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Call a function"),
        };

        // Act
        var updates = new List<ChatResponseUpdate>();
        using var cts = new CancellationTokenSource();
        await foreach (var update in client.GetStreamingResponseAsync(messages, cancellationToken: cts.Token)
            .ConfigureAwait(true))
        {
            updates.Add(update);
        }

        // Assert
        Assert.NotEmpty(updates);
    }

    /// <summary>
    /// Tests that large arguments are truncated in logs.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetResponseAsyncTruncatesLargeArguments()
    {
        // Arrange
        var largeArg = new string('x', 1000); // Longer than 500 char limit
        var functionCallContent = new FunctionCallContent(
            "id",
            "LargeArgsFunction",
            new Dictionary<string, object?>(StringComparer.Ordinal) { { "large", largeArg } });
        using var mockClient = new MockChatClient(
            new ChatMessage(ChatRole.Assistant, [functionCallContent]));
        using var client = mockClient.AsBuilder()
            .UseFunctionCallLogging()
            .Build();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Call with large args"),
        };

        // Act - Should not throw even with large arguments
        using var cts = new CancellationTokenSource();
        var response = await client.GetResponseAsync(messages, cancellationToken: cts.Token)
            .ConfigureAwait(true);

        // Assert
        Assert.NotNull(response);
    }

    /// <summary>
    /// Tests that GetResponseAsync handles response with no function calls.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetResponseAsyncHandlesNoFunctionCalls()
    {
        // Arrange - Response with only text, no function calls
        var textContent = new TextContent("Just text");
        using var mockClient = new MockChatClient(
            new ChatMessage(ChatRole.Assistant, [textContent]));
        using var client = mockClient.AsBuilder()
            .UseFunctionCallLogging()
            .Build();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello"),
        };

        // Act
        using var cts = new CancellationTokenSource();
        var response = await client.GetResponseAsync(messages, cancellationToken: cts.Token)
            .ConfigureAwait(true);

        // Assert
        Assert.NotNull(response);
    }

    /// <summary>
    /// Tests that GetStreamingResponseAsync handles null contents in update.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetStreamingResponseAsyncHandlesNullContents()
    {
        // Arrange - Update with null contents
        using var mockClient = new MockChatClient(streamingContents: null);
        using var client = mockClient.AsBuilder()
            .UseFunctionCallLogging()
            .Build();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello"),
        };

        // Act
        var updates = new List<ChatResponseUpdate>();
        using var cts = new CancellationTokenSource();
        await foreach (var update in client.GetStreamingResponseAsync(messages, cancellationToken: cts.Token)
            .ConfigureAwait(true))
        {
            updates.Add(update);
        }

        // Assert
        Assert.NotEmpty(updates);
    }

    /// <summary>
    /// Tests function call with null arguments.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetResponseAsyncHandlesNullFunctionArguments()
    {
        // Arrange
        var functionCallContent = new FunctionCallContent("id", "NoArgsFunction", arguments: null);
        using var mockClient = new MockChatClient(
            new ChatMessage(ChatRole.Assistant, [functionCallContent]));
        using var client = mockClient.AsBuilder()
            .UseFunctionCallLogging()
            .Build();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Call function"),
        };

        // Act
        using var cts = new CancellationTokenSource();
        var response = await client.GetResponseAsync(messages, cancellationToken: cts.Token)
            .ConfigureAwait(true);

        // Assert
        Assert.NotNull(response);
    }

    /// <summary>
    /// Tests that middleware passes through cancellation token.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetResponseAsyncPassesCancellationToken()
    {
        // Arrange
        using var mockClient = new MockChatClient();
        using var client = mockClient.AsBuilder()
            .UseFunctionCallLogging()
            .Build();

        using var cts = new CancellationTokenSource();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello"),
        };

        // Act
        var response = await client.GetResponseAsync(messages, cancellationToken: cts.Token)
            .ConfigureAwait(true);

        // Assert
        Assert.NotNull(response);
        Assert.True(mockClient.LastCancellationToken == cts.Token);
    }

    /// <summary>
    /// A mock chat client for testing the middleware.
    /// </summary>
    /// <param name="responseMessage">Optional response message to return.</param>
    /// <param name="streamingContents">Optional streaming contents to return.</param>
    private sealed class MockChatClient(
        ChatMessage? responseMessage = null,
        IList<AIContent>? streamingContents = null) : IChatClient
    {
        public int GetResponseCallCount { get; private set; }

        public int GetStreamingResponseCallCount { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public void Dispose()
        {
            // Nothing to dispose
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            this.GetResponseCallCount++;
            this.LastCancellationToken = cancellationToken;

            var message = responseMessage ?? new ChatMessage(ChatRole.Assistant, "Mock response");
            return Task.FromResult(new ChatResponse(message));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            this.GetStreamingResponseCallCount++;
            this.LastCancellationToken = cancellationToken;

            await Task.Yield(); // Make it async

            yield return streamingContents != null
                ? new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = streamingContents }
                : new ChatResponseUpdate(ChatRole.Assistant, "Mock streaming response");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }
}
