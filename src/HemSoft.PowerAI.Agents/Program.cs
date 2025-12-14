// <copyright file="Program.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Agents;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Entry point for the Azure Functions application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static Task Main(string[] args)
    {
        _ = args; // Reserved for future use

        var host = new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureServices(static services =>
            {
                _ = services.AddApplicationInsightsTelemetryWorkerService();
                _ = services.ConfigureFunctionsApplicationInsights();
            })
            .Build();

        return host.RunAsync();
    }
}
