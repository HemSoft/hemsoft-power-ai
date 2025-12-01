// <copyright file="HumanReviewFile.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Models;

using System.Collections.ObjectModel;

/// <summary>
/// Represents the JSON structure for the human review file.
/// </summary>
internal sealed class HumanReviewFile
{
    /// <summary>
    /// Gets or sets the list of domains pending human review.
    /// </summary>
    public Collection<HumanReviewDomain> Domains { get; set; } = [];
}
