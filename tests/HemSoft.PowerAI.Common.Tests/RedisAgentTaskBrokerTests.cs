// <copyright file="RedisAgentTaskBrokerTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Tests;

using HemSoft.PowerAI.Common.Services;

/// <summary>
/// Unit tests for <see cref="RedisAgentTaskBroker"/>.
/// </summary>
public class RedisAgentTaskBrokerTests
{
    /// <summary>
    /// Tests that constructor throws for null connection string.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForNullConnectionString()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new RedisAgentTaskBroker(null!));
        Assert.Contains("connectionString", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that constructor throws for empty connection string.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForEmptyConnectionString()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new RedisAgentTaskBroker(string.Empty));
        Assert.Contains("connectionString", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that constructor throws for whitespace connection string.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForWhitespaceConnectionString()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new RedisAgentTaskBroker("   "));
        Assert.Contains("connectionString", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
