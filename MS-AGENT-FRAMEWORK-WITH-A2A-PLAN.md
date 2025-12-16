# MS Agent Framework with A2A Migration Plan

## Overview

Migrate from custom agent infrastructure to Microsoft Agent Framework with A2A (Agent-to-Agent) protocol support. This establishes a foundation for building an "army of agents" with standardized communication.

## Current State

### Custom Infrastructure (To Be Replaced)

| Component | Location | Purpose |
|-----------|----------|---------|
| `ICollaborativeAgent` | `Agents/Infrastructure/` | Custom agent interface |
| `AgentRegistry` | `Agents/Infrastructure/` | In-process agent discovery |
| `AgentCard` | `Agents/Infrastructure/` | Custom capability metadata |
| `AgentRequest/AgentResponse` | `Agents/Infrastructure/` | Custom messaging |
| `CoordinatorAgent` | `Agents/` | Orchestrator using custom delegation |
| `ResearchAgent` | `Agents/` | Research specialist |

### What's Already Good

- Using `Microsoft.Extensions.AI` for `IChatClient`
- Using `Microsoft.Agents.AI` package (already have it!)
- `ChatClientAgent` usage in `ResearchAgent`
- OpenRouter via OpenAI SDK (provider-agnostic)
- Tools via `AIFunctionFactory.Create()`

## Target Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    HemSoft.PowerAI.Console                      │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    Program.cs                             │  │
│  │  - Creates agents                                         │  │
│  │  - Optionally starts A2A server for remote access         │  │
│  │  - Interactive CLI                                        │  │
│  └───────────────────────────────────────────────────────────┘  │
│                              │                                   │
│  ┌───────────────────────────┼───────────────────────────────┐  │
│  │                   AIAgent Layer                           │  │
│  │  ┌─────────────────┐  ┌─────────────────┐                │  │
│  │  │ CoordinatorAgent│  │  ResearchAgent  │  (+ future)    │  │
│  │  │   (AIAgent)     │  │   (AIAgent)     │                │  │
│  │  └────────┬────────┘  └────────┬────────┘                │  │
│  │           │                    │                          │  │
│  │           │    A2A Protocol    │                          │  │
│  │           └────────┬───────────┘                          │  │
│  │                    │                                      │  │
│  │  ┌─────────────────┴─────────────────┐                   │  │
│  │  │    Agent Registry (MS Framework)   │                   │  │
│  │  │  - Local agents                    │                   │  │
│  │  │  - Remote A2A agents via URL       │                   │  │
│  │  └───────────────────────────────────┘                   │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Required Packages

```xml
<!-- Already have -->
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-preview.251125.1" />
<PackageReference Include="Microsoft.Extensions.AI" Version="10.0.1" />
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="10.0.1-preview.1.25571.5" />

<!-- Need to add for A2A -->
<PackageReference Include="Microsoft.Agents.AI.A2A" Version="1.0.0-preview.*" />

<!-- Optional: For hosting A2A endpoints -->
<PackageReference Include="Microsoft.Agents.AI.Hosting.A2A.AspNetCore" Version="1.0.0-preview.*" />
```

## Phase 1: Replace Custom Infrastructure with MS Agent Framework

### 1.1 Delete Custom Infrastructure

Remove from `Agents/Infrastructure/`:
- `AgentCard.cs`
- `AgentSkill.cs`
- `AgentRequest.cs`
- `AgentResponse.cs`
- `ICollaborativeAgent.cs`
- `IAgentMessage.cs`
- `AgentRegistry.cs`

### 1.2 Create New Agent Base

