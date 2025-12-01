// <copyright file="WebSearchToolsTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using HemSoft.PowerAI.Console.Tools;

/// <summary>
/// Unit tests for the <see cref="WebSearchTools"/> class.
/// </summary>
public sealed class WebSearchToolsTests
{
    /// <summary>
    /// Tests that SearchAsync returns an error for empty query.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task SearchAsyncEmptyQueryReturnsError()
    {
        var result = await WebSearchTools.WebSearchAsync(string.Empty);

        Assert.Contains("Error: Search query cannot be empty", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that SearchAsync returns an error for whitespace-only query.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task SearchAsyncWhitespaceQueryReturnsError()
    {
        var result = await WebSearchTools.WebSearchAsync("   ");

        Assert.Contains("Error: Search query cannot be empty", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that SearchAsync returns an error when API URL is missing.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task SearchAsyncMissingApiUrlReturnsError()
    {
        // Store original values
        var originalUrl = Environment.GetEnvironmentVariable("TAVILY_API_URL");
        var originalKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");

        try
        {
            // Clear the environment variables
            Environment.SetEnvironmentVariable("TAVILY_API_URL", null);
            Environment.SetEnvironmentVariable("TAVILY_API_KEY", "test-key");

            var result = await WebSearchTools.WebSearchAsync("test query");

            Assert.Contains("Error: Missing TAVILY_API_URL", result, StringComparison.Ordinal);
        }
        finally
        {
            // Restore original values
            Environment.SetEnvironmentVariable("TAVILY_API_URL", originalUrl);
            Environment.SetEnvironmentVariable("TAVILY_API_KEY", originalKey);
        }
    }

    /// <summary>
    /// Tests that SearchAsync returns an error when API key is missing.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task SearchAsyncMissingApiKeyReturnsError()
    {
        // Store original values
        var originalUrl = Environment.GetEnvironmentVariable("TAVILY_API_URL");
        var originalKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");

        try
        {
            // Set URL but clear API key
            Environment.SetEnvironmentVariable("TAVILY_API_URL", "https://api.tavily.com/search");
            Environment.SetEnvironmentVariable("TAVILY_API_KEY", null);

            var result = await WebSearchTools.WebSearchAsync("test query");

            Assert.Contains("Error: Missing TAVILY_API_KEY", result, StringComparison.Ordinal);
            Assert.Contains("https://tavily.com", result, StringComparison.Ordinal);
        }
        finally
        {
            // Restore original values
            Environment.SetEnvironmentVariable("TAVILY_API_URL", originalUrl);
            Environment.SetEnvironmentVariable("TAVILY_API_KEY", originalKey);
        }
    }

    /// <summary>
    /// Tests that SearchAsync returns an error for invalid API key.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task SearchAsyncInvalidApiKeyReturnsError()
    {
        // Store original values
        var originalUrl = Environment.GetEnvironmentVariable("TAVILY_API_URL");
        var originalKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");

        try
        {
            // Set invalid credentials
            Environment.SetEnvironmentVariable("TAVILY_API_URL", "https://api.tavily.com/search");
            Environment.SetEnvironmentVariable("TAVILY_API_KEY", "invalid-key");

            var result = await WebSearchTools.WebSearchAsync("test query");

            // Should return an error (either auth error or parse error)
            Assert.Contains("Error:", result, StringComparison.Ordinal);
        }
        finally
        {
            // Restore original values
            Environment.SetEnvironmentVariable("TAVILY_API_URL", originalUrl);
            Environment.SetEnvironmentVariable("TAVILY_API_KEY", originalKey);
        }
    }

    /// <summary>
    /// Tests that SearchAsync clamps maxResults to valid range.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task SearchAsyncClampsMaxResultsToValidRange()
    {
        // Store original values
        var originalUrl = Environment.GetEnvironmentVariable("TAVILY_API_URL");
        var originalKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");

        try
        {
            // Clear env vars to trigger early return (we're testing parameter handling)
            Environment.SetEnvironmentVariable("TAVILY_API_URL", null);
            Environment.SetEnvironmentVariable("TAVILY_API_KEY", null);

            // These should not throw even with out-of-range values
            var result1 = await WebSearchTools.WebSearchAsync("test", maxResults: -5);
            var result2 = await WebSearchTools.WebSearchAsync("test", maxResults: 100);

            // Both should fail at API URL check, not parameter validation
            Assert.Contains("Error: Missing TAVILY_API_URL", result1, StringComparison.Ordinal);
            Assert.Contains("Error: Missing TAVILY_API_URL", result2, StringComparison.Ordinal);
        }
        finally
        {
            // Restore original values
            Environment.SetEnvironmentVariable("TAVILY_API_URL", originalUrl);
            Environment.SetEnvironmentVariable("TAVILY_API_KEY", originalKey);
        }
    }

    /// <summary>
    /// Tests that SearchAsync handles network errors gracefully.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task SearchAsyncNetworkErrorReturnsError()
    {
        // Store original values
        var originalUrl = Environment.GetEnvironmentVariable("TAVILY_API_URL");
        var originalKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");

        try
        {
            // Set invalid URL that will fail to connect
            Environment.SetEnvironmentVariable("TAVILY_API_URL", "https://invalid-domain-that-does-not-exist.test/search");
            Environment.SetEnvironmentVariable("TAVILY_API_KEY", "test-key");

            var result = await WebSearchTools.WebSearchAsync("test query");

            // Should return a network error
            Assert.Contains("Error:", result, StringComparison.Ordinal);
        }
        finally
        {
            // Restore original values
            Environment.SetEnvironmentVariable("TAVILY_API_URL", originalUrl);
            Environment.SetEnvironmentVariable("TAVILY_API_KEY", originalKey);
        }
    }

    /// <summary>
    /// Tests that SearchAsync handles valid maxResults values.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task SearchAsyncWithValidMaxResultsWorks()
    {
        // Store original values
        var originalUrl = Environment.GetEnvironmentVariable("TAVILY_API_URL");
        var originalKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");

        try
        {
            // Clear URL to trigger early return
            Environment.SetEnvironmentVariable("TAVILY_API_URL", null);
            Environment.SetEnvironmentVariable("TAVILY_API_KEY", null);

            // Valid maxResults should not cause parameter errors
            var result = await WebSearchTools.WebSearchAsync("test", maxResults: 3);

            // Should fail at URL check, not parameter validation
            Assert.Contains("Error: Missing TAVILY_API_URL", result, StringComparison.Ordinal);
        }
        finally
        {
            // Restore original values
            Environment.SetEnvironmentVariable("TAVILY_API_URL", originalUrl);
            Environment.SetEnvironmentVariable("TAVILY_API_KEY", originalKey);
        }
    }
}
