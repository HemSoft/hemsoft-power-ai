#!/usr/bin/env pwsh
Push-Location $PSScriptRoot/src/AgentDemo.Console
try {
    dotnet run
}
finally {
    Pop-Location
}
