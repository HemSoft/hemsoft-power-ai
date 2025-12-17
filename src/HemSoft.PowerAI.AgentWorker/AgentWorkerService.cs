// <copyright file="AgentWorkerService.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.AgentWorker;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using A2A;

using HemSoft.PowerAI.AgentWorker.Configuration;
using HemSoft.PowerAI.Common.Agents;
using HemSoft.PowerAI.Common.Models;
using HemSoft.PowerAI.Common.Services;

using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;

using AgentTaskStatus = HemSoft.PowerAI.Common.Models.AgentTaskStatus;

/// <summary>
/// Background service that processes agent tasks from the Redis pub/sub channel.
/// Subscribes to the agents:tasks channel and delegates execution to AgentHost via A2A.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Background service requires Redis infrastructure for integration testing.")]
internal sealed partial class AgentWorkerService : BackgroundService
{
    private const int MaxIterations = 5;
    private const int QualityThreshold = 7;

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

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IAgentTaskBroker broker;
    private readonly ILogger<AgentWorkerService> logger;
    private readonly TimeProvider timeProvider;
    private readonly Uri agentHostUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentWorkerService"/> class.
    /// </summary>
    /// <param name="broker">The task broker for receiving and publishing tasks.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="agentHostSettings">AgentHost configuration settings.</param>
    public AgentWorkerService(
        IAgentTaskBroker broker,
        ILogger<AgentWorkerService> logger,
        IOptions<AgentHostSettings> agentHostSettings)
        : this(broker, logger, agentHostSettings, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentWorkerService"/> class.
    /// </summary>
    /// <param name="broker">The task broker for receiving and publishing tasks.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="agentHostSettings">AgentHost configuration settings.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    public AgentWorkerService(
        IAgentTaskBroker broker,
        ILogger<AgentWorkerService> logger,
        IOptions<AgentHostSettings> agentHostSettings,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(broker);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(agentHostSettings);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.broker = broker;
        this.logger = logger;
        this.timeProvider = timeProvider;
        this.agentHostUrl = agentHostSettings.Value.GetRequiredBaseUrl();
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogServiceStarting(this.logger);

        try
        {
            await this.broker.SubscribeToTasksAsync(
                this.ProcessTaskAsync,
                stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogServiceStopping(this.logger);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "AgentWorkerService starting. Subscribing to task queue...")]
    private static partial void LogServiceStarting(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "AgentWorkerService stopping due to cancellation.")]
    private static partial void LogServiceStopping(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing task {TaskId} of type {AgentType}")]
    private static partial void LogProcessingTask(ILogger logger, string taskId, string agentType);

    [LoggerMessage(Level = LogLevel.Information, Message = "╔══════════════════════════════════════════════════════════════╗")]
    private static partial void LogBannerTop(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "║  AGENT: {AgentName,-52} ║")]
    private static partial void LogBannerAgent(ILogger logger, string agentName);

    [LoggerMessage(Level = LogLevel.Information, Message = "║  Task:  {TaskId,-52} ║")]
    private static partial void LogBannerTask(ILogger logger, string taskId);

