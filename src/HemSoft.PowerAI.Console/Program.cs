// <copyright file="Program.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console;

using System.ClientModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using HemSoft.PowerAI.Console.Agents;
using HemSoft.PowerAI.Console.Configuration;
using HemSoft.PowerAI.Console.Services;
using HemSoft.PowerAI.Console.Telemetry;
using HemSoft.PowerAI.Console.Tools;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OpenAI;

using Spectre.Console;

/// <summary>
/// Main entry point for the Agent Demo console application.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Application entry point with interactive console and external API dependencies")]
internal static partial class Program
{
    private const string SourceName = "HemSoft.PowerAI.Console";
    private const string ModelId = "x-ai/grok-4.1-fast";
    private const string OpenRouterBaseUrlEnvVar = "OPENROUTER_BASE_URL";
    private const string CancelledByUserMessage = "[yellow]Operation cancelled by user.[/]";
    private const string CancelledByUserStatus = "Cancelled by user";
    private const string ReturnedToChatModeMessage = "\n[dim]Returned to chat mode.[/]\n";

    private enum ChatCommand
    {
        Empty,
        Exit,
        Clear,
        Usage,
        Spam,
        SpamScan,
        SpamReview,
        SpamCleanup,
        Message,
    }

    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command line arguments. Use 'spam' to run spam filter agent, or pass a prompt to execute and exit.</param>
    /// <returns>Exit code (0 for success, 1 for configuration error).</returns>
    public static async Task<int> Main(string[] args)
    {
        // Initialize OpenTelemetry (traces, metrics, logs)
        // Exports to Aspire Dashboard when OTEL_EXPORTER_OTLP_ENDPOINT is set
        using var telemetry = new TelemetrySetup(SourceName);

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var settings = new SpamFilterSettings();
        configuration.GetSection(SpamFilterSettings.SectionName).Bind(settings);

        if (args.Length == 0)
        {
            return await RunInteractiveChatAsync(settings, telemetry).ConfigureAwait(false);
        }

        // Check for special commands
        if (args[0].Equals("spam", StringComparison.OrdinalIgnoreCase))
        {
            return await RunSpamFilterAgentAsync(settings, telemetry).ConfigureAwait(false);
        }

        // Treat all arguments as a prompt to execute once and exit
        var prompt = string.Join(" ", args);
        return await RunSinglePromptAsync(prompt, settings, telemetry).ConfigureAwait(false);
    }

