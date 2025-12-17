// <copyright file="AgentTaskResult.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Models;

using System.Text.Json;

/// <summary>
/// Represents the result of an agent task execution.
/// </summary>
/// <param name="TaskId">The unique identifier matching the original request.</param>
/// <param name="Status">The final execution status.</param>
/// <param name="Data">Structured result data (schema varies by agent type).</param>
/// <param name="Error">Error message if the task failed.</param>
/// <param name="CompletedAt">Timestamp when the task completed.</param>
public sealed record AgentTaskResult(
    string TaskId,
    AgentTaskStatus Status,
    JsonDocument? Data,
    string? Error,
    DateTimeOffset CompletedAt);
