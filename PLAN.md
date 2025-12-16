---
title: "PLAN.md"
version: "1.0.9"
lastModified: "2025-12-16"
author: "HemSoft"
purpose: "MS Agent Framework Migration Plan"
---

# MS Agent Framework Migration Plan

## 🌟 North Star Vision

**Autonomous Agent Tasks via Event-Driven Architecture**

The ultimate goal is a system where users submit tasks to agents that execute autonomously in the background, returning structured results via events. This enables:

- **Non-blocking UX** - Submit task, continue chatting, get notified when complete
- **Scalable execution** - Workers can run in-process or as separate services
- **Observable workflows** - Redis provides visibility into task queue and progress
- **Structured results** - Agents return typed JSON responses, not just text

```
┌─────────────────────────────────────────────────────────────────────┐
│                         CONSOLE APP                                  │
│  User: / → Agents → "Research competitor pricing for widgets"       │
│  → Publishes AgentTaskRequest to Redis                              │
│  → Subscribes to results channel                                    │
│  → Continues chatting...                                            │
│  → Receives AgentTaskResult notification                            │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         REDIS                                        │
│  agents:tasks          → Task queue                                 │
│  agents:results:{id}   → Completion notifications                   │
│  agents:progress:{id}  → Optional progress updates                  │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    AGENT WORKER (Background)                         │
│  → Picks up AgentTaskRequest                                        │
│  → Executes agent autonomously (ResearchAgent, etc.)                │
│  → Publishes AgentTaskResult (structured JSON)                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## ✅ Completed Phases (1-8)

Foundation work establishing MS Agent Framework patterns:

| Phase | Summary |
|-------|---------|
| **1. A2A Hosting** | Replaced Azure Functions with ASP.NET Core `MapA2A()` pattern. Created `HemSoft.PowerAI.AgentHost` project. |
| **2. A2A Client** | Simplified to use `A2ACardResolver.GetAIAgentAsync()` directly. Returns `(AIAgent, AgentCard)` tuple. |
| **3. RemoteAgentTool** | Eliminated static state. Now use `agent.AsAIFunction()` directly. |
| **4. Package Updates** | All packages at latest versions. MS Agent Framework preview packages current. |
| **5. Observability** | Added `FunctionCallMiddleware` for OpenTelemetry tracing of tool invocations. |
| **6. Workflows** | Deferred - Will revisit after event-driven architecture. |
| **7. Tools → Agents** | Created `MailAgent` with encapsulated tools. Coordinator uses `AsAIFunction()`. |
| **8. Console UX** | Simplified to `/` menu with Model + Agents options. Modern `❯` prompt. |

**Key artifacts from completed phases:**

- `HemSoft.PowerAI.AgentHost` - A2A server hosting
- `MailAgent`, `ResearchAgent`, `CoordinatorAgent` - Specialist agents
- `FunctionCallMiddleware` - Telemetry integration
- Clean console UX with agent menu

---

## Phase 9: Event-Driven Autonomous Agents 🔴

**The main event.** Transform the agent system from synchronous request-response to asynchronous task execution.

### Data Models

```csharp
// Task submission
public record AgentTaskRequest(
    string TaskId,           // GUID
    string AgentType,        // "research" for now, extensible later
    string Prompt,           // User's request
    DateTime SubmittedAt);

// Task completion
public record AgentTaskResult(
    string TaskId,
    AgentTaskStatus Status,  // Completed, Failed, Cancelled
    JsonDocument? Data,      // Structured result (schema varies by agent)
    string? Error,
    DateTime CompletedAt);

public enum AgentTaskStatus { Pending, Running, Completed, Failed, Cancelled }
```

### Tasks

- [ ] **9.1** Add Redis infrastructure
  - Add `StackExchange.Redis` package
  - Create `IAgentTaskQueue` interface for task submission/subscription
  - Create `RedisAgentTaskQueue` implementation
  - Configuration via `appsettings.json` for Redis connection

- [ ] **9.2** Create Agent Worker service
  - `AgentWorkerService` - Background service that processes tasks
  - Subscribes to `agents:tasks` channel
  - Routes to appropriate agent based on `AgentType`
  - Publishes results to `agents:results:{taskId}`

- [ ] **9.3** Update Console for async task flow
  - `/agents` prompts for task, submits to queue
  - Shows "Task submitted" confirmation with task ID
  - Background listener for results (notification when complete)
  - Option to check task status

- [ ] **9.4** Update ResearchAgent for structured output
  - Define `ResearchResult` schema (findings, sources, recommendations)
  - Return structured JSON instead of markdown text
  - Console renders structured result nicely

### Architecture Decision: In-Process First

Start with the worker running as a background service in the same process:

```csharp
// Program.cs - register worker
builder.Services.AddHostedService<AgentWorkerService>();
builder.Services.AddSingleton<IAgentTaskQueue, RedisAgentTaskQueue>();
```

This keeps deployment simple while the Redis abstraction enables future scaling to separate worker processes.

### Success Criteria

- [ ] User can submit research task and continue chatting
- [ ] Task executes asynchronously via Redis queue
- [ ] Structured `ResearchResult` JSON returned
- [ ] Console displays notification when task completes
- [ ] Task history/status queryable

---

## Phase 10: Multi-Agent Orchestration 🟡

After event-driven foundation is solid, add workflow orchestration.

### Tasks

- [ ] **10.1** Add `Microsoft.Agents.AI.Workflows` package
- [ ] **10.2** Implement TriageAgent for intelligent routing
- [ ] **10.3** Create workflow definitions using `AgentWorkflowBuilder`
- [ ] **10.4** Support multi-step autonomous tasks with handoffs

---

## Files Affected (Phase 9)

| File | Action |
|------|--------|
| `src/HemSoft.PowerAI.Console/Services/IAgentTaskQueue.cs` | NEW - Interface |
| `src/HemSoft.PowerAI.Console/Services/RedisAgentTaskQueue.cs` | NEW - Redis impl |
| `src/HemSoft.PowerAI.Console/Services/AgentWorkerService.cs` | NEW - Background worker |
| `src/HemSoft.PowerAI.Console/Models/AgentTaskRequest.cs` | NEW - Task model |
| `src/HemSoft.PowerAI.Console/Models/AgentTaskResult.cs` | NEW - Result model |
| `src/HemSoft.PowerAI.Console/Agents/ResearchAgent.cs` | Structured output |
| `src/HemSoft.PowerAI.Console/Program.cs` | Register services, update /agents |
| `src/HemSoft.PowerAI.Console/appsettings.json` | Redis configuration |

---

## Current State

- ✅ **648 tests passing**
- ✅ **Build succeeds with no warnings**
- ✅ Console simplified: `/` menu → Model | Agents
- ✅ ResearchAgent works synchronously (placeholder for async)
- 🚧 **Next:** Phase 9 - Redis event-driven architecture

---

## References

- [REFERENCE-MATERIAL.md](./research-material/REFERENCE-MATERIAL.md) - MS Agent Framework docs
- [rwjdk/MicrosoftAgentFrameworkSamples](https://github.com/rwjdk/MicrosoftAgentFrameworkSamples) - Samples
- [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) - Redis client
