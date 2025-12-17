// <copyright file="AgentTaskModelsTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Tests;

using System.Text.Json;

using HemSoft.PowerAI.Common.Models;

/// <summary>
/// Unit tests for agent task models.
/// </summary>
public sealed class AgentTaskModelsTests
{
    /// <summary>
    /// Verifies AgentTaskRequest serialization round-trip.
    /// </summary>
    [Fact]
    public void AgentTaskRequestSerializesCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var request = new AgentTaskRequest(
            TaskId: "test-123",
            AgentType: "research",
            Prompt: "Test prompt",
            SubmittedAt: timestamp);

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<AgentTaskRequest>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(request.TaskId, deserialized.TaskId);
        Assert.Equal(request.AgentType, deserialized.AgentType);
        Assert.Equal(request.Prompt, deserialized.Prompt);
        Assert.Equal(request.SubmittedAt, deserialized.SubmittedAt);
    }

    /// <summary>
    /// Verifies AgentTaskResult serialization round-trip.
    /// </summary>
    [Fact]
    public void AgentTaskResultSerializesCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var data = JsonDocument.Parse("{\"finding\": \"test result\"}");
        var result = new AgentTaskResult(
            TaskId: "test-456",
            Status: AgentTaskStatus.Completed,
            Data: data,
            Error: null,
            CompletedAt: timestamp);

        // Act
        var json = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<AgentTaskResult>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(result.TaskId, deserialized.TaskId);
        Assert.Equal(result.Status, deserialized.Status);
        Assert.NotNull(deserialized.Data);
        Assert.Equal("test result", deserialized.Data.RootElement.GetProperty("finding").GetString());
        Assert.Null(deserialized.Error);
        Assert.Equal(result.CompletedAt, deserialized.CompletedAt);
    }

    /// <summary>
    /// Verifies AgentTaskResult with error serializes correctly.
    /// </summary>
    [Fact]
    public void AgentTaskResultWithErrorSerializesCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var result = new AgentTaskResult(
            TaskId: "test-789",
            Status: AgentTaskStatus.Failed,
            Data: null,
            Error: "Something went wrong",
            CompletedAt: timestamp);

        // Act
        var json = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<AgentTaskResult>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(AgentTaskStatus.Failed, deserialized.Status);

        // Note: JsonDocument serialization round-trip may produce empty document instead of null
        // The important assertion is that Error is preserved correctly
        Assert.Equal("Something went wrong", deserialized.Error);
    }

    /// <summary>
    /// Verifies Pending status has expected value.
    /// </summary>
    [Fact]
    public void AgentTaskStatusPendingHasValueZero() =>
        Assert.Equal(0, (int)AgentTaskStatus.Pending);

    /// <summary>
    /// Verifies Running status has expected value.
    /// </summary>
    [Fact]
    public void AgentTaskStatusRunningHasValueOne() =>
        Assert.Equal(1, (int)AgentTaskStatus.Running);

    /// <summary>
    /// Verifies Completed status has expected value.
    /// </summary>
    [Fact]
    public void AgentTaskStatusCompletedHasValueTwo() =>
        Assert.Equal(2, (int)AgentTaskStatus.Completed);

    /// <summary>
    /// Verifies Failed status has expected value.
    /// </summary>
    [Fact]
    public void AgentTaskStatusFailedHasValueThree() =>
        Assert.Equal(3, (int)AgentTaskStatus.Failed);

    /// <summary>
    /// Verifies Cancelled status has expected value.
    /// </summary>
    [Fact]
    public void AgentTaskStatusCancelledHasValueFour() =>
        Assert.Equal(4, (int)AgentTaskStatus.Cancelled);

    /// <summary>
    /// Verifies AgentTaskRequest with minimal data.
    /// </summary>
    [Fact]
    public void AgentTaskRequestWithEmptyPromptIsValid()
    {
        // Arrange & Act
        var request = new AgentTaskRequest(
            TaskId: "empty-prompt",
            AgentType: "research",
            Prompt: string.Empty,
            SubmittedAt: DateTimeOffset.UtcNow);

        // Assert
        Assert.Empty(request.Prompt);
    }
}
