// <copyright file="ResearchAgentTaskHandler.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.AgentHost.Handlers;

using A2A;

using HemSoft.PowerAI.AgentHost.Abstractions;

using Microsoft.Agents.AI;

/// <summary>
/// Handles A2A task operations for the ResearchAgent.
/// </summary>
/// <param name="agent">The AI agent instance.</param>
/// <param name="agentCard">The agent card describing capabilities.</param>
internal sealed class ResearchAgentTaskHandler(AIAgent agent, AgentCard agentCard) : IA2ATaskHandler
{
    /// <inheritdoc/>
    public Task<AgentMessage> HandleMessageAsync(MessageSendParams messageSendParams, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messageSendParams);
        return this.HandleMessageCoreAsync(messageSendParams, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<AgentCard> GetAgentCardAsync(string agentUrl, CancellationToken cancellationToken)
    {
        var card = agentCard;
        card.Url = agentUrl;
        return Task.FromResult(card);
    }

    private async Task<AgentMessage> HandleMessageCoreAsync(MessageSendParams messageSendParams, CancellationToken cancellationToken)
    {
        var userText = string.Join(
            '\n',
            messageSendParams.Message.Parts
                .OfType<TextPart>()
                .Select(p => p.Text));

        var agentResponse = await agent.RunAsync(userText, cancellationToken: cancellationToken).ConfigureAwait(false);

        return new AgentMessage
        {
            Role = MessageRole.Agent,
            MessageId = Guid.NewGuid().ToString(),
            ContextId = messageSendParams.Message.ContextId,
            Parts = [new TextPart { Text = agentResponse.Text ?? "No response generated." }],
        };
    }
}
