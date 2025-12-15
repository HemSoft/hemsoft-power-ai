// <copyright file="MockGraphClientProvider.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using HemSoft.PowerAI.Console.Services;

using Microsoft.Graph;

/// <summary>
/// Mock implementation of <see cref="IGraphClientProvider"/> for unit testing.
/// Returns null client by default, simulating unconfigured Graph API.
/// </summary>
/// <param name="client">The Graph client to return, or null for unconfigured.</param>
/// <param name="isConfigured">Whether to report as configured.</param>
internal sealed class MockGraphClientProvider(GraphServiceClient? client, bool isConfigured) : IGraphClientProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MockGraphClientProvider"/> class.
    /// Creates an unconfigured provider (returns null client).
    /// </summary>
    public MockGraphClientProvider()
        : this(client: null, isConfigured: false)
    {
    }

    /// <inheritdoc/>
    public GraphServiceClient? Client { get; } = client;

    /// <inheritdoc/>
    public bool IsConfigured() => isConfigured;
}
