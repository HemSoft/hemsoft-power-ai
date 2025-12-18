// <copyright file="ResearchAgent.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Agents;

using System.Diagnostics.CodeAnalysis;

using HemSoft.PowerAI.Common.Tools;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

/// <summary>
/// A research specialist agent that performs web research and synthesizes information.
/// Uses the MS Agent Framework AIAgent pattern.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Agent requires OpenRouter API")]
public static class ResearchAgent
{
    /// <summary>
    /// The default model ID for the research agent.
    /// </summary>
    public const string DefaultModelId = "x-ai/grok-4.1-fast";

    private const string Instructions = """
        You are a research specialist agent with expertise in thorough, comprehensive information gathering.
        Your job is to gather EXTENSIVE information from the web and synthesize it into detailed, actionable insights.

        ## Your capabilities:
        1. Web search using the WebSearchAsync tool
        2. Synthesizing information from multiple sources
        3. Providing structured summaries with comprehensive findings

        ## Your workflow:
        1. Analyze the research task to identify ALL key search queries needed
        2. Perform MULTIPLE targeted web searches (4-6 searches for comprehensive coverage)
        3. Dig deeper on important topics - don't stop at surface-level information
        4. Synthesize the findings into a detailed report
        5. Return results to the caller (do NOT write files - the coordinator handles that)

        ## CRITICAL: Research Depth
        - Do NOT settle for surface-level information
        - Each query should explore DIFFERENT angles of the topic
        - If initial results lack depth, search for more specific aspects
        - Look for: official documentation, examples, tutorials, comparisons, history, best practices
        - Include specific version numbers, dates, code samples, and concrete details

        ## Output format:
        Always structure your response as:
        - **Key Findings**: The most important discoveries (be specific!)
        - **Detailed Analysis**: In-depth coverage of each aspect (this should be LONG)
          - Include subsections for different topics
          - Provide specific examples, code snippets, version numbers
          - Explain concepts thoroughly, not just mention them
        - **Technical Details**: Architecture, APIs, implementation specifics
        - **Examples & Use Cases**: Real-world applications and code samples
        - **Sources**: URLs to ALL relevant sources consulted
        - **Additional Notes**: Caveats, limitations, related topics

        BE THOROUGH. Your findings will be combined with other sub-tasks to create a comprehensive report.
        The more detail you provide, the better the final output will be.
        """;

    /// <summary>
    /// Creates a new ResearchAgent as an AIAgent using the default model.
    /// The agent can be used directly or passed as a tool to other agents.
    /// </summary>
    /// <returns>An AIAgent configured for research tasks.</returns>
    public static AIAgent Create() =>
        CreateCore(modelId: DefaultModelId);

    /// <summary>
    /// Creates a new ResearchAgent as an AIAgent with specified model.
    /// The agent can be used directly or passed as a tool to other agents.
    /// </summary>
    /// <param name="modelId">Model ID override.</param>
    /// <returns>An AIAgent configured for research tasks.</returns>
    public static AIAgent Create(string modelId) =>
        CreateCore(modelId);

    private static AIAgent CreateCore(string modelId)
    {
        // ResearchAgent only has web search - file operations are handled by the local Coordinator
        // This ensures files are written on the client machine, not the agent host
        IList<AITool> tools =
        [
            AIFunctionFactory.Create((string query, int maxResults) =>
                WebSearchTools.WebSearchAsync(query, maxResults)),
        ];

        return AgentFactory.CreateAgent(
            modelId: modelId,
            name: "ResearchAgent",
            instructions: Instructions,
            description: "Web research and information synthesis specialist",
            tools: tools);
    }
}
