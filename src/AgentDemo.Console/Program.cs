// <copyright file="Program.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace AgentDemo.Console;

using System.ClientModel;
using System.Diagnostics;

using AgentDemo.Console.Agents;
using AgentDemo.Console.Configuration;
using AgentDemo.Console.Telemetry;
using AgentDemo.Console.Tools;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

using OpenAI;

using Spectre.Console;

/// <summary>
/// Main entry point for the Agent Demo console application.
/// </summary>
internal static class Program
{
    private const string SourceName = "AgentDemo.Console";
    private const string ModelId = "x-ai/grok-4.1-fast:free";
    private const string OpenRouterBaseUrlEnvVar = "OPENROUTER_BASE_URL";

    private enum ChatCommand
    {
        Empty,
        Exit,
        Clear,
        Usage,
        Spam,
        Message,
    }

    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command line arguments. Use 'spam' to run spam filter agent.</param>
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

        return args.Length > 0 && args[0].Equals("spam", StringComparison.OrdinalIgnoreCase)
            ? await RunSpamFilterAgentAsync(settings, telemetry).ConfigureAwait(false)
            : await RunInteractiveChatAsync(settings, telemetry).ConfigureAwait(false);
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
                cts.Cancel();
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
            AnsiConsole.MarkupLine("[yellow]Operation cancelled by user.[/]");
            activity?.SetStatus(ActivityStatusCode.Ok, "Cancelled by user");
            return 0;
        }
        finally
        {
            System.Console.CancelKeyPress -= cancelHandler;
        }
    }

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
        var chatClient = openAiClient
            .GetChatClient(ModelId)
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

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

        DisplayHeader(tools);

        // Chat history for context
        List<ChatMessage> history = [];

        // Main chat loop
        await RunChatLoopAsync(chatClient, tools, history, settings, telemetry).ConfigureAwait(false);

        AnsiConsole.MarkupLine("[dim]Goodbye![/]");
        activity?.SetStatus(ActivityStatusCode.Ok);
        return 0;
    }

    private static void DisplayHeader(ChatOptions tools)
    {
        AnsiConsole.Write(new FigletText("Agent Demo").Color(Color.Blue));
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
            "Scans inbox for spam, moves known spam to junk, flags candidates for human review",
            "[dim]/spam[/]");

        AnsiConsole.Write(agentsTable);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Commands: /spam (run agent), /clear (reset history), /usage (show tokens), exit[/]\n");
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
                _ => ChatCommand.Message,
            };

    private static async Task RunChatLoopAsync(IChatClient chatClient, ChatOptions tools, List<ChatMessage> history, SpamFilterSettings settings, TelemetrySetup telemetry)
    {
        var sessionTokens = new TokenUsageTracker();

        while (true)
        {
            var userInput = await new TextPrompt<string>("[yellow]You:[/]")
                .AllowEmpty()
                .ShowAsync(AnsiConsole.Console, CancellationToken.None)
                .ConfigureAwait(false);

            var command = ParseCommand(userInput);
            if (await HandleCommandAsync(command, history, sessionTokens, settings, telemetry).ConfigureAwait(false))
            {
                continue;
            }

            if (command == ChatCommand.Exit)
            {
                break;
            }

            history.Add(new ChatMessage(ChatRole.User, userInput));

            await ProcessUserInputAsync(chatClient, tools, history, sessionTokens, telemetry).ConfigureAwait(false);
        }
    }

    private static async Task<bool> HandleCommandAsync(ChatCommand command, List<ChatMessage> history, TokenUsageTracker sessionTokens, SpamFilterSettings settings, TelemetrySetup telemetry)
    {
        switch (command)
        {
            case ChatCommand.Empty:
                return true;

            case ChatCommand.Clear:
                history.Clear();
                sessionTokens.Reset();
                AnsiConsole.MarkupLine("[yellow]Chat history cleared.[/]\n");
                return true;

            case ChatCommand.Usage:
                AnsiConsole.MarkupLine($"[cyan]Session: {sessionTokens.TotalInput:N0} in, {sessionTokens.TotalOutput:N0} out ({sessionTokens.Total:N0} total)[/]");
                AnsiConsole.MarkupLine($"[cyan]History: {history.Count} messages[/]\n");
                return true;

            case ChatCommand.Spam:
                await RunSpamFilterAgentAsync(settings, telemetry).ConfigureAwait(false);
                AnsiConsole.MarkupLine("\n[dim]Returned to chat mode.[/]\n");
                return true;

            default:
                return false;
        }
    }

    private static async Task ProcessUserInputAsync(IChatClient chatClient, ChatOptions tools, List<ChatMessage> history, TokenUsageTracker sessionTokens, TelemetrySetup telemetry)
    {
        using var activity = telemetry.ActivitySource.StartActivity("ProcessUserInput");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
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
                DisplayUsageStatus(response.Usage, history.Count, sessionTokens);

                AnsiConsole.WriteLine();
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (HttpRequestException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            ShowError($"Network error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            ShowError($"Request timed out: {ex.Message}");
        }
    }

    private static void ShowError(string message)
    {
        AnsiConsole.Write(new Panel($"[red]{message}[/]")
            .Header("[red]Error[/]")
            .Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
    }

    private static void DisplayUsageStatus(UsageDetails? usage, int historyCount, TokenUsageTracker sessionTokens)
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

        table.AddRow(string.Join("  [dim]│[/]  ", statusParts));
        AnsiConsole.Write(table);
    }

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
