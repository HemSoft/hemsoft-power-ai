// <copyright file="ChatClientExtensions.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Telemetry;

using Microsoft.Extensions.AI;

/// <summary>
/// Extension methods for adding telemetry middleware to chat clients.
/// </summary>
internal static class ChatClientExtensions
{
    /// <summary>
    /// Adds function call logging middleware to the chat client builder.
    /// Logs all AI function/tool invocations to OpenTelemetry.
    /// </summary>
    /// <param name="builder">The chat client builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static ChatClientBuilder UseFunctionCallLogging(this ChatClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Use(innerClient => new FunctionCallMiddleware(innerClient));
    }
}
