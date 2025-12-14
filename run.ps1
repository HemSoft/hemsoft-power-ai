#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the HemSoft Power AI application.

.DESCRIPTION
    Supports multiple run modes:
    - Default: Interactive chat mode
    - spam: Spam filter agent mode
    - agents: Start Azure Functions A2A agents
    - aspire: Start standalone Aspire Dashboard for telemetry visualization

.PARAMETER Mode
    The run mode: 'chat' (default), 'spam', 'agents', or 'aspire'

.EXAMPLE
    ./run.ps1
    Runs in interactive chat mode.

.EXAMPLE
    ./run.ps1 spam
    Runs the spam filter agent.

.EXAMPLE
    ./run.ps1 agents
    Starts the Azure Functions A2A agents (ResearchAgent).
    Requires Azure Functions Core Tools v4.

.EXAMPLE
    ./run.ps1 aspire
    Starts the Aspire Dashboard for telemetry visualization.
    Run './run.ps1' or './run.ps1 spam' in another terminal to send telemetry.
#>
param(
    [Parameter(Position = 0)]
    [ValidateSet('chat', 'spam', 'agents', 'aspire')]
    [string]$Mode = 'chat'
)

$ErrorActionPreference = 'Stop'

switch ($Mode) {
    'agents' {
        Write-Host "Starting Azure Functions A2A Agents..." -ForegroundColor Cyan
        Write-Host ""

        # Check if Azure Functions Core Tools is available
        $funcAvailable = Get-Command func -ErrorAction SilentlyContinue
        if (-not $funcAvailable) {
            Write-Host "Azure Functions Core Tools is required." -ForegroundColor Red
            Write-Host "Install with: npm install -g azure-functions-core-tools@4" -ForegroundColor Yellow
            Write-Host "Or: winget install Microsoft.Azure.FunctionsCoreTools" -ForegroundColor Yellow
            exit 1
        }

        # Check if Azurite is running (required for local storage)
        $azuriteRunning = Test-NetConnection -ComputerName localhost -Port 10000 -WarningAction SilentlyContinue -ErrorAction SilentlyContinue
        if (-not $azuriteRunning.TcpTestSucceeded) {
            Write-Host "Warning: Azurite does not appear to be running on port 10000." -ForegroundColor Yellow
            Write-Host "Start Azurite with: azurite --silent" -ForegroundColor Yellow
            Write-Host "Or install: npm install -g azurite" -ForegroundColor Yellow
            Write-Host ""
        }

        Write-Host "Endpoints:" -ForegroundColor Green
        Write-Host "  GET  http://localhost:7071/.well-known/agent.json  (Agent discovery)" -ForegroundColor Gray
        Write-Host "  POST http://localhost:7071/api/research            (ResearchAgent)" -ForegroundColor Gray
        Write-Host ""

        Push-Location $PSScriptRoot/src/HemSoft.PowerAI.Agents
        try {
            func start
        }
        finally {
            Pop-Location
        }
    }
    'aspire' {
        Write-Host "Starting Aspire Dashboard (standalone) for observability..." -ForegroundColor Cyan
        Write-Host "Run '.\run.ps1' or '.\run.ps1 spam' in another terminal to send telemetry here." -ForegroundColor Yellow
        Write-Host ""

        # Check if Docker is available
        $dockerAvailable = Get-Command docker -ErrorAction SilentlyContinue
        if (-not $dockerAvailable) {
            Write-Host "Docker is required for standalone Aspire Dashboard." -ForegroundColor Red
            Write-Host "Install Docker Desktop from https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
            exit 1
        }

        # Stop any existing dashboard container
        docker stop aspire-dashboard 2>$null

        # Run the standalone Aspire Dashboard
        Write-Host "Starting Aspire Dashboard container..." -ForegroundColor Cyan
        Write-Host "Dashboard URL: http://localhost:18888" -ForegroundColor Green
        Write-Host ""
        docker run --rm -it `
            -p 18888:18888 `
            -p 4317:18889 `
            -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true `
            --name aspire-dashboard `
            mcr.microsoft.com/dotnet/aspire-dashboard:latest
    }
    'spam' {
        $env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4317"
        $env:OTEL_SERVICE_NAME = "HemSoft.PowerAI.SpamFilter"
        Push-Location $PSScriptRoot/src/HemSoft.PowerAI.Console
        try {
            dotnet run -- spam
        }
        finally {
            Pop-Location
        }
    }
    default {
        $env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4317"
        $env:OTEL_SERVICE_NAME = "HemSoft.PowerAI.Console"
        Push-Location $PSScriptRoot/src/HemSoft.PowerAI.Console
        try {
            dotnet run
        }
        finally {
            Pop-Location
        }
    }
}
