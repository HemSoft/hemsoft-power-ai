// <copyright file="AgentTaskProgress.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Models;

/// <summary>
/// Represents a progress update for an agent task.
/// </summary>
/// <param name="TaskId">The task ID this progress relates to.</param>
/// <param name="Message">The progress message (e.g., "WebSearch: Anthropic Skills.md").</param>
/// <param name="Timestamp">When the progress event occurred.</param>
/// <param name="ToolName">Optional tool name that generated the progress.</param>
/// <param name="AgentName">The name of the agent reporting progress.</param>
/// <param name="ModelId">The model ID being used by the agent.</param>
/// <param name="InputTokens">Current input token count (context usage).</param>
/// <param name="OutputTokens">Current output token count.</param>
public sealed record AgentTaskProgress(
    string TaskId,
    string Message,
    DateTimeOffset Timestamp,
    string? ToolName = null,
    string? AgentName = null,
    string? ModelId = null,
    int? InputTokens = null,
    int? OutputTokens = null);
