// <copyright file="EmailEvaluation.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Models;

/// <summary>
/// Represents the result of evaluating an email for spam.
/// </summary>
internal sealed record EmailEvaluation
{
    /// <summary>
    /// Gets the message ID for the email.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the sender's email address.
    /// </summary>
    public required string Sender { get; init; }

    /// <summary>
    /// Gets the email subject.
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Gets the verdict (e.g., "Legitimate", "Known Spam", "Candidate", "Junked").
    /// </summary>
    public required string Verdict { get; init; }

    /// <summary>
    /// Gets the brief reason for the verdict.
    /// </summary>
    public string? Reason { get; init; }
}
