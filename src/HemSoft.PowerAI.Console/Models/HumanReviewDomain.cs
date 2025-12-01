// <copyright file="HumanReviewDomain.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Models;

using System.Collections.ObjectModel;

/// <summary>
/// Represents a domain pending human review with sample emails.
/// </summary>
internal sealed class HumanReviewDomain
{
    /// <summary>
    /// Gets or sets the domain name (e.g., "SPAMMER.COM").
    /// </summary>
    public required string Domain { get; set; }

    /// <summary>
    /// Gets or sets the total count of emails from this domain.
    /// </summary>
    public int EmailCount { get; set; }

    /// <summary>
    /// Gets or sets sample emails from this domain for review.
    /// </summary>
    public Collection<HumanReviewSample> Samples { get; set; } = [];

    /// <summary>
    /// Gets or sets when this domain was first seen.
    /// </summary>
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this domain was last seen.
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}
