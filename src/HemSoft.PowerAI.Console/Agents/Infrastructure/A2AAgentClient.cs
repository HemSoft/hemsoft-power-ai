// <copyright file="A2AAgentClient.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Agents.Infrastructure;

using System.Diagnostics.CodeAnalysis;

using A2A;

/// <summary>
/// Client for connecting to remote A2A agents.
/// Wraps an A2A client to provide a simple interface for agent communication.
/// </summary>
/// <param name="client">The underlying A2A client for communication.</param>
/// <param name="agentCard">The agent card describing the remote agent's capabilities.</param>
[ExcludeFromCodeCoverage(Justification = "A2A client requires real network connection to A2A server for testing")]
internal sealed class A2AAgentClient(A2AClient client, AgentCard agentCard)
{
    /// <summary>
    /// Gets the agent card describing the remote agent's capabilities.
    /// </summary>
    public AgentCard AgentCard { get; } = agentCard;

    /// <summary>
    /// Gets the name of the connected agent.
    /// </summary>
    public string Name => this.AgentCard.Name;

    /// <summary>
    /// Gets the description of the connected agent.
    /// </summary>
    public string Description => this.AgentCard.Description;

    /// <summary>
    /// Connects to a remote A2A agent at the specified URL.
    /// </summary>
    /// <param name="agentUrl">The URL of the remote A2A agent.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An A2AAgentClient connected to the remote agent.</returns>
    public static async System.Threading.Tasks.Task<A2AAgentClient> ConnectAsync(
        Uri agentUrl,
        CancellationToken cancellationToken = default)
    {
        // Resolve the agent card from the remote URL
        var cardResolver = new A2ACardResolver(agentUrl);
        var resolvedCard = await cardResolver.GetAgentCardAsync(cancellationToken).ConfigureAwait(false);

        // Create the client connected to the agent's URL
        var resolvedClient = new A2AClient(new Uri(resolvedCard.Url));

        return new A2AAgentClient(resolvedClient, resolvedCard);
    }

    /// <summary>
    /// Sends a message to the remote agent and returns the response.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent's response text.</returns>
    public async System.Threading.Tasks.Task<string> SendMessageAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        var response = await client.SendMessageAsync(
            new MessageSendParams
            {
                Message = new AgentMessage
                {
                    Role = MessageRole.User,
                    MessageId = Guid.NewGuid().ToString(),
                    Parts = [new TextPart { Text = message }],
                },
            },
            cancellationToken).ConfigureAwait(false);

        // Extract text from the response
        return ExtractResponseText(response);
    }

    private static string ExtractResponseText(A2AResponse response)
    {
        // Handle different response types
        if (response is AgentMessage agentMessage)
        {
            return string.Join(
                '\n',
                agentMessage.Parts
                    .OfType<TextPart>()
                    .Select(p => p.Text));
        }

        if (response is AgentTask task && task.Artifacts != null)
        {
            var texts = task.Artifacts
                .SelectMany(a => a.Parts)
                .OfType<TextPart>()
                .Select(p => p.Text);
            return string.Join('\n', texts);
        }

        return "No text response received.";
    }
}
