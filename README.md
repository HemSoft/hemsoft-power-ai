# HemSoft Power AI

Powerful AI command-line tools using Microsoft.Extensions.AI with tool-calling capabilities via OpenRouter.

## Features

- **Terminal Tools**: Execute PowerShell commands
- **Web Search Tools**: Search the web for information
- **Outlook Mail Tools**: Access Outlook/Hotmail mailbox (inbox, read, send, search, delete, move, junk)

## Requirements

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `OPENROUTER_API_KEY` | Yes | OpenRouter API key for LLM access |
| `OPENROUTER_BASE_URL` | Yes | OpenRouter endpoint (`https://openrouter.ai/api/v1`) |
| `GRAPH_CLIENT_ID` | For Mail | Azure app registration client ID for Microsoft Graph |

### Microsoft Graph Setup (for Outlook Mail)

1. Register an app at [Microsoft Entra admin center](https://entra.microsoft.com)
2. Set "Supported account types" to **Personal Microsoft accounts only**
3. Add **Mobile and desktop applications** platform with redirect URI: `http://localhost`
4. Enable **Allow public client flows**
5. Add API permissions: `User.Read`, `Mail.Read`, `Mail.ReadWrite`, `Mail.Send`
6. Set `GRAPH_CLIENT_ID` to your Application (client) ID

## Running

```powershell
.\run.ps1
```

Or:

```powershell
dotnet run --project src/HemSoft.PowerAI.Console
```

## Commands

In the chat interface:

- `check inbox` - List recent emails
- `read <id>` - Read a specific email
- `search <query>` - Search emails
- `send <to> <subject> <body>` - Send an email
- `delete <id>` - Delete an email
- `move <id> <folder>` - Move email to folder (inbox, archive, deleteditems, junkemail)
- `exit` - Quit the application
