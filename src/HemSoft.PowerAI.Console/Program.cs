// <copyright file="Program.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console;

using System.ClientModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using HemSoft.PowerAI.Console.Agents;
using HemSoft.PowerAI.Console.Configuration;
using HemSoft.PowerAI.Console.Extensions;
using HemSoft.PowerAI.Console.Hosting;
using HemSoft.PowerAI.Console.Services;
using HemSoft.PowerAI.Console.Telemetry;
using HemSoft.PowerAI.Console.Tools;

using Microsoft.Agents.AI;
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
    private const string ResearchAgentUrlEnvVar = "RESEARCH_AGENT_URL";
    private const string CancelledByUserMessage = "[yellow]Operation cancelled by user.[/]";
    private const string CancelledByUserStatus = "Cancelled by user";
    private const string ReturnedToChatModeMessage = "\n[dim]Returned to chat mode.[/]\n";

    private enum ChatCommand
    {
        Empty = 0,
        Exit = 1,
        Clear = 2,
        Usage = 3,
        Spam = 4,
        SpamScan = 5,
        SpamReview = 6,
        SpamCleanup = 7,
        Coordinate = 8,
        CoordinateDistributed = 9,
        HostResearch = 10,
        Message = 11,
    }

    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command line arguments. Use 'spam', 'host-research', or 'distributed' to run specific agents.</param>
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

        var spamSettings = new SpamFilterSettings();
        configuration.GetSection(SpamFilterSettings.SectionName).Bind(spamSettings);

        var a2aSettings = new Configuration.A2ASettings();
        configuration.GetSection(Configuration.A2ASettings.SectionName).Bind(a2aSettings);

        if (args.Length == 0)
        {
            return await RunInteractiveChatAsync(spamSettings, a2aSettings, telemetry).ConfigureAwait(false);
        }

        // Check for special commands
        var firstArg = args[0].ToUpperInvariant();
        return firstArg switch
        {
            "SPAM" => await RunSpamFilterAgentAsync(spamSettings, telemetry).ConfigureAwait(false),
            "HOST-RESEARCH" => await RunHostResearchAgentAsync(a2aSettings, telemetry).ConfigureAwait(false),
            "DISTRIBUTED" => await RunDistributedCoordinatorAsync(a2aSettings, telemetry).ConfigureAwait(false),
            "COORDINATE" => await RunCoordinatorAgentAsync(
                a2aSettings,
                telemetry,
                args.Length > 1 ? string.Join(' ', args.Skip(1)) : null).ConfigureAwait(false),
            _ => await RunSinglePromptAsync(string.Join(' ', args), spamSettings, telemetry).ConfigureAwait(false),
        };
    }

    private static async Task<int> RunSinglePromptAsync(string prompt, SpamFilterSettings spamSettings, TelemetrySetup telemetry)
    {
        using var activity = telemetry.ActivitySource.StartActivity("RunSinglePrompt");

        var openRouterBaseUrlValue = Environment.GetEnvironmentVariable(OpenRouterBaseUrlEnvVar);
        if (string.IsNullOrEmpty(openRouterBaseUrlValue))
        {
            AnsiConsole.MarkupLine($"[red]Missing {OpenRouterBaseUrlEnvVar} environment variable.[/]");
            _ = activity?.SetStatus(ActivityStatusCode.Error, "Missing OPENROUTER_BASE_URL");
            return 1;
        }

        var openRouterBaseUrl = new Uri(openRouterBaseUrlValue);

        const string apiKeyEnvVar = "OPENROUTER_API_KEY";
        var apiKey = Environment.GetEnvironmentVariable(apiKeyEnvVar);
        if (string.IsNullOrEmpty(apiKey))
        {
            AnsiConsole.MarkupLine($"[red]Missing {apiKeyEnvVar} environment variable.[/]");
            _ = activity?.SetStatus(ActivityStatusCode.Error, "Missing OPENROUTER_API_KEY");
            return 1;
        }

        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = openRouterBaseUrl });

        using var chatClient = CompositeDisposableChatClient.CreateWithFunctionInvocation(openAiClient, ModelId);

        OutlookMailTools.InitializeSpamStorage(spamSettings);

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

            _ = activity?.SetStatus(ActivityStatusCode.Ok);
            return 0;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Network error: {Markup.Escape(ex.Message)}[/]");
            _ = activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return 1;
        }
        catch (TaskCanceledException ex)
        {
            AnsiConsole.MarkupLine($"[red]Request timed out: {Markup.Escape(ex.Message)}[/]");
            _ = activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            _ = activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return 1;
        }
    }

    private static async Task<int> RunSpamFilterAgentAsync(SpamFilterSettings settings, TelemetrySetup telemetry)
    {
        using var activity = telemetry.ActivitySource.StartActivity("RunSpamFilterAgent");

        using var agent = new SpamFilterAgent(settings);

        using var cts = new CancellationTokenSource();

        void CancelHandler(object? sender, ConsoleCancelEventArgs e)
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
        }

        System.Console.CancelKeyPress += CancelHandler;

        try
        {
            await agent.RunAsync(cts.Token).ConfigureAwait(false);
            _ = activity?.SetStatus(ActivityStatusCode.Ok);
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine(CancelledByUserMessage);
            _ = activity?.SetStatus(ActivityStatusCode.Ok, CancelledByUserStatus);
            return 0;
        }
        finally
        {
            System.Console.CancelKeyPress -= CancelHandler;
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
            _ = activity?.SetStatus(ActivityStatusCode.Ok);
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine(CancelledByUserMessage);
            _ = activity?.SetStatus(ActivityStatusCode.Ok, CancelledByUserStatus);
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
            _ = activity?.SetStatus(ActivityStatusCode.Ok);
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine(CancelledByUserMessage);
            _ = activity?.SetStatus(ActivityStatusCode.Ok, CancelledByUserStatus);
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
            _ = activity?.SetStatus(ActivityStatusCode.Ok);
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine(CancelledByUserMessage);
            _ = activity?.SetStatus(ActivityStatusCode.Ok, CancelledByUserStatus);
            return 0;
        }
        finally
        {
            System.Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static async Task<int> RunCoordinatorAgentAsync(
        Configuration.A2ASettings a2aSettings,
        TelemetrySetup telemetry,
        string? initialPrompt = null)
    {
        using var activity = telemetry.ActivitySource.StartActivity("RunCoordinatorAgent");

        using var cts = new CancellationTokenSource();
        var cancelHandler = CreateCancelHandler(cts);
        System.Console.CancelKeyPress += cancelHandler;

        try
        {
            DisplayCoordinatorHeader();

            var coordinatorAgent = await CreateCoordinatorWithRemoteOrLocalAgent(a2aSettings, cts.Token)
                .ConfigureAwait(false);

            AnsiConsole.MarkupLine("[dim]Commands: exit[/]\n");

            await RunCoordinatorChatLoopAsync(coordinatorAgent, cts.Token, initialPrompt).ConfigureAwait(false);

            AnsiConsole.MarkupLine("[dim]Coordinator session ended.[/]");
            _ = activity?.SetStatus(ActivityStatusCode.Ok);
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine(CancelledByUserMessage);
            _ = activity?.SetStatus(ActivityStatusCode.Ok, CancelledByUserStatus);
            return 0;
        }
        finally
        {
            Agents.Infrastructure.RemoteAgentTool.ClearRemoteAgent();
            System.Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static void DisplayCoordinatorHeader()
    {
        AnsiConsole.Write(new FigletText("Coordinator").Color(Color.Blue));
        AnsiConsole.MarkupLine("[dim]Multi-agent orchestration system using MS Agent Framework[/]");
    }

    private static async Task<AIAgent> CreateCoordinatorWithRemoteOrLocalAgent(
        Configuration.A2ASettings a2aSettings,
        CancellationToken cancellationToken)
    {
        var envUrl = Environment.GetEnvironmentVariable(ResearchAgentUrlEnvVar);
        var researchAgentUrl = !string.IsNullOrEmpty(envUrl) ? new Uri(envUrl) : a2aSettings.DefaultResearchAgentUrl;

        try
        {
            AnsiConsole.MarkupLine($"[dim]Attempting to connect to Azure Function at {researchAgentUrl}...[/]");

            var remoteAgent = await Agents.Infrastructure.A2AAgentClient
                .ConnectAsync(researchAgentUrl, cancellationToken)
                .ConfigureAwait(false);

            Agents.Infrastructure.RemoteAgentTool.SetRemoteAgent(remoteAgent);
            var remoteResearchTool = Agents.Infrastructure.RemoteAgentTool.CreateTool();

            AnsiConsole.MarkupLine($"[green]✓ Connected to remote ResearchAgent: {remoteAgent.Name}[/]");
            AnsiConsole.MarkupLine("[dim]Research tasks will be delegated to Azure Function[/]");

            return CoordinatorAgent.CreateWithRemoteAgent(remoteResearchTool);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠ Could not connect to Azure Function: {Markup.Escape(ex.Message)}[/]");
            AnsiConsole.MarkupLine("[dim]Using local ResearchAgent instead[/]");
            return CoordinatorAgent.Create(ResearchAgent.Create());
        }
    }

    private static async Task RunCoordinatorChatLoopAsync(
        AIAgent coordinatorAgent,
        CancellationToken cancellationToken,
        string? initialPrompt = null)
    {
        // Process initial prompt if provided (from /coordinate <prompt> syntax)
        if (!string.IsNullOrWhiteSpace(initialPrompt))
        {
            await ProcessCoordinatorPromptAsync(coordinatorAgent, initialPrompt, cancellationToken)
                .ConfigureAwait(false);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var input = await AnsiConsole.AskAsync<string>("[green]You>[/] ", cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            await ProcessCoordinatorPromptAsync(coordinatorAgent, input, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task ProcessCoordinatorPromptAsync(
        AIAgent coordinatorAgent,
        string input,
        CancellationToken cancellationToken)
    {
        AgentRunResponse? response = null;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync("Coordinating...", async ctx =>
            {
                _ = ctx.Status("Processing task...");
                response = await coordinatorAgent.RunAsync(input, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            })
            .ConfigureAwait(false);

        AnsiConsole.Write(new Panel(Markup.Escape(response?.Text ?? "No response"))
            .Header("[blue]Coordinator[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue));
        AnsiConsole.WriteLine();
    }

    private static async Task<int> RunDistributedCoordinatorAsync(
        Configuration.A2ASettings a2aSettings,
        TelemetrySetup telemetry)
    {
        using var activity = telemetry.ActivitySource.StartActivity("RunDistributedCoordinator");

        using var cts = new CancellationTokenSource();
        var cancelHandler = CreateCancelHandler(cts);
        System.Console.CancelKeyPress += cancelHandler;

        try
        {
            DisplayDistributedHeader();

            var envUrl = Environment.GetEnvironmentVariable(ResearchAgentUrlEnvVar);
            var researchAgentUrl = !string.IsNullOrEmpty(envUrl) ? new Uri(envUrl) : a2aSettings.DefaultResearchAgentUrl;
            AnsiConsole.MarkupLine($"[dim]Research Agent URL: {researchAgentUrl}[/]");
            AnsiConsole.MarkupLine("[dim]Commands: exit[/]\n");

            var remoteResearchAgent = await ConnectToRemoteAgentAsync(researchAgentUrl.ToString(), cts.Token)
                .ConfigureAwait(false);

            if (remoteResearchAgent is null)
            {
                AnsiConsole.MarkupLine("[red]Failed to connect to remote research agent.[/]");
                _ = activity?.SetStatus(ActivityStatusCode.Error, "Failed to connect to remote research agent");
                return 1;
            }

            AnsiConsole.MarkupLine($"[green]Connected to {remoteResearchAgent.Name}[/]");
            AnsiConsole.MarkupLine($"[dim]{remoteResearchAgent.Description}[/]\n");

            await RunDistributedChatLoopAsync(remoteResearchAgent, cts.Token).ConfigureAwait(false);

            AnsiConsole.MarkupLine("[dim]Distributed coordinator session ended.[/]");
            _ = activity?.SetStatus(ActivityStatusCode.Ok);
            return 0;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to connect to remote agent: {Markup.Escape(ex.Message)}[/]");
            AnsiConsole.MarkupLine("[dim]Make sure the remote agent is running and accessible.[/]");
            _ = activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return 1;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine(CancelledByUserMessage);
            _ = activity?.SetStatus(ActivityStatusCode.Ok, CancelledByUserStatus);
            return 0;
        }
        finally
        {
            System.Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static void DisplayDistributedHeader()
    {
        AnsiConsole.Write(new FigletText("Distributed").Color(Color.Magenta1));
        AnsiConsole.MarkupLine("[dim]Multi-agent orchestration via A2A protocol[/]");
        AnsiConsole.MarkupLine("[dim]Connects to remote agents using Agent-to-Agent protocol[/]\n");
    }

    private static async Task<Agents.Infrastructure.A2AAgentClient?> ConnectToRemoteAgentAsync(
        string url,
        CancellationToken cancellationToken)
    {
        Agents.Infrastructure.A2AAgentClient? client = null;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("magenta"))
            .StartAsync("Connecting to remote research agent...", async _ =>
            {
                client = await Agents.Infrastructure.A2AAgentClient
                    .ConnectAsync(new Uri(url), cancellationToken)
                    .ConfigureAwait(false);
            })
            .ConfigureAwait(false);

        return client;
    }

    private static async Task RunDistributedChatLoopAsync(
        Agents.Infrastructure.A2AAgentClient remoteAgent,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var input = await AnsiConsole.AskAsync<string>("[green]You>[/] ", cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            string? response = null;

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("magenta"))
                .StartAsync("Sending to remote agent...", async _ =>
                {
                    response = await remoteAgent.SendMessageAsync(input, cancellationToken)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);

            AnsiConsole.Write(new Panel(Markup.Escape(response ?? "No response"))
                .Header($"[magenta]{remoteAgent.Name}[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Magenta1));
            AnsiConsole.WriteLine();
        }
    }

    private static async Task<int> RunHostResearchAgentAsync(
        Configuration.A2ASettings a2aSettings,
        TelemetrySetup telemetry)
    {
        using var activity = telemetry.ActivitySource.StartActivity("RunHostResearchAgent");

        using var cts = new CancellationTokenSource();
        var cancelHandler = CreateCancelHandler(cts);
        System.Console.CancelKeyPress += cancelHandler;

        try
        {
            DisplayHostResearchHeader(a2aSettings.ResearchAgentHostPort);

            var researchAgent = ResearchAgent.Create();
            var hostUrl = new Uri($"http://localhost:{a2aSettings.ResearchAgentHostPort.ToString(CultureInfo.InvariantCulture)}/");
            var agentCard = AgentCards.CreateResearchAgentCard(hostUrl);

            var host = new A2AAgentHost(
                researchAgent,
                agentCard,
                a2aSettings.ResearchAgentHostPort);

            await using (host.ConfigureAwait(false))
            {
                await host.StartAsync(cts.Token).ConfigureAwait(false);

                AnsiConsole.MarkupLine($"[green]ResearchAgent now listening at {hostUrl}[/]");
                AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop the server[/]\n");
                AnsiConsole.MarkupLine("[dim]To connect from another terminal, use /coordinate-distributed[/]");

                await host.WaitForShutdownAsync(cts.Token).ConfigureAwait(false);
            }

            AnsiConsole.MarkupLine("[dim]A2A server stopped.[/]");
            _ = activity?.SetStatus(ActivityStatusCode.Ok);
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine(CancelledByUserMessage);
            _ = activity?.SetStatus(ActivityStatusCode.Ok, CancelledByUserStatus);
            return 0;
        }
        finally
        {
            System.Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static void DisplayHostResearchHeader(int port)
    {
        AnsiConsole.Write(new FigletText("A2A Host").Color(Color.Green));
        AnsiConsole.MarkupLine("[dim]Hosting ResearchAgent as A2A server[/]");
        AnsiConsole.MarkupLine($"[dim]Port: {port.ToInvariant()}[/]\n");
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

    private static async Task<int> RunInteractiveChatAsync(
        SpamFilterSettings spamSettings,
        Configuration.A2ASettings a2aSettings,
        TelemetrySetup telemetry)
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
            _ = activity?.SetStatus(ActivityStatusCode.Error, "Missing OPENROUTER_BASE_URL");
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
            _ = activity?.SetStatus(ActivityStatusCode.Error, "Missing OPENROUTER_API_KEY");
            return 1;
        }

        // Create OpenAI client pointing to OpenRouter
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = openRouterBaseUrl });

        // Create chat client with function invocation support
        using var chatClient = CompositeDisposableChatClient.CreateWithFunctionInvocation(openAiClient, ModelId);

        // Initialize spam storage for mail tool's spam registry modes
        OutlookMailTools.InitializeSpamStorage(spamSettings);

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
        await RunChatLoopAsync(chatClient, tools, history, spamSettings, a2aSettings, telemetry, modelService)
            .ConfigureAwait(false);

        AnsiConsole.MarkupLine("[dim]Goodbye![/]");
        _ = activity?.SetStatus(ActivityStatusCode.Ok);
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
            _ = toolsTable.AddRow($"[green]{tool.Name}[/]", tool.Description ?? string.Empty);
        }

        AnsiConsole.Write(toolsTable);
        AnsiConsole.WriteLine();

        var agentsTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[blue]Agent[/]")
            .AddColumn("[blue]Description[/]")
            .AddColumn("[blue]Command[/]");

        _ = agentsTable.AddRow(
            "[cyan]SpamFilter[/]",
            "Interactive spam filter with autonomous capabilities",
            "[dim]/spam[/]");
        _ = agentsTable.AddRow(
            "[cyan]SpamScan[/]",
            "Autonomous scan: identifies suspicious domains, flags for human review",
            "[dim]/spam-scan[/]");
        _ = agentsTable.AddRow(
            "[cyan]SpamReview[/]",
            "Human review: batch review flagged domains, add to blocklist",
            "[dim]/spam-review[/]");
        _ = agentsTable.AddRow(
            "[cyan]SpamCleanup[/]",
            "Cleanup: move emails from blocked domains to junk folder",
            "[dim]/spam-cleanup[/]");
        _ = agentsTable.AddRow(
            "[magenta]Coordinator[/]",
            "Multi-agent orchestration: delegates tasks to specialized agents",
            "[dim]/coordinate[/]");
        _ = agentsTable.AddRow(
            "[magenta1]Distributed[/]",
            "A2A protocol: connects to remote agents via Agent-to-Agent protocol",
            "[dim]/coordinate-distributed[/]");
        _ = agentsTable.AddRow(
            "[green]HostResearch[/]",
            "A2A server: hosts ResearchAgent as an A2A endpoint for remote access",
            "[dim]/host-research[/]");

        AnsiConsole.Write(agentsTable);
        AnsiConsole.WriteLine();

        const string availableCommands =
            "[dim]Commands: /spam, /spam-scan, /spam-review, /spam-cleanup, " +
            "/coordinate, /coordinate-distributed, /host-research, /clear, /usage, exit[/]";
        AnsiConsole.MarkupLine(availableCommands + "\n");
    }

    private static (ChatCommand Command, string? Argument) ParseCommand(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (ChatCommand.Empty, null);
        }

        var trimmed = input.Trim();
        var upper = trimmed.ToUpperInvariant();

        // Exact matches for simple commands
        return upper switch
        {
            "EXIT" => (ChatCommand.Exit, null),
            "/CLEAR" => (ChatCommand.Clear, null),
            "/USAGE" => (ChatCommand.Usage, null),
            "/SPAM" => (ChatCommand.Spam, null),
            "/SPAM-SCAN" => (ChatCommand.SpamScan, null),
            "/SPAM-REVIEW" => (ChatCommand.SpamReview, null),
            "/SPAM-CLEANUP" => (ChatCommand.SpamCleanup, null),
            "/COORDINATE" => (ChatCommand.Coordinate, null),
            "/COORDINATE-DISTRIBUTED" => (ChatCommand.CoordinateDistributed, null),
            "/HOST-RESEARCH" => (ChatCommand.HostResearch, null),
            _ when upper.StartsWith("/COORDINATE ", StringComparison.Ordinal) =>
                (ChatCommand.Coordinate, trimmed["/COORDINATE ".Length..].Trim()),
            _ when upper.StartsWith("/COORDINATE-DISTRIBUTED ", StringComparison.Ordinal) =>
                (ChatCommand.CoordinateDistributed, trimmed["/COORDINATE-DISTRIBUTED ".Length..].Trim()),
            _ => (ChatCommand.Message, null),
        };
    }

    private static async Task RunChatLoopAsync(
        IChatClient chatClient,
        ChatOptions tools,
        List<ChatMessage> history,
        SpamFilterSettings spamSettings,
        Configuration.A2ASettings a2aSettings,
        TelemetrySetup telemetry,
        OpenRouterModelService modelService)
    {
        var sessionTokens = new TokenUsageTracker();
        var context = new CommandContext(spamSettings, a2aSettings, telemetry, modelService);

        while (true)
        {
            var userInput = CommandInputService.ReadInput();

            var (command, argument) = ParseCommand(userInput);
            if (await HandleCommandAsync(command, argument, history, sessionTokens, context)
                .ConfigureAwait(false))
            {
                continue;
            }

            if (command == ChatCommand.Exit)
            {
                break;
            }

            history.Add(new ChatMessage(ChatRole.User, userInput));

            await ProcessUserInputAsync(chatClient, tools, history, sessionTokens, telemetry, modelService)
                .ConfigureAwait(false);
        }
    }

    private static async Task<bool> HandleCommandAsync(
        ChatCommand command,
        string? argument,
        List<ChatMessage> history,
        TokenUsageTracker sessionTokens,
        CommandContext context)
    {
        switch (command)
        {
            case ChatCommand.Empty:
                return true;

            case ChatCommand.Clear:
                history.Clear();
                sessionTokens.Reset();
                await FetchAndDisplayModelInfoAsync(context.ModelService).ConfigureAwait(false);
                AnsiConsole.MarkupLine("[yellow]Chat history and token counters cleared.[/]\n");
                return true;

            case ChatCommand.Usage:
                DisplayUsageInfo(sessionTokens, history.Count, context.ModelService.Info?.ContextLength);
                return true;

            case ChatCommand.Spam:
                _ = await RunSpamFilterAgentAsync(context.SpamSettings, context.Telemetry).ConfigureAwait(false);
                AnsiConsole.MarkupLine(ReturnedToChatModeMessage);
                return true;

            case ChatCommand.SpamScan:
                _ = await RunSpamScanAgentAsync(context.SpamSettings, context.Telemetry).ConfigureAwait(false);
                AnsiConsole.MarkupLine(ReturnedToChatModeMessage);
                return true;

            case ChatCommand.SpamReview:
                _ = await RunSpamReviewAgentAsync(context.SpamSettings, context.Telemetry).ConfigureAwait(false);
                AnsiConsole.MarkupLine(ReturnedToChatModeMessage);
                return true;

            case ChatCommand.SpamCleanup:
                _ = await RunSpamCleanupAgentAsync(context.SpamSettings, context.Telemetry).ConfigureAwait(false);
                AnsiConsole.MarkupLine(ReturnedToChatModeMessage);
                return true;

            case ChatCommand.Coordinate:
                _ = await RunCoordinatorAgentAsync(context.A2ASettings, context.Telemetry, argument).ConfigureAwait(false);
                AnsiConsole.MarkupLine(ReturnedToChatModeMessage);
                return true;

            case ChatCommand.CoordinateDistributed:
                _ = await RunDistributedCoordinatorAsync(context.A2ASettings, context.Telemetry).ConfigureAwait(false);
                AnsiConsole.MarkupLine(ReturnedToChatModeMessage);
                return true;

            case ChatCommand.HostResearch:
                _ = await RunHostResearchAgentAsync(context.A2ASettings, context.Telemetry).ConfigureAwait(false);
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
            $"[cyan]Session: {sessionTokens.TotalInput.ToInvariant("N0")} in, " +
            $"{sessionTokens.TotalOutput.ToInvariant("N0")} out ({sessionTokens.Total.ToInvariant("N0")} total)[/]");
        AnsiConsole.MarkupLine($"[cyan]History: {historyCount.ToInvariant()} messages[/]");

        if (contextLength.HasValue)
        {
            var pct = sessionTokens.Total * 100.0 / contextLength.Value;
            var totalStr = sessionTokens.Total.ToInvariant("N0");
            var contextStr = contextLength.Value.ToInvariant("N0");
            var pctStr = pct.ToInvariant("F1");
            AnsiConsole.MarkupLine($"[cyan]Context: {totalStr} / {contextStr} ({pctStr}%)[/]");
        }

        AnsiConsole.WriteLine();
    }

    private static async Task FetchAndDisplayModelInfoAsync(OpenRouterModelService modelService)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("dim"))
            .StartAsync("Fetching model info...", async _ => await modelService.FetchAsync().ConfigureAwait(false))
            .ConfigureAwait(false);

        if (modelService.Info is not null)
        {
            AnsiConsole.MarkupLine($"[dim]Context limit: {modelService.Info.ContextLength.ToInvariant("N0")} tokens[/]");
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
                    _ = ctx.Status("Calling API...");

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

            _ = activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (HttpRequestException ex)
        {
            _ = activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogNetworkError(logger, ex);
            ShowError($"Network error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _ = activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogTimeout(logger, ex);
            ShowError(
                "Request timed out after 10 minutes. This is an application timeout, not OpenRouter. " +
                "Consider breaking your request into smaller steps.");
        }
        catch (InvalidOperationException ex)
        {
            _ = activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogInvalidOperation(logger, ex);
            ShowError($"Error: {ex.Message}");
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            _ = activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogModelNotFound(logger, ModelId, ex);
            ShowError($"Model '{ModelId}' not found. Verify the model ID at https://openrouter.ai/models");
        }
        catch (ClientResultException ex)
        {
            _ = activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogApiError(logger, ex.Status, ex);
            ShowError($"API error ({ex.Status.ToInvariant()}): {ex.Message}");
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
            $"[dim]This:[/] [blue]{inputTokens.ToInvariant("N0")}[/][dim]→[/][green]{outputTokens.ToInvariant("N0")}[/]",
            $"[dim]Session:[/] [cyan]{sessionTokens.Total.ToInvariant("N0")}[/]",
            $"[dim]History:[/] [yellow]{historyCount.ToInvariant()}[/] [dim]msgs[/]",
        };

        // Add context usage percentage if we know the limit
        if (contextLength is > 0)
        {
            var pct = inputTokens * 100.0 / contextLength.Value;
            var color = pct switch
            {
                > 90 => "red",
                > 75 => "yellow",
                _ => "green",
            };
            statusParts.Add($"[dim]Context:[/] [{color}]{pct.ToInvariant("F0")}%[/]");
        }

        _ = table.AddRow(string.Join("  [dim]│[/]  ", statusParts));
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

    /// <summary>
    /// Context record to group command handler dependencies.
    /// </summary>
    /// <param name="SpamSettings">Spam filter configuration.</param>
    /// <param name="A2ASettings">A2A protocol settings.</param>
    /// <param name="Telemetry">Telemetry setup for tracing.</param>
    /// <param name="ModelService">OpenRouter model service.</param>
    private sealed record CommandContext(
        SpamFilterSettings SpamSettings,
        Configuration.A2ASettings A2ASettings,
        TelemetrySetup Telemetry,
        OpenRouterModelService ModelService);
}
