// <copyright file="OpenRouterModelService.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Services;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Fetches model metadata from OpenRouter API.
/// </summary>
internal sealed class OpenRouterModelService : IDisposable
{
    private const string ModelsApiUrlEnvVar = "OPENROUTER_MODELS_URL";

    private static readonly Uri DefaultModelsApiUri = new("https://openrouter.ai/api/v1/models");

    private readonly HttpClient httpClient = new();
    private readonly string modelId;
    private readonly Uri modelsApiUri;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenRouterModelService"/> class.
    /// </summary>
    /// <param name="modelId">The model ID to fetch info for.</param>
    public OpenRouterModelService(string modelId)
    {
        this.modelId = modelId;
        var envUrl = Environment.GetEnvironmentVariable(ModelsApiUrlEnvVar);
        this.modelsApiUri = string.IsNullOrEmpty(envUrl) ? DefaultModelsApiUri : new Uri(envUrl);
    }

    /// <summary>
    /// Gets the cached model info, or null if not yet fetched.
    /// </summary>
    public ModelInfo? Info { get; private set; }

    /// <summary>
    /// Fetches model information from OpenRouter API.
    /// </summary>
    /// <returns>Model info if found, null otherwise.</returns>
    public async Task<ModelInfo?> FetchAsync()
    {
        try
        {
            var response = await this.httpClient.GetFromJsonAsync<ModelsResponse>(this.modelsApiUri).ConfigureAwait(false);

            this.Info = response?.Data?.FirstOrDefault(m =>
                string.Equals(m.Id, this.modelId, StringComparison.OrdinalIgnoreCase));

            return this.Info;
        }
        catch (HttpRequestException)
        {
            // Network error - return null
            return null;
        }
        catch (TaskCanceledException)
        {
            // Timeout - return null
            return null;
        }
    }

    /// <summary>
    /// Clears the cached model info.
    /// </summary>
    public void ClearCache() => this.Info = null;

    /// <inheritdoc/>
    public void Dispose() => this.httpClient.Dispose();

    /// <summary>
    /// Model information from OpenRouter.
    /// </summary>
    internal sealed class ModelInfo
    {
        /// <summary>
        /// Gets the model ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// Gets the model name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the context length in tokens.
        /// </summary>
        [JsonPropertyName("context_length")]
        public int ContextLength { get; init; }

        /// <summary>
        /// Gets the pricing information.
        /// </summary>
        [JsonPropertyName("pricing")]
        public PricingInfo? Pricing { get; init; }
    }

    /// <summary>
    /// Pricing information for a model.
    /// </summary>
    internal sealed class PricingInfo
    {
        /// <summary>
        /// Gets the prompt price per token.
        /// </summary>
        [JsonPropertyName("prompt")]
        public string Prompt { get; init; } = "0";

        /// <summary>
        /// Gets the completion price per token.
        /// </summary>
        [JsonPropertyName("completion")]
        public string Completion { get; init; } = "0";
    }

    /// <summary>
    /// Response from OpenRouter models API.
    /// </summary>
    private sealed class ModelsResponse
    {
        [JsonPropertyName("data")]
        public List<ModelInfo>? Data { get; init; }
    }
}
