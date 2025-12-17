// <copyright file="AgentTaskRequest.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Models;

/// <summary>
/// Represents a task submission to an agent worker.
/// </summary>
/// <param name="TaskId">Unique identifier for tracking the task.</param>
/// <param name="AgentType">The type of agent to execute (e.g., "research").</param>
/// <param name="Prompt">The user's request or instruction.</param>
/// <param name="SubmittedAt">Timestamp when the task was submitted.</param>
/// <param name="OutputPath">Optional file path where the result should be written on the client.</param>
public sealed record AgentTaskRequest(
    string TaskId,
    string AgentType,
    string Prompt,
    DateTimeOffset SubmittedAt,
    string? OutputPath = null);
