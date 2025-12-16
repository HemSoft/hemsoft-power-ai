// <copyright file="Program.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.AgentHost;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using HemSoft.PowerAI.AgentHost.Configuration;
using HemSoft.PowerAI.AgentHost.Extensions;

/// <summary>
/// Main entry point for the A2A Agent Host.
/// Hosts the ResearchAgent as an A2A-compliant endpoint.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Application entry point with ASP.NET Core hosting")]
internal static class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A task representing the async operation.</returns>
    public static Task Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        var options = new AgentHostOptions
        {
            Port = int.Parse(builder.Configuration["PORT"] ?? "5001", CultureInfo.InvariantCulture),
            ModelId = builder.Configuration["ModelId"],
        };

        builder.Services.AddResearchAgent(options);

        var app = builder.Build();

        app.MapAgentEndpoints("ResearchAgent");
        app.Urls.Add(string.Format(CultureInfo.InvariantCulture, "http://localhost:{0}", options.Port));

        return app.RunAsync();
    }
}
