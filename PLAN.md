---
title: "PLAN.md"
version: "1.0.15"
lastModified: "2025-12-17"
author: "HemSoft"
purpose: "MS Agent Framework Migration Plan"
---

# MS Agent Framework Migration Plan

## 🌟 North Star Vision

**Autonomous Agent Tasks via Event-Driven Architecture**

The ultimate goal is a system where users submit tasks to agents that execute autonomously in the background, returning structured results via events. This enables:

- **Non-blocking UX** - Submit task, continue chatting, get notified when complete
- **Scalable execution** - Workers run as separate processes for independent scaling
- **Observable workflows** - Redis provides visibility into task queue and progress
- **Structured results** - Agents return typed JSON responses, not just text

```text
┌─────────────────────────────────────────────────────────────────────┐
│                 HemSoft.PowerAI.Console (Publisher)                  │
│  User: / → Agents → "Research competitor pricing for widgets"       │
│  → Publishes AgentTaskRequest to Redis                              │
│  → Subscribes to results channel                                    │
│  → Continues chatting...                                            │
│  → Receives AgentTaskResult notification                            │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         REDIS (Pub/Sub)                              │
│  agents:tasks          → Task channel for worker consumption        │
│  agents:results:{id}   → Completion notifications per task          │
│  agents:progress:{id}  → Optional progress updates                  │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│               HemSoft.PowerAI.AgentWorker (Subscriber)               │
│  → Subscribes to agents:tasks channel                               │
│  → Executes agent autonomously (ResearchAgent, etc.)                │
│  → Publishes AgentTaskResult to agents:results:{taskId}             │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Project Structure

```text
HemSoft.PowerAI.sln
├── src/
│   ├── HemSoft.PowerAI.Shared/           # Shared models, interfaces, agents
│   │   ├── Models/
│   │   │   ├── AgentTaskRequest.cs
│   │   │   ├── AgentTaskResult.cs
│   │   │   └── AgentTaskStatus.cs
│   │   ├── Services/
│   │   │   ├── IAgentTaskBroker.cs
│   │   │   └── RedisAgentTaskBroker.cs
│   │   └── Agents/
│   │       └── ResearchAgent.cs
│   │
│   ├── HemSoft.PowerAI.Console/          # Interactive UI (Publisher)
│   │   └── Services/
│   │       └── AgentTaskService.cs       # Task submission facade
│   │
│   ├── HemSoft.PowerAI.AgentWorker/      # Background worker (Subscriber)
│   │   ├── AgentWorkerService.cs
│   │   └── Program.cs
│   │
│   └── HemSoft.PowerAI.AgentHost/        # A2A HTTP server (separate protocol)
│
└── tests/
    ├── HemSoft.PowerAI.Shared.Tests/
    ├── HemSoft.PowerAI.Console.Tests/
    └── HemSoft.PowerAI.AgentWorker.Tests/
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

## Phase 9: Event-Driven Autonomous Agents 🟢

**The main event.** Transform the agent system from synchronous request-response to asynchronous task execution.

### Architecture Decision: Separate Worker Process

The worker runs as a dedicated `HemSoft.PowerAI.AgentWorker` project:

- **Single Responsibility** - Console handles UI, Worker handles processing
- **Independent Scaling** - Scale workers based on task volume
- **Fault Isolation** - Worker crash doesn't affect Console UX
- **Clean Separation** - Shared types in `HemSoft.PowerAI.Shared`

### Data Models (in Shared)

```csharp
// Task submission
public record AgentTaskRequest(
    string TaskId,           // GUID
    string AgentType,        // "research" for now, extensible later
    string Prompt,           // User's request
    DateTimeOffset SubmittedAt);

// Task completion
public record AgentTaskResult(
    string TaskId,
    AgentTaskStatus Status,  // Completed, Failed, Cancelled
    JsonDocument? Data,      // Structured result (schema varies by agent)
    string? Error,
    DateTimeOffset CompletedAt);

public enum AgentTaskStatus { Pending, Running, Completed, Failed, Cancelled }
```

### Tasks

- [x] **9.1** Add Redis infrastructure
  - Add `StackExchange.Redis` package
  - Create `IAgentTaskBroker` interface for task submission/subscription
  - Create `RedisAgentTaskBroker` implementation
  - Configuration via `appsettings.json` for Redis connection

- [x] **9.2** Create Agent Worker service
  - `AgentWorkerService` - Background service that processes tasks
  - Subscribes to `agents:tasks` channel
  - Routes to appropriate agent based on `AgentType`
  - Publishes results to `agents:results:{taskId}`

- [x] **9.3** Update Console for async task flow
  - `/agents` menu with Submit Research Task, Check Pending Tasks options
  - `AgentTaskService` facade for task submission and result tracking
  - Background listener for task results via Redis pub/sub
  - Graceful fallback to synchronous mode when Redis unavailable
  - Task ID tracking with short ID display (first 8 chars)
  - Option to wait for results or continue chatting

- [ ] **9.4** Update ResearchAgent for structured output
  - Define `ResearchResult` schema (findings, sources, recommendations)
  - Return structured JSON instead of markdown text
  - Console renders structured result nicely

- [x] **9.5** Separate Worker into own project
  - Create `HemSoft.PowerAI.AgentWorker` project
  - Move shared types to `HemSoft.PowerAI.Shared`
  - Update `run-all.ps1` to start Worker process

### Success Criteria

- [x] User can submit research task and continue chatting
- [x] Task executes asynchronously via Redis pub/sub
- [ ] Structured `ResearchResult` JSON returned
- [x] Console displays notification when task completes
- [x] Task history/status queryable
- [x] Worker runs as separate process

