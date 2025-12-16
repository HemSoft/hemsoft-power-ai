// <copyright file="A2AAgentHost.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Hosting;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using A2A;
using A2A.AspNetCore;

using Microsoft.Agents.AI;

/// <summary>
/// Hosts an AIAgent as an A2A server using the MapA2A pattern from MS Agent Framework.
/// Enables remote agent-to-agent communication via A2A protocol.
/// </summary>
/// <param name="agent">The AIAgent to host.</param>
/// <param name="agentCard">The AgentCard describing the agent's capabilities.</param>
/// <param name="port">The port to host the agent on.</param>
/// <param name="routePath">The route path for the A2A endpoint (default: "/").</param>
[ExcludeFromCodeCoverage(Justification = "A2A host requires ASP.NET Core integration testing with real HTTP endpoints")]
internal sealed class A2AAgentHost(
    AIAgent agent,
    AgentCard agentCard,
    int port,
    string routePath = "/") : IAsyncDisposable
{
    private WebApplication? app;

    /// <summary>
    /// Starts the A2A server asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the server startup.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (this.app != null)
        {
            await this.app.DisposeAsync().ConfigureAwait(false);
        }

        var builder = WebApplication.CreateSlimBuilder();
        _ = builder.WebHost.UseUrls(string.Format(CultureInfo.InvariantCulture, "http://localhost:{0}", port));

        this.app = builder.Build();

        // Create task manager with agent handlers using object initializer pattern
        var taskManager = new TaskManager
        {
            OnMessageReceived = async (messageSendParams, ct) =>
            {
                // Extract the text from the incoming message
                var userText = string.Join(
                    '\n',
                    messageSendParams.Message.Parts
                        .OfType<TextPart>()
                        .Select(p => p.Text));

                // Run the AIAgent with the user's message
                var agentResponse = await agent.RunAsync(userText, cancellationToken: ct).ConfigureAwait(false);

                // Return the response as an AgentMessage
                return new AgentMessage
                {
                    Role = MessageRole.Agent,
                    MessageId = Guid.NewGuid().ToString(),
                    ContextId = messageSendParams.Message.ContextId,
                    Parts = [new TextPart { Text = agentResponse.Text ?? "No response generated." }],
                };
            },
            OnAgentCardQuery = (agentUrl, _) =>
            {
                // Update the URL in the card to match the actual hosting URL
                var card = agentCard;
                card.Url = agentUrl;
                return Task.FromResult(card);
            },
        };

        // Map the A2A endpoints using the framework pattern
        _ = this.app.MapA2A(taskManager, routePath);
        _ = this.app.MapWellKnownAgentCard(taskManager, routePath);

        await this.app.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for the server to shut down.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the shutdown wait.</returns>
    public async Task WaitForShutdownAsync(CancellationToken cancellationToken = default)
    {
        if (this.app != null)
        {
            await this.app.WaitForShutdownAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Stops the A2A server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the server stop.</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (this.app != null)
        {
            await this.app.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (this.app != null)
        {
            await this.app.DisposeAsync().ConfigureAwait(false);
            this.app = null;
        }
    }
}
