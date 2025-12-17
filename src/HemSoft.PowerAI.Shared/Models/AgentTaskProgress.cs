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
public sealed record AgentTaskProgress(
    string TaskId,
    string Message,
    DateTimeOffset Timestamp,
    string? ToolName = null);
