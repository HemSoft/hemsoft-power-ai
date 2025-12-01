// <copyright file="HumanReviewSample.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Models;

/// <summary>
/// Represents a sample email from a domain pending human review.
/// </summary>
internal sealed class HumanReviewSample
{
    /// <summary>
    /// Gets or sets the email subject.
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// Gets or sets the sender's email address.
    /// </summary>
    public required string Sender { get; set; }

    /// <summary>
    /// Gets or sets the message ID for later operations.
    /// </summary>
    public required string MessageId { get; set; }

    /// <summary>
    /// Gets or sets the reason this email was flagged as potential spam.
    /// </summary>
    public string? Reason { get; set; }
}
