// <copyright file="A2ASettings.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Configuration;

/// <summary>
/// Configuration settings for A2A (Agent-to-Agent) protocol endpoints.
/// </summary>
internal sealed class A2ASettings
{
    /// <summary>
    /// The configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "A2A";

    /// <summary>
    /// The default localhost URL for local development.
    /// </summary>
    public static readonly Uri LocalhostDefault = new(Localhost7071);

    private const string Localhost7071 = "http://localhost:7071/";

    /// <summary>
    /// Gets or sets the default URL for the research agent.
    /// Can be overridden via RESEARCH_AGENT_URL environment variable.
    /// </summary>
    public Uri DefaultResearchAgentUrl { get; set; } = LocalhostDefault;

    /// <summary>
    /// Gets or sets the port to use when hosting the research agent as an A2A server.
    /// Defaults to 5001.
    /// </summary>
    public int ResearchAgentHostPort { get; set; } = 5001;
}
