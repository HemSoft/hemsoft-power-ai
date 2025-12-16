// <copyright file="ServiceCollectionExtensions.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.AgentHost.Extensions;

using System.Globalization;

using A2A;

using HemSoft.PowerAI.AgentHost.Abstractions;
using HemSoft.PowerAI.AgentHost.Configuration;
using HemSoft.PowerAI.AgentHost.Handlers;
using HemSoft.PowerAI.Common.Agents;

using Microsoft.Agents.AI;

/// <summary>
/// Extension methods for configuring agent host services.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the ResearchAgent and its task handler to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The agent host options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddResearchAgent(this IServiceCollection services, AgentHostOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton<AIAgent>(_ =>
            string.IsNullOrEmpty(options.ModelId)
                ? ResearchAgent.Create()
                : ResearchAgent.Create(options.ModelId));

        services.AddSingleton(_ =>
        {
            var baseUrl = new Uri(string.Format(CultureInfo.InvariantCulture, "http://localhost:{0}", options.Port));
            return AgentCards.CreateResearchAgentCard(baseUrl);
        });

        services.AddSingleton<IA2ATaskHandler, ResearchAgentTaskHandler>();

        return services;
    }

    /// <summary>
    /// Creates a TaskManager configured with the registered task handler.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>A configured TaskManager.</returns>
    public static TaskManager CreateTaskManager(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var handler = serviceProvider.GetRequiredService<IA2ATaskHandler>();

        return new TaskManager
        {
            OnMessageReceived = async (messageSendParams, ct) =>
                await handler.HandleMessageAsync(messageSendParams, ct).ConfigureAwait(false),
            OnAgentCardQuery = handler.GetAgentCardAsync,
        };
    }
}
