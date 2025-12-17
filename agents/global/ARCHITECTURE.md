---
title: "ARCHITECTURE.md"
version: "1.1.0"
lastModified: "2025-12-16"
author: "HemSoft"
purpose: "System architecture for HemSoft Power AI"
---

# HemSoft Power AI Architecture

## North Star Vision

**Autonomous Agent Tasks via Event-Driven Architecture**

The ultimate goal is a system where users submit tasks to agents that execute autonomously in the background, returning structured results via events. This enables:

- **Non-blocking UX** - Submit task, continue chatting, get notified when complete
- **Scalable execution** - Workers run as separate processes for independent scaling
- **Observable workflows** - Redis provides visibility into task queue and progress
- **Structured results** - Agents return typed JSON responses, not just text

---

## System Overview

```text
┌─────────────────────────────────────────────────────────────────────┐
│                    HemSoft.PowerAI.Console (Publisher)               │
│  User: /agents → "Research competitor pricing for widgets"          │
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
│                 HemSoft.PowerAI.AgentWorker (Subscriber)             │
│  → Subscribes to agents:tasks channel                               │
│  → Executes agent autonomously (ResearchAgent, etc.)                │
│  → Publishes AgentTaskResult to agents:results:{taskId}             │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Project Structure

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│                           HemSoft.PowerAI.Shared                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ Models/              │ Services/               │ Agents/              │  │
│  │ - AgentTaskRequest   │ - IAgentTaskBroker      │ - ResearchAgent      │  │
│  │ - AgentTaskResult    │ - RedisAgentTaskBroker  │ - AgentFactory       │  │
│  │ - AgentTaskStatus    │                         │ - AgentCards         │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                    Shared abstractions, models, and agent definitions        │
└─────────────────────────────────────────────────────────────────────────────┘
              │                       │                       │
              │ references            │ references            │ references
              ▼                       ▼                       ▼
┌───────────────────────┐ ┌───────────────────────┐ ┌───────────────────────┐
│ HemSoft.PowerAI.      │ │ HemSoft.PowerAI.      │ │ HemSoft.PowerAI.      │
│ Console               │ │ AgentWorker           │ │ AgentHost             │
│ ─────────────────     │ │ ─────────────────     │ │ ─────────────────     │
│ - Interactive UI      │ │ - AgentWorkerService  │ │ - A2A HTTP hosting    │
│ - AgentTaskService    │ │ - BackgroundService   │ │ - MapA2A() endpoints  │
│ - Task submission     │ │ - Task processing     │ │ - Agent discovery     │
│ (Publisher)           │ │ (Subscriber)          │ │ (HTTP Protocol)       │
└───────────────────────┘ └───────────────────────┘ └───────────────────────┘
```

### Separation of Concerns

| Project | Concern | Protocol |
|---------|---------|----------|
| **Console** | User interaction, task submission | Redis Pub (Publisher) |
| **AgentWorker** | Background task processing | Redis Sub (Subscriber) |
| **AgentHost** | HTTP-based agent exposure | A2A over HTTP |
| **Shared** | Common models, interfaces, agents | N/A (Library) |

---

## Core Components

### Console Application (`HemSoft.PowerAI.Console`)

The primary user interface providing:

- Interactive chat with AI models (OpenRouter, Azure OpenAI)
- Agent task submission via `/agents` command
- Real-time notifications for completed tasks
- Structured result rendering

**Key Responsibilities:**

- User interaction and command parsing
- Task submission to Redis (publishing)
- Result subscription and display
- Model selection and configuration

**Key Classes:**

- `AgentTaskService` - Facade for task submission and result tracking
- `Program.cs` - Main chat loop and command routing

### Agent Worker (`HemSoft.PowerAI.AgentWorker`)

Dedicated background service that processes agent tasks:

- Subscribes to Redis `agents:tasks` channel
- Routes tasks to appropriate agents
- Publishes results to `agents:results:{taskId}`

**Key Responsibilities:**

- Task subscription (consuming from Redis)
- Agent routing based on `AgentType`
- Result publishing
- Error handling and status reporting

**Key Classes:**

- `AgentWorkerService` - `BackgroundService` that processes tasks
- `Program.cs` - Worker host configuration

### Agent Host (`HemSoft.PowerAI.AgentHost`)

ASP.NET Core service hosting agents via the A2A (Agent-to-Agent) protocol:

- `MapA2A()` pattern for agent exposure
- Agent card resolution for discovery
- HTTP-based agent invocation

**Key Responsibilities:**

- A2A protocol compliance
- Agent lifecycle management
- Remote agent discovery

**Note:** AgentHost is for HTTP-based A2A protocol, **not** Redis pub/sub.

### Shared Library (`HemSoft.PowerAI.Shared`)

Common abstractions and implementations shared across projects:

- Agent definitions (ResearchAgent, etc.)
- Task broker interface and Redis implementation
- Data models for task communication
- Tool definitions

---

## Agent Architecture

### Agent Types

| Agent                     | Purpose                               | Output                      |
| ------------------------- | ------------------------------------- | --------------------------- |
| **CoordinatorAgent**      | Routes requests to specialist agents  | Delegated response          |
| **MailAgent**             | Email operations (read, send, search) | Mail data/confirmation      |
| **ResearchAgent**         | Information gathering and analysis    | Structured `ResearchResult` |
| **TriageAgent** (planned) | Intelligent request routing           | Routing decision            |

### Agent Composition Pattern

Agents expose their capabilities as `AIFunction` instances via `AsAIFunction()`:

