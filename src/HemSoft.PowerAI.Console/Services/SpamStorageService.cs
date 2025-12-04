// <copyright file="SpamStorageService.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Services;

using System.Text.Json;

using HemSoft.PowerAI.Console.Configuration;
using HemSoft.PowerAI.Console.Models;

/// <summary>
/// Service for managing spam domain and candidate storage in JSON files.
/// </summary>
internal sealed class SpamStorageService
{
    private static readonly string DefaultAppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HemSoft.PowerAI");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string dataDirectory;
    private readonly string spamDomainsPath;
    private readonly string spamCandidatesPath;
    private readonly Lock lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SpamStorageService"/> class.
    /// Uses the user's AppData folder for persistent storage.
    /// </summary>
    /// <param name="settings">The spam filter settings.</param>
    public SpamStorageService(SpamFilterSettings settings)
        : this(settings, DefaultAppDataDir)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpamStorageService"/> class.
    /// Allows specifying a custom directory for testing.
    /// </summary>
    /// <param name="settings">The spam filter settings.</param>
    /// <param name="dataDirectory">The directory to store data files in.</param>
    internal SpamStorageService(SpamFilterSettings settings, string dataDirectory)
    {
        this.dataDirectory = dataDirectory;
        this.spamDomainsPath = Path.Combine(dataDirectory, Path.GetFileName(settings.SpamDomainsFilePath));
        this.spamCandidatesPath = Path.Combine(dataDirectory, Path.GetFileName(settings.SpamCandidatesFilePath));

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
        return domains.Exists(d => string.Equals(d.Domain, domain, StringComparison.OrdinalIgnoreCase));
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

            if (file.Domains.Any(d => string.Equals(d.Domain, domain, StringComparison.OrdinalIgnoreCase)))
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

            if (file.Candidates.Any(c => string.Equals(c.MessageId, candidate.MessageId, StringComparison.Ordinal)))
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

            var toRemove = file.Candidates.Where(c => string.Equals(c.MessageId, messageId, StringComparison.Ordinal)).ToList();

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
            .GroupBy(c => c.SenderDomain.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    private void EnsureFilesExist()
    {
        if (!Directory.Exists(this.dataDirectory))
        {
            Directory.CreateDirectory(this.dataDirectory);
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