```csharp
// Agents/Infrastructure/AgentFactory.cs
namespace HemSoft.PowerAI.Console.Agents.Infrastructure;

using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

internal static class AgentFactory
{
    private const string OpenRouterBaseUrlEnvVar = "OPENROUTER_BASE_URL";
    private const string ApiKeyEnvVar = "OPENROUTER_API_KEY";

    public static IChatClient CreateChatClient(string modelId)
    {
        var baseUrl = Environment.GetEnvironmentVariable(OpenRouterBaseUrlEnvVar)
            ?? throw new InvalidOperationException($"Missing {OpenRouterBaseUrlEnvVar}");

        var apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvVar)
            ?? throw new InvalidOperationException($"Missing {ApiKeyEnvVar}");

        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(baseUrl) });

        return client.GetChatClient(modelId).AsIChatClient();
    }

    public static AIAgent CreateAgent(
        string modelId,
        string name,
        string instructions,
        string? description = null,
        IEnumerable<AITool>? tools = null)
    {
        var chatClient = CreateChatClient(modelId);

        return new ChatClientAgent(
            chatClient,
            instructions: instructions,
            name: name,
            description: description,
            tools: tools);
    }
}
```

### 1.3 Rewrite ResearchAgent

```csharp
// Agents/ResearchAgent.cs
namespace HemSoft.PowerAI.Console.Agents;

using HemSoft.PowerAI.Console.Agents.Infrastructure;
using HemSoft.PowerAI.Console.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

internal sealed class ResearchAgent
{
    private const string ModelId = "x-ai/grok-4.1-fast";

    private const string Instructions = """
        You are a research specialist agent. Your job is to gather information
        from the web and synthesize it into clear, actionable insights.

        ## Workflow:
        1. Analyze the research task to identify key search queries
        2. Perform targeted web searches (2-3 for comprehensive coverage)
        3. Synthesize findings into a clear summary

        ## Output format:
        - **Key Findings**: Most important discoveries
        - **Details**: Supporting information
        - **Sources**: Relevant URLs
        """;

    public static AIAgent Create()
    {
        var tools = new AITool[]
        {
            AIFunctionFactory.Create(WebSearchTools.WebSearchAsync),
        };

        return AgentFactory.CreateAgent(
            modelId: ModelId,
            name: "ResearchAgent",
            instructions: Instructions,
            description: "Web research and information synthesis specialist",
            tools: tools);
    }
}
```

### 1.4 Rewrite CoordinatorAgent

```csharp
// Agents/CoordinatorAgent.cs
namespace HemSoft.PowerAI.Console.Agents;

using HemSoft.PowerAI.Console.Agents.Infrastructure;
using HemSoft.PowerAI.Console.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

internal sealed class CoordinatorAgent
{
    private const string ModelId = "x-ai/grok-4.1-fast";

    public static AIAgent Create(AIAgent researchAgent)
    {
        var instructions = BuildInstructions();

        // The research agent IS the tool - this is the key MS Agent Framework pattern
        var tools = new AITool[]
        {
            researchAgent,  // Agent-as-tool!
            AIFunctionFactory.Create(FileTools.QueryFileSystem),
            AIFunctionFactory.Create(FileTools.ModifyFileSystem),
        };

        return AgentFactory.CreateAgent(
            modelId: ModelId,
            name: "CoordinatorAgent",
            instructions: instructions,
            description: "Orchestrates tasks by delegating to specialized agents",
            tools: tools);
    }

    private static string BuildInstructions() => """
        You are a coordinator agent that orchestrates complex tasks.

        ## Available Agents (use as tools):
        - ResearchAgent: Performs web research and synthesizes information

        ## Workflow:
        1. Analyze incoming task
        2. Delegate research tasks to ResearchAgent
        3. Use file tools to read/write files when needed
        4. Synthesize results into coherent response

        ## Guidelines:
        - For research tasks, ALWAYS use ResearchAgent
        - Handle simple questions directly
        - Be specific when delegating tasks
        """;
}
```

### 1.5 Update Program.cs

