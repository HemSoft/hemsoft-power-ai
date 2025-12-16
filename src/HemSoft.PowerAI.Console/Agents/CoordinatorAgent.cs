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
        - MailAgent: Handles email operations (read, send, search, delete, spam management)

        ## Your workflow:
        1. Analyze the incoming task to understand what's needed
        2. Delegate research tasks to ResearchAgent
        3. Delegate email tasks to MailAgent
        4. Use file tools to read/write files when needed
        5. Synthesize the results into a coherent response

        ## Guidelines:
        - For research tasks, ALWAYS use ResearchAgent
        - For email tasks, ALWAYS use MailAgent
        - Handle simple questions directly without delegation
        - Be specific when delegating tasks to agents
        - When asked to write reports/files, use ModifyFileSystem with mode='write'

        ## Tools Available:
        - ResearchAgent: Use when web research is needed
        - MailAgent: Use for all email operations (inbox, read, send, search, delete, move, spam)
        - QueryFileSystem: Read files (mode='read'), list directories (mode='list'), get file info (mode='info')
        - ModifyFileSystem: Write files (mode='write'), create folders (mode='mkdir'), delete (mode='delete')

        ## Important:
        - Provide clear, actionable summaries of all findings
        - When asked to save/file a report, use ModifyFileSystem with mode='write'
        - The 'write' mode takes content in the destination parameter
        """;

    /// <summary>
    /// Creates a new CoordinatorAgent as an AIAgent using local agents.
    /// The agents are passed as tools, enabling the agent-as-tool pattern.
    /// </summary>
    /// <param name="researchAgent">The ResearchAgent to use as a tool for research tasks.</param>
    /// <param name="mailAgent">The MailAgent to use as a tool for email tasks.</param>
    /// <returns>An AIAgent configured for coordination tasks.</returns>
    public static AIAgent Create(AIAgent researchAgent, AIAgent mailAgent)
    {
        // The agents ARE the tools - this is the key MS Agent Framework pattern
        // Use AsAIFunction() to convert each AIAgent to an AITool
        IList<AITool> tools =
        [
            researchAgent.AsAIFunction(),  // Agent-as-tool!
            mailAgent.AsAIFunction(),      // Agent-as-tool!
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
    /// <param name="mailAgent">The MailAgent to use as a tool for email tasks.</param>
    /// <returns>An AIAgent configured for coordination tasks with remote research delegation.</returns>
    public static AIAgent CreateWithRemoteAgent(AITool remoteResearchTool, AIAgent mailAgent)
    {
        IList<AITool> tools =
        [
            remoteResearchTool,           // Remote agent-as-tool via A2A!
            mailAgent.AsAIFunction(),     // Local mail agent-as-tool
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
