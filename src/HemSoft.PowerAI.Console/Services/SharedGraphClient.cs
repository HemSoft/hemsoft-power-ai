// <copyright file="SharedGraphClient.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Services;

using System.Diagnostics.CodeAnalysis;

using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

/// <summary>
/// Factory for creating and sharing a single GraphServiceClient instance.
/// Ensures only one device code authentication flow is triggered.
/// Uses MSAL directly with proper cross-platform token cache persistence.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Authentication infrastructure requires interactive login")]
internal sealed class SharedGraphClient : IDisposable
{
    /// <summary>
    /// The environment variable name for the Graph client ID.
    /// </summary>
    public const string ClientIdEnvVar = "GRAPH_CLIENT_ID";

    private const string TenantIdEnvVar = "GRAPH_TENANT_ID";
    private const string AuthorityBaseUrlEnvVar = "GRAPH_AUTHORITY_BASE_URL";
    private const string CacheFileName = "hemsoft_powerai_graph.cache";

    private static readonly Uri DefaultAuthorityBaseUri = new("https://login.microsoftonline.com/");
    private static readonly string[] Scopes = ["User.Read", "Mail.Read", "Mail.ReadWrite", "Mail.Send"];

    private static readonly object LockObject = new();
    private static SharedGraphClient? instance;

    private readonly GraphServiceClient? graphClient;
    private bool disposed;

    private SharedGraphClient()
    {
        var clientId = GetEnvironmentVariable(ClientIdEnvVar);
        if (string.IsNullOrEmpty(clientId))
        {
            this.graphClient = null;
            return;
        }

        var tenantId = GetEnvironmentVariable(TenantIdEnvVar) ?? "consumers";
        var authorityBaseUrlEnv = GetEnvironmentVariable(AuthorityBaseUrlEnvVar);
        var authorityBaseUri = string.IsNullOrEmpty(authorityBaseUrlEnv)
            ? DefaultAuthorityBaseUri
            : new Uri(authorityBaseUrlEnv);
        var authority = new Uri(authorityBaseUri, tenantId);

        // Build MSAL public client with proper configuration
        var msalClient = PublicClientApplicationBuilder.Create(clientId)
            .WithAuthority(authority)
            .WithDefaultRedirectUri()
            .Build();

        // Set up cross-platform token cache persistence
        SetupTokenCacheAsync(msalClient).GetAwaiter().GetResult();

        // Create a custom token provider that uses MSAL with persistent cache
        var tokenProvider = new MsalTokenProvider(msalClient, Scopes);
        this.graphClient = new GraphServiceClient(tokenProvider);
    }

    /// <summary>
    /// Gets the shared GraphServiceClient instance.
    /// Returns null if GRAPH_CLIENT_ID is not configured.
    /// </summary>
    /// <returns>The shared GraphServiceClient or null.</returns>
    public static GraphServiceClient? GetClient()
    {
        var current = instance;
        if (current is not null)
        {
            return current.graphClient;
        }

        lock (LockObject)
        {
            instance ??= new SharedGraphClient();
            return instance.graphClient;
        }
    }

    /// <summary>
    /// Gets whether the Graph client is configured (GRAPH_CLIENT_ID is set).
    /// </summary>
    /// <returns>True if configured, false otherwise.</returns>
    public static bool IsConfigured()
    {
        var clientId = GetEnvironmentVariable(ClientIdEnvVar);
        return !string.IsNullOrEmpty(clientId);
    }

    /// <summary>
    /// Resets the singleton instance. Used for testing.
    /// </summary>
    public static void Reset()
    {
        lock (LockObject)
        {
            instance?.Dispose();
            instance = null;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.graphClient?.Dispose();
        this.disposed = true;
    }

    private static async Task SetupTokenCacheAsync(IPublicClientApplication msalClient)
    {
        // Get a platform-appropriate cache directory
        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HemSoft.PowerAI");

        Directory.CreateDirectory(cacheDir);

        var storageProperties = new StorageCreationPropertiesBuilder(CacheFileName, cacheDir)
            .WithUnprotectedFile() // Cross-platform compatible, uses ACL protection on Windows
            .Build();

        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties).ConfigureAwait(false);
        cacheHelper.RegisterCache(msalClient.UserTokenCache);
    }

    private static string? GetEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Fall back to user-level registry (handles VS Code terminal inheritance issues)
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
    }

    /// <summary>
    /// Custom token provider that uses MSAL for authentication with persistent caching.
    /// </summary>
    private sealed class MsalTokenProvider(IPublicClientApplication msalClient, string[] scopes)
        : Azure.Core.TokenCredential
    {
        public override Azure.Core.AccessToken GetToken(
            Azure.Core.TokenRequestContext requestContext,
            CancellationToken cancellationToken) =>
            this.GetTokenAsync(requestContext, cancellationToken).AsTask().GetAwaiter().GetResult();

        public override async ValueTask<Azure.Core.AccessToken> GetTokenAsync(
            Azure.Core.TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            var effectiveScopes = requestContext.Scopes.Length > 0 ? requestContext.Scopes : scopes;

            // Try to get token silently first (from cache)
            var accounts = await msalClient.GetAccountsAsync().ConfigureAwait(false);
            var account = accounts.FirstOrDefault();

            AuthenticationResult? result = null;

            if (account is not null)
            {
                try
                {
                    result = await msalClient.AcquireTokenSilent(effectiveScopes, account)
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (MsalUiRequiredException)
                {
                    // Token expired or requires interaction, fall through to device code flow
                }
            }

            // If silent acquisition failed, use device code flow
            result ??= await msalClient.AcquireTokenWithDeviceCode(effectiveScopes, deviceCodeResult =>
            {
                System.Console.WriteLine(deviceCodeResult.Message);
                return Task.CompletedTask;
            }).ExecuteAsync(cancellationToken).ConfigureAwait(false);

            return new Azure.Core.AccessToken(result.AccessToken, result.ExpiresOn);
        }
    }
}