```csharp
// In RunCoordinatorAgentAsync
private static async Task<int> RunCoordinatorAgentAsync(TelemetrySetup telemetry)
{
    using var activity = telemetry.ActivitySource.StartActivity("RunCoordinatorAgent");

    // Create agents using MS Agent Framework
    var researchAgent = ResearchAgent.Create();
    var coordinatorAgent = CoordinatorAgent.Create(researchAgent);

    using var cts = new CancellationTokenSource();
    var cancelHandler = CreateCancelHandler(cts);
    Console.CancelKeyPress += cancelHandler;

    try
    {
        // Interactive loop using AIAgent.RunAsync
        while (!cts.Token.IsCancellationRequested)
        {
            var input = await AnsiConsole.AskAsync<string>("[green]You>[/] ", cts.Token);

            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

            var response = await coordinatorAgent.RunAsync(input, cancellationToken: cts.Token);

            AnsiConsole.Write(new Panel(Markup.Escape(response.Text ?? "No response"))
                .Header("[blue]Coordinator[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue));
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
        return 0;
    }
    catch (OperationCanceledException)
    {
        AnsiConsole.MarkupLine(CancelledByUserMessage);
        return 0;
    }
    finally
    {
        Console.CancelKeyPress -= cancelHandler;
        researchAgent.Dispose();
        coordinatorAgent.Dispose();
    }
}
```

## Phase 2: Add A2A Protocol Support

### 2.1 Add A2A Packages

```xml
<PackageReference Include="Microsoft.Agents.AI.A2A" Version="1.0.0-preview.*" />
<PackageReference Include="Microsoft.Agents.AI.Hosting.A2A.AspNetCore" Version="1.0.0-preview.*" />
```

### 2.2 Create A2A Server Host

```csharp
// Hosting/A2AAgentHost.cs
namespace HemSoft.PowerAI.Console.Hosting;

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.A2A.AspNetCore;

internal static class A2AAgentHost
{
    public static async Task StartAsync(AIAgent agent, AgentCard card, int port, CancellationToken ct)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls($"http://localhost:{port}");

        var app = builder.Build();

        // Expose agent via A2A protocol at root
        app.MapA2A("/", agent, card);

        await app.RunAsync(ct);
    }
}
```

### 2.3 Define Agent Cards for A2A

```csharp
// Agents/AgentCards.cs
namespace HemSoft.PowerAI.Console.Agents;

using Microsoft.Agents.AI.A2A;

internal static class AgentCards
{
    public static AgentCard ResearchAgent => new()
    {
        Name = "ResearchAgent",
        Description = "Web research and information synthesis specialist",
        Version = "1.0",
        Url = "http://localhost:5001/",
        DefaultInputModes = ["text"],
        DefaultOutputModes = ["text"],
        Skills =
        [
            new AgentSkill
            {
                Id = "web-research",
                Name = "Web Research",
                Description = "Search the web for current information",
                Tags = ["search", "web", "research"],
                Examples = ["Research the latest AI developments"],
            },
        ],
    };

    public static AgentCard CoordinatorAgent => new()
    {
        Name = "CoordinatorAgent",
        Description = "Orchestrates tasks by delegating to specialized agents",
        Version = "1.0",
        Url = "http://localhost:5000/",
        DefaultInputModes = ["text"],
        DefaultOutputModes = ["text"],
        Skills =
        [
            new AgentSkill
            {
                Id = "orchestration",
                Name = "Task Orchestration",
                Description = "Break down and delegate complex tasks",
                Tags = ["orchestration", "delegation"],
            },
        ],
    };
}
```

### 2.4 Connect to Remote A2A Agents

```csharp
// Agents/Infrastructure/A2AAgentClient.cs
namespace HemSoft.PowerAI.Console.Agents.Infrastructure;

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.A2A;

internal static class A2AAgentClient
{
    public static async Task<AIAgent> ConnectAsync(string url, CancellationToken ct = default)
    {
        // Resolve the agent card from the remote URL
        var resolver = new A2ACardResolver();
        var remoteAgent = await resolver.ResolveAsync(new Uri(url), ct);

        // The framework gives us an AIAgent we can use as a tool
        return remoteAgent;
    }
}
```

### 2.5 Hybrid Mode: Local or Remote Agents

