// <copyright file="ResearchSubTask.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Models;

using System.Collections.Immutable;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a focused sub-task within a larger research plan.
/// Created by the EvaluatorAgent to break down complex queries.
/// </summary>
/// <param name="Id">Unique identifier for ordering and dependency tracking.</param>
/// <param name="Query">The specific research query for this sub-task.</param>
/// <param name="Rationale">Why this sub-task is needed for the overall research goal.</param>
/// <param name="DependsOn">IDs of sub-tasks that must complete before this one.</param>
/// <param name="ExpectedOutcome">What information this sub-task should uncover.</param>
public sealed record ResearchSubTask(
    [property: JsonPropertyName("id")]
    int Id,

    [property: JsonPropertyName("query")]
    string Query,

    [property: JsonPropertyName("rationale")]
    string Rationale,

    [property: JsonPropertyName("dependsOn")]
    ImmutableArray<int> DependsOn,

    [property: JsonPropertyName("expectedOutcome")]
    string ExpectedOutcome)
{
    /// <summary>
    /// Gets or sets the findings from executing this sub-task.
    /// Null until the sub-task has been researched.
    /// </summary>
    [JsonPropertyName("findings")]
    public string? Findings { get; set; }

    /// <summary>
    /// Gets or sets the quality score assigned by the evaluator.
    /// Null until evaluated.
    /// </summary>
    [JsonPropertyName("qualityScore")]
    public int? QualityScore { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this sub-task is complete.
    /// </summary>
    [JsonPropertyName("isComplete")]
    public bool IsComplete { get; set; }
}
