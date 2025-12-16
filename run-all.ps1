#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Starts the complete HemSoft Power AI stack.

.DESCRIPTION
    Launches all components in the correct order:
    1. Aspire Dashboard (Docker container for telemetry)
    2. A2A Agent Host (ASP.NET Core - separate terminal)
    3. Console App (current terminal)

.PARAMETER Mode
    Console mode: 'chat' (default) or 'spam'

.PARAMETER SkipAspire
    Skip starting the Aspire Dashboard

.PARAMETER SkipAgents
    Skip starting the A2A Agent Host

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
    [switch]$SkipAgents
)

$ErrorActionPreference = 'Stop'
$ScriptRoot = $PSScriptRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " HemSoft Power AI - Full Stack Startup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Start Aspire Dashboard
if (-not $SkipAspire) {
    Write-Host "[1/3] Starting Aspire Dashboard..." -ForegroundColor Yellow

    $dockerAvailable = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $dockerAvailable) {
        Write-Host "  Docker not available, skipping Aspire Dashboard." -ForegroundColor DarkYellow
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

        Write-Host "  Aspire Dashboard: http://localhost:18888" -ForegroundColor Green
    }
}
else {
    Write-Host "[1/3] Skipping Aspire Dashboard" -ForegroundColor DarkGray
}

# Step 2: Start A2A Agent Host
if (-not $SkipAgents) {
    Write-Host "[2/3] Starting A2A Agent Host..." -ForegroundColor Yellow

    # Start agent host in new terminal
    $agentHostPath = Join-Path $ScriptRoot "src/HemSoft.PowerAI.AgentHost"
    Start-Process pwsh -ArgumentList "-NoExit", "-Command", "Set-Location '$agentHostPath'; dotnet run"

    Write-Host "  Agent Host started in new terminal" -ForegroundColor Green
    Write-Host "  Agent Card: http://localhost:5001/.well-known/agent-card.json" -ForegroundColor Gray
    Write-Host "  Health:     http://localhost:5001/health" -ForegroundColor Gray

    # Give agent host time to start
    Write-Host "  Waiting 5 seconds for agent host to initialize..." -ForegroundColor DarkGray
    Start-Sleep -Seconds 5
}
else {
    Write-Host "[2/3] Skipping A2A Agent Host" -ForegroundColor DarkGray
}

# Step 3: Start Console App
Write-Host "[3/3] Starting Console App ($Mode mode)..." -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
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
