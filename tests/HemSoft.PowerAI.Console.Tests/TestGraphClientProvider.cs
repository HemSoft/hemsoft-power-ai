// <copyright file="TestGraphClientProvider.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using System.Diagnostics.CodeAnalysis;

using HemSoft.PowerAI.Console.Services;

using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

/// <summary>
/// A test implementation of <see cref="IGraphClientProvider"/> that uses a mock HTTP handler.
/// Allows simulating Graph API responses without actual network calls.
/// </summary>
[SuppressMessage(
    "IDisposableAnalyzers.Correctness",
    "IDISP014:Use a single instance of HttpClient",
    Justification = "Test fixture creates disposable instances with mock handlers")]
internal sealed class TestGraphClientProvider : IGraphClientProvider, IDisposable
{
    private readonly HttpClient httpClient;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestGraphClientProvider"/> class.
    /// </summary>
    /// <param name="mockHandler">The mock HTTP handler to use for simulating Graph API responses.</param>
    public TestGraphClientProvider(MockHttpMessageHandler mockHandler)
    {
        this.httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("https://graph.microsoft.com/v1.0/"),
        };

        // Create a GraphServiceClient with our mock handler using anonymous auth (no real auth needed for tests)
        this.Client = new GraphServiceClient(this.httpClient, new AnonymousAuthenticationProvider());
        this.MockHandler = mockHandler;
    }

    /// <inheritdoc/>
    public GraphServiceClient? Client { get; }

    /// <summary>
    /// Gets the mock HTTP handler for configuring responses.
    /// </summary>
    public MockHttpMessageHandler MockHandler { get; }

    /// <inheritdoc/>
    public bool IsConfigured() => true;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        (this.Client as IDisposable)?.Dispose();
        this.httpClient.Dispose();
        this.disposed = true;
    }
}
