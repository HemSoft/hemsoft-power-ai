// <copyright file="AgentHostSettings.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.AgentWorker.Configuration;

/// <summary>
/// Configuration settings for connecting to the AgentHost A2A server.
/// </summary>
internal sealed class AgentHostSettings
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "AgentHost";

    /// <summary>
    /// Gets the base URL of the AgentHost A2A server.
    /// Loaded from configuration; parsed to Uri at startup.
    /// </summary>
    public Uri? BaseUrl { get; init; }

    /// <summary>
    /// Gets the base URL, throwing if not configured.
    /// </summary>
    /// <returns>The base URL as a Uri.</returns>
    /// <exception cref="InvalidOperationException">Thrown when BaseUrl is not configured.</exception>
    public Uri GetRequiredBaseUrl() =>
        this.BaseUrl ?? throw new InvalidOperationException("AgentHost:BaseUrl is not configured.");
}
