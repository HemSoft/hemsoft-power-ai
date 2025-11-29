// <copyright file="Program.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace AgentDemo.Console;

using System.ClientModel;

using AgentDemo.Console.Agents;
using AgentDemo.Console.Configuration;
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
    private const string ModelId = "x-ai/grok-4.1-fast:free";
    private const string OpenRouterBaseUrlEnvVar = "OPENROUTER_BASE_URL";

    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command line arguments. Use 'spam' to run spam filter agent.</param>
    /// <returns>Exit code (0 for success, 1 for configuration error).</returns>
    public static Task<int> Main(string[] args) =>
        args.Length > 0 && args[0].Equals("spam", StringComparison.OrdinalIgnoreCase)
            ? RunSpamFilterAgentAsync()
            : RunInteractiveChatAsync();

    private static async Task<int> RunSpamFilterAgentAsync()
    {
        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var settings = new SpamFilterSettings();
        configuration.GetSection(SpamFilterSettings.SectionName).Bind(settings);

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
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled by user.[/]");
            return 0;
        }
        finally
        {
            System.Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static async Task<int> RunInteractiveChatAsync()
    {
        var openRouterBaseUrlValue = Environment.GetEnvironmentVariable(OpenRouterBaseUrlEnvVar);
        if (string.IsNullOrEmpty(openRouterBaseUrlValue))
        {
            AnsiConsole.Write(new Panel(
                $"[red]Missing {OpenRouterBaseUrlEnvVar} environment variable.[/]\n\n" +
                "Set it with:\n" +
                $"[dim]$env:{OpenRouterBaseUrlEnvVar} = \"https://openrouter.ai/api/v1\"[/]")
                .Header("[yellow]Configuration Error[/]")
                .Border(BoxBorder.Rounded));
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
                AIFunctionFactory.Create(TerminalTools.Execute),
                AIFunctionFactory.Create(WebSearchTools.SearchAsync),
                AIFunctionFactory.Create(OutlookMailTools.MailAsync),
            ],
        };

        DisplayHeader(tools);

        // Chat history for context
        List<ChatMessage> history = [];

        // Main chat loop
        await RunChatLoopAsync(chatClient, tools, history).ConfigureAwait(false);

        AnsiConsole.MarkupLine("[dim]Goodbye![/]");
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
        AnsiConsole.MarkupLine("[dim]Type '/spam' to run an agent, or 'exit' to quit.[/]\n");
    }

    private static async Task RunChatLoopAsync(IChatClient chatClient, ChatOptions tools, List<ChatMessage> history)
    {
        while (true)
        {
            var userInput = await new TextPrompt<string>("[yellow]You:[/]")
                .AllowEmpty()
                .ShowAsync(AnsiConsole.Console, CancellationToken.None)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(userInput))
            {
                continue;
            }

            if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (userInput.Equals("/spam", StringComparison.OrdinalIgnoreCase))
            {
                await RunSpamFilterAgentAsync().ConfigureAwait(false);
                AnsiConsole.MarkupLine("\n[dim]Returned to chat mode.[/]\n");
                continue;
            }

            history.Add(new ChatMessage(ChatRole.User, userInput));

            await ProcessUserInputAsync(chatClient, tools, history).ConfigureAwait(false);
        }
    }

    private static async Task ProcessUserInputAsync(IChatClient chatClient, ChatOptions tools, List<ChatMessage> history)
    {
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
                AnsiConsole.WriteLine();
            }
        }
        catch (HttpRequestException ex)
        {
            ShowError($"Network error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
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
}
