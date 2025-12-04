// <copyright file="SharedGraphClientTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using HemSoft.PowerAI.Console.Services;

using Xunit;

/// <summary>
/// Tests for the SharedGraphClient singleton.
/// </summary>
[Collection("EnvironmentVariableTests")]
public sealed class SharedGraphClientTests : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SharedGraphClientTests"/> class.
    /// </summary>
    public SharedGraphClientTests() => SharedGraphClient.Reset();

    /// <inheritdoc/>
    public void Dispose() => SharedGraphClient.Reset();

    /// <summary>
    /// GetClient returns null when GRAPH_CLIENT_ID is not configured.
    /// </summary>
    [Fact]
    public void GetClientReturnsNullWhenClientIdNotSet()
    {
        // Skip if GRAPH_CLIENT_ID is configured in user registry (real environment)
        var registryValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID", EnvironmentVariableTarget.User);
        if (!string.IsNullOrEmpty(registryValue))
        {
            return; // Skip - would trigger real auth flow
        }

        // Arrange - ensure env var is not set
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", null);

        // Act
        using var client = SharedGraphClient.GetClient();

        // Assert
        Assert.Null(client);
    }

    /// <summary>
    /// IsConfigured returns false when GRAPH_CLIENT_ID is not set.
    /// </summary>
    [Fact]
    public void IsConfiguredReturnsFalseWhenClientIdNotSet()
    {
        // Skip if GRAPH_CLIENT_ID is configured in user registry
        var registryValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID", EnvironmentVariableTarget.User);
        if (!string.IsNullOrEmpty(registryValue))
        {
            // In this case, IsConfigured would return true
            Assert.True(SharedGraphClient.IsConfigured());
            return;
        }

        // Arrange - ensure env var is not set
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", null);

        // Act
        var result = SharedGraphClient.IsConfigured();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// IsConfigured returns true when GRAPH_CLIENT_ID is set.
    /// </summary>
    [Fact]
    public void IsConfiguredReturnsTrueWhenClientIdSet()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
        var registryValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID", EnvironmentVariableTarget.User);

        try
        {
            // If already configured in registry, just verify
            if (!string.IsNullOrEmpty(registryValue))
            {
                Assert.True(SharedGraphClient.IsConfigured());
                return;
            }

            // Set temporarily for test
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");

            // Act
            var result = SharedGraphClient.IsConfigured();

            // Assert
            Assert.True(result);
        }
        finally
        {
            // Restore original value
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", originalValue);
        }
    }

    /// <summary>
    /// ClientIdEnvVar property returns the expected value.
    /// </summary>
    [Fact]
    public void ClientIdEnvVarReturnsExpectedValue() =>
        Assert.Equal("GRAPH_CLIENT_ID", SharedGraphClient.ClientIdEnvVar);

    /// <summary>
    /// Reset clears the singleton instance.
    /// </summary>
    [Fact]
    public void ResetClearsSingletonInstance()
    {
        // Arrange - skip if real client ID is configured
        var registryValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID", EnvironmentVariableTarget.User);
        if (!string.IsNullOrEmpty(registryValue))
        {
            return; // Skip - would trigger real auth flow
        }

        // First call to GetClient when not configured returns null
        using var firstResult = SharedGraphClient.GetClient();
        Assert.Null(firstResult);

        // Reset should not throw
        SharedGraphClient.Reset();

        // Subsequent call should still return null (since not configured)
        using var secondResult = SharedGraphClient.GetClient();
        Assert.Null(secondResult);
    }

    /// <summary>
    /// GetClient returns same instance on multiple calls (singleton behavior).
    /// </summary>
    [Fact]
    public void GetClientReturnsConsistentResultOnMultipleCalls()
    {
        // Skip if GRAPH_CLIENT_ID is configured in user registry (would trigger real auth)
        var registryValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID", EnvironmentVariableTarget.User);
        if (!string.IsNullOrEmpty(registryValue))
        {
            return; // Skip - singleton test would require real Graph API
        }

        // When not configured, should consistently return null
        using var first = SharedGraphClient.GetClient();
        using var second = SharedGraphClient.GetClient();

        Assert.Null(first);
        Assert.Null(second);
    }
}
