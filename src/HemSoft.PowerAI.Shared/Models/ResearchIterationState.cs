// <copyright file="ResearchIterationState.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Models;

using System.Collections.ObjectModel;
using System.Globalization;

/// <summary>
/// Represents the complete state of an iterative research session.
/// Holds all iterations and the final synthesis.
/// </summary>
/// <param name="originalQuery">The original research query.</param>
/// <param name="timeProvider">Time provider for timestamps.</param>
public sealed class ResearchIterationState(string originalQuery, TimeProvider timeProvider)
{
    private readonly List<ResearchIteration> iterations = [];

    /// <summary>
    /// Gets the original research query.
    /// </summary>
    public string OriginalQuery { get; } = originalQuery;

    /// <summary>
    /// Gets the list of research iterations.
    /// </summary>
    public ReadOnlyCollection<ResearchIteration> Iterations => this.iterations.AsReadOnly();

    /// <summary>
    /// Gets or sets a value indicating whether the research is complete.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Gets or sets the final synthesized result from all iterations.
    /// </summary>
    public string? FinalSynthesis { get; set; }

    /// <summary>
    /// Gets the current iteration number.
    /// </summary>
    public int CurrentIteration => this.iterations.Count;

    /// <summary>
    /// Gets the latest evaluation, or null if no iterations yet.
    /// </summary>
    public ResearchEvaluation? LatestEvaluation =>
        this.iterations.Count > 0 ? this.iterations[^1].Evaluation : null;

    /// <summary>
    /// Gets all findings concatenated from all iterations.
    /// </summary>
    public string AllFindings =>
        string.Join("\n\n---\n\n", this.iterations.Select(i =>
            string.Format(CultureInfo.InvariantCulture, "## Iteration {0}: {1}\n\n{2}", i.IterationNumber, i.Query, i.Findings)));

    /// <summary>
    /// Adds a new iteration to the state.
    /// </summary>
    /// <param name="query">The query used.</param>
    /// <param name="findings">The findings from this iteration.</param>
    /// <param name="evaluation">The evaluation of the findings.</param>
    public void AddIteration(string query, string findings, ResearchEvaluation evaluation)
    {
        var iteration = new ResearchIteration(
            IterationNumber: this.iterations.Count + 1,
            Query: query,
            Findings: findings,
            Evaluation: evaluation,
            Timestamp: timeProvider.GetUtcNow());

        this.iterations.Add(iteration);
    }
}
