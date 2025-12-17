// <copyright file="IterativeResearchService.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Services;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using HemSoft.PowerAI.Common.Agents;
using HemSoft.PowerAI.Common.Models;

using Microsoft.Agents.AI;

/// <summary>
/// Orchestrates iterative research by coordinating ResearchAgent and EvaluatorAgent.
/// Continues research until quality threshold is met or max iterations reached.
/// </summary>
/// <param name="maxIterations">Maximum number of research iterations.</param>
/// <param name="qualityThreshold">Minimum quality score to accept (1-10).</param>
/// <param name="onProgress">Optional callback for progress updates.</param>
/// <param name="timeProvider">Time provider for timestamps.</param>
[ExcludeFromCodeCoverage(Justification = "Service requires OpenRouter API")]
public sealed partial class IterativeResearchService(
    int maxIterations,
    int qualityThreshold,
    Action<string>? onProgress,
    TimeProvider timeProvider)
{
    private static readonly CompositeFormat EvaluationPromptFormat = CompositeFormat.Parse("""
        ## Original Research Question
        {0}

        ## Current Query
        {1}

        ## Research Findings
        {2}

        ---
        Please evaluate these findings and respond with your JSON assessment.
        """);

    private static readonly CompositeFormat SynthesisPromptFormat = CompositeFormat.Parse("""
        ## Original Question
        {0}

        ## Research Findings (from {1} iterations)
        {2}

        ---
        Please synthesize these findings into a comprehensive, well-organized response
        that directly answers the original question. Combine insights from all iterations,
        remove redundancy, and present the most complete picture possible.
        """);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="IterativeResearchService"/> class
    /// with default settings.
    /// </summary>
    public IterativeResearchService()
        : this(maxIterations: 3, qualityThreshold: 7, onProgress: null, timeProvider: TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IterativeResearchService"/> class
    /// with a progress callback.
    /// </summary>
    /// <param name="progressCallback">Callback for progress updates.</param>
    public IterativeResearchService(Action<string> progressCallback)
        : this(maxIterations: 3, qualityThreshold: 7, onProgress: progressCallback, timeProvider: TimeProvider.System)
    {
    }

    /// <summary>
    /// Executes iterative research on the given query.
    /// </summary>
    /// <param name="query">The research query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete research state with all iterations and final synthesis.</returns>
    public async Task<ResearchIterationState> ResearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var state = new ResearchIterationState(query, timeProvider);
        var currentQuery = query;

        var researchAgent = ResearchAgent.Create();
        var evaluatorAgent = EvaluatorAgent.Create();

        this.ReportProgress("Starting iterative research: \"{0}\"", query);

        while (state.CurrentIteration < maxIterations && !cancellationToken.IsCancellationRequested)
        {
            currentQuery = await this.ExecuteIterationAsync(
                state,
                currentQuery,
                researchAgent,
                evaluatorAgent,
                cancellationToken).ConfigureAwait(false);

            if (currentQuery is null)
            {
                break;
            }
        }

        // Synthesize final result
        state.FinalSynthesis = await SynthesizeAsync(researchAgent, state, cancellationToken)
            .ConfigureAwait(false);
        state.IsComplete = true;

        this.ReportProgress("Research complete after {0} iteration(s).", state.CurrentIteration);

        return state;
    }

    private static async Task<string> RunResearchAsync(
        AIAgent agent,
        string query,
        CancellationToken cancellationToken)
    {
        var response = await agent.RunAsync(query, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return response.Text ?? "No findings returned.";
    }

    private static async Task<ResearchEvaluation> RunEvaluationAsync(
        AIAgent agent,
        string originalQuery,
        string currentQuery,
        string findings,
        CancellationToken cancellationToken)
    {
        var prompt = string.Format(
            CultureInfo.InvariantCulture,
            EvaluationPromptFormat,
            originalQuery,
            currentQuery,
            findings);

        var response = await agent.RunAsync(prompt, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return ParseEvaluation(response.Text ?? string.Empty);
    }

    private static async Task<string> SynthesizeAsync(
        AIAgent agent,
        ResearchIterationState state,
        CancellationToken cancellationToken)
    {
        if (state.Iterations.Count == 1)
        {
            return state.Iterations[0].Findings;
        }

        var prompt = string.Format(
            CultureInfo.InvariantCulture,
            SynthesisPromptFormat,
            state.OriginalQuery,
            state.Iterations.Count,
            state.AllFindings);

        var response = await agent.RunAsync(prompt, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return response.Text ?? state.AllFindings;
    }

    private static ResearchEvaluation ParseEvaluation(string response)
    {
        try
        {
            var jsonMatch = JsonBlockRegex().Match(response);
            var json = jsonMatch.Success ? jsonMatch.Groups["json"].Value : response;

            if (!json.TrimStart().StartsWith('{'))
            {
                var startIndex = json.IndexOf('{', StringComparison.Ordinal);
                var endIndex = json.LastIndexOf('}');
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    json = json[startIndex..(endIndex + 1)];
                }
            }

            return JsonSerializer.Deserialize<ResearchEvaluation>(json, JsonOptions)
                ?? ResearchEvaluation.CreateDefault();
        }
        catch (JsonException)
        {
            return ResearchEvaluation.CreateDefault();
        }
    }

    [GeneratedRegex(
        @"```(?:json)?\s*(?<json>[\s\S]*?)\s*```",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex JsonBlockRegex();

    private async Task<string?> ExecuteIterationAsync(
        ResearchIterationState state,
        string currentQuery,
        AIAgent researchAgent,
        AIAgent evaluatorAgent,
        CancellationToken cancellationToken)
    {
        // Step 1: Research
        this.ReportProgress("Iteration {0}: Researching \"{1}\"", state.CurrentIteration + 1, currentQuery);
        var findings = await RunResearchAsync(researchAgent, currentQuery, cancellationToken)
            .ConfigureAwait(false);

        // Step 2: Evaluate
        this.ReportProgress("Iteration {0}: Evaluating findings...", state.CurrentIteration + 1);
        var evaluation = await RunEvaluationAsync(
            evaluatorAgent,
            state.OriginalQuery,
            currentQuery,
            findings,
            cancellationToken).ConfigureAwait(false);

        // Step 3: Record iteration
        state.AddIteration(currentQuery, findings, evaluation);

        this.ReportProgress(
            "Iteration {0}: Score {1}/10, Satisfactory: {2}",
            state.CurrentIteration,
            evaluation.QualityScore,
            evaluation.IsSatisfactory);

        // Step 4: Check if we're done
        if (evaluation.IsSatisfactory || evaluation.QualityScore >= qualityThreshold)
        {
            this.ReportProgress("Research meets quality threshold, synthesizing results...");
            return null;
        }

        // Step 5: Prepare next iteration
        if (!string.IsNullOrWhiteSpace(evaluation.RefinedQuery))
        {
            this.ReportProgress("Refining query to: \"{0}\"", evaluation.RefinedQuery);
            return evaluation.RefinedQuery;
        }

        if (evaluation.FollowUpQuestions.Length > 0)
        {
            this.ReportProgress("Following up with: \"{0}\"", evaluation.FollowUpQuestions[0]);
            return evaluation.FollowUpQuestions[0];
        }

        this.ReportProgress("No refinement suggestions, ending research.");
        return null;
    }

    private void ReportProgress(string format, params object[] args) =>
        onProgress?.Invoke(string.Format(CultureInfo.InvariantCulture, format, args));
}