    [LoggerMessage(Level = LogLevel.Information, Message = "╚══════════════════════════════════════════════════════════════╝")]
    private static partial void LogBannerBottom(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Task {TaskId} completed successfully")]
    private static partial void LogTaskCompleted(ILogger logger, string taskId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Task {TaskId} was cancelled")]
    private static partial void LogTaskCancelled(ILogger logger, string taskId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Task {TaskId} failed with error: {ErrorMessage}")]
    private static partial void LogTaskFailed(ILogger logger, Exception ex, string taskId, string errorMessage);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Calling AgentHost at {Url} for prompt: {Prompt}")]
    private static partial void LogCallingAgentHost(ILogger logger, Uri url, string prompt);

    [LoggerMessage(Level = LogLevel.Information, Message = "AgentHost resolved agent: {AgentName}")]
    private static partial void LogAgentResolved(ILogger logger, string agentName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Iteration {IterationNum}: Researching \"{Query}\"")]
    private static partial void LogIterationResearching(ILogger logger, int iterationNum, string query);

    [LoggerMessage(Level = LogLevel.Information, Message = "Iteration {IterationNum}: Evaluating findings")]
    private static partial void LogIterationEvaluating(ILogger logger, int iterationNum);

    [LoggerMessage(Level = LogLevel.Information, Message = "Iteration {IterationNum}: Score {Score}/10, Satisfactory: {IsSatisfactory}")]
    private static partial void LogIterationComplete(ILogger logger, int iterationNum, int score, bool isSatisfactory);

    [GeneratedRegex(
        @"```(?:json)?\s*(?<json>[\s\S]*?)\s*```",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex JsonBlockRegex();

    private static string GetNextQuery(ResearchEvaluation evaluation, string fallbackQuery)
    {
        var refinedQuery = evaluation.RefinedQuery;
        if (!string.IsNullOrWhiteSpace(refinedQuery))
        {
            return refinedQuery;
        }

        var followUpQuestions = evaluation.FollowUpQuestions;
        return followUpQuestions.Length > 0 ? followUpQuestions[0] : fallbackQuery;
    }

    private static JsonDocument BuildIterativeResultJson(
        ResearchIterationState state,
        string agentName,
        Uri agentHostUrl,
        TimeProvider timeProvider)
    {
        var resultData = new
        {
            text = state.FinalSynthesis,
            agentType = "iterative-research",
            agentName,
            agentHostUrl = agentHostUrl.ToString(),
            iterations = state.CurrentIteration,
            finalScore = state.LatestEvaluation?.QualityScore ?? 0,
            originalQuery = state.OriginalQuery,
            timestamp = timeProvider.GetUtcNow(),
        };

        var json = JsonSerializer.Serialize(resultData, SerializerOptions);
        return JsonDocument.Parse(json);
    }

    private static async Task<ResearchEvaluation> RunEvaluationAsync(
        AIAgent agent,
        string originalQuery,
        string currentQuery,
        string findings,
        CancellationToken cancellationToken)
    {
        var evalPrompt = string.Format(
            CultureInfo.InvariantCulture,
            EvaluationPromptFormat,
            originalQuery,
            currentQuery,
            findings);

        var response = await agent.RunAsync(evalPrompt, cancellationToken: cancellationToken)
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

        var synthesisPrompt = string.Format(
            CultureInfo.InvariantCulture,
            SynthesisPromptFormat,
            state.OriginalQuery,
            state.Iterations.Count,
            state.AllFindings);

        var response = await agent.RunAsync(synthesisPrompt, cancellationToken: cancellationToken)
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

            return JsonSerializer.Deserialize<ResearchEvaluation>(json, SerializerOptions)
                ?? ResearchEvaluation.CreateDefault();
        }
        catch (JsonException)
        {
            return ResearchEvaluation.CreateDefault();
        }
    }

    private static Task ReportProgressAsync(AgentTaskContext? context, string message, CancellationToken cancellationToken) =>
        context?.ReportProgressAsync(message, cancellationToken) ?? Task.CompletedTask;

    private async Task ProcessTaskAsync(AgentTaskRequest request, CancellationToken cancellationToken)
    {
        LogProcessingTask(this.logger, request.TaskId, request.AgentType);

        // Display prominent agent banner
        var agentName = request.AgentType.ToUpperInvariant() switch
        {
            "RESEARCH" => "ResearchAgent",
            "ITERATIVE-RESEARCH" => "IterativeResearchAgent",
            _ => request.AgentType,
        };

        LogBannerTop(this.logger);
        LogBannerAgent(this.logger, agentName);
        LogBannerTask(this.logger, request.TaskId);
        LogBannerBottom(this.logger);

        // Set up task context for progress reporting
        var taskContext = new AgentTaskContext(request.TaskId, this.broker, this.timeProvider);
        using var scope = AgentTaskContext.SetCurrent(taskContext);

        AgentTaskResult result;

        try
        {
            var data = await this.ExecuteAgentAsync(request, cancellationToken).ConfigureAwait(false);

            result = new AgentTaskResult(
                TaskId: request.TaskId,
                Status: AgentTaskStatus.Completed,
                Data: data,
                Error: null,
                CompletedAt: this.timeProvider.GetUtcNow());

            LogTaskCompleted(this.logger, request.TaskId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            result = new AgentTaskResult(
                TaskId: request.TaskId,
                Status: AgentTaskStatus.Cancelled,
                Data: null,
                Error: "Task was cancelled.",
                CompletedAt: this.timeProvider.GetUtcNow());

            LogTaskCancelled(this.logger, request.TaskId);
        }
        catch (NotSupportedException ex)
        {
            result = new AgentTaskResult(
                TaskId: request.TaskId,
                Status: AgentTaskStatus.Failed,
                Data: null,
                Error: ex.Message,
                CompletedAt: this.timeProvider.GetUtcNow());

            LogTaskFailed(this.logger, ex, request.TaskId, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            result = new AgentTaskResult(
                TaskId: request.TaskId,
                Status: AgentTaskStatus.Failed,
                Data: null,
                Error: ex.Message,
                CompletedAt: this.timeProvider.GetUtcNow());

            LogTaskFailed(this.logger, ex, request.TaskId, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            result = new AgentTaskResult(
                TaskId: request.TaskId,
                Status: AgentTaskStatus.Failed,
                Data: null,
                Error: ex.Message,
                CompletedAt: this.timeProvider.GetUtcNow());

            LogTaskFailed(this.logger, ex, request.TaskId, ex.Message);
        }

        await this.broker.PublishResultAsync(result, cancellationToken).ConfigureAwait(false);
    }

    private Task<JsonDocument?> ExecuteAgentAsync(
        AgentTaskRequest request,
        CancellationToken cancellationToken) =>
        request.AgentType.ToUpperInvariant() switch
        {
            "RESEARCH" => this.ExecuteResearchAgentAsync(request.Prompt, cancellationToken),
            "ITERATIVE-RESEARCH" => this.ExecuteIterativeResearchAgentAsync(request.Prompt, cancellationToken),
            _ => throw new NotSupportedException($"Unknown agent type: {request.AgentType}"),
        };

    private async Task<JsonDocument?> ExecuteResearchAgentAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        LogCallingAgentHost(this.logger, this.agentHostUrl, prompt);

        // Resolve the agent from AgentHost via A2A protocol
        var cardResolver = new A2ACardResolver(this.agentHostUrl);
        var agentCard = await cardResolver.GetAgentCardAsync(cancellationToken).ConfigureAwait(false);

        LogAgentResolved(this.logger, agentCard.Name);

        // Get AIAgent and execute the prompt
        // Note: GetAIAgentAsync doesn't accept CancellationToken in current API
#pragma warning disable CA2016, MA0040 // Forward the CancellationToken parameter
        var agent = await cardResolver.GetAIAgentAsync().ConfigureAwait(false);
#pragma warning restore CA2016, MA0040

        var response = await agent.RunAsync(prompt, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Wrap the text response in a structured JSON document
        var resultData = new
        {
            text = response.Text ?? string.Empty,
            agentType = "research",
            agentName = agentCard.Name,
            agentHostUrl = this.agentHostUrl.ToString(),
            timestamp = this.timeProvider.GetUtcNow(),
        };

        var json = JsonSerializer.Serialize(resultData, SerializerOptions);

        return JsonDocument.Parse(json);
    }

    private async Task<JsonDocument?> ExecuteIterativeResearchAgentAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        var taskContext = AgentTaskContext.Instance;
        var state = new ResearchIterationState(prompt, this.timeProvider);

        // Resolve the research agent from AgentHost via A2A
        LogCallingAgentHost(this.logger, this.agentHostUrl, prompt);
        var cardResolver = new A2ACardResolver(this.agentHostUrl);
        var agentCard = await cardResolver.GetAgentCardAsync(cancellationToken).ConfigureAwait(false);

        LogAgentResolved(this.logger, agentCard.Name);

#pragma warning disable CA2016, MA0040 // Forward the CancellationToken parameter
        var researchAgent = await cardResolver.GetAIAgentAsync().ConfigureAwait(false);
#pragma warning restore CA2016, MA0040

        // EvaluatorAgent runs locally (not exposed via A2A)
        var evaluatorAgent = EvaluatorAgent.Create();

        await ReportProgressAsync(taskContext, "Starting iterative research", cancellationToken)
            .ConfigureAwait(false);

        // Run iterations
        await this.ExecuteResearchIterationsAsync(
            state,
            taskContext,
            researchAgent,
            evaluatorAgent,
            prompt,
            cancellationToken).ConfigureAwait(false);

        // Synthesize final result
        state.FinalSynthesis = await SynthesizeAsync(researchAgent, state, cancellationToken)
            .ConfigureAwait(false);
        state.IsComplete = true;

        var completionMsg = string.Create(
            CultureInfo.InvariantCulture,
            $"Research complete after {state.CurrentIteration} iteration(s)");
        await ReportProgressAsync(taskContext, completionMsg, cancellationToken).ConfigureAwait(false);

        return BuildIterativeResultJson(state, agentCard.Name, this.agentHostUrl, this.timeProvider);
    }

    private async Task ExecuteResearchIterationsAsync(
        ResearchIterationState state,
        AgentTaskContext? taskContext,
        AIAgent researchAgent,
        AIAgent evaluatorAgent,
        string originalPrompt,
        CancellationToken cancellationToken)
    {
        var currentQuery = originalPrompt;

        while (state.CurrentIteration < MaxIterations && !cancellationToken.IsCancellationRequested)
        {
            var iterationNum = state.CurrentIteration + 1;
            var iterationContext = new IterationContext(
                state,
                taskContext,
                researchAgent,
                evaluatorAgent,
                currentQuery,
                originalPrompt,
                iterationNum);

            var (evaluation, shouldContinue) = await this.ExecuteSingleIterationAsync(
                iterationContext,
                cancellationToken).ConfigureAwait(false);

            if (!shouldContinue)
            {
                break;
            }

            currentQuery = GetNextQuery(evaluation, currentQuery);
            if (string.Equals(currentQuery, originalPrompt, StringComparison.Ordinal))
            {
                break;
            }
        }
    }

    private async Task<(ResearchEvaluation Evaluation, bool ShouldContinue)> ExecuteSingleIterationAsync(
        IterationContext ctx,
        CancellationToken cancellationToken)
    {
        // Step 1: Research via AgentHost
        var researchMsg = string.Create(CultureInfo.InvariantCulture, $"Iteration {ctx.IterationNum}: Researching...");
        await ReportProgressAsync(ctx.TaskContext, researchMsg, cancellationToken).ConfigureAwait(false);
        LogIterationResearching(this.logger, ctx.IterationNum, ctx.CurrentQuery);

        var researchResponse = await ctx.ResearchAgent.RunAsync(ctx.CurrentQuery, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var findings = researchResponse.Text ?? "No findings returned.";

        // Step 2: Evaluate locally
        var evalMsg = string.Create(CultureInfo.InvariantCulture, $"Iteration {ctx.IterationNum}: Evaluating findings...");
        await ReportProgressAsync(ctx.TaskContext, evalMsg, cancellationToken).ConfigureAwait(false);
        LogIterationEvaluating(this.logger, ctx.IterationNum);

        var evaluation = await RunEvaluationAsync(
            ctx.EvaluatorAgent,
            ctx.OriginalPrompt,
            ctx.CurrentQuery,
            findings,
            cancellationToken).ConfigureAwait(false);

        // Step 3: Record iteration
        ctx.State.AddIteration(ctx.CurrentQuery, findings, evaluation);
        LogIterationComplete(this.logger, ctx.IterationNum, evaluation.QualityScore, evaluation.IsSatisfactory);

        var scoreMsg = string.Create(
            CultureInfo.InvariantCulture,
            $"Iteration {ctx.IterationNum}: Score {evaluation.QualityScore}/10, Satisfactory: {evaluation.IsSatisfactory}");
        await ReportProgressAsync(ctx.TaskContext, scoreMsg, cancellationToken).ConfigureAwait(false);

        // Step 4: Check if done
        if (evaluation.IsSatisfactory || evaluation.QualityScore >= QualityThreshold)
        {
            await ReportProgressAsync(ctx.TaskContext, "Research meets quality threshold, synthesizing...", cancellationToken)
                .ConfigureAwait(false);
            return (evaluation, ShouldContinue: false);
        }

        return (evaluation, ShouldContinue: true);
    }

    /// <summary>
    /// Context for a single research iteration.
    /// </summary>
    /// <param name="State">The research iteration state.</param>
    /// <param name="TaskContext">The task context for progress reporting.</param>
    /// <param name="ResearchAgent">The research agent.</param>
    /// <param name="EvaluatorAgent">The evaluator agent.</param>
    /// <param name="CurrentQuery">The current query being researched.</param>
    /// <param name="OriginalPrompt">The original prompt from the user.</param>
    /// <param name="IterationNum">The current iteration number.</param>
    private sealed record IterationContext(
        ResearchIterationState State,
        AgentTaskContext? TaskContext,
        AIAgent ResearchAgent,
        AIAgent EvaluatorAgent,
        string CurrentQuery,
        string OriginalPrompt,
        int IterationNum);
}