```csharp
// Program.cs - New command: /coordinate-distributed
private static async Task<int> RunDistributedCoordinatorAsync(TelemetrySetup telemetry)
{
    // Connect to remote ResearchAgent via A2A
    var researchAgentUrl = Environment.GetEnvironmentVariable("RESEARCH_AGENT_URL")
        ?? "http://localhost:5001/";

    var researchAgent = await A2AAgentClient.ConnectAsync(researchAgentUrl);

    // Coordinator uses remote agent as a tool - no code change needed!
    var coordinatorAgent = CoordinatorAgent.Create(researchAgent);

    // ... same interactive loop
}
```

## Phase 3: Multi-Agent Orchestration Patterns

### 3.1 Workflow-Based Orchestration (Optional)

For complex multi-step tasks, use MS Agent Framework Workflows:

```csharp
// Workflows/ResearchWorkflow.cs
using Microsoft.Agents.AI.Workflows;

var workflow = new WorkflowBuilder()
    .AddAgent("research", researchAgent)
    .AddAgent("summarizer", summarizerAgent)
    .AddEdge("research", "summarizer")
    .Build();

var result = await workflow.RunAsync("Research AI trends and summarize");
```

### 3.2 Add More Agents

Future agents follow the same pattern:

```csharp
// Example: CodeReviewAgent
internal sealed class CodeReviewAgent
{
    public static AIAgent Create()
    {
        return AgentFactory.CreateAgent(
            modelId: "anthropic/claude-3.5-sonnet",
            name: "CodeReviewAgent",
            instructions: "You are a code review specialist...",
            tools: [ /* code analysis tools */ ]);
    }
}
```

## Migration Checklist

### Phase 1: Core Migration
- [x] Add any missing packages
- [x] Create `AgentFactory.cs`
- [x] Rewrite `ResearchAgent` using `AIAgent`
- [x] Rewrite `CoordinatorAgent` using agent-as-tool pattern
- [x] Update `Program.cs` coordinator entry point
- [x] Delete old infrastructure files
- [x] Update tests
- [x] Build and test locally

### Phase 2: A2A Protocol
- [x] Add A2A packages (A2A 0.3.3-preview, A2A.AspNetCore 0.3.3-preview)
- [x] Create `AgentCards.cs`
- [x] Create `A2AAgentHost.cs`
- [x] Create `A2AAgentClient.cs`
- [x] Add `/coordinate-distributed` command
- [x] Add `/host-research` command to host ResearchAgent as A2A server
- [x] Add `A2ASettings.cs` configuration class
- [x] Add unit tests for `A2ASettings` and `AgentCards`
- [x] Document A2A endpoints

### Phase 3: Expand Agent Army
- [ ] Identify additional agent specializations
- [ ] Create new agents following the pattern
- [ ] Register in coordinator's tool list
- [ ] Optional: Add workflow orchestration

## Key Benefits of This Migration

| Aspect | Before (Custom) | After (MS Agent Framework) |
|--------|-----------------|---------------------------|
| Agent abstraction | `ICollaborativeAgent` | `AIAgent` (standard) |
| Agent registry | `AgentRegistry` (custom) | Built-in + A2A discovery |
| Agent communication | `AgentRequest/Response` | Agent-as-tool + A2A protocol |
| Remote agents | Not supported | A2A protocol with agent cards |
| Multi-agent patterns | Manual delegation | Workflows, orchestration patterns |
| Provider lock-in | None | None (IChatClient abstraction) |

## Files to Delete After Migration

```
src/HemSoft.PowerAI.Console/Agents/Infrastructure/
├── AgentCard.cs           # DELETE
├── AgentSkill.cs          # DELETE
├── AgentRequest.cs        # DELETE
├── AgentResponse.cs       # DELETE
├── ICollaborativeAgent.cs # DELETE
├── IAgentMessage.cs       # DELETE
└── AgentRegistry.cs       # DELETE
```

## Files Created/Modified (Phase 2)

