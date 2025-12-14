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
        You are a research specialist agent. Your job is to gather information from the web
        and synthesize it into clear, actionable insights. You can also save your findings to files.

        ## Your capabilities:
        1. Web search using the WebSearchAsync tool
        2. Synthesizing information from multiple sources
        3. Providing structured summaries with key findings
        4. Reading and writing files using QueryFileSystem and ModifyFileSystem tools

        ## Your workflow:
        1. Analyze the research task to identify key search queries
        2. Perform targeted web searches (usually 2-3 searches for comprehensive coverage)
        3. Synthesize the findings into a clear summary
        4. If requested, save the report to a file using ModifyFileSystem with mode 'write'
        5. Include relevant sources and citations

        ## File operations:
        - To write a file: ModifyFileSystem(mode: "write", path: "C:\path\file.md", destination: "content here")
        - To read a file: QueryFileSystem(mode: "read", path: "C:\path\file.md")
        - To list files: QueryFileSystem(mode: "list", path: "C:\path")

        ## Output format:
        Always structure your response as:
        - **Key Findings**: The most important discoveries
        - **Details**: Supporting information and context
        - **Sources**: URLs to the most relevant sources
        - **Recommendations**: If applicable, suggest next steps or related topics

        Be thorough but concise. Focus on facts and actionable information.
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
        IList<AITool> tools =
        [
            AIFunctionFactory.Create((string query, int maxResults) =>
                WebSearchTools.WebSearchAsync(query, maxResults)),
            AIFunctionFactory.Create(FileTools.QueryFileSystem),
            AIFunctionFactory.Create((string mode, string path, string destination) =>
                FileTools.ModifyFileSystem(mode, path, destination)),
        ];

        return AgentFactory.CreateAgent(
            modelId: modelId,
            name: "ResearchAgent",
            instructions: Instructions,
            description: "Web research and information synthesis specialist",
            tools: tools);
    }
}
