#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Starts the complete HemSoft Power AI stack.

.DESCRIPTION
    Launches all components in the correct order:
    1. Aspire Dashboard (Docker container for telemetry)
    2. A2A Agent Host (ASP.NET Core - separate terminal)
    3. Agent Worker (Background service for Redis tasks - separate terminal)
    4. Console App (current terminal)

.PARAMETER Mode
    Console mode: 'chat' (default) or 'spam'

.PARAMETER SkipAspire
    Skip starting the Aspire Dashboard

.PARAMETER SkipAgents
    Skip starting the A2A Agent Host

.PARAMETER SkipWorker
    Skip starting the Agent Worker

.EXAMPLE
    ./run-all.ps1
    Starts everything with chat mode.

.EXAMPLE
    ./run-all.ps1 spam
    Starts everything with spam filter mode.

.EXAMPLE
    ./run-all.ps1 -SkipAspire
    Starts agents and console without Aspire Dashboard.
#>
param(
    [Parameter(Position = 0)]
    [ValidateSet('chat', 'spam')]
    [string]$Mode = 'chat',

    [switch]$SkipAspire,
    [switch]$SkipAgents,
    [switch]$SkipWorker
)

$ErrorActionPreference = 'Stop'
$ScriptRoot = $PSScriptRoot

Write-Host "HemSoft Power AI" -ForegroundColor Cyan -NoNewline
Write-Host " - " -NoNewline

# Step 1: Start Aspire Dashboard
if (-not $SkipAspire) {
    Write-Host "[1/4] Aspire" -ForegroundColor Yellow -NoNewline

    $dockerAvailable = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $dockerAvailable) {
        Write-Host " (skipped)" -ForegroundColor DarkYellow -NoNewline
    }
    else {
        # Stop existing container if running
        docker stop aspire-dashboard 2>$null | Out-Null
        docker rm aspire-dashboard 2>$null | Out-Null

        # Start in detached mode
        docker run -d `
            -p 18888:18888 `
            -p 4317:18889 `
            -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true `
            --name aspire-dashboard `
            mcr.microsoft.com/dotnet/aspire-dashboard:latest | Out-Null

        Write-Host " ✓" -ForegroundColor Green -NoNewline
    }
}
else {
    Write-Host "[1/4] -" -ForegroundColor DarkGray -NoNewline
}

Write-Host " → " -ForegroundColor DarkGray -NoNewline

# Step 2: Start A2A Agent Host
if (-not $SkipAgents) {
    Write-Host "[2/4] AgentHost" -ForegroundColor Yellow -NoNewline

    # Start agent host in new terminal
    $agentHostPath = Join-Path $ScriptRoot "src/HemSoft.PowerAI.AgentHost"
    Start-Process pwsh -ArgumentList "-NoExit", "-Command", "Set-Location '$agentHostPath'; dotnet run"

    Write-Host " ✓" -ForegroundColor Green -NoNewline
}
else {
    Write-Host "[2/4] -" -ForegroundColor DarkGray -NoNewline
}

Write-Host " → " -ForegroundColor DarkGray -NoNewline

# Step 3: Start Agent Worker
if (-not $SkipWorker) {
    Write-Host "[3/4] Worker" -ForegroundColor Yellow -NoNewline

    # Start worker in new terminal
    $workerPath = Join-Path $ScriptRoot "src/HemSoft.PowerAI.AgentWorker"
    Start-Process pwsh -ArgumentList "-NoExit", "-Command", "Set-Location '$workerPath'; `$env:OTEL_EXPORTER_OTLP_ENDPOINT = 'http://localhost:4317'; dotnet run"

    Write-Host " ✓" -ForegroundColor Green -NoNewline

    # Give services time to start (show countdown)
    Write-Host " (init" -ForegroundColor DarkGray -NoNewline
    for ($i = 3; $i -gt 0; $i--) {
        Write-Host "." -ForegroundColor DarkGray -NoNewline
        Start-Sleep -Seconds 1
    }
    Write-Host ")" -ForegroundColor DarkGray -NoNewline
}
else {
    Write-Host "[3/4] -" -ForegroundColor DarkGray -NoNewline
}

Write-Host " → " -ForegroundColor DarkGray -NoNewline

# Step 4: Start Console App
Write-Host "[4/4] Console" -ForegroundColor Yellow -NoNewline
Write-Host " ✓" -ForegroundColor Green
Write-Host ""

$env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4317"
$env:OTEL_SERVICE_NAME = if ($Mode -eq 'spam') { "HemSoft.PowerAI.SpamFilter" } else { "HemSoft.PowerAI.Console" }
$env:RESEARCH_AGENT_URL = "http://localhost:5001/"

Push-Location $ScriptRoot/src/HemSoft.PowerAI.Console
try {
    if ($Mode -eq 'spam') {
        dotnet run -- spam
    }
    else {
        dotnet run
    }
}
finally {
    Pop-Location
}
