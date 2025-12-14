// <copyright file="CoordinatorAgent.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Agents;

using System.Diagnostics.CodeAnalysis;

using HemSoft.PowerAI.Console.Agents.Infrastructure;
using HemSoft.PowerAI.Console.Tools;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

/// <summary>
/// A coordinator agent that orchestrates tasks by delegating to specialized agents.
/// Uses the MS Agent Framework agent-as-tool pattern.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Agent requires OpenRouter API")]
internal static class CoordinatorAgent
{
    private const string ModelId = "x-ai/grok-4.1-fast";

    private const string Instructions = """
        You are a coordinator agent that orchestrates complex tasks by delegating to specialized agents.

        ## Available Agents (use as tools):
        - ResearchAgent: Performs web research and synthesizes information

        ## Your workflow:
        1. Analyze the incoming task to understand what's needed
        2. Delegate research tasks to ResearchAgent
        3. Use file tools to read/write files when needed
        4. Synthesize the results into a coherent response

        ## Guidelines:
        - For research tasks, ALWAYS use ResearchAgent
        - Handle simple questions directly without delegation
        - Be specific when delegating tasks to agents
        - When asked to write reports/files, use ModifyFileSystem with mode='write'

        ## Tools Available:
        - ResearchAgent: Use when web research is needed
        - QueryFileSystem: Read files (mode='read'), list directories (mode='list'), get file info (mode='info')
        - ModifyFileSystem: Write files (mode='write'), create folders (mode='mkdir'), delete (mode='delete')

        ## Important:
        - Provide clear, actionable summaries of all findings
        - When asked to save/file a report, use ModifyFileSystem with mode='write'
        - The 'write' mode takes content in the destination parameter
        """;

    /// <summary>
    /// Creates a new CoordinatorAgent as an AIAgent using a local ResearchAgent.
    /// The ResearchAgent is passed as a tool, enabling the agent-as-tool pattern.
    /// </summary>
    /// <param name="researchAgent">The ResearchAgent to use as a tool for delegation.</param>
    /// <returns>An AIAgent configured for coordination tasks.</returns>
    public static AIAgent Create(AIAgent researchAgent)
    {
        // The research agent IS the tool - this is the key MS Agent Framework pattern
        // Use AsAIFunction() to convert the AIAgent to an AITool
        IList<AITool> tools =
        [
            researchAgent.AsAIFunction(),  // Agent-as-tool!
            AIFunctionFactory.Create(FileTools.QueryFileSystem),
            AIFunctionFactory.Create(FileTools.ModifyFileSystem),
        ];

        return AgentFactory.CreateAgent(
            modelId: ModelId,
            name: "CoordinatorAgent",
            instructions: Instructions,
            description: "Orchestrates tasks by delegating to specialized agents",
            tools: tools);
    }

    /// <summary>
    /// Creates a new CoordinatorAgent that delegates to a remote ResearchAgent via A2A protocol.
    /// </summary>
    /// <param name="remoteResearchTool">The AITool wrapping the remote ResearchAgent.</param>
    /// <returns>An AIAgent configured for coordination tasks with remote delegation.</returns>
    public static AIAgent CreateWithRemoteAgent(AITool remoteResearchTool)
    {
        IList<AITool> tools =
        [
            remoteResearchTool,  // Remote agent-as-tool via A2A!
            AIFunctionFactory.Create(FileTools.QueryFileSystem),
            AIFunctionFactory.Create(FileTools.ModifyFileSystem),
        ];

        return AgentFactory.CreateAgent(
            modelId: ModelId,
            name: "CoordinatorAgent",
            instructions: Instructions,
            description: "Orchestrates tasks by delegating to specialized agents (remote)",
            tools: tools);
    }
}