---

## Phase 9.6: Iterative Research with EvaluatorAgent 🟢

**Goal:** Enable the ResearchAgent to iterate on findings rather than settling for a single pass. An `EvaluatorAgent` judges research quality and provides refinement guidance.

### Architecture

```text
┌─────────────────────────────────────────────────────────────────────┐
│                    IterativeResearchService                          │
│                                                                      │
│   ┌─────────────┐    findings    ┌─────────────────┐                │
│   │ Research    │ ────────────▶  │ Evaluator       │                │
│   │ Agent       │                │ Agent           │                │
│   │             │ ◀────────────  │                 │                │
│   └─────────────┘  refinedQuery  └─────────────────┘                │
│         │              │                                             │
│         │              ▼                                             │
│         │    ┌─────────────────────┐                                │
│         │    │ Iteration State     │                                │
│         │    │ - findings[]        │                                │
│         │    │ - evaluations[]     │                                │
│         └──▶ │ - currentQuery      │                                │
│              │ - iteration #       │                                │
│              └─────────────────────┘                                │
└─────────────────────────────────────────────────────────────────────┘
```

### Data Models

```csharp
// In-memory state for a research session
public record ResearchIterationState(
    string OriginalQuery,
    List<ResearchIteration> Iterations,
    bool IsComplete,
    string? FinalSynthesis);

public record ResearchIteration(
    int IterationNumber,
    string Query,
    string Findings,
    ResearchEvaluation Evaluation,
    DateTimeOffset Timestamp);

public record ResearchEvaluation(
    bool IsSatisfactory,
    int QualityScore,           // 1-10
    string[] Gaps,              // What's missing
    string[] FollowUpQuestions, // Suggested refinements
    string? RefinedQuery);      // Next query if not satisfactory
```

### Tasks

- [x] **9.6.1** Create `EvaluatorAgent` in `HemSoft.PowerAI.Shared/Agents`
  - Judges research completeness and quality
  - Returns structured `ResearchEvaluation`
  - No tools - pure reasoning agent

- [x] **9.6.2** Create research iteration models
  - `ResearchIterationState`, `ResearchIteration`, `ResearchEvaluation`
  - Simple in-memory storage for now

- [x] **9.6.3** Create `IterativeResearchService`
  - Orchestrates the research loop
  - Configurable max iterations (default: 3)
  - Accumulates findings across iterations
  - Synthesizes final result from all iterations

- [x] **9.6.4** Add `Deep Research (Iterative)` command to Console
  - Submits iterative research task
  - Shows iteration progress
  - Displays final synthesized result

- [x] **9.6.5** Unit tests for research iteration models

### Success Criteria

- [x] EvaluatorAgent correctly identifies research gaps
- [x] Research iterates until quality threshold met OR max iterations
- [x] Final synthesis incorporates findings from all iterations
- [x] User can see iteration progress in console

---

## Phase 10: Multi-Agent Orchestration 🟡

After event-driven foundation is solid, add workflow orchestration.

### Tasks

- [ ] **10.1** Add `Microsoft.Agents.AI.Workflows` package
- [ ] **10.2** Implement TriageAgent for intelligent routing
- [ ] **10.3** Create workflow definitions using `AgentWorkflowBuilder`
- [ ] **10.4** Support multi-step autonomous tasks with handoffs

---

## Files Affected (Phase 9.5 - Worker Separation)

| File | Action |
|------|--------|
| `src/HemSoft.PowerAI.Shared/Models/AgentTaskRequest.cs` | ✅ Moved from Console |
| `src/HemSoft.PowerAI.Shared/Models/AgentTaskResult.cs` | ✅ Moved from Console |
| `src/HemSoft.PowerAI.Shared/Models/AgentTaskStatus.cs` | ✅ Moved from Console |
| `src/HemSoft.PowerAI.Shared/Services/IAgentTaskBroker.cs` | ✅ Moved from Console |
| `src/HemSoft.PowerAI.Shared/Services/RedisAgentTaskBroker.cs` | ✅ Moved from Console |
| `src/HemSoft.PowerAI.AgentWorker/AgentWorkerService.cs` | ✅ Moved from Console |
| `src/HemSoft.PowerAI.AgentWorker/Program.cs` | ✅ New worker host |
| `src/HemSoft.PowerAI.AgentWorker/appsettings.json` | ✅ Redis config |
| `run-all.ps1` | ✅ Start Worker process |

---

## Current State

- ✅ **707 tests passing**
- ✅ **Build succeeds with no warnings**
- ✅ Console simplified: `/` menu → Model | Agents
- ✅ Phase 9.1 complete - Redis infrastructure with `IAgentTaskBroker` and `RedisAgentTaskBroker`
- ✅ Phase 9.2 complete - `AgentWorkerService` background worker
- ✅ Phase 9.3 complete - Async task flow in Console with `AgentTaskService`
- ✅ Phase 9.5 complete - Worker separated into `HemSoft.PowerAI.AgentWorker`
- ✅ Phase 9.6 complete - Iterative Research with EvaluatorAgent
- 🚧 **Next:** Phase 10 - Multi-Agent Orchestration

---

## References

- [ARCHITECTURE.md](./agents/global/ARCHITECTURE.md) - System architecture and design decisions
- [REFERENCE-MATERIAL.md](./research-material/REFERENCE-MATERIAL.md) - MS Agent Framework docs
- [rwjdk/MicrosoftAgentFrameworkSamples](https://github.com/rwjdk/MicrosoftAgentFrameworkSamples) - Samples
- [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) - Redis client
