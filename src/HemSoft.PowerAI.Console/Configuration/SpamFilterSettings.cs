// <copyright file="SpamFilterSettings.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Configuration;

/// <summary>
/// Configuration settings for the spam filter agent.
/// </summary>
internal sealed class SpamFilterSettings
{
    /// <summary>
    /// The configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "SpamFilter";

    /// <summary>
    /// Gets or sets the number of emails to process in each batch.
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the delay in seconds between processing batches.
    /// </summary>
    public int DelayBetweenBatchesSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the file path for storing known spam domains.
    /// </summary>
    public string SpamDomainsFilePath { get; set; } = "Data/SpamDomains.json";

    /// <summary>
    /// Gets or sets the file path for storing spam candidates awaiting review.
    /// </summary>
    public string SpamCandidatesFilePath { get; set; } = "Data/SpamCandidates.json";

    /// <summary>
    /// Gets or sets the file path for storing domains pending human review.
    /// </summary>
    public string HumanReviewFilePath { get; set; } = "Data/HumanReview.json";

    /// <summary>
    /// Gets or sets the number of domains to display per batch during human review.
    /// </summary>
    public int ReviewBatchSize { get; set; } = 20;
}
