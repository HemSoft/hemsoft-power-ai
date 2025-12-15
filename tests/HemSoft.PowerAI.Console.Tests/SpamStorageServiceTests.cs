// <copyright file="SpamStorageServiceTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using HemSoft.PowerAI.Console.Configuration;
using HemSoft.PowerAI.Console.Models;
using HemSoft.PowerAI.Console.Services;

/// <summary>
/// Unit tests for <see cref="SpamStorageService"/>.
/// </summary>
public class SpamStorageServiceTests : IDisposable
{
    private readonly string testDirectory;
    private readonly SpamFilterSettings settings;
    private readonly SpamStorageService sut;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpamStorageServiceTests"/> class.
    /// </summary>
    public SpamStorageServiceTests()
    {
        this.testDirectory = Path.Combine(Path.GetTempPath(), "SpamStorageServiceTests" + Guid.NewGuid().ToString("N")[..8]);
        _ = Directory.CreateDirectory(this.testDirectory);

        this.settings = new SpamFilterSettings
        {
            SpamDomainsFilePath = "spam_domains.json",
            SpamCandidatesFilePath = "spam_candidates.json",
        };

        // Use the internal constructor with explicit directory to ensure test isolation
        this.sut = new SpamStorageService(this.settings, this.testDirectory);
    }

    /// <summary>
    /// Tests that GetSpamDomains returns empty collection when file doesn't exist.
    /// </summary>
    [Fact]
    public void GetSpamDomainsReturnsEmptyWhenFileDoesNotExist()
    {
        // Act
        var domains = this.sut.GetSpamDomains();

        // Assert
        Assert.Empty(domains);
    }

    /// <summary>
    /// Tests that AddSpamDomain successfully adds a new domain.
    /// </summary>
    [Fact]
    public void AddSpamDomainAddsNewDomain()
    {
        // Arrange
        const string domain = "spam.example.com";
        const string reason = "Test reason";

        // Act
        var result = this.sut.AddSpamDomain(domain, reason);

        // Assert
        Assert.True(result);
        var domains = this.sut.GetSpamDomains();
        _ = Assert.Single(domains);
        Assert.Equal("SPAM.EXAMPLE.COM", domains[0].Domain);
    }

    /// <summary>
    /// Tests that AddSpamDomain returns false for duplicate domain.
    /// </summary>
    [Fact]
    public void AddSpamDomainReturnsFalseForDuplicate()
    {
        // Arrange
        const string domain = "spam.example.com";
        _ = this.sut.AddSpamDomain(domain, "First");

        // Act
        var result = this.sut.AddSpamDomain(domain, "Second");

        // Assert
        Assert.False(result);
        _ = Assert.Single(this.sut.GetSpamDomains());
    }

