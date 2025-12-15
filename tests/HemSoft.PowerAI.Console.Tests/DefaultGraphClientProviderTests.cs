// <copyright file="DefaultGraphClientProviderTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using HemSoft.PowerAI.Console.Services;

/// <summary>
/// Unit tests for <see cref="DefaultGraphClientProvider"/>.
/// </summary>
[Collection("EnvironmentVariableTests")]
public class DefaultGraphClientProviderTests
{
    /// <summary>
    /// Tests that Client returns null when GRAPH_CLIENT_ID is not set (and no registry fallback).
    /// </summary>
    [Fact]
    public void ClientReturnsNullWhenClientIdNotSetAndNoRegistryFallback()
    {
        // Skip if GRAPH_CLIENT_ID is configured in user registry (code falls back to registry)
        var userRegistryValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID", EnvironmentVariableTarget.User);
        if (!string.IsNullOrEmpty(userRegistryValue))
        {
            return; // Test not applicable when user has Graph configured in registry
        }

        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", value: null);
        SharedGraphClient.Reset();

        try
        {
            var provider = new DefaultGraphClientProvider();

            // Act
            var client = provider.Client;

            // Assert
            Assert.Null(client);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", originalValue);
            SharedGraphClient.Reset();
        }
    }

    /// <summary>
    /// Tests that IsConfigured returns false when GRAPH_CLIENT_ID is not set (and no registry fallback).
    /// </summary>
    [Fact]
    public void IsConfiguredReturnsFalseWhenClientIdNotSetAndNoRegistryFallback()
    {
        // Skip if GRAPH_CLIENT_ID is configured in user registry (code falls back to registry)
        var userRegistryValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID", EnvironmentVariableTarget.User);
        if (!string.IsNullOrEmpty(userRegistryValue))
        {
            return; // Test not applicable when user has Graph configured in registry
        }

        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", value: null);
        SharedGraphClient.Reset();

        try
        {
            var provider = new DefaultGraphClientProvider();

            // Act
            var isConfigured = provider.IsConfigured();

            // Assert
            Assert.False(isConfigured);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", originalValue);
            SharedGraphClient.Reset();
        }
    }

    /// <summary>
    /// Tests that IsConfigured returns true when GRAPH_CLIENT_ID is set.
    /// </summary>
    [Fact]
    public void IsConfiguredReturnsTrueWhenClientIdSet()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");
        SharedGraphClient.Reset();

        try
        {
            var provider = new DefaultGraphClientProvider();

            // Act
            var isConfigured = provider.IsConfigured();

            // Assert
            Assert.True(isConfigured);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", originalValue);
            SharedGraphClient.Reset();
        }
    }

    /// <summary>
    /// Tests that Client returns the same instance from SharedGraphClient.
    /// </summary>
    [Fact]
    public void ClientReturnsInstanceFromSharedGraphClient()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");
        SharedGraphClient.Reset();

        try
        {
            var provider = new DefaultGraphClientProvider();

            // Act
            var client1 = provider.Client;
            var client2 = provider.Client;

            // Assert - both should be the same instance
            Assert.Same(client1, client2);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", originalValue);
            SharedGraphClient.Reset();
        }
    }

    /// <summary>
    /// Tests that multiple provider instances share the same client.
    /// </summary>
    [Fact]
    public void MultipleProviderInstancesShareSameClient()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");
        SharedGraphClient.Reset();

        try
        {
            var provider1 = new DefaultGraphClientProvider();
            var provider2 = new DefaultGraphClientProvider();

            // Act
            var client1 = provider1.Client;
            var client2 = provider2.Client;

            // Assert - both should be the same instance (from singleton)
            Assert.Same(client1, client2);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", originalValue);
            SharedGraphClient.Reset();
        }
    }

    /// <summary>
    /// Tests that provider can be created without throwing.
    /// </summary>
    [Fact]
    public void ProviderCanBeCreatedWithoutThrowing()
    {
        // Act
        var exception = Record.Exception(() => new DefaultGraphClientProvider());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that IsConfigured returns true when set via environment variable.
    /// </summary>
    [Fact]
    public void IsConfiguredReturnsTrueWhenSetViaEnvironmentVariable()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
        SharedGraphClient.Reset();

        try
        {
            // Configure it via process environment
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "configured-value");
            SharedGraphClient.Reset();

            var provider = new DefaultGraphClientProvider();

            // Act
            var isConfigured = provider.IsConfigured();

            // Assert
            Assert.True(isConfigured);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", originalValue);
            SharedGraphClient.Reset();
        }
    }

    /// <summary>
    /// Tests that provider returns non-null client when configured.
    /// </summary>
    [Fact]
    public void ProviderReturnsNonNullClientWhenConfigured()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");
        SharedGraphClient.Reset();

        try
        {
            var provider = new DefaultGraphClientProvider();

            // Act
            var client = provider.Client;

            // Assert
            Assert.NotNull(client);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", originalValue);
            SharedGraphClient.Reset();
        }
    }
}
