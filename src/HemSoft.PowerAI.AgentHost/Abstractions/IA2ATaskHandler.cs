// <copyright file="IA2ATaskHandler.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.AgentHost.Abstractions;

using A2A;

/// <summary>
/// Defines a handler for A2A task operations.
/// </summary>
internal interface IA2ATaskHandler
{
    /// <summary>
    /// Handles an incoming A2A message and returns the agent's response.
    /// </summary>
    /// <param name="messageSendParams">The incoming message parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent's response message.</returns>
    Task<AgentMessage> HandleMessageAsync(MessageSendParams messageSendParams, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the agent card for the specified URL.
    /// </summary>
    /// <param name="agentUrl">The URL where the agent is hosted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent card.</returns>
    Task<AgentCard> GetAgentCardAsync(string agentUrl, CancellationToken cancellationToken);
}