    private static async Task<int> RunSinglePromptAsync(string prompt, SpamFilterSettings settings, TelemetrySetup telemetry)
    {
        using var activity = telemetry.ActivitySource.StartActivity("RunSinglePrompt");

        var openRouterBaseUrlValue = Environment.GetEnvironmentVariable(OpenRouterBaseUrlEnvVar);
        if (string.IsNullOrEmpty(openRouterBaseUrlValue))
        {
            AnsiConsole.MarkupLine($"[red]Missing {OpenRouterBaseUrlEnvVar} environment variable.[/]");
            activity?.SetStatus(ActivityStatusCode.Error, "Missing OPENROUTER_BASE_URL");
            return 1;
        }

        var openRouterBaseUrl = new Uri(openRouterBaseUrlValue);

        const string apiKeyEnvVar = "OPENROUTER_API_KEY";
        var apiKey = Environment.GetEnvironmentVariable(apiKeyEnvVar);
        if (string.IsNullOrEmpty(apiKey))
        {
            AnsiConsole.MarkupLine($"[red]Missing {apiKeyEnvVar} environment variable.[/]");
            activity?.SetStatus(ActivityStatusCode.Error, "Missing OPENROUTER_API_KEY");
            return 1;
        }

        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = openRouterBaseUrl });

        using var chatClient = CompositeDisposableChatClient.CreateWithFunctionInvocation(openAiClient, ModelId);

        OutlookMailTools.InitializeSpamStorage(settings);

        var tools = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(TerminalTools.Terminal),
                AIFunctionFactory.Create(WebSearchTools.WebSearchAsync),
                AIFunctionFactory.Create(OutlookMailTools.MailAsync),
            ],
        };

        List<ChatMessage> history = [new(ChatRole.User, prompt)];

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var response = await chatClient.GetResponseAsync(history, tools, cts.Token).ConfigureAwait(false);

            var assistantMessage = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant);
            var responseText = assistantMessage?.Text ?? "[No response]";

            AnsiConsole.Write(new Panel(Markup.Escape(responseText))
                .Header("[green]Agent[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green));

            activity?.SetStatus(ActivityStatusCode.Ok);
            return 0;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Network error: {Markup.Escape(ex.Message)}[/]");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return 1;
        }
        catch (TaskCanceledException ex)
        {
            AnsiConsole.MarkupLine($"[red]Request timed out: {Markup.Escape(ex.Message)}[/]");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return 1;
        }
    }

    private static async Task<int> RunSpamFilterAgentAsync(SpamFilterSettings settings, TelemetrySetup telemetry)
    {
        using var activity = telemetry.ActivitySource.StartActivity("RunSpamFilterAgent");

        using var agent = new SpamFilterAgent(settings);

        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, e) =>
        {
            e.Cancel = true;
            try
            {
                cts.CancelAsync().GetAwaiter().GetResult();
            }
            catch (ObjectDisposedException)
            {
                // CTS already disposed, ignore
            }

            AnsiConsole.MarkupLine("\n[yellow]Cancellation requested...[/]");
        };

        System.Console.CancelKeyPress += cancelHandler;

        try
        {
            await agent.RunAsync(cts.Token).ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine(CancelledByUserMessage);
            activity?.SetStatus(ActivityStatusCode.Ok, CancelledByUserStatus);
            return 0;
        }
        finally
        {
            System.Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static async Task<int> RunSpamScanAgentAsync(SpamFilterSettings settings, TelemetrySetup telemetry)
    {
        using var activity = telemetry.ActivitySource.StartActivity("RunSpamScanAgent");

        using var agent = new SpamScanAgent(settings);

        using var cts = new CancellationTokenSource();
        var cancelHandler = CreateCancelHandler(cts);
        System.Console.CancelKeyPress += cancelHandler;

        try
        {
            await agent.RunAsync(cts.Token).ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine(CancelledByUserMessage);
            activity?.SetStatus(ActivityStatusCode.Ok, CancelledByUserStatus);
            return 0;
        }
        finally
        {
            System.Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static async Task<int> RunSpamReviewAgentAsync(SpamFilterSettings settings, TelemetrySetup telemetry)
    {
        using var activity = telemetry.ActivitySource.StartActivity("RunSpamReviewAgent");

        using var agent = new SpamReviewAgent(settings);

        using var cts = new CancellationTokenSource();
        var cancelHandler = CreateCancelHandler(cts);
        System.Console.CancelKeyPress += cancelHandler;

        try
        {
            await agent.RunAsync(cts.Token).ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine(CancelledByUserMessage);
            activity?.SetStatus(ActivityStatusCode.Ok, CancelledByUserStatus);
            return 0;
        }
        finally
        {
            System.Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static async Task<int> RunSpamCleanupAgentAsync(SpamFilterSettings settings, TelemetrySetup telemetry)
    {
        using var activity = telemetry.ActivitySource.StartActivity("RunSpamCleanupAgent");

        using var agent = new SpamCleanupAgent(settings);

        using var cts = new CancellationTokenSource();
        var cancelHandler = CreateCancelHandler(cts);
        System.Console.CancelKeyPress += cancelHandler;

        try
        {
            await agent.RunAsync(cts.Token).ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine(CancelledByUserMessage);
            activity?.SetStatus(ActivityStatusCode.Ok, CancelledByUserStatus);
            return 0;
        }
        finally
        {
            System.Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static ConsoleCancelEventHandler CreateCancelHandler(CancellationTokenSource cts) =>
        (_, e) =>
        {
            e.Cancel = true;
            try
            {
                cts.CancelAsync().GetAwaiter().GetResult();
            }
            catch (ObjectDisposedException)
            {
                // CTS already disposed, ignore
            }

            AnsiConsole.MarkupLine("\n[yellow]Cancellation requested...[/]");
        };

    private static async Task<int> RunInteractiveChatAsync(SpamFilterSettings settings, TelemetrySetup telemetry)
    {
        using var activity = telemetry.ActivitySource.StartActivity("RunInteractiveChat");

        var openRouterBaseUrlValue = Environment.GetEnvironmentVariable(OpenRouterBaseUrlEnvVar);
        if (string.IsNullOrEmpty(openRouterBaseUrlValue))
        {
            AnsiConsole.Write(new Panel(
                $"[red]Missing {OpenRouterBaseUrlEnvVar} environment variable.[/]\n\n" +
                "Set it with:\n" +
                $"[dim]$env:{OpenRouterBaseUrlEnvVar} = \"https://openrouter.ai/api/v1\"[/]")
                .Header("[yellow]Configuration Error[/]")
                .Border(BoxBorder.Rounded));
            activity?.SetStatus(ActivityStatusCode.Error, "Missing OPENROUTER_BASE_URL");
            return 1;
        }

        var openRouterBaseUrl = new Uri(openRouterBaseUrlValue);

        // Validate API key
        const string apiKeyEnvVar = "OPENROUTER_API_KEY";
        var apiKey = Environment.GetEnvironmentVariable(apiKeyEnvVar);
        if (string.IsNullOrEmpty(apiKey))
        {
            AnsiConsole.Write(new Panel(
                $"[red]Missing {apiKeyEnvVar} environment variable.[/]\n\n" +
                "Set it with:\n" +
                $"[dim]$env:{apiKeyEnvVar} = \"your-api-key\"[/]")
                .Header("[yellow]Configuration Error[/]")
                .Border(BoxBorder.Rounded));
            activity?.SetStatus(ActivityStatusCode.Error, "Missing OPENROUTER_API_KEY");
            return 1;
        }

        // Create OpenAI client pointing to OpenRouter
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = openRouterBaseUrl });

        // Create chat client with function invocation support
        using var chatClient = CompositeDisposableChatClient.CreateWithFunctionInvocation(openAiClient, ModelId);

        // Initialize spam storage for mail tool's spam registry modes
        OutlookMailTools.InitializeSpamStorage(settings);

        // Register tools
        var tools = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(TerminalTools.Terminal),
                AIFunctionFactory.Create(WebSearchTools.WebSearchAsync),
                AIFunctionFactory.Create(OutlookMailTools.MailAsync),
            ],
        };

        // Fetch model info from OpenRouter
        using var modelService = new OpenRouterModelService(ModelId);
        await FetchAndDisplayModelInfoAsync(modelService).ConfigureAwait(false);

        DisplayHeader(tools);

        // Chat history for context
        List<ChatMessage> history = [];

        // Main chat loop
        await RunChatLoopAsync(chatClient, tools, history, settings, telemetry, modelService).ConfigureAwait(false);

        AnsiConsole.MarkupLine("[dim]Goodbye![/]");
        activity?.SetStatus(ActivityStatusCode.Ok);
        return 0;
    }

    private static void DisplayHeader(ChatOptions tools)
    {
        AnsiConsole.Write(new FigletText("Power AI").Color(Color.Blue));
        AnsiConsole.MarkupLine($"[dim]Model: {ModelId}[/]\n");

        var toolsTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[blue]Tool[/]")
            .AddColumn("[blue]Description[/]");

        foreach (var tool in (tools.Tools ?? []).OfType<AIFunction>())
        {
            toolsTable.AddRow($"[green]{tool.Name}[/]", tool.Description ?? string.Empty);
        }

        AnsiConsole.Write(toolsTable);
        AnsiConsole.WriteLine();

        var agentsTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[blue]Agent[/]")
            .AddColumn("[blue]Description[/]")
            .AddColumn("[blue]Command[/]");

        agentsTable.AddRow(
            "[cyan]SpamFilter[/]",
            "Interactive spam filter with autonomous capabilities",
            "[dim]/spam[/]");
        agentsTable.AddRow(
            "[cyan]SpamScan[/]",
            "Autonomous scan: identifies suspicious domains, flags for human review",
            "[dim]/spam-scan[/]");
        agentsTable.AddRow(
            "[cyan]SpamReview[/]",
            "Human review: batch review flagged domains, add to blocklist",
            "[dim]/spam-review[/]");
        agentsTable.AddRow(
            "[cyan]SpamCleanup[/]",
            "Cleanup: move emails from blocked domains to junk folder",
            "[dim]/spam-cleanup[/]");

        AnsiConsole.Write(agentsTable);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Commands: /spam, /spam-scan, /spam-review, /spam-cleanup, /clear, /usage, exit[/]\n");
    }

    private static ChatCommand ParseCommand(string? input) =>
        string.IsNullOrWhiteSpace(input)
            ? ChatCommand.Empty
            : input.Trim().ToUpperInvariant() switch
            {
                "EXIT" => ChatCommand.Exit,
                "/CLEAR" => ChatCommand.Clear,
                "/USAGE" => ChatCommand.Usage,
                "/SPAM" => ChatCommand.Spam,
                "/SPAM-SCAN" => ChatCommand.SpamScan,
                "/SPAM-REVIEW" => ChatCommand.SpamReview,
                "/SPAM-CLEANUP" => ChatCommand.SpamCleanup,
                _ => ChatCommand.Message,
            };

    private static async Task RunChatLoopAsync(
        IChatClient chatClient,
        ChatOptions tools,
        List<ChatMessage> history,
        SpamFilterSettings settings,
        TelemetrySetup telemetry,
        OpenRouterModelService modelService)
    {
        var sessionTokens = new TokenUsageTracker();

        while (true)
        {
            var userInput = CommandInputService.ReadInput();

            var command = ParseCommand(userInput);
            if (await HandleCommandAsync(
                command, history, sessionTokens, settings, telemetry, modelService).ConfigureAwait(false))
            {
                continue;
            }

            if (command == ChatCommand.Exit)
            {
                break;
            }

            history.Add(new ChatMessage(ChatRole.User, userInput));

            await ProcessUserInputAsync(chatClient, tools, history, sessionTokens, telemetry, modelService).ConfigureAwait(false);
        }
    }

    private static async Task<bool> HandleCommandAsync(
        ChatCommand command,
        List<ChatMessage> history,
        TokenUsageTracker sessionTokens,
        SpamFilterSettings settings,
        TelemetrySetup telemetry,
        OpenRouterModelService modelService)
    {
        switch (command)
        {
            case ChatCommand.Empty:
                return true;

            case ChatCommand.Clear:
                history.Clear();
                sessionTokens.Reset();
                await FetchAndDisplayModelInfoAsync(modelService).ConfigureAwait(false);
                AnsiConsole.MarkupLine("[yellow]Chat history and token counters cleared.[/]\n");
                return true;

            case ChatCommand.Usage:
                DisplayUsageInfo(sessionTokens, history.Count, modelService.Info?.ContextLength);
                return true;

            case ChatCommand.Spam:
                await RunSpamFilterAgentAsync(settings, telemetry).ConfigureAwait(false);
                AnsiConsole.MarkupLine(ReturnedToChatModeMessage);
                return true;

            case ChatCommand.SpamScan:
                await RunSpamScanAgentAsync(settings, telemetry).ConfigureAwait(false);
                AnsiConsole.MarkupLine(ReturnedToChatModeMessage);
                return true;

            case ChatCommand.SpamReview:
                await RunSpamReviewAgentAsync(settings, telemetry).ConfigureAwait(false);
                AnsiConsole.MarkupLine(ReturnedToChatModeMessage);
                return true;

            case ChatCommand.SpamCleanup:
                await RunSpamCleanupAgentAsync(settings, telemetry).ConfigureAwait(false);
                AnsiConsole.MarkupLine(ReturnedToChatModeMessage);
                return true;

            default:
                return false;
        }
    }

    private static void DisplayUsageInfo(
        TokenUsageTracker sessionTokens,
        int historyCount,
        int? contextLength)
    {
        AnsiConsole.MarkupLine(
            $"[cyan]Session: {sessionTokens.TotalInput:N0} in, " +
            $"{sessionTokens.TotalOutput:N0} out ({sessionTokens.Total:N0} total)[/]");
        AnsiConsole.MarkupLine($"[cyan]History: {historyCount} messages[/]");

        if (contextLength.HasValue)
        {
            var pct = sessionTokens.Total * 100.0 / contextLength.Value;
            AnsiConsole.MarkupLine($"[cyan]Context: {sessionTokens.Total:N0} / {contextLength.Value:N0} ({pct:F1}%)[/]");
        }

        AnsiConsole.WriteLine();
    }

    private static async Task FetchAndDisplayModelInfoAsync(OpenRouterModelService modelService)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("dim"))
            .StartAsync("Fetching model info...", async _ =>
            {
                await modelService.FetchAsync().ConfigureAwait(false);
            })
            .ConfigureAwait(false);

        if (modelService.Info is not null)
        {
            AnsiConsole.MarkupLine($"[dim]Context limit: {modelService.Info.ContextLength:N0} tokens[/]");
        }
    }

    private static async Task ProcessUserInputAsync(
        IChatClient chatClient,
        ChatOptions tools,
        List<ChatMessage> history,
        TokenUsageTracker sessionTokens,
        TelemetrySetup telemetry,
        OpenRouterModelService modelService)
    {
        using var activity = telemetry.ActivitySource.StartActivity("ProcessUserInput");
        var logger = telemetry.LoggerFactory.CreateLogger("HemSoft.PowerAI.Console.Program");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            ChatResponse? response = null;

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("blue"))
                .StartAsync("Thinking...", async ctx =>
                {
                    ctx.Status("Calling API...");

                    response = await chatClient.GetResponseAsync(history, tools, cts.Token).ConfigureAwait(false);
                })
                .ConfigureAwait(false);

            if (response is not null)
            {
                var assistantMessage = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant);
                var responseText = assistantMessage?.Text ?? "[No response]";

                history.AddRange(response.Messages);

                AnsiConsole.Write(new Panel(Markup.Escape(responseText))
                    .Header("[green]Agent[/]")
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Green));

                // Display usage status bar
                DisplayUsageStatus(response.Usage, history.Count, sessionTokens, modelService.Info?.ContextLength);

                AnsiConsole.WriteLine();
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (HttpRequestException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogNetworkError(logger, ex);
            ShowError($"Network error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogTimeout(logger, ex);
            ShowError(
                "Request timed out after 10 minutes. This is an application timeout, not OpenRouter. " +
                "Consider breaking your request into smaller steps.");
        }
        catch (InvalidOperationException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogInvalidOperation(logger, ex);
            ShowError($"Error: {ex.Message}");
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogModelNotFound(logger, ModelId, ex);
            ShowError($"Model '{ModelId}' not found. Verify the model ID at https://openrouter.ai/models");
        }
        catch (ClientResultException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogApiError(logger, ex.Status, ex);
            ShowError($"API error ({ex.Status}): {ex.Message}");
        }
    }

    private static void ShowError(string message)
    {
        AnsiConsole.Write(new Panel($"[red]{message}[/]")
            .Header("[red]Error[/]")
            .Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
    }

    private static void DisplayUsageStatus(UsageDetails? usage, int historyCount, TokenUsageTracker sessionTokens, int? contextLength)
    {
        var inputTokens = usage?.InputTokenCount ?? 0;
        var outputTokens = usage?.OutputTokenCount ?? 0;

        if (inputTokens > 0 || outputTokens > 0)
        {
            sessionTokens.Add(inputTokens, outputTokens);
        }

        // Build status line with key metrics
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn(string.Empty).NoWrap());

        var statusParts = new List<string>
        {
            $"[dim]This:[/] [blue]{inputTokens:N0}[/][dim]→[/][green]{outputTokens:N0}[/]",
            $"[dim]Session:[/] [cyan]{sessionTokens.Total:N0}[/]",
            $"[dim]History:[/] [yellow]{historyCount}[/] [dim]msgs[/]",
        };

        // Add context usage percentage if we know the limit
        if (contextLength.HasValue && contextLength.Value > 0)
        {
            var pct = inputTokens * 100.0 / contextLength.Value;
            var color = pct switch
            {
                > 90 => "red",
                > 75 => "yellow",
                _ => "green",
            };
            statusParts.Add($"[dim]Context:[/] [{color}]{pct:F0}%[/]");
        }

        table.AddRow(string.Join("  [dim]│[/]  ", statusParts));
        AnsiConsole.Write(table);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Network error during chat")]
    private static partial void LogNetworkError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Request timed out")]
    private static partial void LogTimeout(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid operation during chat processing")]
    private static partial void LogInvalidOperation(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Model not found: {ModelId}")]
    private static partial void LogModelNotFound(ILogger logger, string modelId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "API error with status {StatusCode}")]
    private static partial void LogApiError(ILogger logger, int statusCode, Exception ex);

    /// <summary>
    /// Tracks cumulative token usage across a chat session.
    /// </summary>
    private sealed class TokenUsageTracker
    {
        public long TotalInput { get; private set; }

        public long TotalOutput { get; private set; }

        public long Total => this.TotalInput + this.TotalOutput;

        public void Add(long input, long output)
        {
            this.TotalInput += input;
            this.TotalOutput += output;
        }

        public void Reset()
        {
            this.TotalInput = 0;
            this.TotalOutput = 0;
        }
    }
}
