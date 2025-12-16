# Microsoft Agent Framework Reference Material

## Table of Contents

1. [Overview](#overview)
2. [Current Implementation Analysis](#current-implementation-analysis)
3. [MS Agent Framework Core Concepts](#ms-agent-framework-core-concepts)
4. [Agent Types](#agent-types)
5. [Multi-Agent Orchestration Patterns](#multi-agent-orchestration-patterns)
6. [A2A Protocol (Agent-to-Agent)](#a2a-protocol-agent-to-agent)
7. [Hosting Agents](#hosting-agents)
8. [Code Examples from rwjdk/MicrosoftAgentFrameworkSamples](#code-examples-from-rwjdkmicrosoftagentframeworksamples)
9. [Recommendations for Current Implementation](#recommendations-for-current-implementation)

---

## Overview

The Microsoft Agent Framework is a production-ready, open-source framework for building agentic AI applications. It provides:

- **Multi-agent orchestration**: Sequential, concurrent, group chat, handoff, and magentic patterns
- **Cloud/provider flexibility**: Cloud-agnostic (containers, on-premises, multi-cloud) and provider-agnostic (OpenAI, Azure AI Foundry, etc.)
- **Enterprise features**: OpenTelemetry observability, Microsoft Entra security, responsible AI
- **Standards-based interoperability**: A2A protocol and MCP for agent discovery and tool interaction

### Key NuGet Packages

```xml
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-preview.*" />
<PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.0.0-preview.*" />
<PackageReference Include="Microsoft.Agents.AI.Hosting" Version="1.0.0-preview.*" />
<PackageReference Include="Microsoft.Agents.AI.Hosting.A2A" Version="1.0.0-preview.*" />
<PackageReference Include="Microsoft.Agents.AI.Hosting.A2A.AspNetCore" Version="1.0.0-preview.*" />
<PackageReference Include="Microsoft.Agents.AI.A2A" Version="1.0.0-preview.*" />
<PackageReference Include="Microsoft.Agents.AI.Workflows" Version="1.0.0-preview.*" />
```

---

## Current Implementation Analysis

### What We Have

The `/coordinator` command currently:

1. **Uses `AIAgent`/`ChatClientAgent`** - Correct pattern from MS Agent Framework
2. **Implements agent-as-tool via `AsAIFunction()`** - Correct pattern
3. **Has custom A2A integration** - Custom implementation in `A2AAgentClient`
4. **Azure Function hosting** - Manual HTTP endpoint implementation

### Issues Identified

| Component | Current State | MS Agent Framework Approach |
|-----------|---------------|----------------------------|
| A2A Client | Custom `A2AAgentClient` wrapping `A2A.A2AClient` | Use `A2ACardResolver.GetAIAgentAsync()` directly |
| Azure Function | Manual HTTP endpoints with JSON serialization | Use `MapA2A()` with ASP.NET Core integration |
| Agent-as-Tool | Static `RemoteAgentTool` class | Use built-in `AsAIFunction()` pattern |
| Multi-agent | Direct agent orchestration | Consider using `WorkflowBuilder` or `MagenticBuilder` |

---

## MS Agent Framework Core Concepts

### AIAgent Base Class

All agents derive from `AIAgent`, which provides:

```csharp
public abstract class AIAgent
{
    public abstract AgentThread GetNewThread();
    public abstract Task<AgentRunResponse> RunAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default);
    public abstract IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(...);
}
```

### ChatClientAgent

The primary agent implementation for chat-based inference:

```csharp
// Creating a ChatClientAgent
IChatClient chatClient = azureOpenAIClient
    .GetChatClient(deploymentName)
    .AsIChatClient();

ChatClientAgent agent = new ChatClientAgent(
    chatClient,
    instructions: "You are a helpful assistant",
    name: "MyAgent",
    description: "Agent description",
    tools: [AIFunctionFactory.Create(MyTool)]
);
```

### AgentRunResponse

The response from running an agent:

```csharp
AgentRunResponse response = await agent.RunAsync("Hello");
Console.WriteLine(response.Text);  // Response text
Console.WriteLine(response.Usage); // Token usage
```

---

## Agent Types

### 1. ChatClientAgent

For any `IChatClient` implementation (OpenAI, Azure OpenAI, etc.):

```csharp
// Using extension method
ChatClientAgent agent = openAIClient
    .GetChatClient("gpt-4.1")
    .CreateAIAgent(
        name: "MyAgent",
        instructions: "You are helpful",
        tools: [...]);
```

### 2. A2A Agent (Remote Proxy)

For connecting to remote agents via A2A protocol:

```csharp
// Using well-known agent card location
A2ACardResolver resolver = new(new Uri("https://agent-host/"));
AIAgent remoteAgent = await resolver.GetAIAgentAsync();

// Direct connection
A2AClient client = new(new Uri("https://agent-host/echo"));
AIAgent agent = client.GetAIAgent();
```

### 3. Workflow Host Agent

For exposing workflows as agents:

```csharp
var workflowAsAgent = builder
    .AddWorkflow("my-workflow", (sp, key) => { ... })
    .AddAsAIAgent();
```

### 4. AI Host Agent

For hosting agents with thread management:

```csharp
AIHostAgent hostAgent = new AIHostAgent(
    innerAgent,
    new InMemoryAgentThreadStore());
```

---

## Multi-Agent Orchestration Patterns

### Pattern 1: Agent-as-Tool (Current Approach)

The simplest pattern - one agent can call another as a tool:

```csharp
// Create specialized agents
AIAgent stringAgent = client.GetChatClient(model).CreateAIAgent(
    name: "StringAgent",
    instructions: "You are a string manipulator",
    tools: [
        AIFunctionFactory.Create(StringTools.Reverse),
        AIFunctionFactory.Create(StringTools.Uppercase)
    ]);

AIAgent numberAgent = client.GetChatClient(model).CreateAIAgent(
    name: "NumberAgent",
    instructions: "You are a number expert",
    tools: [
        AIFunctionFactory.Create(NumberTools.RandomNumber)
    ]);

// Create coordinator that delegates to other agents
AIAgent coordinatorAgent = client.GetChatClient(model).CreateAIAgent(
    name: "CoordinatorAgent",
    instructions: "Delegate string/number tasks to appropriate agents",
    tools: [
        stringAgent.AsAIFunction(new AIFunctionFactoryOptions { Name = "StringAgent" }),
        numberAgent.AsAIFunction(new AIFunctionFactoryOptions { Name = "NumberAgent" })
    ]);
```

### Pattern 2: Sequential Workflow

Agents process in sequence, each receiving output from previous:

```csharp
using Microsoft.Agents.AI.Workflows;

ChatClientAgent summaryAgent = chatClient.CreateAIAgent(
    name: "SummaryAgent",
    instructions: "Summarize text to max 20 words");

ChatClientAgent translationAgent = chatClient.CreateAIAgent(
    name: "TranslationAgent",
    instructions: "Translate text to French");

// Build sequential workflow
Workflow workflow = AgentWorkflowBuilder.BuildSequential(summaryAgent, translationAgent);

// Execute
var messages = new List<ChatMessage> { new(ChatRole.User, inputText) };
StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);
```

### Pattern 3: Concurrent Workflow

Multiple agents process the same input simultaneously:

```csharp
ChatClientAgent legalAgent = chatClient.CreateAIAgent(
    name: "LegalAgent",
    instructions: "Evaluate if text is legal");

ChatClientAgent spellingAgent = chatClient.CreateAIAgent(
    name: "SpellingAgent",
    instructions: "Check for spelling errors");

Workflow workflow = AgentWorkflowBuilder.BuildConcurrent([legalAgent, spellingAgent]);
```

### Pattern 4: Handoff Workflow

Intent-based routing between agents:

```csharp
ChatClientAgent intentAgent = client.GetChatClient("gpt-4.1-mini").CreateAIAgent(
    name: "IntentAgent",
    instructions: "Determine question type. Never answer yourself");

ChatClientAgent movieNerd = client.GetChatClient("gpt-4.1").CreateAIAgent(
    name: "MovieNerd",
    instructions: "You are a Movie expert");

ChatClientAgent musicNerd = client.GetChatClient("gpt-4.1").CreateAIAgent(
    name: "MusicNerd",
    instructions: "You are a Music expert");

Workflow workflow = AgentWorkflowBuilder
    .CreateHandoffBuilderWith(intentAgent)
    .WithHandoffs(intentAgent, [movieNerd, musicNerd])
    .WithHandoffs([movieNerd, musicNerd], intentAgent)
    .Build();
```

### Pattern 5: Magentic Orchestration

AI-managed coordination with specialized agents:

```csharp
using Microsoft.Agents.AI;

var researcherAgent = new ChatAgent(
    name: "ResearcherAgent",
    description: "Specialist in research",
    instructions: "You find information",
    chat_client: OpenAIChatClient());

var coderAgent = new ChatAgent(
    name: "CoderAgent",
    description: "Code expert",
    instructions: "You solve questions using code",
    tools: [HostedCodeInterpreterTool()]);

var workflow = MagenticBuilder()
    .participants(researcher=researcherAgent, coder=coderAgent)
    .on_event(callback, mode=MagenticCallbackMode.STREAMING)
    .with_standard_manager(
        chat_client: OpenAIChatClient(),
        max_round_count: 10)
    .build();
```

---

## A2A Protocol (Agent-to-Agent)

### Overview

A2A enables standardized communication between agents:

- **Agent discovery** through agent cards
- **Message-based communication**
- **Long-running processes** via tasks
- **Cross-platform interoperability**

### Agent Card Structure

```csharp
AgentCard agentCard = new()
{
    Name = "ResearchAgent",
    Description = "Web research specialist",
    Version = "1.0.0",
    Url = "http://localhost:5000",
    DefaultInputModes = ["text/plain"],
    DefaultOutputModes = ["text/plain"],
    Capabilities = new AgentCapabilities
    {
        Streaming = false,
        PushNotifications = false,
    },
    Skills = [
        new AgentSkill
        {
            Id = "web-research",
            Name = "Web Research",
            Description = "Searches and synthesizes information",
            Tags = ["search", "research"],
            Examples = ["Research AI trends"]
        }
    ]
};
```

### Hosting with ASP.NET Core (Recommended Pattern)

```csharp
using A2A.AspNetCore;
using Microsoft.Agents.AI.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register chat client
IChatClient chatClient = new AzureOpenAIClient(endpoint, credential)
    .GetChatClient(deploymentName)
    .AsIChatClient();
builder.Services.AddSingleton(chatClient);

// Register agent
var researchAgent = builder.AddAIAgent(
    "research",
    instructions: "You are a research specialist");

var app = builder.Build();

// Expose via A2A - THIS IS THE KEY PATTERN
app.MapA2A(researchAgent, path: "/a2a/research", agentCard: new()
{
    Name = "ResearchAgent",
    Description = "Web research specialist",
    Version = "1.0"
});

// Well-known agent card location is automatically served at:
// /a2a/research/.well-known/agent-card.json

app.Run();
```

### Connecting as A2A Client

```csharp
using A2A;
using Microsoft.Agents.AI.A2A;

// Using well-known location (recommended)
A2ACardResolver resolver = new(new Uri("http://localhost:5000/a2a/research"));
AIAgent remoteAgent = await resolver.GetAIAgentAsync();

// Use remote agent as a tool
AIAgent coordinator = client.GetChatClient(model).CreateAIAgent(
    name: "Coordinator",
    instructions: "Delegate research to ResearchAgent",
    tools: [remoteAgent.AsAIFunction()]);
```

### A2A Message Format

Request:
```json
{
  "id": "1",
  "jsonrpc": "2.0",
  "method": "message/send",
  "params": {
    "id": "conversation-123",
    "message": {
      "kind": "message",
      "role": "user",
      "messageId": "msg_1",
      "parts": [{ "kind": "text", "text": "Research AI trends" }]
    }
  }
}
```

Response:
```json
{
  "id": "1",
  "jsonrpc": "2.0",
  "result": {
    "message": {
      "kind": "message",
      "role": "assistant",
      "messageId": "resp_1",
      "parts": [{ "kind": "text", "text": "Here are the findings..." }]
    }
  }
}
```

---

## Hosting Agents

### ASP.NET Core with A2A (Recommended)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add A2A services
builder.Services.AddSingleton(chatClient);
var agent = builder.AddAIAgent("myagent", "You are helpful");

var app = builder.Build();

// Map A2A endpoint with well-known card discovery
app.MapA2A(agent, "/a2a/myagent", agentCard: card,
    taskManager => app.MapWellKnownAgentCard(taskManager, "/a2a/myagent"));

app.Run();
```

### Azure Functions (Current Approach - Manual)

While functional, our current Azure Function approach requires manual HTTP handling. Consider migrating to ASP.NET Core for native A2A support.

Current pattern (manual):
```csharp
[Function("ResearchAgentCard")]
public Task<HttpResponseData> GetAgentCardAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get",
        Route = ".well-known/agent-card.json")]
    HttpRequestData req)
{
    // Manual JSON serialization
    var card = AgentCards.CreateResearchAgentCard(baseUrl);
    // ... serialize and return
}

[Function("ResearchAgentMessage")]
public Task<HttpResponseData> HandleMessageAsync(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "research")]
    HttpRequestData req)
{
    // Manual message parsing and agent invocation
}
```

Recommended ASP.NET Core pattern:
```csharp
app.MapA2A(agent, "/research", agentCard: card);
// All the routing, serialization handled automatically
```

### DevUI for Development

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChatClient(chatClient);
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

builder.AddAIAgent("MyAgent", "You are helpful");

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.MapOpenAIResponses();
    app.MapOpenAIConversations();
    app.MapDevUI();  // Interactive debugging UI
}

app.Run();
```

---

## Code Examples from rwjdk/MicrosoftAgentFrameworkSamples

### Agent-to-Agent Server (A2A Server)

```csharp
// From: src/Agent2Agent.Server/Program.cs
using A2A;
using A2A.AspNetCore;
using Microsoft.Agents.AI;

FileSystemTools target = new();
MethodInfo[] methods = typeof(FileSystemTools)
    .GetMethods(BindingFlags.Public | BindingFlags.Instance);
List<AITool> tools = methods
    .Select(x => AIFunctionFactory.Create(x, target))
    .Cast<AITool>()
    .ToList();

AIAgent agent = client
    .GetChatClient("gpt-4.1-mini")
    .CreateAIAgent(
        name: "FileAgent",
        instructions: "You are a File Expert",
        tools: tools)
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

AgentCard agentCard = new()
{
    Name = "FilesAgent",
    Description = "Handles file operations",
    Version = "1.0.0",
    Url = "http://localhost:5000",
    // ... skills, capabilities
};

// KEY: MapA2A handles all the protocol details
app.MapA2A(
    agent,
    path: "/",
    agentCard: agentCard,
    taskManager => app.MapWellKnownAgentCard(taskManager, "/"));

await app.RunAsync();
```

### Agent-to-Agent Client (A2A Client)

```csharp
// From: src/Agent2Agent.Client/Program.cs
using A2A;
using Microsoft.Agents.AI;

// Connect to remote agent
A2ACardResolver agentCardResolver = new(new Uri("http://localhost:5000/"));
AIAgent remoteAgent = await agentCardResolver.GetAIAgentAsync();

// Use remote agent as a tool in local coordinator
ChatClientAgent coordinator = azureOpenAIClient
    .GetChatClient("gpt-4.1")
    .CreateAIAgent(
        name: "ClientAgent",
        instructions: "Handle queries using your tools",
        tools: [remoteAgent.AsAIFunction()]);  // KEY: AsAIFunction()

AgentThread thread = coordinator.GetNewThread();
while (true)
{
    string message = Console.ReadLine();
    AgentRunResponse response = await coordinator.RunAsync(message, thread);
    Console.WriteLine(response);
}
```

### Multi-Agent with Agent-as-Tool

```csharp
// From: src/MultiAgent.AgentAsTool/Program.cs
AIAgent stringAgent = client.GetChatClient(model).CreateAIAgent(
    name: "StringAgent",
    instructions: "You are string manipulator",
    tools: [
        AIFunctionFactory.Create(StringTools.Reverse),
        AIFunctionFactory.Create(StringTools.Uppercase),
        AIFunctionFactory.Create(StringTools.Lowercase)
    ])
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

AIAgent numberAgent = client.GetChatClient(model).CreateAIAgent(
    name: "NumberAgent",
    instructions: "You are a number expert",
    tools: [
        AIFunctionFactory.Create(NumberTools.RandomNumber),
        AIFunctionFactory.Create(NumberTools.AnswerToEverythingNumber)
    ])
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

// Coordinator delegates to specialized agents
AIAgent delegationAgent = client.GetChatClient(model).CreateAIAgent(
    name: "DelegateAgent",
    instructions: "Delegator of String and Number Tasks. Never do work yourself",
    tools: [
        stringAgent.AsAIFunction(new AIFunctionFactoryOptions { Name = "StringAgentAsTool" }),
        numberAgent.AsAIFunction(new AIFunctionFactoryOptions { Name = "NumberAgentAsTool" })
    ])
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();
```

### Orchestrator with Handoffs (Trello Agent)

```csharp
// From: src/Trello.Agent/Program.cs
ChatClientAgent orchestratorAgent = agentFactory.GetOrchestratorAgent();
ChatClientAgent trelloAgent = await agentFactory.GetTrelloAgent();

// Handoff workflow: orchestrator routes to specialized agents
Workflow workflow = AgentWorkflowBuilder
    .CreateHandoffBuilderWith(orchestratorAgent)
    .WithHandoffs(orchestratorAgent, [trelloAgent])
    .WithHandoffs([trelloAgent], orchestratorAgent)
    .Build();

// Run workflow
StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

await foreach (WorkflowEvent @event in run.WatchStreamAsync())
{
    switch (@event)
    {
        case AgentRunUpdateEvent e:
            Console.Write(e.Update.Text);
            break;
        case WorkflowOutputEvent output:
            return output.As<List<ChatMessage>>();
    }
}
```

### Sequential Workflow

```csharp
// From: src/Workflow.Sequential/Program.cs
ChatClientAgent summaryAgent = chatClient.CreateAIAgent(
    name: "SummaryAgent",
    instructions: "Summarize text to max 20 words");

ChatClientAgent translationAgent = chatClient.CreateAIAgent(
    name: "TranslationAgent",
    instructions: "Translate text to French");

// Chain agents sequentially
Workflow workflow = AgentWorkflowBuilder.BuildSequential(
    summaryAgent,
    translationAgent);

// Execute
var messages = new List<ChatMessage> { new(ChatRole.User, inputText) };
StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

List<ChatMessage> result = [];
await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is WorkflowOutputEvent completed)
    {
        result = (List<ChatMessage>)completed.Data!;
        break;
    }
}
```

### Function Middleware Pattern

```csharp
// Logging/monitoring middleware for function calls
async ValueTask<object?> FunctionCallMiddleware(
    AIAgent callingAgent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken)
{
    Console.WriteLine($"Tool Call: '{context.Function.Name}'");
    if (context.Arguments.Count > 0)
    {
        var args = string.Join(",",
            context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"));
        Console.WriteLine($"Args: {args}");
    }
    return await next(context, cancellationToken);
}

// Apply middleware
AIAgent agent = client.GetChatClient(model)
    .CreateAIAgent(name: "Agent", tools: [...])
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();
```

---

## Recommendations for Current Implementation

### 1. Azure Function → ASP.NET Core Migration

**Current**: Manual HTTP endpoints in Azure Functions
**Recommended**: Use `MapA2A()` with ASP.NET Core

```csharp
// Instead of Azure Function with manual JSON handling:
[Function("ResearchAgentMessage")]
public async Task<HttpResponseData> HandleMessageAsync(...)
{
    // Manual parsing, agent invocation, response building
}

// Use ASP.NET Core:
var agent = builder.AddAIAgent("research", instructions);
app.MapA2A(agent, "/research", agentCard: card);
```

Benefits:
- Built-in A2A protocol handling
- Automatic agent card discovery at `.well-known/agent-card.json`
- Streaming support
- Proper error handling

### 2. Simplify A2A Client Connection

**Current**: Custom `A2AAgentClient` wrapper
**Recommended**: Use built-in `A2ACardResolver`

```csharp
// Instead of:
var client = await A2AAgentClient.ConnectAsync(url);
string response = await client.SendMessageAsync(message);

// Use:
A2ACardResolver resolver = new(url);
AIAgent agent = await resolver.GetAIAgentAsync();
AgentRunResponse response = await agent.RunAsync(message);
```

### 3. Remote Agent as Tool

**Current**: Custom `RemoteAgentTool` static class
**Recommended**: Use `AsAIFunction()` directly

```csharp
// Instead of:
RemoteAgentTool.SetRemoteAgent(client);
var tool = RemoteAgentTool.CreateTool();

// Use:
AIAgent remoteAgent = await resolver.GetAIAgentAsync();
var tool = remoteAgent.AsAIFunction();
```

### 4. Consider Workflow Patterns

For complex orchestration, evaluate using `WorkflowBuilder`:

```csharp
// Current: Manual coordination via instructions
AIAgent coordinator = CreateAgent(
    instructions: "Delegate to ResearchAgent...",
    tools: [researchAgent.AsAIFunction()]);

// Alternative: Explicit workflow
Workflow workflow = AgentWorkflowBuilder
    .CreateHandoffBuilderWith(intentAgent)
    .WithHandoffs(intentAgent, [researchAgent, fileAgent])
    .Build();
```

### 5. Add Middleware for Observability

```csharp
AIAgent agent = baseAgent
    .AsBuilder()
    .Use(async (ctx, next) =>
    {
        // Log inputs
        logger.LogInformation("Agent input: {Input}", ctx.Messages);
        await next(ctx);
        // Log outputs
        logger.LogInformation("Agent output: {Output}", ctx.Response);
    })
    .Build();
```

---

## Quick Reference

### Creating Agents

```csharp
// From ChatClient
ChatClientAgent agent = client.GetChatClient(model).CreateAIAgent(
    name: "Name",
    instructions: "Instructions",
    tools: [tools]);

// From IChatClient
ChatClientAgent agent = new(chatClient, instructions: "...");
```

### Agent-as-Tool

```csharp
AIAgent child = CreateChildAgent();
AIAgent parent = CreateAgent(tools: [child.AsAIFunction()]);
```

### A2A Hosting

```csharp
var agent = builder.AddAIAgent("name", "instructions");
app.MapA2A(agent, "/path", agentCard: card);
```

### A2A Client

```csharp
A2ACardResolver resolver = new(new Uri("http://host/"));
AIAgent agent = await resolver.GetAIAgentAsync();
```

### Workflows

```csharp
// Sequential
Workflow w = AgentWorkflowBuilder.BuildSequential(a1, a2, a3);

// Concurrent
Workflow w = AgentWorkflowBuilder.BuildConcurrent([a1, a2]);

// Handoff
Workflow w = AgentWorkflowBuilder
    .CreateHandoffBuilderWith(intent)
    .WithHandoffs(intent, [expert1, expert2])
    .Build();
```

---

## References

- [Microsoft Agent Framework Documentation](https://learn.microsoft.com/en-us/agent-framework/)
- [A2A Protocol Specification](https://a2a-protocol.org/latest/)
- [rwjdk/MicrosoftAgentFrameworkSamples](https://github.com/rwjdk/MicrosoftAgentFrameworkSamples)
- [Microsoft Agent Framework GitHub](https://github.com/microsoft/agent-framework)
