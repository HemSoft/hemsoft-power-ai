// <copyright file="ResearchEvaluation.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Models;

using System.Collections.Immutable;
using System.Text.Json.Serialization;

/// <summary>
/// Represents the EvaluatorAgent's assessment of research findings.
/// </summary>
/// <param name="IsSatisfactory">Whether the research meets quality threshold.</param>
/// <param name="QualityScore">Quality score from 1-10.</param>
/// <param name="Gaps">Identified gaps in the research.</param>
/// <param name="FollowUpQuestions">Suggested questions for deeper research.</param>
/// <param name="RefinedQuery">Suggested refined query for next iteration, null if satisfactory.</param>
/// <param name="Reasoning">Brief explanation of the evaluation.</param>
public sealed record ResearchEvaluation(
    [property: JsonPropertyName("isSatisfactory")]
    bool IsSatisfactory,

    [property: JsonPropertyName("qualityScore")]
    int QualityScore,

    [property: JsonPropertyName("gaps")]
    ImmutableArray<string> Gaps,

    [property: JsonPropertyName("followUpQuestions")]
    ImmutableArray<string> FollowUpQuestions,

    [property: JsonPropertyName("refinedQuery")]
    string? RefinedQuery,

    [property: JsonPropertyName("reasoning")]
    string Reasoning)
{
    /// <summary>
    /// Creates a default satisfactory evaluation for when parsing fails.
    /// </summary>
    /// <returns>A satisfactory evaluation with default values.</returns>
    public static ResearchEvaluation CreateDefault() =>
        new(
            IsSatisfactory: true,
            QualityScore: 7,
            Gaps: [],
            FollowUpQuestions: [],
            RefinedQuery: null,
            Reasoning: "Evaluation parsing failed, accepting research as satisfactory.");
}
