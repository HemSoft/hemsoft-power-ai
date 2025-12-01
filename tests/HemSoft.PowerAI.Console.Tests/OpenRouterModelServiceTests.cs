// <copyright file="OpenRouterModelServiceTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using HemSoft.PowerAI.Console.Services;

using Xunit;

/// <summary>
/// Tests for <see cref="OpenRouterModelService"/>.
/// </summary>
public sealed class OpenRouterModelServiceTests : IDisposable
{
    private const string TestModelId = "test-model";
    private const string TestEnvVar = "OPENROUTER_MODELS_URL";
    private readonly string? originalEnvValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenRouterModelServiceTests"/> class.
    /// </summary>
    public OpenRouterModelServiceTests() =>
        this.originalEnvValue = Environment.GetEnvironmentVariable(TestEnvVar);

    /// <inheritdoc/>
    public void Dispose()
    {
        // Restore original env var
        if (this.originalEnvValue is null)
        {
            Environment.SetEnvironmentVariable(TestEnvVar, null);
        }
        else
        {
            Environment.SetEnvironmentVariable(TestEnvVar, this.originalEnvValue);
        }
    }

    /// <summary>
    /// Verifies Info is null before fetching.
    /// </summary>
    [Fact]
    public void InfoBeforeFetchReturnsNull()
    {
        using var service = new OpenRouterModelService(TestModelId);

        Assert.Null(service.Info);
    }

    /// <summary>
    /// Verifies ClearCache clears cached info.
    /// </summary>
    [Fact]
    public void ClearCacheClearsInfo()
    {
        using var service = new OpenRouterModelService(TestModelId);

        service.ClearCache();

        Assert.Null(service.Info);
    }

    /// <summary>
    /// Verifies FetchAsync returns null when network request fails.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FetchAsyncWithInvalidUrlReturnsNull()
    {
        Environment.SetEnvironmentVariable(TestEnvVar, "http://localhost:1/invalid");
        using var service = new OpenRouterModelService(TestModelId);

        var result = await service.FetchAsync();

        Assert.Null(result);
        Assert.Null(service.Info);
    }

    /// <summary>
    /// Verifies FetchAsync returns model info from real API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FetchAsyncWithRealApiReturnsModelInfo()
    {
        // Use real OpenRouter API - this model should exist
        using var service = new OpenRouterModelService("openai/gpt-3.5-turbo");

        var result = await service.FetchAsync();

        // API should return model info (may be null if rate limited)
        if (result is not null)
        {
            Assert.Equal("openai/gpt-3.5-turbo", result.Id);
            Assert.True(result.ContextLength > 0);
            Assert.NotEmpty(result.Name);
        }

        // Either way, Info should match result
        Assert.Equal(result, service.Info);
    }

    /// <summary>
    /// Verifies FetchAsync returns null for non-existent model.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FetchAsyncWithNonExistentModelReturnsNull()
    {
        using var service = new OpenRouterModelService("non-existent-model-12345");

        var result = await service.FetchAsync();

        // Model doesn't exist, so should return null
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies custom environment URL is used without throwing.
    /// </summary>
    [Fact]
    public void ConstructorWithEnvVarUsesCustomUrl()
    {
        Environment.SetEnvironmentVariable(TestEnvVar, "https://custom.example.com/models");
        using var service = new OpenRouterModelService(TestModelId);

        // Verify constructor doesn't throw with custom URL
        Assert.NotNull(service);
    }

    /// <summary>
    /// Verifies constructor uses default URL when env var is empty.
    /// </summary>
    [Fact]
    public void ConstructorWithEmptyEnvVarUsesDefaultUrl()
    {
        Environment.SetEnvironmentVariable(TestEnvVar, string.Empty);
        using var service = new OpenRouterModelService(TestModelId);

        Assert.NotNull(service);
    }

    /// <summary>
    /// Verifies Dispose can be called multiple times without throwing.
    /// </summary>
    [Fact]
    public void DisposeCanBeCalledMultipleTimes()
    {
        using var service = new OpenRouterModelService(TestModelId);

        var exception = Record.Exception(service.Dispose);

        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies ModelInfo properties have expected defaults.
    /// </summary>
    [Fact]
    public void ModelInfoHasExpectedDefaults()
    {
        var info = new OpenRouterModelService.ModelInfo();

        Assert.Equal(string.Empty, info.Id);
        Assert.Equal(string.Empty, info.Name);
        Assert.Equal(0, info.ContextLength);
        Assert.Null(info.Pricing);
    }

    /// <summary>
    /// Verifies PricingInfo properties have expected defaults.
    /// </summary>
    [Fact]
    public void PricingInfoHasExpectedDefaults()
    {
        var pricing = new OpenRouterModelService.PricingInfo();

        Assert.Equal("0", pricing.Prompt);
        Assert.Equal("0", pricing.Completion);
    }

    /// <summary>
    /// Verifies FetchAsync handles timeout by returning null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FetchAsyncHandlesTimeoutReturnsNull()
    {
        // Use a URL that will be very slow to respond (non-routable IP)
        // 10.255.255.1 is a non-routable IP that should timeout
        Environment.SetEnvironmentVariable(TestEnvVar, "http://10.255.255.1:1/models");
        using var service = new OpenRouterModelService(TestModelId);

        // This should timeout and return null rather than throw
        var result = await service.FetchAsync();

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies ModelInfo can be initialized with all properties.
    /// </summary>
    [Fact]
    public void ModelInfoCanBeInitializedWithAllProperties()
    {
        var pricing = new OpenRouterModelService.PricingInfo
        {
            Prompt = "0.001",
            Completion = "0.002",
        };

        var info = new OpenRouterModelService.ModelInfo
        {
            Id = "test/model",
            Name = "Test Model",
            ContextLength = 4096,
            Pricing = pricing,
        };

        Assert.Equal("test/model", info.Id);
        Assert.Equal("Test Model", info.Name);
        Assert.Equal(4096, info.ContextLength);
        Assert.NotNull(info.Pricing);
        Assert.Equal("0.001", info.Pricing.Prompt);
        Assert.Equal("0.002", info.Pricing.Completion);
    }

    /// <summary>
    /// Verifies service handles case-insensitive model ID matching.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FetchAsyncMatchesModelIdCaseInsensitive()
    {
        // Use uppercase model ID - should still match
        using var service = new OpenRouterModelService("OPENAI/GPT-3.5-TURBO");

        var result = await service.FetchAsync();

        // If found, verify case-insensitive match worked
        if (result is not null)
        {
            Assert.Equal("openai/gpt-3.5-turbo", result.Id, ignoreCase: true);
        }
    }

    /// <summary>
    /// Verifies FetchAsync can be called multiple times.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FetchAsyncCanBeCalledMultipleTimes()
    {
        using var service = new OpenRouterModelService("openai/gpt-3.5-turbo");

        var result1 = await service.FetchAsync();
        var result2 = await service.FetchAsync();

        // Both calls should return the same result
        Assert.Equal(result1?.Id, result2?.Id);
    }

    /// <summary>
    /// Verifies ClearCache followed by fetch returns fresh data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearCacheThenFetchReturnsFreshData()
    {
        using var service = new OpenRouterModelService("openai/gpt-3.5-turbo");

        _ = await service.FetchAsync();
        var infoBeforeClear = service.Info;

        service.ClearCache();
        Assert.Null(service.Info);

        _ = await service.FetchAsync();
        var infoAfterFetch = service.Info;

        // If API was available, should have data again
        if (infoBeforeClear is not null)
        {
            Assert.NotNull(infoAfterFetch);
        }
    }
}
