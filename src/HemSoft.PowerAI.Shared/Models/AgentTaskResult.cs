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
/// <param name="Data">Structured result data (schema varies by agent type). May be null if data is stored externally.</param>
/// <param name="Error">Error message if the task failed.</param>
/// <param name="CompletedAt">Timestamp when the task completed.</param>
/// <param name="ResultStorageKey">Optional storage key for retrieving large result data from persistent storage.</param>
/// <remarks>
/// For large results, <see cref="Data"/> may be null and <see cref="ResultStorageKey"/> will contain
/// the key to retrieve the full data from the result storage service. This avoids issues with
/// message size limits in pub/sub channels.
/// </remarks>
public sealed record AgentTaskResult(
    string TaskId,
    AgentTaskStatus Status,
    JsonDocument? Data,
    string? Error,
    DateTimeOffset CompletedAt,
    string? ResultStorageKey = null)
{
    /// <summary>
    /// Gets a value indicating whether the result data is stored externally.
    /// </summary>
    public bool HasStoredResult => !string.IsNullOrEmpty(this.ResultStorageKey);
}
