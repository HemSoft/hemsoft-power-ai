// <copyright file="SpamFilterToolsTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using System.Reflection;
using System.Text.Json;

using HemSoft.PowerAI.Console.Configuration;
using HemSoft.PowerAI.Console.Models;
using HemSoft.PowerAI.Console.Services;
using HemSoft.PowerAI.Console.Tools;

/// <summary>
/// Unit tests for <see cref="SpamFilterTools"/>.
/// </summary>
[Collection("EnvironmentVariableTests")]
public class SpamFilterToolsTests : IDisposable
{
    private readonly string testDirectory;
    private readonly SpamFilterSettings settings;
    private readonly SpamStorageService storageService;
    private readonly SpamFilterTools sut;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpamFilterToolsTests"/> class.
    /// </summary>
    public SpamFilterToolsTests()
    {
        this.testDirectory = Path.Combine(Path.GetTempPath(), "SpamFilterToolsTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(this.testDirectory);

        this.settings = new SpamFilterSettings
        {
            HumanReviewFilePath = Path.Combine(this.testDirectory, "human_review.json"),
            SpamDomainsFilePath = "spam_domains.json",
            SpamCandidatesFilePath = "spam_candidates.json",
        };

        this.storageService = new SpamStorageService(this.settings, this.testDirectory);
        this.sut = new SpamFilterTools(this.storageService);
    }

    /// <summary>
    /// Tests that GetKnownSpamDomains returns empty array when no domains.
    /// </summary>
    [Fact]
    public void GetKnownSpamDomainsReturnsEmptyArrayWhenNoDomains()
    {
        // Act
        var result = this.sut.GetKnownSpamDomains();
        var domains = JsonSerializer.Deserialize<string[]>(result);

        // Assert
        Assert.NotNull(domains);
        Assert.Empty(domains);
    }

    /// <summary>
    /// Tests that GetKnownSpamDomains returns domains as JSON array.
    /// </summary>
    [Fact]
    public void GetKnownSpamDomainsReturnsDomainAsJsonArray()
    {
        // Arrange
        this.storageService.AddSpamDomain("spam.com", "Test");
        this.storageService.AddSpamDomain("junk.com", "Test");

        // Act
        var result = this.sut.GetKnownSpamDomains();
        var domains = JsonSerializer.Deserialize<string[]>(result);

        // Assert
        Assert.NotNull(domains);
        Assert.Equal(2, domains.Length);
        Assert.Contains("SPAM.COM", domains);
        Assert.Contains("JUNK.COM", domains);
    }

    /// <summary>
    /// Tests that IsKnownSpamDomain returns true for known domains.
    /// </summary>
    [Fact]
    public void IsKnownSpamDomainReturnsTrueForKnownDomain()
    {
        // Arrange
        this.storageService.AddSpamDomain("known.com", "Test");

        // Act & Assert
        Assert.True(this.sut.IsKnownSpamDomain("known.com"));
        Assert.True(this.sut.IsKnownSpamDomain("KNOWN.COM"));
    }

    /// <summary>
    /// Tests that IsKnownSpamDomain returns false for unknown domains.
    /// </summary>
    [Fact]
    public void IsKnownSpamDomainReturnsFalseForUnknownDomain() =>

        // Act & Assert
        Assert.False(this.sut.IsKnownSpamDomain("unknown.com"));

    /// <summary>
    /// Tests that ReportEmailEvaluation invokes callback.
    /// </summary>
    [Fact]
    public void ReportEmailEvaluationInvokesCallback()
    {
        // Arrange
        EmailEvaluation? receivedEvaluation = null;
        this.sut.SetEvaluationCallback(e => receivedEvaluation = e);

        // Act
        var result = this.sut.ReportEmailEvaluation("msg123", "sender@test.com", "Test Subject", "Legitimate", "Not spam");

        // Assert
        Assert.Equal("Evaluation recorded.", result);
        Assert.NotNull(receivedEvaluation);
        Assert.Equal("msg123", receivedEvaluation.MessageId);
        Assert.Equal("sender@test.com", receivedEvaluation.Sender);
        Assert.Equal("Test Subject", receivedEvaluation.Subject);
        Assert.Equal("Legitimate", receivedEvaluation.Verdict);
        Assert.Equal("Not spam", receivedEvaluation.Reason);
    }

    /// <summary>
    /// Tests that ReportEmailEvaluation handles null values.
    /// </summary>
    [Fact]
    public void ReportEmailEvaluationHandlesNullValues()
    {
        // Arrange
        EmailEvaluation? receivedEvaluation = null;
        this.sut.SetEvaluationCallback(e => receivedEvaluation = e);

        // Act
        var result = this.sut.ReportEmailEvaluation(null!, null!, null!, null!, null);

        // Assert
        Assert.Equal("Evaluation recorded.", result);
        Assert.NotNull(receivedEvaluation);
        Assert.Equal(string.Empty, receivedEvaluation.MessageId);
        Assert.Equal(string.Empty, receivedEvaluation.Sender);
        Assert.Equal(string.Empty, receivedEvaluation.Subject);
        Assert.Equal("Unknown", receivedEvaluation.Verdict);
        Assert.Null(receivedEvaluation.Reason);
    }

    /// <summary>
    /// Tests that ReportEmailEvaluation handles null callback.
    /// </summary>
    [Fact]
    public void ReportEmailEvaluationHandlesNullCallback()
    {
        // Arrange
        this.sut.SetEvaluationCallback(null);

        // Act & Assert - should not throw
        var result = this.sut.ReportEmailEvaluation("msg123", "sender@test.com", "Test Subject", "Legitimate", null);
        Assert.Equal("Evaluation recorded.", result);
    }

    /// <summary>
    /// Tests that RecordSpamCandidate records candidate.
    /// </summary>
    [Fact]
    public void RecordSpamCandidateRecordsCandidate()
    {
        // Act
        var result = this.sut.RecordSpamCandidate("msg123", "spam@test.com", "Win Money!", "Suspicious content", 0.85);

        // Assert
        Assert.Contains("TEST.COM", result, StringComparison.Ordinal);
        var candidates = this.storageService.GetSpamCandidates();
        Assert.Single(candidates);
        Assert.Equal("msg123", candidates[0].MessageId);
    }

    /// <summary>
    /// Tests that RecordSpamCandidate clamps confidence score.
    /// </summary>
    [Fact]
    public void RecordSpamCandidateClampsConfidenceScore()
    {
        // Act
        this.sut.RecordSpamCandidate("msg1", "a@test.com", "Subject", "Reason", 1.5);
        this.sut.RecordSpamCandidate("msg2", "b@test.com", "Subject", "Reason", -0.5);

        // Assert
        var candidates = this.storageService.GetSpamCandidates();
        Assert.Equal(2, candidates.Count);
        Assert.Equal(1.0, candidates.Find(c => string.Equals(c.MessageId, "msg1", StringComparison.Ordinal))?.ConfidenceScore);
        Assert.Equal(0.0, candidates.Find(c => string.Equals(c.MessageId, "msg2", StringComparison.Ordinal))?.ConfidenceScore);
    }

    /// <summary>
    /// Tests that RecordSpamCandidate returns message for duplicate.
    /// </summary>
    [Fact]
    public void RecordSpamCandidateReturnsDuplicateMessage()
    {
        // Arrange
        this.sut.RecordSpamCandidate("msg123", "spam@test.com", "Subject", "Reason", 0.8);

        // Act
        var result = this.sut.RecordSpamCandidate("msg123", "spam@test.com", "Subject", "Reason", 0.8);

        // Assert
        Assert.Contains("already recorded", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that AddToSpamDomainList adds domain.
    /// </summary>
    [Fact]
    public void AddToSpamDomainListAddsDomain()
    {
        // Act
        var result = this.sut.AddToSpamDomainList("spam.com", "Test reason");

        // Assert
        Assert.Contains("Added", result, StringComparison.Ordinal);
        Assert.True(this.storageService.IsKnownSpamDomain("spam.com"));
    }

    /// <summary>
    /// Tests that AddToSpamDomainList returns message for duplicate.
    /// </summary>
    [Fact]
    public void AddToSpamDomainListReturnsDuplicateMessage()
    {
        // Arrange
        this.sut.AddToSpamDomainList("spam.com", "Test");

        // Act
        var result = this.sut.AddToSpamDomainList("spam.com", "Test");

        // Assert
        Assert.Contains("already in spam list", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that GetPendingSpamCandidatesByDomain returns grouped candidates.
    /// </summary>
    [Fact]
    public void GetPendingSpamCandidatesByDomainReturnsGroupedCandidates()
    {
        // Arrange
        this.sut.RecordSpamCandidate("msg1", "a@domain1.com", "Subject 1", "Reason 1", 0.8);
        this.sut.RecordSpamCandidate("msg2", "b@domain1.com", "Subject 2", "Reason 2", 0.9);
        this.sut.RecordSpamCandidate("msg3", "c@domain2.com", "Subject 3", "Reason 3", 0.7);

        // Act
        var result = this.sut.GetPendingSpamCandidatesByDomain();
        var grouped = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(result);

        // Assert
        Assert.NotNull(grouped);
        Assert.Equal(2, grouped.Count);
        Assert.True(grouped.ContainsKey("DOMAIN1.COM"));
        Assert.True(grouped.ContainsKey("DOMAIN2.COM"));
    }

    /// <summary>
    /// Tests that ClearProcessedCandidates clears all candidates.
    /// </summary>
    [Fact]
    public void ClearProcessedCandidatesClearsAllCandidates()
    {
        // Arrange
        this.sut.RecordSpamCandidate("msg1", "a@test.com", "Subject", "Reason", 0.8);
        this.sut.RecordSpamCandidate("msg2", "b@test.com", "Subject", "Reason", 0.9);

        // Act
        var result = this.sut.ClearProcessedCandidates();

        // Assert
        Assert.Equal("Cleared all spam candidates.", result);
        Assert.Empty(this.storageService.GetSpamCandidates());
    }

    /// <summary>
    /// Tests that GetInboxEmailsAsync returns error when client ID not set.
    /// </summary>
    /// <returns>A task representing the async test operation.</returns>
    [Fact]
    public async Task GetInboxEmailsAsyncReturnsErrorWhenClientIdNotSet()
    {
        // Skip if GRAPH_CLIENT_ID is configured in user registry (code falls back to registry)
        var userRegistryValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID", EnvironmentVariableTarget.User);
        if (!string.IsNullOrEmpty(userRegistryValue))
        {
            return; // Test not applicable when user has Graph configured
        }

        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", null);

        try
        {
            // Act
            var result = await this.sut.GetInboxEmailsAsync(10);

            // Assert
            Assert.Contains("GRAPH_CLIENT_ID", result, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", originalValue);
        }
    }

    /// <summary>
    /// Tests that ReadEmailAsync returns error when client ID not set.
    /// </summary>
    /// <returns>A task representing the async test operation.</returns>
    [Fact]
    public async Task ReadEmailAsyncReturnsErrorWhenClientIdNotSet()
    {
        // Skip if GRAPH_CLIENT_ID is configured in user registry (code falls back to registry)
        var userRegistryValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID", EnvironmentVariableTarget.User);
        if (!string.IsNullOrEmpty(userRegistryValue))
        {
            return; // Test not applicable when user has Graph configured
        }

        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", null);

        try
        {
            // Act
            var result = await this.sut.ReadEmailAsync("msg123");

            // Assert
            Assert.Contains("GRAPH_CLIENT_ID", result, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", originalValue);
        }
    }

    /// <summary>
    /// Tests that MoveToJunkAsync returns error when client ID not set.
    /// </summary>
    /// <returns>A task representing the async test operation.</returns>
    [Fact]
    public async Task MoveToJunkAsyncReturnsErrorWhenClientIdNotSet()
    {
        // Skip if GRAPH_CLIENT_ID is configured in user registry (code falls back to registry)
        var userRegistryValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID", EnvironmentVariableTarget.User);
        if (!string.IsNullOrEmpty(userRegistryValue))
        {
            return; // Test not applicable when user has Graph configured
        }

        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", null);

        try
        {
            // Act
            var result = await this.sut.MoveToJunkAsync("msg123");

            // Assert
            Assert.Contains("GRAPH_CLIENT_ID", result, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", originalValue);
        }
    }

    /// <summary>
    /// Tests that MoveToJunkAsync returns error for empty message ID.
    /// </summary>
    /// <returns>A task representing the async test operation.</returns>
    [Fact]
    public async Task MoveToJunkAsyncReturnsErrorForEmptyMessageId()
    {
        // Arrange - need client ID set to get past first check
        // Save original values at both Process and User levels
        var originalProcessValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
        var originalUserValue = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID", EnvironmentVariableTarget.User);

        // Reset SharedGraphClient to ensure clean state
        SharedGraphClient.Reset();

        // Clear User-level (SharedGraphClient falls back to it) and set Process-level
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", null, EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", "test-client-id");

        try
        {
            using var freshSut = new SpamFilterTools(this.storageService);

            // Act
            var result = await freshSut.MoveToJunkAsync(string.Empty);

            // Assert
            Assert.Contains("messageId is required", result, StringComparison.Ordinal);
        }
        finally
        {
            // Restore original values
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", originalProcessValue);
            Environment.SetEnvironmentVariable("GRAPH_CLIENT_ID", originalUserValue, EnvironmentVariableTarget.User);
            SharedGraphClient.Reset();
        }
    }

    /// <summary>
    /// Tests that ExtractDomain extracts domain from email.
    /// </summary>
    [Fact]
    public void ExtractDomainExtractsDomainFromEmail()
    {
        // Arrange
        var method = typeof(SpamFilterTools).GetMethod(
            "ExtractDomain",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act
        var result = method.Invoke(null, ["user@example.com"]);

        // Assert
        Assert.Equal("EXAMPLE.COM", result);
    }

    /// <summary>
    /// Tests that ExtractDomain handles email without @.
    /// </summary>
    [Fact]
    public void ExtractDomainHandlesEmailWithoutAt()
    {
        // Arrange
        var method = typeof(SpamFilterTools).GetMethod(
            "ExtractDomain",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act
        var result = method.Invoke(null, ["nodomain"]);

        // Assert
        Assert.Equal("NODOMAIN", result);
    }

    /// <summary>
    /// Tests that TruncateBody handles null.
    /// </summary>
    [Fact]
    public void TruncateBodyHandlesNull()
    {
        // Arrange
        var method = typeof(SpamFilterTools).GetMethod(
            "TruncateBody",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act
        var result = method.Invoke(null, [null, 500]);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    /// Tests that TruncateBody strips HTML tags.
    /// </summary>
    [Fact]
    public void TruncateBodyStripsHtmlTags()
    {
        // Arrange
        var method = typeof(SpamFilterTools).GetMethod(
            "TruncateBody",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act
        var result = method.Invoke(null, ["<p>Hello <b>World</b></p>", 500]);

        // Assert
        Assert.Equal("Hello World", result);
    }

    /// <summary>
    /// Tests that Dispose can be called multiple times.
    /// </summary>
    [Fact]
    public void DisposeCanBeCalledMultipleTimes()
    {
        // Arrange
        using var tools = new SpamFilterTools(this.storageService);

        // Act
        var exception = Record.Exception(() =>
        {
            tools.Dispose();
            tools.Dispose();
        });

        // Assert
        Assert.Null(exception);
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
            this.sut.Dispose();

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
