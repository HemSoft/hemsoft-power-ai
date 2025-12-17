// <copyright file="RedisSettings.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Configuration;

/// <summary>
/// Configuration settings for Redis connection.
/// </summary>
public sealed class RedisSettings
{
    /// <summary>
    /// The configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Redis";

    /// <summary>
    /// Gets or sets the Redis connection string.
    /// Defaults to localhost for development.
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";
}
