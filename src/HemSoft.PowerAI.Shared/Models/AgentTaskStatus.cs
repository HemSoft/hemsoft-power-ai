// <copyright file="AgentTaskStatus.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Models;

/// <summary>
/// Represents the execution status of an agent task.
/// </summary>
public enum AgentTaskStatus
{
    /// <summary>
    /// Task is queued but not yet started.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Task is currently being executed by a worker.
    /// </summary>
    Running = 1,

    /// <summary>
    /// Task completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Task failed during execution.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Task was cancelled before completion.
    /// </summary>
    Cancelled = 4,
}
