// <copyright file="IGraphClientProvider.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Services;

using Microsoft.Graph;

/// <summary>
/// Provides access to a Microsoft Graph client for API operations.
/// </summary>
/// <remarks>
/// This interface abstracts the Graph client access to enable dependency injection
/// and unit testing with mocks. Production code uses <see cref="DefaultGraphClientProvider"/>,
/// while tests can inject mock implementations.
/// </remarks>
internal interface IGraphClientProvider
{
    /// <summary>
    /// Gets the GraphServiceClient instance for making Graph API calls.
    /// </summary>
    GraphServiceClient? Client { get; }

    /// <summary>
    /// Gets a value indicating whether the Graph client is configured.
    /// </summary>
    /// <returns>True if GRAPH_CLIENT_ID is set, false otherwise.</returns>
    bool IsConfigured();
}
