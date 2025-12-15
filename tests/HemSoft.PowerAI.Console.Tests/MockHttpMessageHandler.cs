// <copyright file="MockHttpMessageHandler.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using System.Net;

/// <summary>
/// A mock HTTP message handler for testing Graph API calls.
/// Allows configuring responses for specific endpoints.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode StatusCode, string Content)> responses = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<HttpRequestMessage> recordedRequests = [];

    /// <summary>
    /// Gets the list of recorded HTTP requests for verification.
    /// </summary>
    public IReadOnlyList<HttpRequestMessage> RecordedRequests => this.recordedRequests.AsReadOnly();

    /// <summary>
    /// Gets or sets the default response status code for unmatched requests.
    /// </summary>
    public HttpStatusCode DefaultStatusCode { get; set; } = HttpStatusCode.OK;

    /// <summary>
    /// Gets or sets the default response content for unmatched requests.
    /// </summary>
    public string DefaultContent { get; set; } = "{}";

    /// <summary>
    /// Configures a response for a specific URL pattern.
    /// </summary>
    /// <param name="urlContains">Substring that must appear in the URL.</param>
    /// <param name="statusCode">HTTP status code to return.</param>
    /// <param name="content">JSON content to return.</param>
    public void SetupResponse(string urlContains, HttpStatusCode statusCode, string content) =>
        this.responses[urlContains] = (statusCode, content);

    /// <summary>
    /// Configures a successful response for a specific URL pattern.
    /// </summary>
    /// <param name="urlContains">Substring that must appear in the URL.</param>
    /// <param name="content">JSON content to return.</param>
    public void SetupResponse(string urlContains, string content) =>
        this.SetupResponse(urlContains, HttpStatusCode.OK, content);

    /// <summary>
    /// Configures an error response for a specific URL pattern.
    /// </summary>
    /// <param name="urlContains">Substring that must appear in the URL.</param>
    /// <param name="statusCode">HTTP status code to return.</param>
    /// <param name="errorCode">OData error code.</param>
    /// <param name="errorMessage">Error message.</param>
    public void SetupError(string urlContains, HttpStatusCode statusCode, string errorCode, string errorMessage) =>
        this.SetupResponse(urlContains, statusCode, $"{{\"error\": {{\"code\": \"{errorCode}\", \"message\": \"{errorMessage}\"}}}}");

    /// <summary>
    /// Clears all configured responses.
    /// </summary>
    public void ClearResponses()
    {
        this.responses.Clear();
        this.recordedRequests.Clear();
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        this.recordedRequests.Add(request);

        var url = request.RequestUri?.ToString() ?? string.Empty;

        foreach (var (urlPattern, (statusCode, content)) in this.responses)
        {
            if (url.Contains(urlPattern, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json"),
                });
            }
        }

        // Default response
        return Task.FromResult(new HttpResponseMessage(this.DefaultStatusCode)
        {
            Content = new StringContent(this.DefaultContent, System.Text.Encoding.UTF8, "application/json"),
        });
    }
}
