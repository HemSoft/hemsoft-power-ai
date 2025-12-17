#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the HemSoft Power AI application.

.DESCRIPTION
    Supports multiple run modes:
    - Default: Interactive chat mode
    - spam: Spam filter agent mode
    - aspire: Start standalone Aspire Dashboard for telemetry visualization

.PARAMETER Mode
    The run mode: 'chat' (default), 'spam', or 'aspire'

.EXAMPLE
    ./run.ps1
    Runs in interactive chat mode.

.EXAMPLE
    ./run.ps1 spam
    Runs the spam filter agent.

.EXAMPLE
    ./run.ps1 aspire
    Starts the Aspire Dashboard for telemetry visualization.
    Run './run.ps1' or './run.ps1 spam' in another terminal to send telemetry.
#>
param(
    [Parameter(Position = 0)]
    [ValidateSet('chat', 'spam', 'aspire')]
    [string]$Mode = 'chat'
)

$ErrorActionPreference = 'Stop'

switch ($Mode) {
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
        Write-Host ""
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
        Write-Host "  Dashboard URL: " -ForegroundColor White -NoNewline
        Write-Host "http://localhost:18888" -ForegroundColor Blue
        Write-Host "  OTEL Receiver: " -ForegroundColor White -NoNewline
        Write-Host "http://localhost:4317" -ForegroundColor DarkGray
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
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
