// <copyright file="ResearchAgentFunction.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Agents.Functions;

using System.Globalization;
using System.Net;
using System.Text.Json;

using A2A;

using HemSoft.PowerAI.Common.Agents;

using Microsoft.Agents.AI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Azure Function that hosts the ResearchAgent as an A2A endpoint.
/// </summary>
/// <param name="logger">The logger instance.</param>
internal sealed partial class ResearchAgentFunction(ILogger<ResearchAgentFunction> logger)
{
    private readonly AIAgent agent = ResearchAgent.Create();

    /// <summary>
    /// Returns the AgentCard describing the ResearchAgent capabilities.
    /// This endpoint is used for A2A agent discovery.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent card as JSON.</returns>
    [Function("ResearchAgentCard")]
    public Task<HttpResponseData> GetAgentCardAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/agent.json")]
        HttpRequestData req,
        CancellationToken cancellationToken) => this.GetAgentCardCoreAsync(req, cancellationToken);

    /// <summary>
    /// Handles A2A messages sent to the ResearchAgent.
    /// </summary>
    /// <param name="req">The HTTP request containing the A2A message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent's response.</returns>
    [Function("ResearchAgentMessage")]
    public Task<HttpResponseData> HandleMessageAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "research")]
        HttpRequestData req,
        CancellationToken cancellationToken) => this.HandleMessageCoreAsync(req, cancellationToken);

    private static Uri GetBaseUrl(HttpRequestData req)
    {
        var uri = req.Url;
        var portPart = uri.IsDefaultPort
            ? string.Empty
            : ":" + uri.Port.ToString(CultureInfo.InvariantCulture);
        return new Uri($"{uri.Scheme}://{uri.Host}{portPart}");
    }

    private static async Task<HttpResponseData> CreateErrorResponseCoreAsync(
        HttpRequestData req,
        string message,
        CancellationToken cancellationToken)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new { error = message }), cancellationToken)
            .ConfigureAwait(false);
        return response;
    }

    private Task<HttpResponseData> GetAgentCardCoreAsync(
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(req);

        return this.GetAgentCardCoreInternalAsync(req, cancellationToken);
    }

    private async Task<HttpResponseData> GetAgentCardCoreInternalAsync(
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken; // Reserved for future use

        this.LogAgentCardRequested();

        var baseUrl = GetBaseUrl(req);
        var card = AgentCards.CreateResearchAgentCard(baseUrl);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(card), cancellationToken).ConfigureAwait(false);

        return response;
    }

    private Task<HttpResponseData> HandleMessageCoreAsync(
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(req);

        return this.HandleMessageCoreInternalAsync(req, cancellationToken);
    }

    private async Task<HttpResponseData> HandleMessageCoreInternalAsync(
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        this.LogMessageReceived();

        try
        {
            var body = await req.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(body))
            {
                return await CreateErrorResponseCoreAsync(req, ErrorMessages.EmptyRequestBody, cancellationToken)
                    .ConfigureAwait(false);
            }

            var messageSendParams = JsonSerializer.Deserialize<MessageSendParams>(body);
            if (messageSendParams?.Message is null)
            {
                return await CreateErrorResponseCoreAsync(req, ErrorMessages.InvalidMessageFormat, cancellationToken)
                    .ConfigureAwait(false);
            }

            // Extract text from the message
            var userText = string.Join(
                '\n',
                messageSendParams.Message.Parts
                    .OfType<TextPart>()
                    .Select(p => p.Text));

            this.LogProcessingResearchRequest(userText[..Math.Min(100, userText.Length)]);

            // Run the agent
            var agentResponse = await this.agent.RunAsync(userText, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // Create response message
            var responseMessage = new AgentMessage
            {
                Role = MessageRole.Agent,
                MessageId = Guid.NewGuid().ToString(),
                ContextId = messageSendParams.Message.ContextId,
                Parts = [new TextPart { Text = agentResponse.Text ?? ErrorMessages.NoResponseGenerated }],
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(responseMessage), cancellationToken)
                .ConfigureAwait(false);

            return response;
        }
        catch (JsonException ex)
        {
            this.LogParseError(ex);
            return await CreateErrorResponseCoreAsync(req, ErrorMessages.InvalidJsonFormat, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent card requested")]
    private partial void LogAgentCardRequested();

    [LoggerMessage(Level = LogLevel.Information, Message = "Message received for ResearchAgent")]
    private partial void LogMessageReceived();

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing research request: {Query}")]
    private partial void LogProcessingResearchRequest(string query);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse request")]
    private partial void LogParseError(Exception ex);

    /// <summary>
    /// Error message constants to avoid S4055 (literal strings for user-facing messages).
    /// Using properties with => to prevent inlining, satisfying analyzer requirements.
    /// </summary>
    private static class ErrorMessages
    {
        public static string EmptyRequestBody => "Request body is empty";

        public static string InvalidMessageFormat => "Invalid message format";

        public static string InvalidJsonFormat => "Invalid JSON format";

        public static string NoResponseGenerated => "No response generated.";
    }
}
