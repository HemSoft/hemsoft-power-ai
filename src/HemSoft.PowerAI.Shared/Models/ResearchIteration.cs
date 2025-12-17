// <copyright file="ResearchIteration.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Models;

/// <summary>
/// Represents a single iteration of the research process.
/// </summary>
/// <param name="IterationNumber">The iteration number (1-based).</param>
/// <param name="Query">The search query used for this iteration.</param>
/// <param name="Findings">The research findings from this iteration.</param>
/// <param name="Evaluation">The evaluator's assessment of these findings.</param>
/// <param name="Timestamp">When this iteration completed.</param>
public sealed record ResearchIteration(
    int IterationNumber,
    string Query,
    string Findings,
    ResearchEvaluation Evaluation,
    DateTimeOffset Timestamp);
