// <copyright file="SpamCandidate.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace AgentDemo.Console.Models;

/// <summary>
/// Represents an email identified as a potential spam candidate awaiting human review.
/// </summary>
internal sealed class SpamCandidate
{
    /// <summary>
    /// Gets or sets the unique message ID from Outlook.
    /// </summary>
    public required string MessageId { get; set; }

    /// <summary>
    /// Gets or sets the sender's email address.
    /// </summary>
    public required string SenderEmail { get; set; }

    /// <summary>
    /// Gets or sets the sender's domain extracted from the email address.
    /// </summary>
    public required string SenderDomain { get; set; }

    /// <summary>
    /// Gets or sets the email subject.
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// Gets or sets when the email was received.
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// Gets or sets when this candidate was identified.
    /// </summary>
    public DateTime IdentifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the AI's reasoning for flagging this as spam.
    /// </summary>
    public string? SpamReason { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0.0 to 1.0) that this is spam.
    /// </summary>
    public double ConfidenceScore { get; set; }
}
