// <copyright file="SpamDomain.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Models;

/// <summary>
/// Represents a known spam domain.
/// </summary>
internal sealed class SpamDomain
{
    /// <summary>
    /// Gets or sets the domain name (e.g., "spammer.com").
    /// </summary>
    public required string Domain { get; set; }

    /// <summary>
    /// Gets or sets the date when this domain was added to the list.
    /// </summary>
    public required DateTimeOffset AddedAt { get; set; }

    /// <summary>
    /// Gets or sets an optional reason for marking this domain as spam.
    /// </summary>
    public string? Reason { get; set; }
}
