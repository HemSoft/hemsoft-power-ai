# HemSoft Power AI

AI-powered command-line assistant with multi-agent orchestration using Microsoft Agent Framework and OpenRouter.

## Quick Start

1. Set required environment variables:
   
   ```powershell
   $env:OPENROUTER_API_KEY = "your-api-key"
   $env:OPENROUTER_BASE_URL = "https://openrouter.ai/api/v1"
   ```

2. Run the app:
   
   ```powershell
   .\run.ps1
   ```

3. Chat naturally or type `/` to select an agent.

## Usage

The app has two modes:

| Mode       | How       | What it does                                                       |
| ---------- | --------- | ------------------------------------------------------------------ |
| **Chat**   | Just type | Natural conversation with tool access (terminal, web search, mail) |
| **Agents** | Type `/`  | Select a specialized agent for specific tasks                      |

### Available Agents

| Agent            | Purpose                                                           |
| ---------------- | ----------------------------------------------------------------- |
| **Coordinator**  | Multi-agent orchestration - delegates tasks to specialized agents |
| **SpamFilter**   | Interactive spam filter with autonomous capabilities              |
| **SpamScan**     | Autonomous scan: identifies suspicious email domains              |
| **SpamReview**   | Human review: batch review flagged domains                        |
| **SpamCleanup**  | Cleanup: move emails from blocked domains to junk                 |
| **HostResearch** | A2A server: hosts ResearchAgent for remote access                 |
| **Distributed**  | A2A client: connects to remote agents                             |

### Built-in Tools

The main chat has access to:

- **Terminal** - Execute PowerShell commands
- **WebSearch** - Search the web for information
- **Mail** - Full Outlook/Hotmail access (read, send, delete, search, move, spam management)

## Configuration

### Environment Variables

| Variable              | Required | Description                                      |
| --------------------- | -------- | ------------------------------------------------ |
| `OPENROUTER_API_KEY`  | ✅ Yes    | Your OpenRouter API key                          |
| `OPENROUTER_BASE_URL` | ✅ Yes    | `https://openrouter.ai/api/v1`                   |
| `GRAPH_CLIENT_ID`     | For Mail | Azure app registration client ID                 |
| `RESEARCH_AGENT_URL`  | Optional | Remote research agent URL (for distributed mode) |

### Application Settings (appsettings.json)

```json
{
  "SpamFilter": {
    "BatchSize": 10,
    "DelayBetweenBatchesSeconds": 30,
    "SpamDomainsFilePath": "Data/SpamDomains.json",
    "SpamCandidatesFilePath": "Data/SpamCandidates.json"
  },
  "A2A": {
    "DefaultResearchAgentUrl": "http://localhost:7071/",
    "ResearchAgentHostPort": 5001
  }
}
```

### Microsoft Graph Setup (for Outlook Mail)

1. Register an app at [Microsoft Entra admin center](https://entra.microsoft.com)
2. Set "Supported account types" to **Personal Microsoft accounts only**
3. Add **Mobile and desktop applications** platform with redirect URI: `http://localhost`
4. Enable **Allow public client flows**
5. Add API permissions: `User.Read`, `Mail.Read`, `Mail.ReadWrite`, `Mail.Send`
6. Set `GRAPH_CLIENT_ID` to your Application (client) ID

## Running

### Interactive Mode (default)

```powershell
.\run.ps1
# or
dotnet run --project src/HemSoft.PowerAI.Console
```

### Direct Commands

```powershell
# Run coordinator with a prompt
dotnet run --project src/HemSoft.PowerAI.Console -- coordinate "research quantum computing"

# Run spam filter agent
dotnet run --project src/HemSoft.PowerAI.Console -- spam

# Host research agent as A2A server
dotnet run --project src/HemSoft.PowerAI.Console -- host-research

# Connect to distributed agents
dotnet run --project src/HemSoft.PowerAI.Console -- distributed
```

## Architecture

Built on Microsoft Agent Framework with A2A (Agent-to-Agent) protocol support:

- **Local agents** run in-process for low latency
- **Remote agents** connect via A2A protocol for distributed workloads
- **Coordinator** intelligently routes tasks to specialist agents
