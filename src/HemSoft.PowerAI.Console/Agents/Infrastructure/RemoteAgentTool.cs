// <copyright file="RemoteAgentTool.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Agents.Infrastructure;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.AI;

/// <summary>
/// Provides tool methods for invoking remote A2A agents.
/// Enables remote agents to be used as tools in the agent-as-tool pattern.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Remote agent tool requires real network connection for testing")]
internal static class RemoteAgentTool
{
    private static A2AAgentClient? currentRemoteAgent;

    /// <summary>
    /// Sets the remote agent client to be used by the tool.
    /// </summary>
    /// <param name="client">The A2A agent client.</param>
    public static void SetRemoteAgent(A2AAgentClient client) => currentRemoteAgent = client;

    /// <summary>
    /// Clears the current remote agent.
    /// </summary>
    public static void ClearRemoteAgent() => currentRemoteAgent = null;

    /// <summary>
    /// Invokes the remote ResearchAgent via A2A protocol.
    /// Use this tool to delegate research tasks to the remote research agent.
    /// </summary>
    /// <param name="task">The research task or question to send to the ResearchAgent.</param>
    /// <returns>The research results from the remote agent.</returns>
    [Description("Web research and information synthesis specialist")]
    public static async Task<string> ResearchAgent(
        [Description("The research task or question to investigate")] string task) =>
        currentRemoteAgent is null
            ? "Error: Remote ResearchAgent is not connected."
            : await currentRemoteAgent.SendMessageAsync(task).ConfigureAwait(false);

    /// <summary>
    /// Creates an AITool for the remote research agent.
    /// </summary>
    /// <returns>An AITool that invokes the remote research agent.</returns>
    public static AITool CreateTool() => AIFunctionFactory.Create(ResearchAgent);
}
