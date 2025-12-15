// <copyright file="DefaultGraphClientProvider.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Services;

using Microsoft.Graph;

/// <summary>
/// Default implementation of <see cref="IGraphClientProvider"/> that wraps
/// the <see cref="SharedGraphClient"/> singleton.
/// </summary>
/// <remarks>
/// This is the production implementation used by all Graph API-dependent tools.
/// For unit testing, inject a mock <see cref="IGraphClientProvider"/> instead.
/// </remarks>
internal sealed class DefaultGraphClientProvider : IGraphClientProvider
{
    /// <inheritdoc/>
    public GraphServiceClient? Client => SharedGraphClient.GetClient();

    /// <inheritdoc/>
    public bool IsConfigured() => SharedGraphClient.IsConfigured();
}
