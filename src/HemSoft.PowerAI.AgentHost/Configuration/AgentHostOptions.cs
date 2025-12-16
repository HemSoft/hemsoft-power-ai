// <copyright file="AgentHostOptions.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.AgentHost.Configuration;

/// <summary>
/// Configuration options for the A2A Agent Host.
/// </summary>
internal sealed class AgentHostOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "AgentHost";

    /// <summary>
    /// Gets or sets the port to host the agent on.
    /// </summary>
    public int Port { get; set; } = 5001;

    /// <summary>
    /// Gets or sets the model ID to use for the agent.
    /// </summary>
    public string? ModelId { get; set; }
}
