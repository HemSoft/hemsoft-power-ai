// <copyright file="RemoteAgentToolTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using HemSoft.PowerAI.Console.Agents.Infrastructure;

using Xunit;

/// <summary>
/// Unit tests for the RemoteAgentTool class.
/// </summary>
public sealed class RemoteAgentToolTests
{
    /// <summary>
    /// Verifies that ResearchAgentAsync returns an error when no remote agent is set.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ResearchAgentAsyncWithNoRemoteAgentReturnsError()
    {
        // Arrange
        RemoteAgentTool.ClearRemoteAgent();

        // Act
        var result = await RemoteAgentTool.ResearchAgentAsync("test task").ConfigureAwait(true);

        // Assert
        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not connected", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that CreateTool returns a non-null AITool.
    /// </summary>
    [Fact]
    public void CreateToolReturnsNonNullTool()
    {
        // Act
        var tool = RemoteAgentTool.CreateTool();

        // Assert
        Assert.NotNull(tool);
    }

    /// <summary>
    /// Verifies that ClearRemoteAgent can be called without error.
    /// </summary>
    [Fact]
    public void ClearRemoteAgentDoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(RemoteAgentTool.ClearRemoteAgent);
        Assert.Null(exception);
    }
}