```csharp
// Agent as a callable function
var mailFunction = mailAgent.AsAIFunction();

// Coordinator uses agent functions as tools
coordinator.AddTool(mailFunction);
```

This enables:

- Loose coupling between agents
- Runtime agent discovery
- Consistent invocation pattern

---

## Event-Driven Task Processing

### Data Models

```csharp
// Task submission
public record AgentTaskRequest(
    string TaskId,           // GUID for tracking
    string AgentType,        // "research", "mail", etc.
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

### Redis Channels

| Channel                    | Purpose                           |
| -------------------------- | --------------------------------- |
| `agents:tasks`             | Task queue for worker consumption |
| `agents:results:{taskId}`  | Completion notifications per task |
| `agents:progress:{taskId}` | Optional progress updates         |

### Task Flow

1. **Submit**: Console publishes `AgentTaskRequest` to `agents:tasks`
2. **Acknowledge**: Worker picks up task, updates status to `Running`
3. **Execute**: Worker routes to appropriate agent based on `AgentType`
4. **Complete**: Worker publishes `AgentTaskResult` to `agents:results:{taskId}`
5. **Notify**: Console receives result via subscription

---

## Deployment Model

### Local Development

All three processes run locally via `run-all.ps1`:

```text
┌─────────────────────────────────────────────────────────────────────┐
│                        run-all.ps1                                   │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │
│  │ Aspire Dashboard │  │ AgentHost        │  │ Console          │   │
│  │ (Telemetry)      │  │ (A2A HTTP)       │  │ (Interactive)    │   │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘   │
│                                                                      │
│  ┌──────────────────┐                                               │
│  │ AgentWorker      │   ← NEW: Separate process                     │
│  │ (Redis Sub)      │                                               │
│  └──────────────────┘                                               │
└─────────────────────────────────────────────────────────────────────┘
```

### Production Deployment

Scale workers independently via containers:

```text
┌──────────────┐     ┌───────┐     ┌──────────────┐
│   Console    │────▶│ Redis │◀────│   Worker 1   │
│  (Publisher) │     │       │     │  (Subscriber)│
└──────────────┘     └───────┘     └──────────────┘
                         ▲
                         │
                    ┌────┴───────┐
                    │  Worker 2  │
                    │  (Scale)   │
                    └────────────┘
```

**Benefits:**

- **Independent scaling** - Add workers based on task volume
- **Fault isolation** - Worker crash doesn't affect Console
- **Resource optimization** - Workers can run on cheaper compute
- **Zero-downtime deployments** - Update workers without Console restart

---

## Observability

### Telemetry Stack

- **OpenTelemetry** - Distributed tracing and metrics
- **Application Insights** - Azure-hosted telemetry (optional)
- **File Trace Exporter** - Local development traces

### Key Instrumentation Points

| Component                | Traces                             |
| ------------------------ | ---------------------------------- |
| `FunctionCallMiddleware` | Tool invocations with parameters   |
| `AgentWorkerService`     | Task pickup, execution, completion |
| `RedisAgentTaskQueue`    | Queue operations                   |

### Correlation

All operations within a task share a correlation ID:

- Task ID serves as trace correlation
- Enables end-to-end request tracking
- Visible in Redis channel names

---

## Security Considerations

### Secrets Management

- Connection strings in `appsettings.json` (local dev)
- Azure Key Vault for production secrets
- Environment variables for CI/CD

### Agent Isolation

- Agents operate with principle of least privilege
- Tool permissions scoped per agent
- No shared mutable state between agents

### Redis Security

- TLS 1.2+ for encrypted connections
- Authentication via connection string
- Network isolation in production

---

## Design Decisions

### Why Redis?

| Consideration   | Decision                                  |
| --------------- | ----------------------------------------- |
| **Simplicity**  | Single dependency for queue + pub/sub     |
| **Performance** | Sub-millisecond latency                   |
| **Flexibility** | Supports caching, sessions, rate limiting |
| **Evolution**   | Can graduate to Service Bus if needed     |

### Why In-Process First?

| Consideration  | Decision                                |
| -------------- | --------------------------------------- |
| **Deployment** | Single artifact to deploy               |
| **Debugging**  | Unified debugging experience            |
| **Complexity** | Defer distributed system concerns       |
| **Migration**  | Redis abstraction enables later scaling |

### Why Structured Results?

| Consideration   | Decision                                |
| --------------- | --------------------------------------- |
| **Type Safety** | Compile-time schema validation          |
| **Rendering**   | Rich UI presentation options            |
| **Integration** | Machine-readable for downstream systems |
| **Versioning**  | Schema evolution support                |

---

## Future Considerations

### Multi-Agent Orchestration (Phase 10)

- `Microsoft.Agents.AI.Workflows` for workflow definitions
- `AgentWorkflowBuilder` for declarative workflows
- TriageAgent for intelligent routing

### Scaling Patterns

- Horizontal worker scaling via container instances
- Queue partitioning for high throughput
- Result caching for repeated queries

### Additional Agents

- **CalendarAgent** - Meeting scheduling and availability
- **DocumentAgent** - Document analysis and summarization
- **CodeAgent** - Code generation and review

---

## References

- [PLAN.md](../../PLAN.md) - Migration plan and implementation status
- [REFERENCE-MATERIAL.md](../../research-material/REFERENCE-MATERIAL.md) - MS Agent Framework documentation
- [Azure Cache for Redis Best Practices](https://learn.microsoft.com/en-us/azure/azure-cache-for-redis/cache-best-practices-development)
- [Cloud Design Patterns](https://learn.microsoft.com/en-us/azure/architecture/patterns/)
