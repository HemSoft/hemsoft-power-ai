// <copyright file="Program.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace AgentDemo.Console;

using System.ClientModel;
using System.Diagnostics;
using AgentDemo.Console.Tools;
using Microsoft.Extensions.AI;
using OpenAI;
using Spectre.Console;

/// <summary>
/// Main entry point for the Agent Demo console application.
/// </summary>
internal static class Program
{
    private const string ModelId = "x-ai/grok-4.1-fast:free";

#pragma warning disable S1075 // URIs should not be hardcoded
    private const string DefaultOpenRouterUrl = "https://openrouter.ai/api/v1";
#pragma warning restore S1075

    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <returns>Exit code (0 for success, 1 for configuration error).</returns>
    public static async Task<int> Main()
    {
        var openRouterBaseUrl = new Uri(Environment.GetEnvironmentVariable("OPENROUTER_BASE_URL") ?? DefaultOpenRouterUrl);

        // Validate API key
        var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            AnsiConsole.Write(new Panel(
                "[red]Missing OPENROUTER_API_KEY environment variable.[/]\n\n" +
                "Set it with:\n" +
                "[dim]$env:OPENROUTER_API_KEY = \"your-api-key\"[/]")
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
                AIFunctionFactory.Create(FileTools.ListFiles),
                AIFunctionFactory.Create(FileTools.CountFiles),
                AIFunctionFactory.Create(FileTools.CreateFolder),
                AIFunctionFactory.Create(FileTools.GetFileInfo),
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
        AnsiConsole.MarkupLine("[dim]Type 'exit' to quit.[/]\n");
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

            history.Add(new ChatMessage(ChatRole.User, userInput));

            await ProcessUserInputAsync(chatClient, tools, history).ConfigureAwait(false);
        }
    }

    private static async Task ProcessUserInputAsync(IChatClient chatClient, ChatOptions tools, List<ChatMessage> history)
    {
        try
        {
            ChatResponse? response = null;
            var stopwatch = Stopwatch.StartNew();

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("blue"))
                .StartAsync("Thinking...", async ctx =>
                {
                    ctx.Status("Calling API...");
                    AnsiConsole.MarkupLine("[dim][[DEBUG]] Starting API call...[/]");

                    response = await chatClient.GetResponseAsync(history, tools).ConfigureAwait(false);

                    stopwatch.Stop();
                    AnsiConsole.MarkupLine($"[dim][[DEBUG]] API call completed in {stopwatch.ElapsedMilliseconds}ms[/]");
                })
                .ConfigureAwait(false);

            if (response is not null)
            {
                var assistantMessage = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant);
                var responseText = assistantMessage?.Text ?? "[No response]";

                history.AddRange(response.Messages);

                AnsiConsole.Write(new Panel(responseText)
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
