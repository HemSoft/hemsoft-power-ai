// <copyright file="MockGraphClientProviderTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

/// <summary>
/// Unit tests for <see cref="MockGraphClientProvider"/>.
/// </summary>
public class MockGraphClientProviderTests
{
    /// <summary>
    /// Tests that default constructor creates unconfigured provider.
    /// </summary>
    [Fact]
    public void DefaultConstructorCreatesUnconfiguredProvider()
    {
        // Act
        var provider = new MockGraphClientProvider();

        // Assert
        Assert.Null(provider.Client);
        Assert.False(provider.IsConfigured());
    }

    /// <summary>
    /// Tests that parameterized constructor sets client correctly.
    /// </summary>
    [Fact]
    public void ParameterizedConstructorSetsClientCorrectly()
    {
        // Note: We can't easily create a real GraphServiceClient without auth,
        // so we test with null which is a valid value
        var provider = new MockGraphClientProvider(client: null, isConfigured: true);

        // Assert
        Assert.Null(provider.Client);
        Assert.True(provider.IsConfigured());
    }

    /// <summary>
    /// Tests that IsConfigured returns the value passed to constructor.
    /// </summary>
    /// <param name="configuredValue">The configured value to test.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsConfiguredReturnsValuePassedToConstructor(bool configuredValue)
    {
        // Arrange
        var provider = new MockGraphClientProvider(client: null, isConfigured: configuredValue);

        // Act
        var result = provider.IsConfigured();

        // Assert
        Assert.Equal(configuredValue, result);
    }

    /// <summary>
    /// Tests that Client property is read-only after construction.
    /// </summary>
    [Fact]
    public void ClientPropertyIsReadOnlyAfterConstruction()
    {
        // Arrange
        var provider = new MockGraphClientProvider();

        // Act
        var client1 = provider.Client;
        var client2 = provider.Client;

        // Assert - should return same value
        Assert.Equal(client1, client2);
    }

    /// <summary>
    /// Tests that multiple calls to IsConfigured return consistent value.
    /// </summary>
    [Fact]
    public void MultipleCallsToIsConfiguredReturnConsistentValue()
    {
        // Arrange
        var provider = new MockGraphClientProvider(client: null, isConfigured: true);

        // Act
        var result1 = provider.IsConfigured();
        var result2 = provider.IsConfigured();
        var result3 = provider.IsConfigured();

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
    }

    /// <summary>
    /// Tests that default constructor chains to parameterized constructor.
    /// </summary>
    [Fact]
    public void DefaultConstructorChainsToParameterizedConstructor()
    {
        // Act
        var defaultProvider = new MockGraphClientProvider();
        var explicitProvider = new MockGraphClientProvider(client: null, isConfigured: false);

        // Assert - both should behave the same
        Assert.Equal(defaultProvider.Client, explicitProvider.Client);
        Assert.Equal(defaultProvider.IsConfigured(), explicitProvider.IsConfigured());
    }

    /// <summary>
    /// Tests that provider can simulate unconfigured state for testing.
    /// </summary>
    [Fact]
    public void ProviderCanSimulateUnconfiguredStateForTesting()
    {
        // Arrange
        var unconfiguredProvider = new MockGraphClientProvider();

        // Act & Assert
        Assert.Null(unconfiguredProvider.Client);
        Assert.False(unconfiguredProvider.IsConfigured());
    }

    /// <summary>
    /// Tests that provider can simulate configured state without real client.
    /// </summary>
    [Fact]
    public void ProviderCanSimulateConfiguredStateWithoutRealClient()
    {
        // Arrange - configured but with null client (for testing configuration detection)
        var configuredButNullProvider = new MockGraphClientProvider(client: null, isConfigured: true);

        // Act & Assert
        Assert.Null(configuredButNullProvider.Client);
        Assert.True(configuredButNullProvider.IsConfigured());
    }
}
