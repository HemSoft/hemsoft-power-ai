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
    private readonly Uri modelsApiUri;

    private List<ModelInfo>? allModels;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenRouterModelService"/> class.
    /// </summary>
    /// <param name="modelId">The model ID to fetch info for.</param>
    public OpenRouterModelService(string modelId)
    {
        this.ModelId = modelId;
        var envUrl = Environment.GetEnvironmentVariable(ModelsApiUrlEnvVar);
        this.modelsApiUri = string.IsNullOrEmpty(envUrl) ? DefaultModelsApiUri : new Uri(envUrl);
    }

    /// <summary>
    /// Gets or sets the current model ID.
    /// </summary>
    public string ModelId { get; set; }

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
            this.allModels = response?.Data;

            this.Info = this.allModels?.Find(m =>
                string.Equals(m.Id, this.ModelId, StringComparison.OrdinalIgnoreCase));

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
    /// Gets all available models, filtering by optional criteria.
    /// </summary>
    /// <param name="supportsTools">If true, only return models that support tool calling.</param>
    /// <returns>List of matching models.</returns>
    public IReadOnlyList<ModelInfo> GetAvailableModels(bool supportsTools = false)
    {
        if (this.allModels is null)
        {
            return [];
        }

        var models = this.allModels
            .Where(m => !string.IsNullOrEmpty(m.Name))
            .OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase);

        if (supportsTools)
        {
            models = models.Where(m => m.SupportsTools).OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase);
        }

        return [.. models];
    }

    /// <summary>
    /// Changes the current model by ID.
    /// </summary>
    /// <param name="modelId">The new model ID.</param>
    /// <returns>True if the model was found, false otherwise.</returns>
    public bool SetModel(string modelId)
    {
        var model = this.allModels?.Find(m =>
            string.Equals(m.Id, modelId, StringComparison.OrdinalIgnoreCase));

        if (model is null)
        {
            return false;
        }

        this.ModelId = modelId;
        this.Info = model;
        return true;
    }

    /// <summary>
    /// Clears the cached model info.
    /// </summary>
    public void ClearCache()
    {
        this.Info = null;
        this.allModels = null;
    }

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
        /// Gets the model description.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; init; }

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

        /// <summary>
        /// Gets the model architecture information.
        /// </summary>
        [JsonPropertyName("architecture")]
        public ArchitectureInfo? Architecture { get; init; }

        /// <summary>
        /// Gets the top provider information.
        /// </summary>
        [JsonPropertyName("top_provider")]
        public TopProviderInfo? TopProvider { get; init; }

        /// <summary>
        /// Gets the supported parameters.
        /// </summary>
        [JsonPropertyName("supported_parameters")]
        public List<string>? SupportedParameters { get; init; }

        /// <summary>
        /// Gets a value indicating whether this model supports tool/function calling.
        /// </summary>
        public bool SupportsTools =>
            this.SupportedParameters?.Contains("tools", StringComparer.OrdinalIgnoreCase) == true;

        /// <summary>
        /// Gets a value indicating whether this model supports reasoning mode.
        /// </summary>
        public bool SupportsReasoning =>
            this.SupportedParameters?.Contains("reasoning", StringComparer.OrdinalIgnoreCase) == true;

        /// <summary>
        /// Gets the max completion tokens from top provider info.
        /// </summary>
        public int? MaxCompletionTokens => this.TopProvider?.MaxCompletionTokens;

        /// <summary>
        /// Gets the modality (e.g., "text->text", "text+image->text").
        /// </summary>
        public string Modality => this.Architecture?.Modality ?? "text->text";

        /// <summary>
        /// Gets the input modalities supported.
        /// </summary>
        public IReadOnlyList<string> InputModalities =>
            this.Architecture?.InputModalities ?? ["text"];

        /// <summary>
        /// Gets the output modalities supported.
        /// </summary>
        public IReadOnlyList<string> OutputModalities =>
            this.Architecture?.OutputModalities ?? ["text"];
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
    /// Architecture information for a model.
    /// </summary>
    internal sealed class ArchitectureInfo
    {
        /// <summary>
        /// Gets the modality (e.g., "text->text", "text+image->text").
        /// </summary>
        [JsonPropertyName("modality")]
        public string? Modality { get; init; }

        /// <summary>
        /// Gets the input modalities.
        /// </summary>
        [JsonPropertyName("input_modalities")]
        public List<string>? InputModalities { get; init; }

        /// <summary>
        /// Gets the output modalities.
        /// </summary>
        [JsonPropertyName("output_modalities")]
        public List<string>? OutputModalities { get; init; }

        /// <summary>
        /// Gets the tokenizer type.
        /// </summary>
        [JsonPropertyName("tokenizer")]
        public string? Tokenizer { get; init; }
    }

    /// <summary>
    /// Top provider information for a model.
    /// </summary>
    internal sealed class TopProviderInfo
    {
        /// <summary>
        /// Gets the context length.
        /// </summary>
        [JsonPropertyName("context_length")]
        public int? ContextLength { get; init; }

        /// <summary>
        /// Gets the max completion tokens.
        /// </summary>
        [JsonPropertyName("max_completion_tokens")]
        public int? MaxCompletionTokens { get; init; }

        /// <summary>
        /// Gets a value indicating whether the model is moderated.
        /// </summary>
        [JsonPropertyName("is_moderated")]
        public bool IsModerated { get; init; }
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
