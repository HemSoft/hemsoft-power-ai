// <copyright file="EvaluatorAgent.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Agents;

using System.Diagnostics.CodeAnalysis;

using Microsoft.Agents.AI;

/// <summary>
/// An evaluator agent that judges research quality and identifies gaps.
/// Pure reasoning agent with no tools - evaluates findings and suggests refinements.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Agent requires OpenRouter API")]
public static class EvaluatorAgent
{
    /// <summary>
    /// The default model ID for the evaluator agent.
    /// Uses a fast model since evaluation is reasoning-only.
    /// </summary>
    public const string DefaultModelId = "x-ai/grok-4.1-fast";

    private const string Instructions = """
        You are a research quality evaluator. Your job is to critically assess research findings
        and determine if they adequately answer the original question.

        ## Your evaluation criteria:
        1. **Completeness** - Does the research cover all aspects of the question?
        2. **Depth** - Is the information detailed enough to be actionable?
        3. **Accuracy** - Are sources cited? Is information current?
        4. **Relevance** - Does the research directly address the question?

        ## Your workflow:
        1. Read the original research query carefully
        2. Analyze the findings against the query
        3. Identify specific gaps or missing information
        4. Suggest follow-up questions that would improve the research
        5. Provide a quality score and recommendation

        ## Output format (ALWAYS use this exact JSON structure):
        ```json
        {
            "isSatisfactory": true/false,
            "qualityScore": 1-10,
            "gaps": ["gap 1", "gap 2"],
            "followUpQuestions": ["question 1", "question 2"],
            "refinedQuery": "optional - a better search query if not satisfactory",
            "reasoning": "Brief explanation of your evaluation"
        }
        ```

        ## Guidelines:
        - Be constructively critical - aim for quality, not perfection
        - Score 7+ means satisfactory for most practical purposes
        - If satisfactory, refinedQuery should be null
        - Limit gaps and followUpQuestions to 3 items max (most important ones)
        - Consider diminishing returns - sometimes good enough IS good enough
        """;

    /// <summary>
    /// Creates a new EvaluatorAgent as an AIAgent using the default model.
    /// </summary>
    /// <returns>An AIAgent configured for research evaluation.</returns>
    public static AIAgent Create() =>
        CreateCore(modelId: DefaultModelId);

    /// <summary>
    /// Creates a new EvaluatorAgent as an AIAgent with specified model.
    /// </summary>
    /// <param name="modelId">Model ID override.</param>
    /// <returns>An AIAgent configured for research evaluation.</returns>
    public static AIAgent Create(string modelId) =>
        CreateCore(modelId);

    private static AIAgent CreateCore(string modelId) =>
        AgentFactory.CreateAgent(
            modelId: modelId,
            name: "EvaluatorAgent",
            instructions: Instructions,
            description: "Research quality evaluator that identifies gaps and suggests refinements");
}
