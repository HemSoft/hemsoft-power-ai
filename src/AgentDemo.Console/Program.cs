using System.ClientModel;
using AgentDemo.Console.Tools;
using Microsoft.Extensions.AI;
using OpenAI;
using Spectre.Console;

const string ModelId = "x-ai/grok-4.1-fast:free";
var openRouterBaseUrl = new Uri(Environment.GetEnvironmentVariable("OPENROUTER_BASE_URL") ?? "https://openrouter.ai/api/v1");

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
IChatClient chatClient = openAiClient
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
        AIFunctionFactory.Create(FileTools.GetFileInfo)
    ]
};

// Display header and available tools
AnsiConsole.Write(new FigletText("Agent Demo").Color(Color.Blue));
AnsiConsole.MarkupLine($"[dim]Model: {ModelId}[/]\n");

var toolsTable = new Table()
    .Border(TableBorder.Rounded)
    .AddColumn("[blue]Tool[/]")
    .AddColumn("[blue]Description[/]");

foreach (var tool in tools.Tools.OfType<AIFunction>())
{
    toolsTable.AddRow($"[green]{tool.Name}[/]", tool.Description ?? "");
}

AnsiConsole.Write(toolsTable);
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[dim]Type 'exit' to quit.[/]\n");

// Chat history for context
List<ChatMessage> history = [];

// Main chat loop
while (true)
{
    var userInput = await new TextPrompt<string>("[yellow]You:[/]")
        .AllowEmpty()
        .ShowAsync(AnsiConsole.Console, CancellationToken.None);

    if (string.IsNullOrWhiteSpace(userInput))
        continue;

    if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    history.Add(new ChatMessage(ChatRole.User, userInput));

    try
    {
        ChatResponse? response = null;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync("Thinking...", async ctx =>
            {
                ctx.Status("Calling API...");
                AnsiConsole.MarkupLine("[dim][[DEBUG]] Starting API call...[/]");

                response = await chatClient.GetResponseAsync(history, tools);

                stopwatch.Stop();
                AnsiConsole.MarkupLine($"[dim][[DEBUG]] API call completed in {stopwatch.ElapsedMilliseconds}ms[/]");
            });

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
    catch (Exception ex)
    {
        AnsiConsole.Write(new Panel($"[red]{ex.Message}[/]")
            .Header("[red]Error[/]")
            .Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
    }
}

AnsiConsole.MarkupLine("[dim]Goodbye![/]");
return 0;
