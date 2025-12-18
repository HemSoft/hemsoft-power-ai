// <copyright file="ResearchPlan.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Models;

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

/// <summary>
/// Represents the EvaluatorAgent's decomposition of a query into sub-tasks.
/// Created on the first iteration to structure the research effort.
/// </summary>
/// <param name="OriginalQuery">The original research query being decomposed.</param>
/// <param name="SubTasks">The ordered list of sub-tasks to complete.</param>
/// <param name="Rationale">Overall explanation of the decomposition strategy.</param>
public sealed record ResearchPlan(
    [property: JsonPropertyName("originalQuery")]
    string OriginalQuery,

    [property: JsonPropertyName("subTasks")]
    ImmutableArray<ResearchSubTask> SubTasks,

    [property: JsonPropertyName("rationale")]
    string Rationale)
{
    /// <summary>
    /// Gets a value indicating whether all sub-tasks are complete.
    /// </summary>
    public bool AllSubTasksComplete => this.SubTasks.All(st => st.IsComplete);

    /// <summary>
    /// Gets the count of completed sub-tasks.
    /// </summary>
    public int CompletedCount => this.SubTasks.Count(st => st.IsComplete);

    /// <summary>
    /// Gets all findings concatenated from completed sub-tasks.
    /// Each sub-task's findings are preserved in full with clear section demarcation.
    /// </summary>
    public string AllFindings
    {
        get
        {
            var completedTasks = this.SubTasks
                .Where(st => st.IsComplete && !string.IsNullOrWhiteSpace(st.Findings))
                .ToList();

            if (completedTasks.Count == 0)
            {
                return string.Empty;
            }

            // Build findings with clear section headers that preserve markdown structure
            var sb = new StringBuilder();
            foreach (var st in completedTasks)
            {
                if (sb.Length > 0)
                {
                    // Clear separator between sub-tasks
                    sb.AppendLine()
                      .AppendLine("---")
                      .AppendLine();
                }

                // Header for this sub-task's findings, preserve full content
                sb.AppendLine(CultureInfo.InvariantCulture, $"## Sub-task {st.Id}: {st.Query}")
                  .AppendLine()
                  .AppendLine(st.Findings?.Trim());
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets the next sub-task that is ready to execute.
    /// A sub-task is ready if it's not complete and all dependencies are complete.
    /// Returns null if none are ready.
    /// </summary>
    public ResearchSubTask? NextReadySubTask
    {
        get
        {
            foreach (var subTask in this.SubTasks)
            {
                if (subTask.IsComplete)
                {
                    continue;
                }

                var dependenciesMet = subTask.DependsOn.Length == 0 ||
                    subTask.DependsOn.All(depId =>
                        this.SubTasks.Any(st => st.Id == depId && st.IsComplete));

                if (dependenciesMet)
                {
                    return subTask;
                }
            }

            return null;
        }
    }
}
