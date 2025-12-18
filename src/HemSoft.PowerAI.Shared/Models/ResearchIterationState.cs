// <copyright file="ResearchIterationState.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Models;

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

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
    /// Preserves full markdown content from each iteration with clear section demarcation.
    /// </summary>
    public string AllFindings
    {
        get
        {
            if (this.iterations.Count == 0)
            {
                return string.Empty;
            }

            // Build findings with clear section headers that preserve markdown structure
            var sb = new StringBuilder();
            foreach (var i in this.iterations)
            {
                if (sb.Length > 0)
                {
                    // Clear separator between iterations
                    sb.AppendLine()
                      .AppendLine("---")
                      .AppendLine();
                }

                // Header for this iteration's findings, preserve full content
                sb.AppendLine(CultureInfo.InvariantCulture, $"## Iteration {i.IterationNumber}: {i.Query}")
                  .AppendLine()
                  .AppendLine(i.Findings?.Trim());
            }

            return sb.ToString();
        }
    }

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