### Files Created
```
src/HemSoft.PowerAI.Console/
├── Agents/
│   ├── Infrastructure/
│   │   └── A2AAgentClient.cs     # NEW - Client for A2A communication
│   └── AgentCards.cs             # NEW - Agent card definitions
├── Configuration/
│   └── A2ASettings.cs            # NEW - A2A configuration settings
├── Hosting/
│   └── A2AAgentHost.cs           # NEW - A2A server hosting
└── appsettings.json              # UPDATED - Added A2A section

tests/HemSoft.PowerAI.Console.Tests/
├── A2ASettingsTests.cs           # NEW - Unit tests for settings
└── AgentCardsTests.cs            # NEW - Unit tests for agent cards
```

### Files Modified
```
src/HemSoft.PowerAI.Console/
├── HemSoft.PowerAI.Console.csproj # UPDATED - Added A2A packages, changed to Web SDK
└── Program.cs                     # UPDATED - Added /coordinate-distributed command
```

### A2A Packages Added
- `A2A` version `0.3.3-preview`
- `A2A.AspNetCore` version `0.3.3-preview`

## A2A Protocol Documentation

### Commands

| Command | Description |
|---------|-------------|
| `/host-research` | Starts the ResearchAgent as an A2A server on the configured port (default: 5001) |
| `/coordinate-distributed` | Connects to a remote ResearchAgent via A2A protocol |

### Testing Local A2A Communication

To test A2A communication locally:

1. **Terminal 1 - Start the A2A Server:**
   ```powershell
   cd src/HemSoft.PowerAI.Console
   dotnet run
   # Type: /host-research
   ```
   This starts the ResearchAgent listening on `http://localhost:5001/`

2. **Terminal 2 - Connect as Client:**
   ```powershell
   cd src/HemSoft.PowerAI.Console
   dotnet run
   # Type: /coordinate-distributed
   ```
   This connects to the remote ResearchAgent and allows you to send messages.

### Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `A2A:DefaultResearchAgentUrl` | `http://localhost:5001/` | URL to connect to for distributed mode |
| `A2A:ResearchAgentHostPort` | `5001` | Port to host the research agent on |

Environment variable override:
```powershell
$env:RESEARCH_AGENT_URL = "http://remote-host:5001/"
```

### A2A Endpoints Exposed

When running `/host-research`, the following endpoints are exposed:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/.well-known/agent-card.json` | GET | Returns the AgentCard describing capabilities |
| `/` | POST | A2A message endpoint for sending tasks |

### AgentCard Schema

The ResearchAgent exposes itself with the following capabilities:

```json
{
  "name": "ResearchAgent",
  "description": "A web research and information synthesis specialist...",
  "version": "1.0.0",
  "protocolVersion": "0.3.0",
  "url": "http://localhost:5001/",
  "defaultInputModes": ["text/plain"],
  "defaultOutputModes": ["text/plain"],
  "capabilities": {
    "streaming": false,
    "pushNotifications": false,
    "stateTransitionHistory": false
  },
  "skills": [
    {
      "id": "web-research",
      "name": "Web Research",
      "description": "Searches the web for current information...",
      "tags": ["search", "web", "research", "information", "synthesis"],
      "examples": [
        "Research the latest AI developments in 2025",
        "Find information about Microsoft Agent Framework"
      ]
    }
  ]
}
```

## Risk Mitigation

1. **Package Preview Status**: MS Agent Framework is preview. Pin versions, test thoroughly.
2. **Breaking Changes**: Monitor GitHub releases for API changes.
3. **Fallback**: Keep a git tag of current working state before migration.

## References

- [MS Agent Framework GitHub](https://github.com/microsoft/agent-framework)
- [A2A Protocol Spec](https://a2a-protocol.org/latest/)
- [A2A .NET SDK](https://github.com/a2aproject/a2a-dotnet)
- [MS Agent Framework Docs](https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview)
- [Rasmus Jensen Video](https://www.youtube.com/watch?v=g72ks3rY9qQ) (research material)
