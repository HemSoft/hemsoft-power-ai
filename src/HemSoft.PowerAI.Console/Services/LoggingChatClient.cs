// <copyright file="LoggingChatClient.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Services;

using System.ClientModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.AI;

using Spectre.Console;

/// <summary>
/// A chat client decorator that logs request/response timing and adds rate limit delays.
/// </summary>
/// <param name="innerClient">The inner chat client to wrap.</param>
/// <param name="minDelayBetweenRequests">Minimum delay between API requests to avoid rate limiting.</param>
[ExcludeFromCodeCoverage(Justification = "Diagnostic wrapper for API calls")]
internal sealed class LoggingChatClient(
    IChatClient innerClient,
    TimeSpan? minDelayBetweenRequests = null) : IChatClient
{
    private readonly TimeSpan minDelay = minDelayBetweenRequests ?? TimeSpan.FromMilliseconds(500);
    private readonly Stopwatch timeSinceLastRequest = Stopwatch.StartNew();
    private int requestCount;

    /// <summary>
    /// Gets a response from the chat client.
    /// </summary>
    /// <param name="messages">The chat messages.</param>
    /// <param name="options">The chat options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The chat response.</returns>
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await this.EnsureRateLimitDelayAsync(cancellationToken).ConfigureAwait(false);

        var requestNum = Interlocked.Increment(ref this.requestCount);
        var toolCount = options?.Tools?.Count ?? 0;

        LogRequestStart(requestNum, toolCount);
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await innerClient
                .GetResponseAsync(messages, options, cancellationToken)
                .ConfigureAwait(false);

            sw.Stop();
            this.timeSinceLastRequest.Restart();
            LogSuccessResponse(requestNum, sw.Elapsed, response);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            this.timeSinceLastRequest.Restart();
            LogExceptionAndRethrow(requestNum, sw.Elapsed, ex);
            throw; // Unreachable but required for compiler
        }
    }

    /// <summary>
    /// Gets a streaming response from the chat client.
    /// </summary>
    /// <param name="messages">The chat messages.</param>
    /// <param name="options">The chat options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The streaming chat response updates.</returns>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestNum = Interlocked.Increment(ref this.requestCount);
        AnsiConsole.MarkupLine(
            "[dim]    API #" + requestNum.ToString(CultureInfo.InvariantCulture) +
            ": Starting streaming request...[/]");
        var sw = Stopwatch.StartNew();

        var chunkCount = 0;
        await foreach (var update in innerClient
            .GetStreamingResponseAsync(messages, options, cancellationToken)
            .ConfigureAwait(false))
        {
            chunkCount++;
            yield return update;
        }

        sw.Stop();
        AnsiConsole.MarkupLine(
            $"[dim]    API #{requestNum.ToString(CultureInfo.InvariantCulture)}: " +
            $"Stream complete in {sw.Elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)}s " +
            $"({chunkCount.ToString(CultureInfo.InvariantCulture)} chunks)[/]");
    }

    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <typeparam name="TService">The type of service.</typeparam>
    /// <param name="key">The optional key.</param>
    /// <returns>The service instance or null.</returns>
    public TService? GetService<TService>(object? key = null)
        where TService : class =>
        innerClient.GetService<TService>(key);

    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <param name="serviceType">The type of service.</param>
    /// <param name="serviceKey">The optional key.</param>
    /// <returns>The service instance or null.</returns>
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        innerClient.GetService(serviceType, serviceKey);

    /// <summary>
    /// Disposes resources. Note: This decorator does not own the inner client.
    /// </summary>
    public void Dispose()
    {
        // Do not dispose innerClient - it's owned by CompositeDisposableChatClient
    }

    private static void LogRequestStart(int requestNum, int toolCount) =>
        AnsiConsole.MarkupLine(
            $"[dim]    API #{requestNum.ToString(CultureInfo.InvariantCulture)}: " +
            $"Sending request ({toolCount.ToString(CultureInfo.InvariantCulture)} tools available)...[/]");

    private static void LogSuccessResponse(int requestNum, TimeSpan elapsed, ChatResponse response)
    {
        var contentLength = response.Text?.Length ?? 0;
        var finishReason = response.FinishReason?.ToString() ?? "unknown";
        var toolCalls = response.Messages
            .SelectMany(m => m.Contents.OfType<FunctionCallContent>())
            .Select(f => f.Name)
            .ToList();

        var message = toolCalls.Count > 0
            ? $"[dim]    API #{requestNum.ToString(CultureInfo.InvariantCulture)}: " +
              $"Response in {elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)}s - " +
              $"Tool calls: {string.Join(", ", toolCalls)}[/]"
            : $"[dim]    API #{requestNum.ToString(CultureInfo.InvariantCulture)}: " +
              $"Response in {elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)}s - " +
              $"{contentLength.ToString(CultureInfo.InvariantCulture)} chars, finish: {finishReason}[/]";
        AnsiConsole.MarkupLine(message);
    }

    private static void LogExceptionAndRethrow(int requestNum, TimeSpan elapsed, Exception ex)
    {
        var elapsedStr = elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture);
        var requestStr = requestNum.ToString(CultureInfo.InvariantCulture);

        switch (ex)
        {
            case OperationCanceledException:
                AnsiConsole.MarkupLine($"[yellow]    API #{requestStr}: Cancelled after {elapsedStr}s[/]");
                break;
            case ClientResultException cre:
                var statusText = cre.Status switch
                {
                    429 => "Rate Limited (429)",
                    503 => "Service Unavailable (503)",
                    502 => "Bad Gateway (502)",
                    504 => "Gateway Timeout (504)",
                    _ => "HTTP " + cre.Status.ToString(CultureInfo.InvariantCulture),
                };
                AnsiConsole.MarkupLine($"[red]    API #{requestStr}: {statusText} after {elapsedStr}s[/]");
                break;
            default:
                var errorType = Markup.Escape(ex.GetType().Name);
                AnsiConsole.MarkupLine($"[red]    API #{requestStr}: Failed after {elapsedStr}s - {errorType}[/]");
                break;
        }
    }

    private async Task EnsureRateLimitDelayAsync(CancellationToken cancellationToken)
    {
        var elapsed = this.timeSinceLastRequest.Elapsed;
        if (elapsed < this.minDelay)
        {
            var waitTime = this.minDelay - elapsed;
            await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
        }
    }
}
