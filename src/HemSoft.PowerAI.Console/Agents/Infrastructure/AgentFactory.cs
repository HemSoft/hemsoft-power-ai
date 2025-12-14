// <copyright file="AgentFactory.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Agents.Infrastructure;

using System.ClientModel;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;

/// <summary>
/// Factory for creating AI agents using the Microsoft Agent Framework.
/// Provides standardized creation of IChatClient and AIAgent instances.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Factory requires OpenRouter API")]
internal static class AgentFactory
{
    private const string OpenRouterBaseUrlEnvVar = "OPENROUTER_BASE_URL";
    private const string ApiKeyEnvVar = "OPENROUTER_API_KEY";

    /// <summary>
    /// Creates an IChatClient configured for OpenRouter with function invocation support.
    /// </summary>
    /// <param name="modelId">The model ID to use (e.g., "x-ai/grok-4.1-fast").</param>
    /// <returns>A configured IChatClient.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required environment variables are missing.</exception>
    public static IChatClient CreateChatClient(string modelId)
    {
        var baseUrl = Environment.GetEnvironmentVariable(OpenRouterBaseUrlEnvVar)
            ?? throw new InvalidOperationException($"Missing {OpenRouterBaseUrlEnvVar} environment variable.");

        var apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvVar)
            ?? throw new InvalidOperationException($"Missing {ApiKeyEnvVar} environment variable.");

#pragma warning disable IDISP004 // Don't ignore created IDisposable - lifecycle owned by returned IChatClient wrapper
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(baseUrl) });

        return openAiClient
            .GetChatClient(modelId)
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
#pragma warning restore IDISP004
    }

    /// <summary>
    /// Creates an AIAgent using the ChatClientAgent pattern from MS Agent Framework.
    /// </summary>
    /// <param name="modelId">The model ID to use.</param>
    /// <param name="name">The agent's name.</param>
    /// <param name="instructions">The system instructions for the agent.</param>
    /// <param name="description">Optional description of the agent's capabilities.</param>
    /// <param name="tools">Optional tools available to the agent.</param>
    /// <returns>A configured AIAgent.</returns>
    public static AIAgent CreateAgent(
        string modelId,
        string name,
        string instructions,
        string? description = null,
        IList<AITool>? tools = null)
    {
#pragma warning disable CA2000, IDISP001 // Dispose created - lifecycle owned by returned ChatClientAgent
        var chatClient = CreateChatClient(modelId);
#pragma warning restore CA2000, IDISP001

        return new ChatClientAgent(
            chatClient,
            instructions: instructions,
            name: name,
            description: description,
            tools: tools);
    }
}
