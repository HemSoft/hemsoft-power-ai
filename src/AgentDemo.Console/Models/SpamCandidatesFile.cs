// <copyright file="SpamCandidatesFile.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace AgentDemo.Console.Models;

using System.Collections.ObjectModel;

/// <summary>
/// Represents the JSON structure for the spam candidates file.
/// </summary>
internal sealed class SpamCandidatesFile
{
    /// <summary>
    /// Gets or sets the list of spam candidates awaiting review.
    /// </summary>
    public Collection<SpamCandidate> Candidates { get; set; } = [];
}