    /// <summary>
    /// Tests that IsKnownSpamDomain returns true for known domain.
    /// </summary>
    [Fact]
    public void IsKnownSpamDomainReturnsTrueForKnownDomain()
    {
        // Arrange
        _ = this.sut.AddSpamDomain("SPAM.COM", "Test");

        // Act - test case-insensitivity
        var result = this.sut.IsKnownSpamDomain("spam.com");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsKnownSpamDomain returns false for unknown domain.
    /// </summary>
    [Fact]
    public void IsKnownSpamDomainReturnsFalseForUnknownDomain()
    {
        // Act
        var result = this.sut.IsKnownSpamDomain("unknown.com");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that GetSpamCandidates returns empty collection when file doesn't exist.
    /// </summary>
    [Fact]
    public void GetSpamCandidatesReturnsEmptyWhenFileDoesNotExist()
    {
        // Act
        var candidates = this.sut.GetSpamCandidates();

        // Assert
        Assert.Empty(candidates);
    }

    /// <summary>
    /// Tests that AddSpamCandidate successfully adds a new candidate.
    /// </summary>
    [Fact]
    public void AddSpamCandidateAddsNewCandidate()
    {
        // Arrange
        var candidate = new SpamCandidate
        {
            MessageId = "msg123",
            SenderEmail = "spammer@test.com",
            SenderDomain = "TEST.COM",
            Subject = "Win a prize!",
            SpamReason = "Suspicious content",
            ConfidenceScore = 0.9,
            ReceivedAt = DateTimeOffset.UtcNow,
            IdentifiedAt = DateTimeOffset.UtcNow,
        };

        // Act
        var result = this.sut.AddSpamCandidate(candidate);

        // Assert
        Assert.True(result);
        var candidates = this.sut.GetSpamCandidates();
        _ = Assert.Single(candidates);
        Assert.Equal("msg123", candidates[0].MessageId);
    }

    /// <summary>
    /// Tests that AddSpamCandidate returns false for duplicate message ID.
    /// </summary>
    [Fact]
    public void AddSpamCandidateReturnsFalseForDuplicate()
    {
        // Arrange
        var candidate = new SpamCandidate
        {
            MessageId = "msg123",
            SenderEmail = "spammer@test.com",
            SenderDomain = "TEST.COM",
            Subject = "Win a prize!",
            SpamReason = "Suspicious content",
            ConfidenceScore = 0.9,
            ReceivedAt = DateTimeOffset.UtcNow,
            IdentifiedAt = DateTimeOffset.UtcNow,
        };
        _ = this.sut.AddSpamCandidate(candidate);

        // Act
        var result = this.sut.AddSpamCandidate(candidate);

        // Assert
        Assert.False(result);
        _ = Assert.Single(this.sut.GetSpamCandidates());
    }

    /// <summary>
    /// Tests that RemoveSpamCandidate removes existing candidate.
    /// </summary>
    [Fact]
    public void RemoveSpamCandidateRemovesExistingCandidate()
    {
        // Arrange
        var candidate = new SpamCandidate
        {
            MessageId = "msg123",
            SenderEmail = "spammer@test.com",
            SenderDomain = "TEST.COM",
            Subject = "Win a prize!",
            SpamReason = "Suspicious content",
            ConfidenceScore = 0.9,
            ReceivedAt = DateTimeOffset.UtcNow,
            IdentifiedAt = DateTimeOffset.UtcNow,
        };
        _ = this.sut.AddSpamCandidate(candidate);

        // Act
        var result = this.sut.RemoveSpamCandidate("msg123");

        // Assert
        Assert.True(result);
        Assert.Empty(this.sut.GetSpamCandidates());
    }

    /// <summary>
    /// Tests that GetCandidatesGroupedByDomain groups correctly.
    /// </summary>
    [Fact]
    public void GetCandidatesGroupedByDomainGroupsCorrectly()
    {
        // Arrange
        _ = this.sut.AddSpamCandidate(new SpamCandidate
        {
            MessageId = "msg1",
            SenderEmail = "a@domain1.com",
            SenderDomain = "DOMAIN1.COM",
            Subject = "Subject1",
            SpamReason = "Reason1",
            ConfidenceScore = 0.8,
            ReceivedAt = DateTimeOffset.UtcNow,
            IdentifiedAt = DateTimeOffset.UtcNow,
        });
        _ = this.sut.AddSpamCandidate(new SpamCandidate
        {
            MessageId = "msg2",
            SenderEmail = "b@domain1.com",
            SenderDomain = "DOMAIN1.COM",
            Subject = "Subject2",
            SpamReason = "Reason2",
            ConfidenceScore = 0.7,
            ReceivedAt = DateTimeOffset.UtcNow,
            IdentifiedAt = DateTimeOffset.UtcNow,
        });
        _ = this.sut.AddSpamCandidate(new SpamCandidate
        {
            MessageId = "msg3",
            SenderEmail = "c@domain2.com",
            SenderDomain = "DOMAIN2.COM",
            Subject = "Subject3",
            SpamReason = "Reason3",
            ConfidenceScore = 0.9,
            ReceivedAt = DateTimeOffset.UtcNow,
            IdentifiedAt = DateTimeOffset.UtcNow,
        });

        // Act
        var grouped = this.sut.GetCandidatesGroupedByDomain();

        // Assert
        Assert.Equal(2, grouped.Count);
        Assert.Equal(2, grouped["DOMAIN1.COM"].Count);
        _ = Assert.Single(grouped["DOMAIN2.COM"]);
    }

    /// <summary>
    /// Tests that ClearSpamCandidates removes all candidates.
    /// </summary>
    [Fact]
    public void ClearSpamCandidatesRemovesAllCandidates()
    {
        // Arrange
        _ = this.sut.AddSpamCandidate(new SpamCandidate
        {
            MessageId = "msg1",
            SenderEmail = "a@test.com",
            SenderDomain = "TEST.COM",
            Subject = "Test",
            SpamReason = "Test",
            ConfidenceScore = 0.5,
            ReceivedAt = DateTimeOffset.UtcNow,
            IdentifiedAt = DateTimeOffset.UtcNow,
        });

        // Act
        this.sut.ClearSpamCandidates();

        // Assert
        Assert.Empty(this.sut.GetSpamCandidates());
    }

    /// <summary>
    /// Tests that RemoveSpamCandidate returns false when candidate does not exist.
    /// </summary>
    [Fact]
    public void RemoveSpamCandidateReturnsFalseWhenNotFound()
    {
        // Act - try to remove a candidate that was never added
        var result = this.sut.RemoveSpamCandidate("nonexistent-msg-id");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that SpamStorageService creates directory if it does not exist.
    /// </summary>
    [Fact]
    public void ConstructorCreatesDirectoryIfNotExists()
    {
        // Arrange - create a path to a non-existent directory
        var newDirectory = Path.Combine(
            Path.GetTempPath(),
            "SpamStorageServiceTests_New_" + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            // Act - create service with non-existent directory
            var newSettings = new SpamFilterSettings
            {
                HumanReviewFilePath = Path.Combine(newDirectory, "human_review.json"),
                SpamDomainsFilePath = "spam_domains.json",
                SpamCandidatesFilePath = "spam_candidates.json",
            };
            _ = new SpamStorageService(newSettings, newDirectory);

            // Assert - directory should now exist
            Assert.True(Directory.Exists(newDirectory));

            // Also verify files were created
            Assert.True(File.Exists(Path.Combine(newDirectory, "spam_domains.json")));
            Assert.True(File.Exists(Path.Combine(newDirectory, "spam_candidates.json")));
        }
        finally
        {
            // Cleanup
            try
            {
                if (Directory.Exists(newDirectory))
                {
                    Directory.Delete(newDirectory, recursive: true);
                }
            }
            catch (IOException)
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(disposing: true);
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
                    Directory.Delete(this.testDirectory, recursive: true);
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
