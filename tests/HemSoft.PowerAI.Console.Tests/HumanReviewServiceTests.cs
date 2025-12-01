// <copyright file="HumanReviewServiceTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using HemSoft.PowerAI.Console.Configuration;
using HemSoft.PowerAI.Console.Services;

/// <summary>
/// Unit tests for <see cref="HumanReviewService"/>.
/// </summary>
public class HumanReviewServiceTests : IDisposable
{
    private readonly string testDirectory;
    private readonly SpamFilterSettings settings;
    private readonly HumanReviewService sut;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HumanReviewServiceTests"/> class.
    /// </summary>
    public HumanReviewServiceTests()
    {
        this.testDirectory = Path.Combine(Path.GetTempPath(), "HumanReviewServiceTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(this.testDirectory);

        this.settings = new SpamFilterSettings
        {
            HumanReviewFilePath = Path.Combine(this.testDirectory, "human_review.json"),
            SpamDomainsFilePath = Path.Combine(this.testDirectory, "spam_domains.json"),
            SpamCandidatesFilePath = Path.Combine(this.testDirectory, "spam_candidates.json"),
        };

        this.sut = new HumanReviewService(this.settings);
    }

    /// <summary>
    /// Tests that GetPendingDomains returns empty collection when no domains exist.
    /// </summary>
    [Fact]
    public void GetPendingDomainsReturnsEmptyWhenNoDomains()
    {
        // Act
        var domains = this.sut.GetPendingDomains();

        // Assert
        Assert.Empty(domains);
    }

    /// <summary>
    /// Tests that GetPendingCount returns zero when no domains exist.
    /// </summary>
    [Fact]
    public void GetPendingCountReturnsZeroWhenNoDomains()
    {
        // Act
        var count = this.sut.GetPendingCount();

        // Assert
        Assert.Equal(0, count);
    }

    /// <summary>
    /// Tests that AddOrUpdateDomain adds a new domain and returns true.
    /// </summary>
    [Fact]
    public void AddOrUpdateDomainAddsNewDomainReturnsTrue()
    {
        // Arrange
        const string domain = "spam.example.com";
        const string messageId = "msg123";
        const string senderEmail = "spammer@spam.example.com";
        const string subject = "Win a prize!";
        const string reason = "Suspicious content";

        // Act
        var result = this.sut.AddOrUpdateDomain(domain, messageId, senderEmail, subject, reason);

        // Assert
        Assert.True(result);
        var domains = this.sut.GetPendingDomains();
        Assert.Single(domains);
        Assert.Equal("SPAM.EXAMPLE.COM", domains[0].Domain);
        Assert.Equal(1, domains[0].EmailCount);
        Assert.Single(domains[0].Samples);
        Assert.Equal(messageId, domains[0].Samples[0].MessageId);
    }

    /// <summary>
    /// Tests that AddOrUpdateDomain updates existing domain and returns false.
    /// </summary>
    [Fact]
    public void AddOrUpdateDomainUpdatesExistingDomainReturnsFalse()
    {
        // Arrange
        const string domain = "spam.example.com";
        this.sut.AddOrUpdateDomain(domain, "msg1", "a@spam.example.com", "Subject 1", "Reason 1");

        // Act
        var result = this.sut.AddOrUpdateDomain(domain, "msg2", "b@spam.example.com", "Subject 2", "Reason 2");

        // Assert
        Assert.False(result);
        var domains = this.sut.GetPendingDomains();
        Assert.Single(domains);
        Assert.Equal(2, domains[0].EmailCount);
        Assert.Equal(2, domains[0].Samples.Count);
    }

    /// <summary>
    /// Tests that AddOrUpdateDomain limits samples to maximum of 2.
    /// </summary>
    [Fact]
    public void AddOrUpdateDomainLimitsSamplesToMaximum()
    {
        // Arrange
        const string domain = "spam.example.com";
        this.sut.AddOrUpdateDomain(domain, "msg1", "a@spam.example.com", "Subject 1", "Reason 1");
        this.sut.AddOrUpdateDomain(domain, "msg2", "b@spam.example.com", "Subject 2", "Reason 2");

        // Act - add a third message
        this.sut.AddOrUpdateDomain(domain, "msg3", "c@spam.example.com", "Subject 3", "Reason 3");

        // Assert - should still only have 2 samples but count 3
        var domains = this.sut.GetPendingDomains();
        Assert.Single(domains);
        Assert.Equal(3, domains[0].EmailCount);
        Assert.Equal(2, domains[0].Samples.Count);
    }

    /// <summary>
    /// Tests that AddOrUpdateDomain does not add duplicate message ID to samples.
    /// </summary>
    [Fact]
    public void AddOrUpdateDomainDoesNotAddDuplicateMessageIdToSamples()
    {
        // Arrange
        const string domain = "spam.example.com";
        const string messageId = "msg1";
        this.sut.AddOrUpdateDomain(domain, messageId, "a@spam.example.com", "Subject 1", "Reason 1");

        // Act - add same message ID again
        this.sut.AddOrUpdateDomain(domain, messageId, "a@spam.example.com", "Subject 1", "Reason 1");

        // Assert - should have 2 email count but only 1 sample
        var domains = this.sut.GetPendingDomains();
        Assert.Single(domains);
        Assert.Equal(2, domains[0].EmailCount);
        Assert.Single(domains[0].Samples);
    }

    /// <summary>
    /// Tests that RemoveDomain removes an existing domain and returns it.
    /// </summary>
    [Fact]
    public void RemoveDomainRemovesExistingDomainAndReturnsIt()
    {
        // Arrange
        const string domain = "spam.example.com";
        this.sut.AddOrUpdateDomain(domain, "msg1", "a@spam.example.com", "Subject 1", "Reason 1");

        // Act
        var removed = this.sut.RemoveDomain(domain);

        // Assert
        Assert.NotNull(removed);
        Assert.Equal("SPAM.EXAMPLE.COM", removed.Domain);
        Assert.Empty(this.sut.GetPendingDomains());
    }

    /// <summary>
    /// Tests that RemoveDomain returns null for non-existent domain.
    /// </summary>
    [Fact]
    public void RemoveDomainReturnsNullForNonExistentDomain()
    {
        // Act
        var removed = this.sut.RemoveDomain("nonexistent.com");

        // Assert
        Assert.Null(removed);
    }

    /// <summary>
    /// Tests that RemoveDomains removes multiple domains and returns count.
    /// </summary>
    [Fact]
    public void RemoveDomainsRemovesMultipleDomainsAndReturnsCount()
    {
        // Arrange
        this.sut.AddOrUpdateDomain("domain1.com", "msg1", "a@domain1.com", "Subject 1", "Reason 1");
        this.sut.AddOrUpdateDomain("domain2.com", "msg2", "a@domain2.com", "Subject 2", "Reason 2");
        this.sut.AddOrUpdateDomain("domain3.com", "msg3", "a@domain3.com", "Subject 3", "Reason 3");

        // Act
        var removedCount = this.sut.RemoveDomains(["domain1.com", "domain3.com"]);

        // Assert
        Assert.Equal(2, removedCount);
        var remaining = this.sut.GetPendingDomains();
        Assert.Single(remaining);
        Assert.Equal("DOMAIN2.COM", remaining[0].Domain);
    }

    /// <summary>
    /// Tests that RemoveDomains returns zero when no matching domains found.
    /// </summary>
    [Fact]
    public void RemoveDomainsReturnsZeroWhenNoMatchingDomains()
    {
        // Arrange
        this.sut.AddOrUpdateDomain("domain1.com", "msg1", "a@domain1.com", "Subject 1", "Reason 1");

        // Act
        var removedCount = this.sut.RemoveDomains(["nonexistent.com"]);

        // Assert
        Assert.Equal(0, removedCount);
        Assert.Single(this.sut.GetPendingDomains());
    }

    /// <summary>
    /// Tests that ClearAll removes all domains.
    /// </summary>
    [Fact]
    public void ClearAllRemovesAllDomains()
    {
        // Arrange
        this.sut.AddOrUpdateDomain("domain1.com", "msg1", "a@domain1.com", "Subject 1", "Reason 1");
        this.sut.AddOrUpdateDomain("domain2.com", "msg2", "a@domain2.com", "Subject 2", "Reason 2");

        // Act
        this.sut.ClearAll();

        // Assert
        Assert.Empty(this.sut.GetPendingDomains());
        Assert.Equal(0, this.sut.GetPendingCount());
    }

    /// <summary>
    /// Tests that IsPendingReview returns true for pending domain.
    /// </summary>
    [Fact]
    public void IsPendingReviewReturnsTrueForPendingDomain()
    {
        // Arrange
        this.sut.AddOrUpdateDomain("spam.com", "msg1", "a@spam.com", "Subject", "Reason");

        // Act - test case-insensitivity
        var result = this.sut.IsPendingReview("SPAM.COM");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsPendingReview returns false for non-pending domain.
    /// </summary>
    [Fact]
    public void IsPendingReviewReturnsFalseForNonPendingDomain()
    {
        // Act
        var result = this.sut.IsPendingReview("nonexistent.com");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that constructor creates directory when it doesn't exist.
    /// </summary>
    [Fact]
    public void ConstructorCreatesDirectoryWhenMissing()
    {
        // Arrange
        var nestedDir = Path.Combine(this.testDirectory, "nested", "subdir");
        var nestedSettings = new SpamFilterSettings
        {
            HumanReviewFilePath = Path.Combine(nestedDir, "review.json"),
            SpamDomainsFilePath = Path.Combine(nestedDir, "domains.json"),
            SpamCandidatesFilePath = Path.Combine(nestedDir, "candidates.json"),
        };

        // Act
        _ = new HumanReviewService(nestedSettings);

        // Assert
        Assert.True(File.Exists(nestedSettings.HumanReviewFilePath));
    }

    /// <summary>
    /// Tests that AddOrUpdateDomain is case-insensitive for domain matching.
    /// </summary>
    [Fact]
    public void AddOrUpdateDomainIsCaseInsensitiveForDomainMatching()
    {
        // Arrange
        this.sut.AddOrUpdateDomain("SPAM.COM", "msg1", "a@spam.com", "Subject 1", "Reason 1");

        // Act - add with different case
        var result = this.sut.AddOrUpdateDomain("spam.com", "msg2", "b@spam.com", "Subject 2", "Reason 2");

        // Assert - should update existing, not create new
        Assert.False(result);
        Assert.Single(this.sut.GetPendingDomains());
    }

    /// <summary>
    /// Tests that GetPendingCount returns correct count after multiple operations.
    /// </summary>
    [Fact]
    public void GetPendingCountReturnsCorrectCountAfterOperations()
    {
        // Arrange & Act
        this.sut.AddOrUpdateDomain("domain1.com", "msg1", "a@domain1.com", "Subject 1", "Reason 1");
        this.sut.AddOrUpdateDomain("domain2.com", "msg2", "a@domain2.com", "Subject 2", "Reason 2");
        this.sut.AddOrUpdateDomain("domain3.com", "msg3", "a@domain3.com", "Subject 3", "Reason 3");
        this.sut.RemoveDomain("domain2.com");

        // Assert
        Assert.Equal(2, this.sut.GetPendingCount());
    }

    /// <summary>
    /// Tests that AddOrUpdateDomain updates FirstSeen and LastSeen timestamps.
    /// </summary>
    [Fact]
    public void AddOrUpdateDomainUpdatesTimestamps()
    {
        // Arrange
        const string domain = "timestamp.com";
        var beforeFirstAdd = DateTime.UtcNow;

        // Act - first add
        this.sut.AddOrUpdateDomain(domain, "msg1", "a@timestamp.com", "Subject 1", "Reason 1");
        var afterFirstAdd = DateTime.UtcNow;

        var domainsAfterFirst = this.sut.GetPendingDomains();
        var firstSeen = domainsAfterFirst[0].FirstSeen;

        // Act - second add
        this.sut.AddOrUpdateDomain(domain, "msg2", "b@timestamp.com", "Subject 2", "Reason 2");
        var domainsAfterSecond = this.sut.GetPendingDomains();
        var lastSeenAfterSecond = domainsAfterSecond[0].LastSeen;

        // Assert
        Assert.True(firstSeen >= beforeFirstAdd);
        Assert.True(firstSeen <= afterFirstAdd);
        Assert.Equal(firstSeen, domainsAfterSecond[0].FirstSeen); // FirstSeen should not change
        Assert.True(lastSeenAfterSecond >= firstSeen);
    }

    /// <summary>
    /// Tests that RemoveDomains handles empty list.
    /// </summary>
    [Fact]
    public void RemoveDomainsHandlesEmptyList()
    {
        // Arrange
        this.sut.AddOrUpdateDomain("domain1.com", "msg1", "a@domain1.com", "Subject 1", "Reason 1");

        // Act
        var removedCount = this.sut.RemoveDomains([]);

        // Assert
        Assert.Equal(0, removedCount);
        Assert.Single(this.sut.GetPendingDomains());
    }

    /// <summary>
    /// Tests that ClearAll is idempotent.
    /// </summary>
    [Fact]
    public void ClearAllIsIdempotent()
    {
        // Act - call clear on empty list
        this.sut.ClearAll();

        // Assert
        Assert.Empty(this.sut.GetPendingDomains());

        // Act - call clear again
        this.sut.ClearAll();

        // Assert
        Assert.Empty(this.sut.GetPendingDomains());
    }

    /// <summary>
    /// Tests that IsPendingReview is case-insensitive.
    /// </summary>
    [Fact]
    public void IsPendingReviewIsCaseInsensitive()
    {
        // Arrange
        this.sut.AddOrUpdateDomain("SpAm.CoM", "msg1", "a@spam.com", "Subject", "Reason");

        // Act & Assert - all case variations should return true
        Assert.True(this.sut.IsPendingReview("spam.com"));
        Assert.True(this.sut.IsPendingReview("SPAM.COM"));
        Assert.True(this.sut.IsPendingReview("Spam.Com"));
    }

    /// <summary>
    /// Tests that RemoveDomain is case-insensitive.
    /// </summary>
    [Fact]
    public void RemoveDomainIsCaseInsensitive()
    {
        // Arrange
        this.sut.AddOrUpdateDomain("SPAM.COM", "msg1", "a@spam.com", "Subject", "Reason");

        // Act - remove with different case
        var removed = this.sut.RemoveDomain("spam.com");

        // Assert
        Assert.NotNull(removed);
        Assert.Empty(this.sut.GetPendingDomains());
    }

    /// <summary>
    /// Tests that sample preserves all properties correctly.
    /// </summary>
    [Fact]
    public void SamplePreservesAllPropertiesCorrectly()
    {
        // Arrange
        const string domain = "test.com";
        const string messageId = "unique-msg-id";
        const string senderEmail = "sender@test.com";
        const string subject = "Test Subject";
        const string reason = "Test Reason";

        // Act
        this.sut.AddOrUpdateDomain(domain, messageId, senderEmail, subject, reason);

        // Assert
        var domains = this.sut.GetPendingDomains();
        Assert.Single(domains);
        var sample = domains[0].Samples[0];
        Assert.Equal(messageId, sample.MessageId);
        Assert.Equal(senderEmail, sample.Sender);
        Assert.Equal(subject, sample.Subject);
        Assert.Equal(reason, sample.Reason);
    }

    /// <summary>
    /// Tests that multiple domains can be added and retrieved independently.
    /// </summary>
    [Fact]
    public void MultipleDomainsCanBeAddedAndRetrievedIndependently()
    {
        // Arrange
        this.sut.AddOrUpdateDomain("domain1.com", "msg1", "a@domain1.com", "Subject 1", "Reason 1");
        this.sut.AddOrUpdateDomain("domain2.com", "msg2", "a@domain2.com", "Subject 2", "Reason 2");
        this.sut.AddOrUpdateDomain("domain3.com", "msg3", "a@domain3.com", "Subject 3", "Reason 3");

        // Act
        var domains = this.sut.GetPendingDomains();

        // Assert
        Assert.Equal(3, domains.Count);
        Assert.Contains(domains, d => d.Domain == "DOMAIN1.COM");
        Assert.Contains(domains, d => d.Domain == "DOMAIN2.COM");
        Assert.Contains(domains, d => d.Domain == "DOMAIN3.COM");
    }

    /// <summary>
    /// Tests that RemoveDomains is case-insensitive.
    /// </summary>
    [Fact]
    public void RemoveDomainsIsCaseInsensitive()
    {
        // Arrange
        this.sut.AddOrUpdateDomain("DOMAIN1.COM", "msg1", "a@domain1.com", "Subject 1", "Reason 1");
        this.sut.AddOrUpdateDomain("DOMAIN2.COM", "msg2", "a@domain2.com", "Subject 2", "Reason 2");

        // Act - remove with different case
        var removedCount = this.sut.RemoveDomains(["domain1.com", "domain2.com"]);

        // Assert
        Assert.Equal(2, removedCount);
        Assert.Empty(this.sut.GetPendingDomains());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        if (disposing)
        {
            try
            {
                if (Directory.Exists(this.testDirectory))
                {
                    Directory.Delete(this.testDirectory, true);
                }
            }
            catch (IOException)
            {
                // Ignore cleanup errors in tests
            }
        }

        this.disposed = true;
    }
}
