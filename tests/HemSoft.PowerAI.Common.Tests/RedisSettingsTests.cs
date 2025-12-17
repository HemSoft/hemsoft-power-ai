// <copyright file="RedisSettingsTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Tests;

using HemSoft.PowerAI.Common.Configuration;

/// <summary>
/// Unit tests for <see cref="RedisSettings"/>.
/// </summary>
public class RedisSettingsTests
{
    /// <summary>
    /// Tests that SectionName constant is correct.
    /// </summary>
    [Fact]
    public void SectionNameConstantIsRedis() => Assert.Equal("Redis", RedisSettings.SectionName);

    /// <summary>
    /// Tests that ConnectionString has correct default value.
    /// </summary>
    [Fact]
    public void ConnectionStringDefaultsToLocalhost()
    {
        // Arrange
        var settings = new RedisSettings();

        // Assert
        Assert.Equal("localhost:6379", settings.ConnectionString);
    }

    /// <summary>
    /// Tests that ConnectionString can be set.
    /// </summary>
    [Fact]
    public void ConnectionStringCanBeSet()
    {
        // Arrange & Act
        var settings = new RedisSettings { ConnectionString = "redis.example.com:6380" };

        // Assert
        Assert.Equal("redis.example.com:6380", settings.ConnectionString);
    }

    /// <summary>
    /// Tests that ConnectionString can be set to null.
    /// </summary>
    [Fact]
    public void ConnectionStringCanBeSetToNull()
    {
        // Arrange & Act
        var settings = new RedisSettings { ConnectionString = null! };

        // Assert
        Assert.Null(settings.ConnectionString);
    }

    /// <summary>
    /// Tests that ConnectionString can be set to empty.
    /// </summary>
    [Fact]
    public void ConnectionStringCanBeSetToEmpty()
    {
        // Arrange & Act
        var settings = new RedisSettings { ConnectionString = string.Empty };

        // Assert
        Assert.Empty(settings.ConnectionString);
    }
}
