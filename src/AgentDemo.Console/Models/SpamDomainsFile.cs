// <copyright file="SpamDomainsFile.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace AgentDemo.Console.Models;

using System.Collections.ObjectModel;

/// <summary>
/// Represents the JSON structure for the spam domains file.
/// </summary>
internal sealed class SpamDomainsFile
{
    /// <summary>
    /// Gets or sets the list of known spam domains.
    /// </summary>
    public Collection<SpamDomain> Domains { get; set; } = [];
}
