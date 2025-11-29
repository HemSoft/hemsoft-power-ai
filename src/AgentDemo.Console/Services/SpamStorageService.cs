// <copyright file="SpamStorageService.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace AgentDemo.Console.Services;

using System.Text.Json;

using AgentDemo.Console.Configuration;
using AgentDemo.Console.Models;

/// <summary>
/// Service for managing spam domain and candidate storage in JSON files.
/// </summary>
internal sealed class SpamStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string spamDomainsPath;
    private readonly string spamCandidatesPath;
    private readonly object lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SpamStorageService"/> class.
    /// </summary>
    /// <param name="settings">The spam filter settings.</param>
    public SpamStorageService(SpamFilterSettings settings)
    {
        var baseDir = AppContext.BaseDirectory;
        this.spamDomainsPath = Path.Combine(baseDir, settings.SpamDomainsFilePath);
        this.spamCandidatesPath = Path.Combine(baseDir, settings.SpamCandidatesFilePath);

        this.EnsureFilesExist();
    }

    /// <summary>
    /// Gets all known spam domains.
    /// </summary>
    /// <returns>A list of spam domains.</returns>
    public List<SpamDomain> GetSpamDomains()
    {
        lock (this.lockObject)
        {
            var json = File.ReadAllText(this.spamDomainsPath);
            var file = JsonSerializer.Deserialize<SpamDomainsFile>(json, JsonOptions);
            return file?.Domains.ToList() ?? [];
        }
    }

    /// <summary>
    /// Checks if a domain is in the known spam list.
    /// </summary>
    /// <param name="domain">The domain to check.</param>
    /// <returns>True if the domain is known spam, false otherwise.</returns>
    public bool IsKnownSpamDomain(string domain)
    {
        var domains = this.GetSpamDomains();
        return domains.Exists(d => d.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Adds a domain to the known spam list.
    /// </summary>
    /// <param name="domain">The domain to add.</param>
    /// <param name="reason">Optional reason for marking as spam.</param>
    /// <returns>True if added, false if already exists.</returns>
    public bool AddSpamDomain(string domain, string? reason = null)
    {
        lock (this.lockObject)
        {
            var json = File.ReadAllText(this.spamDomainsPath);
            var file = JsonSerializer.Deserialize<SpamDomainsFile>(json, JsonOptions) ?? new SpamDomainsFile();

            if (file.Domains.Any(d => d.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            file.Domains.Add(new SpamDomain
            {
                Domain = domain.ToUpperInvariant(),
                AddedAt = DateTime.UtcNow,
                Reason = reason,
            });

            File.WriteAllText(this.spamDomainsPath, JsonSerializer.Serialize(file, JsonOptions));
            return true;
        }
    }

    /// <summary>
    /// Gets all spam candidates awaiting review.
    /// </summary>
    /// <returns>A list of spam candidates.</returns>
    public List<SpamCandidate> GetSpamCandidates()
    {
        lock (this.lockObject)
        {
            var json = File.ReadAllText(this.spamCandidatesPath);
            var file = JsonSerializer.Deserialize<SpamCandidatesFile>(json, JsonOptions);
            return file?.Candidates.ToList() ?? [];
        }
    }

    /// <summary>
    /// Adds a spam candidate for later review.
    /// </summary>
    /// <param name="candidate">The candidate to add.</param>
    /// <returns>True if added, false if a candidate with the same message ID already exists.</returns>
    public bool AddSpamCandidate(SpamCandidate candidate)
    {
        lock (this.lockObject)
        {
            var json = File.ReadAllText(this.spamCandidatesPath);
            var file = JsonSerializer.Deserialize<SpamCandidatesFile>(json, JsonOptions) ?? new SpamCandidatesFile();

            if (file.Candidates.Any(c => c.MessageId == candidate.MessageId))
            {
                return false;
            }

            file.Candidates.Add(candidate);
            File.WriteAllText(this.spamCandidatesPath, JsonSerializer.Serialize(file, JsonOptions));
            return true;
        }
    }

    /// <summary>
    /// Removes a spam candidate after it has been processed.
    /// </summary>
    /// <param name="messageId">The message ID to remove.</param>
    /// <returns>True if removed, false if not found.</returns>
    public bool RemoveSpamCandidate(string messageId)
    {
        lock (this.lockObject)
        {
            var json = File.ReadAllText(this.spamCandidatesPath);
            var file = JsonSerializer.Deserialize<SpamCandidatesFile>(json, JsonOptions) ?? new SpamCandidatesFile();

            var toRemove = file.Candidates.Where(c => c.MessageId == messageId).ToList();

            if (toRemove.Count > 0)
            {
                foreach (var item in toRemove)
                {
                    file.Candidates.Remove(item);
                }

                File.WriteAllText(this.spamCandidatesPath, JsonSerializer.Serialize(file, JsonOptions));
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Clears all spam candidates (after they have been processed).
    /// </summary>
    public void ClearSpamCandidates()
    {
        lock (this.lockObject)
        {
            var file = new SpamCandidatesFile();
            File.WriteAllText(this.spamCandidatesPath, JsonSerializer.Serialize(file, JsonOptions));
        }
    }

    /// <summary>
    /// Gets spam candidates grouped by sender domain.
    /// </summary>
    /// <returns>A dictionary of domain to candidates.</returns>
    public Dictionary<string, List<SpamCandidate>> GetCandidatesGroupedByDomain()
    {
        var candidates = this.GetSpamCandidates();
        return candidates
            .GroupBy(c => c.SenderDomain.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private void EnsureFilesExist()
    {
        var domainsDir = Path.GetDirectoryName(this.spamDomainsPath);
        var candidatesDir = Path.GetDirectoryName(this.spamCandidatesPath);

        if (!string.IsNullOrEmpty(domainsDir) && !Directory.Exists(domainsDir))
        {
            Directory.CreateDirectory(domainsDir);
        }

        if (!string.IsNullOrEmpty(candidatesDir) && !Directory.Exists(candidatesDir))
        {
            Directory.CreateDirectory(candidatesDir);
        }

        if (!File.Exists(this.spamDomainsPath))
        {
            File.WriteAllText(this.spamDomainsPath, JsonSerializer.Serialize(new SpamDomainsFile(), JsonOptions));
        }

        if (!File.Exists(this.spamCandidatesPath))
        {
            File.WriteAllText(this.spamCandidatesPath, JsonSerializer.Serialize(new SpamCandidatesFile(), JsonOptions));
        }
    }
}
