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
        AgentMenu = 3,
        Spam = 4,
        SpamScan = 5,
        SpamReview = 6,
        SpamCleanup = 7,
        Coordinate = 8,
        CoordinateDistributed = 9,
        HostResearch = 10,
        Message = 11,
        Model = 12,
        Agents = 13,
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
                spamSettings,
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

        var graphClientProvider = new DefaultGraphClientProvider();
        var spamStorageService = new SpamStorageService(spamSettings);
        var outlookMailTools = new OutlookMailTools(graphClientProvider, spamStorageService);

        var tools = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(TerminalTools.Terminal),
                AIFunctionFactory.Create(WebSearchTools.WebSearchAsync),
                AIFunctionFactory.Create(outlookMailTools.MailAsync),
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
        SpamFilterSettings spamSettings,
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

            // Initialize Graph client and spam storage for MailAgent
            var graphClientProvider = new DefaultGraphClientProvider();
            var spamStorageService = new SpamStorageService(spamSettings);

            var coordinatorAgent = await CreateCoordinatorWithRemoteOrLocalAgent(
                a2aSettings,
                graphClientProvider,
                spamStorageService,
                cts.Token).ConfigureAwait(false);

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
        IGraphClientProvider graphClientProvider,
        SpamStorageService? spamStorage,
        CancellationToken cancellationToken)
    {
        var envUrl = Environment.GetEnvironmentVariable(ResearchAgentUrlEnvVar);
        var researchAgentUrl = !string.IsNullOrEmpty(envUrl) ? new Uri(envUrl) : a2aSettings.DefaultResearchAgentUrl;

        AnsiConsole.MarkupLine("[green]✓ CoordinatorAgent initialized[/]");
        AnsiConsole.MarkupLine("[dim]  └─ Connecting sub-agents...[/]");

        // Create MailAgent - always local since it needs Graph client
        var mailAgent = MailAgent.Create(graphClientProvider, spamStorage);
        AnsiConsole.MarkupLine("[green]     └─ MailAgent: ✓ Ready (local)[/]");
        AnsiConsole.MarkupLine("[green]     └─ FileTools: ✓ Ready (local)[/]");

        try
        {
            AnsiConsole.MarkupLine($"[dim]     └─ ResearchAgent: Connecting to {researchAgentUrl}...[/]");

            var (remoteAgent, _) = await Agents.Infrastructure.A2AAgentClient
                .ConnectAsync(researchAgentUrl, cancellationToken)
                .ConfigureAwait(false);

            // Use AsAIFunction() directly - the MS Agent Framework pattern
            var remoteResearchTool = remoteAgent.AsAIFunction();

            var host = researchAgentUrl.Host;
            var port = researchAgentUrl.Port.ToString(CultureInfo.InvariantCulture);
            AnsiConsole.MarkupLine($"[green]     └─ ResearchAgent: ✓ Connected (remote @ {host}:{port})[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[blue]Ready to coordinate. What would you like me to do?[/]");

            return CoordinatorAgent.CreateWithRemoteAgent(remoteResearchTool, mailAgent);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            AnsiConsole.MarkupLine($"[yellow]     └─ ResearchAgent: ⚠ Remote unavailable ({Markup.Escape(ex.Message)})[/]");
            AnsiConsole.MarkupLine("[green]     └─ ResearchAgent: ✓ Ready (local fallback)[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[blue]Ready to coordinate. What would you like me to do?[/]");

            return CoordinatorAgent.Create(ResearchAgent.Create(), mailAgent);
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

            var remoteResult = await ConnectToRemoteAgentAsync(researchAgentUrl.ToString(), cts.Token)
                .ConfigureAwait(false);

            if (remoteResult is null)
            {
                AnsiConsole.MarkupLine("[red]Failed to connect to remote research agent.[/]");
                _ = activity?.SetStatus(ActivityStatusCode.Error, "Failed to connect to remote research agent");
                return 1;
            }

            var (remoteAgent, agentCard) = remoteResult.Value;

            AnsiConsole.MarkupLine($"[green]Connected to {agentCard.Name}[/]");
            AnsiConsole.MarkupLine($"[dim]{agentCard.Description}[/]\n");

            await RunDistributedChatLoopAsync(remoteAgent, agentCard.Name, cts.Token).ConfigureAwait(false);

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

    private static async Task<(AIAgent Agent, A2A.AgentCard Card)?> ConnectToRemoteAgentAsync(
        string url,
        CancellationToken cancellationToken)
    {
        (AIAgent Agent, A2A.AgentCard Card)? result = null;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("magenta"))
            .StartAsync("Connecting to remote research agent...", async _ =>
            {
                result = await Agents.Infrastructure.A2AAgentClient
                    .ConnectAsync(new Uri(url), cancellationToken)
                    .ConfigureAwait(false);
            })
            .ConfigureAwait(false);

        return result;
    }

    private static async Task RunDistributedChatLoopAsync(
        AIAgent remoteAgent,
        string agentName,
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

            AgentRunResponse? response = null;

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("magenta"))
                .StartAsync("Sending to remote agent...", async _ =>
                {
                    response = await remoteAgent.RunAsync(input, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);

            AnsiConsole.Write(new Panel(Markup.Escape(response?.Text ?? "No response"))
                .Header($"[magenta]{agentName}[/]")
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

        // Initialize graph client and mail tools
        var graphClientProvider = new DefaultGraphClientProvider();
        var spamStorageService = new SpamStorageService(spamSettings);
        var outlookMailTools = new OutlookMailTools(graphClientProvider, spamStorageService);

        // Register tools
        var tools = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(TerminalTools.Terminal),
                AIFunctionFactory.Create(WebSearchTools.WebSearchAsync),
                AIFunctionFactory.Create(outlookMailTools.MailAsync),
            ],
        };

        // Fetch model info from OpenRouter
        using var modelService = new OpenRouterModelService(ModelId);
        await FetchModelInfoAsync(modelService).ConfigureAwait(false);

        DisplayHeader(tools, modelService);

        // Chat history for context
        List<ChatMessage> history = [];

        // Main chat loop
        await RunChatLoopAsync(chatClient, tools, history, spamSettings, a2aSettings, telemetry, modelService)
            .ConfigureAwait(false);

        AnsiConsole.MarkupLine("[dim]Goodbye![/]");
        _ = activity?.SetStatus(ActivityStatusCode.Ok);
        return 0;
    }

    private static void DisplayHeader(ChatOptions tools, OpenRouterModelService modelService)
    {
        _ = tools; // Tools parameter kept for future use
        AnsiConsole.Write(new FigletText("Power AI").Color(Color.Blue));

        var info = modelService.Info;
        if (info is null)
        {
            AnsiConsole.MarkupLine($"[dim]Model: {modelService.ModelId}[/]\n");
            return;
        }

        // Build compact capability indicators
        var capabilities = new List<string>();
        if (info.SupportsTools)
        {
            capabilities.Add("[green]tools[/]");
        }

        if (info.SupportsReasoning)
        {
            capabilities.Add("[magenta]reasoning[/]");
        }

        // Input modalities beyond text
        var inputMods = info.InputModalities
            .Where(m => !string.Equals(m, "text", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (inputMods.Count > 0)
        {
            capabilities.Add($"[yellow]+{string.Join('+', inputMods)}[/]");
        }

        // Use [[ and ]] to escape literal brackets in Spectre.Console markup
        var capStr = capabilities.Count > 0 ? $" [[{string.Join(' ', capabilities)}]]" : string.Empty;

        // Context and output token info
        var contextStr = $"[cyan]{info.ContextLength.ToInvariant("N0")}[/][dim]in[/]";
        var outputStr = info.MaxCompletionTokens.HasValue
            ? $" [cyan]{info.MaxCompletionTokens.Value.ToInvariant("N0")}[/][dim]out[/]"
            : string.Empty;

        AnsiConsole.MarkupLine($"[dim]Model:[/] {modelService.ModelId}{capStr} [dim]([/]{contextStr}{outputStr}[dim])[/]");
        AnsiConsole.MarkupLine("[dim]Type '/' for agent menu or '/model' to change model.[/]\n");
    }

    private static (ChatCommand Command, string? Argument) ParseCommand(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (ChatCommand.Empty, null);
        }

        var trimmed = input.Trim();
        var upper = trimmed.ToUpperInvariant();

        // Simple command matches - most are now invoked via / menu
        return upper switch
        {
            "EXIT" or "QUIT" => (ChatCommand.Exit, null),
            "/CLEAR" or "CLEAR" => (ChatCommand.Clear, null),
            "/" => (ChatCommand.AgentMenu, null),
            "/MODEL" => (ChatCommand.Model, null),
            "/AGENTS" => (ChatCommand.Agents, null),
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
                await FetchModelInfoAsync(context.ModelService).ConfigureAwait(false);
                AnsiConsole.MarkupLine("[yellow]Chat history cleared.[/]\n");
                return true;

            case ChatCommand.AgentMenu:
                // Show agent menu and handle selection
                var selectedCommand = CommandInputService.ShowAgentMenu();
                if (selectedCommand is not null)
                {
                    var (parsedCommand, parsedArg) = ParseCommand(selectedCommand);
                    return await HandleCommandAsync(parsedCommand, parsedArg, history, sessionTokens, context)
                        .ConfigureAwait(false);
                }

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
                _ = await RunCoordinatorAgentAsync(
                    context.A2ASettings,
                    context.SpamSettings,
                    context.Telemetry,
                    argument).ConfigureAwait(false);
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

            case ChatCommand.Model:
                await ShowModelPickerAsync(context.ModelService).ConfigureAwait(false);
                return true;

            case ChatCommand.Agents:
                await RunAgentsMenuAsync(context).ConfigureAwait(false);
                return true;

            default:
                return false;
        }
    }

    private static Task<OpenRouterModelService.ModelInfo?> FetchModelInfoAsync(OpenRouterModelService modelService) =>
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("dim"))
            .StartAsync("Fetching model info...", _ => modelService.FetchAsync());

    private static async Task ShowModelPickerAsync(OpenRouterModelService modelService)
    {
        // Ensure models are loaded
        if (modelService.Info is null)
        {
            await FetchModelInfoAsync(modelService).ConfigureAwait(false);
        }

        var allModels = modelService.GetAvailableModels(supportsTools: true);
        if (allModels.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No models available.[/]");
            return;
        }

        // Create selection choices showing model capabilities
        var choices = allModels
            .Select(m =>
            {
                var caps = new List<string>();
                if (m.SupportsReasoning)
                {
                    caps.Add("reasoning");
                }

                var inputMods = m.InputModalities
                    .Where(mod => !string.Equals(mod, "text", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (inputMods.Count > 0)
                {
                    caps.Add($"+{string.Join('+', inputMods)}");
                }

                var capStr = caps.Count > 0 ? $" [{string.Join(' ', caps)}]" : string.Empty;
                var ctxStr = string.Create(CultureInfo.InvariantCulture, $"{m.ContextLength / 1000}k");
                return $"{m.Id}{capStr} ({ctxStr})";
            })
            .ToList();

        // Find current model index - unused but kept for reference
        _ = allModels
            .ToList()
            .FindIndex(m => string.Equals(m.Id, modelService.ModelId, StringComparison.OrdinalIgnoreCase));

        var selection = await AnsiConsole.PromptAsync(
            new SelectionPrompt<string>()
                .Title("[blue]Select a model[/] [dim](all support tools)[/]")
                .PageSize(/* size: */ 15)
                .HighlightStyle(Style.Parse("cyan"))
                .AddChoices(choices)
                .UseConverter(choice => choice)).ConfigureAwait(false);

        // Extract model ID from selection (before capabilities markers)
        var selectedId = selection.Split(' ')[0];
        AnsiConsole.MarkupLine(modelService.SetModel(selectedId)
            ? $"[green]Switched to:[/] {selectedId}\n"
            : $"[yellow]Model not found:[/] {selectedId}\n");
    }

    private static async Task RunAgentsMenuAsync(CommandContext context)
    {
        _ = context; // Reserved for future use with event-driven architecture

        // For now, just ResearchAgent - will expand with event-driven architecture
        var prompt = await AnsiConsole.PromptAsync(
            new TextPrompt<string>("[blue]Research Agent[/] - Enter your research task:")
                .AllowEmpty()).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(prompt))
        {
            AnsiConsole.MarkupLine("[dim]Cancelled.[/]");
            return;
        }

        // Phase 9: Replace with event-driven task submission via Redis
        // For now, run synchronously using ResearchAgent
        AnsiConsole.MarkupLine("[dim]Running research task...[/]");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var researchAgent = Agents.ResearchAgent.Create();

            AgentRunResponse? response = null;
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("cyan"))
                .StartAsync(
                    status: "Researching...",
                    action: async _ =>
                        response = await researchAgent.RunAsync(prompt, cancellationToken: cts.Token)
                            .ConfigureAwait(false))
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(response?.Text))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Panel(Markup.Escape(response.Text))
                    .Header("[cyan]Research Result[/]")
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Cyan));
                AnsiConsole.WriteLine();
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No response from agent.[/]");
            }
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Research task timed out.[/]");
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Network error:[/] {Markup.Escape(ex.Message)}");
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
