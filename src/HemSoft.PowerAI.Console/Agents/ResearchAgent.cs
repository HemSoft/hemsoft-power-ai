// <copyright file="ResearchAgent.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Agents;

using System.Diagnostics.CodeAnalysis;

using HemSoft.PowerAI.Console.Agents.Infrastructure;
using HemSoft.PowerAI.Console.Tools;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

/// <summary>
/// A research specialist agent that performs web research and synthesizes information.
/// Uses the MS Agent Framework AIAgent pattern.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Agent requires OpenRouter API")]
internal static class ResearchAgent
{
    private const string ModelId = "x-ai/grok-4.1-fast";

    private const string Instructions = """
        You are a research specialist agent. Your job is to gather information from the web
        and synthesize it into clear, actionable insights.

        ## Your capabilities:
        1. Web search using the WebSearchAsync tool
        2. Synthesizing information from multiple sources
        3. Providing structured summaries with key findings

        ## Your workflow:
        1. Analyze the research task to identify key search queries
        2. Perform targeted web searches (usually 2-3 searches for comprehensive coverage)
        3. Synthesize the findings into a clear summary
        4. Include relevant sources and citations

        ## Output format:
        Always structure your response as:
        - **Key Findings**: The most important discoveries
        - **Details**: Supporting information and context
        - **Sources**: URLs to the most relevant sources
        - **Recommendations**: If applicable, suggest next steps or related topics

        Be thorough but concise. Focus on facts and actionable information.
        """;

    /// <summary>
    /// Creates a new ResearchAgent as an AIAgent.
    /// The agent can be used directly or passed as a tool to other agents.
    /// </summary>
    /// <returns>An AIAgent configured for research tasks.</returns>
    public static AIAgent Create()
    {
        IList<AITool> tools =
        [
            AIFunctionFactory.Create(WebSearchTools.WebSearchAsync),
        ];

        return AgentFactory.CreateAgent(
            modelId: ModelId,
            name: "ResearchAgent",
            instructions: Instructions,
            description: "Web research and information synthesis specialist",
            tools: tools);
    }
}
