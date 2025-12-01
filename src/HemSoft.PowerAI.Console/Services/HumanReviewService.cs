// <copyright file="HumanReviewService.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Services;

using System.Text.Json;

using HemSoft.PowerAI.Console.Configuration;
using HemSoft.PowerAI.Console.Models;

/// <summary>
/// Service for managing domains pending human review in a JSON file.
/// </summary>
internal sealed class HumanReviewService
{
    private const int MaxSamplesPerDomain = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string humanReviewPath;
    private readonly object lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HumanReviewService"/> class.
    /// </summary>
    /// <param name="settings">The spam filter settings.</param>
    public HumanReviewService(SpamFilterSettings settings)
    {
        var baseDir = AppContext.BaseDirectory;
        this.humanReviewPath = Path.Combine(baseDir, settings.HumanReviewFilePath);
        this.EnsureFileExists();
    }

    /// <summary>
    /// Gets all domains pending human review.
    /// </summary>
    /// <returns>A list of domains pending review.</returns>
    public List<HumanReviewDomain> GetPendingDomains()
    {
        lock (this.lockObject)
        {
            var json = File.ReadAllText(this.humanReviewPath);
            var file = JsonSerializer.Deserialize<HumanReviewFile>(json, JsonOptions);
            return file?.Domains.ToList() ?? [];
        }
    }

    /// <summary>
    /// Gets the count of domains pending review.
    /// </summary>
    /// <returns>The number of domains pending review.</returns>
    public int GetPendingCount()
    {
        lock (this.lockObject)
        {
            var json = File.ReadAllText(this.humanReviewPath);
            var file = JsonSerializer.Deserialize<HumanReviewFile>(json, JsonOptions);
            return file?.Domains.Count ?? 0;
        }
    }

    /// <summary>
    /// Adds or updates a domain in the review queue with sample email information.
    /// </summary>
    /// <param name="domain">The domain name.</param>
    /// <param name="messageId">The message ID of the sample email.</param>
    /// <param name="senderEmail">The sender's email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="reason">The reason this email was flagged.</param>
    /// <returns>True if this is a new domain, false if it was updated.</returns>
    public bool AddOrUpdateDomain(string domain, string messageId, string senderEmail, string subject, string? reason)
    {
        lock (this.lockObject)
        {
            var json = File.ReadAllText(this.humanReviewPath);
            var file = JsonSerializer.Deserialize<HumanReviewFile>(json, JsonOptions) ?? new HumanReviewFile();

            var normalizedDomain = domain.ToUpperInvariant();
            var existing = file.Domains.FirstOrDefault(d =>
                d.Domain.Equals(normalizedDomain, StringComparison.OrdinalIgnoreCase));

            var isNew = existing is null;
            var now = DateTime.UtcNow;

            if (isNew)
            {
                existing = new HumanReviewDomain
                {
                    Domain = normalizedDomain,
                    EmailCount = 0,
                    FirstSeen = now,
                };
                file.Domains.Add(existing);
            }

            existing!.EmailCount++;
            existing.LastSeen = now;

            // Add sample if we have room and this message isn't already in samples
            if (existing.Samples.Count < MaxSamplesPerDomain &&
                !existing.Samples.Any(s => s.MessageId == messageId))
            {
                existing.Samples.Add(new HumanReviewSample
                {
                    MessageId = messageId,
                    Sender = senderEmail,
                    Subject = subject,
                    Reason = reason,
                });
            }

            File.WriteAllText(this.humanReviewPath, JsonSerializer.Serialize(file, JsonOptions));
            return isNew;
        }
    }

    /// <summary>
    /// Removes a domain from the review queue (after processing).
    /// </summary>
    /// <param name="domain">The domain to remove.</param>
    /// <returns>The removed domain info, or null if not found.</returns>
    public HumanReviewDomain? RemoveDomain(string domain)
    {
        lock (this.lockObject)
        {
            var json = File.ReadAllText(this.humanReviewPath);
            var file = JsonSerializer.Deserialize<HumanReviewFile>(json, JsonOptions) ?? new HumanReviewFile();

            var normalizedDomain = domain.ToUpperInvariant();
            var toRemove = file.Domains.FirstOrDefault(d =>
                d.Domain.Equals(normalizedDomain, StringComparison.OrdinalIgnoreCase));

            if (toRemove is not null)
            {
                file.Domains.Remove(toRemove);
                File.WriteAllText(this.humanReviewPath, JsonSerializer.Serialize(file, JsonOptions));
            }

            return toRemove;
        }
    }

    /// <summary>
    /// Removes multiple domains from the review queue.
    /// </summary>
    /// <param name="domains">The domains to remove.</param>
    /// <returns>The count of domains removed.</returns>
    public int RemoveDomains(IEnumerable<string> domains)
    {
        lock (this.lockObject)
        {
            var json = File.ReadAllText(this.humanReviewPath);
            var file = JsonSerializer.Deserialize<HumanReviewFile>(json, JsonOptions) ?? new HumanReviewFile();

            var normalizedDomains = domains.Select(d => d.ToUpperInvariant()).ToHashSet();
            var originalCount = file.Domains.Count;

            var toKeep = file.Domains
                .Where(d => !normalizedDomains.Contains(d.Domain.ToUpperInvariant()))
                .ToList();

            file.Domains.Clear();
            foreach (var domain in toKeep)
            {
                file.Domains.Add(domain);
            }

            File.WriteAllText(this.humanReviewPath, JsonSerializer.Serialize(file, JsonOptions));
            return originalCount - file.Domains.Count;
        }
    }

    /// <summary>
    /// Clears all domains from the review queue.
    /// </summary>
    public void ClearAll()
    {
        lock (this.lockObject)
        {
            var file = new HumanReviewFile();
            File.WriteAllText(this.humanReviewPath, JsonSerializer.Serialize(file, JsonOptions));
        }
    }

    /// <summary>
    /// Checks if a domain is already in the review queue.
    /// </summary>
    /// <param name="domain">The domain to check.</param>
    /// <returns>True if the domain is pending review, false otherwise.</returns>
    public bool IsPendingReview(string domain)
    {
        var domains = this.GetPendingDomains();
        return domains.Exists(d => d.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));
    }

    private void EnsureFileExists()
    {
        var dir = Path.GetDirectoryName(this.humanReviewPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (!File.Exists(this.humanReviewPath))
        {
            File.WriteAllText(this.humanReviewPath, JsonSerializer.Serialize(new HumanReviewFile(), JsonOptions));
        }
    }
}
