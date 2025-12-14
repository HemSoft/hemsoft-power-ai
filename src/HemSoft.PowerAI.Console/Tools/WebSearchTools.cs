// <copyright file="WebSearchTools.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tools;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Provides web search capabilities for the AI agent using Tavily API.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Requires Tavily API key")]
internal static class WebSearchTools
{
    private const string ApiUrlEnvVar = "TAVILY_API_URL";
    private const string ApiKeyEnvVar = "TAVILY_API_KEY";
    private const int DefaultMaxResults = 5;

    private static readonly HttpClient HttpClient = new();

    /// <summary>
    /// Searches the web for current information using Tavily API.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="maxResults">Maximum number of results to return (1-10, default: 5).</param>
    /// <returns>Formatted search results or error message.</returns>
    [Description("Search the web for current information. Returns summarized results with titles, URLs, and content snippets.")]
    public static async Task<string> WebSearchAsync(string query, int maxResults = DefaultMaxResults)
    {
        System.Console.WriteLine($"[Tool] WebSearch: {query}");

        if (string.IsNullOrWhiteSpace(query))
        {
            return "Error: Search query cannot be empty";
        }

        var apiUrl = Environment.GetEnvironmentVariable(ApiUrlEnvVar);
        if (string.IsNullOrEmpty(apiUrl))
        {
            return $"Error: Missing {ApiUrlEnvVar} environment variable. Set it to: https://api.tavily.com/search";
        }

        var apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvVar);
        if (string.IsNullOrEmpty(apiKey))
        {
            return $"Error: Missing {ApiKeyEnvVar} environment variable. Get a free API key at https://tavily.com";
        }

        maxResults = Math.Clamp(maxResults, 1, 10);

        return await ExecuteSearchAsync(query, apiKey, maxResults, apiUrl).ConfigureAwait(false);
    }

    private static async Task<string> ExecuteSearchAsync(string query, string apiKey, int maxResults, string apiUrl)
    {
        try
        {
            var request = new TavilyRequest
            {
                ApiKey = apiKey,
                Query = query,
                MaxResults = maxResults,
                SearchDepth = "basic",
                IncludeAnswer = true,
            };

            using var response = await HttpClient.PostAsJsonAsync(apiUrl, request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return $"Error: Search failed with status {response.StatusCode}. {errorContent}";
            }

            var result = await response.Content.ReadFromJsonAsync<TavilyResponse>().ConfigureAwait(false);

            return result is null ? "Error: Failed to parse search results" : FormatResults(result);
        }
        catch (HttpRequestException ex)
        {
            return $"Error: Network error - {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            return "Error: Search request timed out";
        }
        catch (JsonException ex)
        {
            return $"Error: Failed to parse response - {ex.Message}";
        }
    }

    private static string FormatResults(TavilyResponse response)
    {
        var sb = new StringBuilder();

        AppendSummary(sb, response.Answer);
        AppendSources(sb, response.Results);

        return sb.Length > 0 ? sb.ToString().TrimEnd() : "No results found";
    }

    private static void AppendSummary(StringBuilder sb, string? answer)
    {
        if (string.IsNullOrEmpty(answer))
        {
            return;
        }

        _ = sb.AppendLine("## Summary")
          .AppendLine(answer)
          .AppendLine();
    }

    private static void AppendSources(StringBuilder sb, List<TavilyResult>? results)
    {
        if (results is not { Count: > 0 })
        {
            return;
        }

        _ = sb.AppendLine("## Sources");
        foreach (var result in results)
        {
            AppendSingleResult(sb, result);
        }
    }

    private static void AppendSingleResult(StringBuilder sb, TavilyResult result)
    {
        _ = sb.Append("- **")
          .Append(result.Title)
          .AppendLine("**")
          .Append("  URL: ")
          .AppendLine(result.ResultUrl.ToString());

        if (!string.IsNullOrEmpty(result.Content))
        {
            var snippet = result.Content.Length > 200
                ? $"{result.Content.AsSpan(0, 200)}..."
                : result.Content;
            _ = sb.Append("  ").AppendLine(snippet);
        }

        _ = sb.AppendLine();
    }

    /// <summary>
    /// Request payload for Tavily API.
    /// </summary>
    private sealed class TavilyRequest
    {
        [JsonPropertyName("api_key")]
        public required string ApiKey { get; init; }

        [JsonPropertyName("query")]
        public required string Query { get; init; }

        [JsonPropertyName("max_results")]
        public int MaxResults { get; init; }

        [JsonPropertyName("search_depth")]
        public string SearchDepth { get; init; } = "basic";

        [JsonPropertyName("include_answer")]
        public bool IncludeAnswer { get; init; }
    }

    /// <summary>
    /// Response payload from Tavily API.
    /// </summary>
    private sealed class TavilyResponse
    {
        [JsonPropertyName("answer")]
        public string? Answer { get; init; }

        [JsonPropertyName("results")]
        public List<TavilyResult>? Results { get; init; }
    }

    /// <summary>
    /// Individual search result from Tavily API.
    /// </summary>
    private sealed class TavilyResult
    {
        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("url")]
        public Uri ResultUrl { get; init; } = new Uri("about:blank");

        [JsonPropertyName("content")]
        public string? Content { get; init; }
    }
}
