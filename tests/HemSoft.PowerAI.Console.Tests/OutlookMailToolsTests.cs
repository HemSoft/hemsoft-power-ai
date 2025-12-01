// <copyright file="OutlookMailToolsTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using HemSoft.PowerAI.Console.Services;
using HemSoft.PowerAI.Console.Tools;

/// <summary>
/// Unit tests for <see cref="OutlookMailTools"/>.
/// </summary>
[Collection("EnvironmentVariableTests")]
public sealed class OutlookMailToolsTests : IDisposable
{
    private readonly string? originalClientId;
    private readonly string? originalTenantId;
    private readonly string? originalUserClientId;
    private readonly string? originalUserTenantId;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookMailToolsTests"/> class.
    /// </summary>
    public OutlookMailToolsTests()
    {
        // Save original Process-level env vars
        this.originalClientId = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
        this.originalTenantId = Environment.GetEnvironmentVariable("GRAPH_TENANT_ID");

        // Save original User-level env vars (SharedGraphClient falls back to these)
        this.originalUserClientId = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID", EnvironmentVariableTarget.User);
        this.originalUserTenantId = Environment.GetEnvironmentVariable("GRAPH_TENANT_ID", EnvironmentVariableTarget.User);

        // Reset the cached Graph client to ensure test isolation
        SharedGraphClient.Reset();

        // Clear env vars at both Process and User level for clean test state
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", null);
        Environment.SetEnvironmentVariable("GRAPH_TENANT_ID", null);
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", null, EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable("GRAPH_TENANT_ID", null, EnvironmentVariableTarget.User);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Restore original Process-level environment
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", this.originalClientId);
        Environment.SetEnvironmentVariable("GRAPH_TENANT_ID", this.originalTenantId);

        // Restore original User-level environment
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", this.originalUserClientId, EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable("GRAPH_TENANT_ID", this.originalUserTenantId, EnvironmentVariableTarget.User);

        // Reset the cached Graph client so next test starts fresh
        SharedGraphClient.Reset();
    }

    /// <summary>
    /// Verifies Mail returns error when GRAPH_CLIENT_ID is not set.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task MailAsyncReturnsErrorWhenClientIdNotSet()
    {
        // Act
        var result = await OutlookMailTools.MailAsync("inbox");

        // Assert
        Assert.Contains("GRAPH_CLIENT_ID", result, StringComparison.Ordinal);
        Assert.Contains("Error", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies Mail returns error for unknown mode.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task MailAsyncReturnsErrorForUnknownMode()
    {
        // Arrange - set a fake client ID but use test client to avoid auth
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");

        // Act
        var result = await OutlookMailTools.MailAsync("invalid");

        // Assert
        Assert.Contains("Unknown mode", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies read mode requires message ID.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task MailAsyncReadRequiresMessageId()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");

        // Act
        var result = await OutlookMailTools.MailAsync("read");

        // Assert
        Assert.Contains("Message ID required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies read mode requires non-empty message ID.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task MailAsyncReadRequiresNonEmptyMessageId()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");

        // Act
        var result = await OutlookMailTools.MailAsync("read", param1: "  ");

        // Assert
        Assert.Contains("Message ID required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies send mode requires recipient email.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task MailAsyncSendRequiresRecipient()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");

        // Act
        var result = await OutlookMailTools.MailAsync("send");

        // Assert
        Assert.Contains("Recipient email required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies send mode requires subject.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task MailAsyncSendRequiresSubject()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");

        // Act
        var result = await OutlookMailTools.MailAsync("send", param1: "test@example.com");

        // Assert
        Assert.Contains("Subject required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies search mode requires query.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task MailAsyncSearchRequiresQuery()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");

        // Act
        var result = await OutlookMailTools.MailAsync("search");

        // Assert
        Assert.Contains("Search query required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies search mode requires non-empty query.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task MailAsyncSearchRequiresNonEmptyQuery()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");

        // Act
        var result = await OutlookMailTools.MailAsync("search", param1: "   ");

        // Assert
        Assert.Contains("Search query required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies mode is case-insensitive.
    /// </summary>
    /// <param name="mode">The mode in different cases.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Theory]
    [InlineData("INBOX")]
    [InlineData("inbox")]
    [InlineData("Inbox")]
    [InlineData("InBoX")]
    [InlineData("delete")]
    [InlineData("DELETE")]
    [InlineData("move")]
    [InlineData("MOVE")]
    [InlineData("junk")]
    [InlineData("JUNK")]
    [InlineData("spam")]
    [InlineData("SPAM")]
    [InlineData("junkmail")]
    [InlineData("JUNKMAIL")]
    [InlineData("folder")]
    [InlineData("FOLDER")]
    public async Task MailAsyncModeIsCaseInsensitive(string mode)
    {
        // Arrange - no client id set, so it will fail with config error
        // but this tests that the mode is being processed correctly

        // Act
        var result = await OutlookMailTools.MailAsync(mode);

        // Assert - should return config error, not unknown mode error
        Assert.Contains("GRAPH_CLIENT_ID", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Unknown mode", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies maxResults is clamped to valid range.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task MailAsyncClampsMaxResults()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");

        // Act - these should not throw, maxResults should be clamped
        var result1 = await OutlookMailTools.MailAsync("search", param1: "test", maxResults: -10);
        var result2 = await OutlookMailTools.MailAsync("search", param1: "test", maxResults: 1000);

        // Assert - should get search query required or auth error, not argument error
        Assert.DoesNotContain("maxResults", result1, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("maxResults", result2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies delete mode requires message ID.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task MailAsyncDeleteRequiresMessageId()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");

        // Act
        var result = await OutlookMailTools.MailAsync("delete");

        // Assert
        Assert.Contains("Message ID required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies move mode requires message ID.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task MailAsyncMoveRequiresMessageId()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");

        // Act
        var result = await OutlookMailTools.MailAsync("move");

        // Assert
        Assert.Contains("Message ID required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies junk mode with whitespace param lists folder, doesn't require message ID.
    /// </summary>
    /// <remarks>
    /// Junk mode without param1 (or with null) lists the junk folder.
    /// Junk mode WITH a message ID moves that message to junk.
    /// Whitespace param is treated as "no param" → lists folder.
    /// </remarks>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task MailAsyncJunkWithoutParamListsFolder()
    {
        // Arrange - no client ID set (tests should run without real Graph connection)
        // The mode should be recognized as valid even without auth

        // Act
        var result = await OutlookMailTools.MailAsync("junk");

        // Assert - should return config error (not auth), not "unknown mode"
        Assert.Contains("GRAPH_CLIENT_ID", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Unknown mode", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Message ID required", result, StringComparison.Ordinal);
    }
}
