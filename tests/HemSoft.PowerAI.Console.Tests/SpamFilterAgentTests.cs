// <copyright file="SpamFilterAgentTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using System.Reflection;

using HemSoft.PowerAI.Console.Agents;
using HemSoft.PowerAI.Console.Configuration;

/// <summary>
/// Unit tests for <see cref="SpamFilterAgent"/>.
/// </summary>
public class SpamFilterAgentTests : IDisposable
{
    private readonly string testDirectory;
    private readonly SpamFilterSettings settings;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpamFilterAgentTests"/> class.
    /// </summary>
    public SpamFilterAgentTests()
    {
        this.testDirectory = Path.Combine(Path.GetTempPath(), "SpamFilterAgentTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(this.testDirectory);

        this.settings = new SpamFilterSettings
        {
            HumanReviewFilePath = Path.Combine(this.testDirectory, "human_review.json"),
            SpamDomainsFilePath = Path.Combine(this.testDirectory, "spam_domains.json"),
            SpamCandidatesFilePath = Path.Combine(this.testDirectory, "spam_candidates.json"),
            BatchSize = 10,
            DelayBetweenBatchesSeconds = 1,
        };
    }

    /// <summary>
    /// Tests that constructor creates an agent without throwing.
    /// </summary>
    [Fact]
    public void ConstructorCreatesAgentWithoutThrowing()
    {
        // Act & Assert
        using var agent = new SpamFilterAgent(this.settings);
        Assert.NotNull(agent);
    }

    /// <summary>
    /// Tests that Dispose can be called multiple times without throwing.
    /// </summary>
    [Fact]
    public void DisposeCanBeCalledMultipleTimes()
    {
        // Arrange
        using var agent = new SpamFilterAgent(this.settings);

        // Act
        var exception = Record.Exception(() =>
        {
            agent.Dispose();
            agent.Dispose();
        });

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that ParseBatchResult detects empty inbox variations.
    /// </summary>
    /// <param name="responseText">The response text to parse.</param>
    [Theory]
    [InlineData("No emails in inbox")]
    [InlineData("The inbox is empty")]
    [InlineData("Inbox empty")]
    [InlineData("No more emails to process")]
    [InlineData("Empty array returned")]
    [InlineData("[]")]
    [InlineData("Found 0 emails in the inbox")]
    public void ParseBatchResultDetectsEmptyInbox(string responseText)
    {
        // Act
        var result = InvokeParseBatchResult(responseText);

        // Assert
        Assert.True(GetInboxWasEmpty(result));
    }

    /// <summary>
    /// Tests that ParseBatchResult parses BATCH_STATS format.
    /// </summary>
    [Fact]
    public void ParseBatchResultParsesBatchStatsFormat()
    {
        // Arrange
        const string responseText = "I processed the emails.\nBATCH_STATS: processed=5, junked=2, candidates=1\nDone.";

        // Act
        var result = InvokeParseBatchResult(responseText);

        // Assert
        Assert.False(GetInboxWasEmpty(result));
        Assert.Equal(5, GetEmailsProcessed(result));
        Assert.Equal(2, GetMovedToJunk(result));
        Assert.Equal(1, GetCandidatesRecorded(result));
    }

    /// <summary>
    /// Tests that ParseBatchResult handles case-insensitive BATCH_STATS.
    /// </summary>
    [Fact]
    public void ParseBatchResultHandlesCaseInsensitiveBatchStats()
    {
        // Arrange
        const string responseText = "batch_stats: processed=3, junked=1, candidates=0";

        // Act
        var result = InvokeParseBatchResult(responseText);

        // Assert
        Assert.Equal(3, GetEmailsProcessed(result));
        Assert.Equal(1, GetMovedToJunk(result));
        Assert.Equal(0, GetCandidatesRecorded(result));
    }

    /// <summary>
    /// Tests that ParseBatchResult falls back to pattern extraction.
    /// </summary>
    [Fact]
    public void ParseBatchResultFallsBackToPatternExtraction()
    {
        // Arrange
        const string responseText = "I have processed 10 emails. Moved 3 to junk. Flagged 2 candidates.";

        // Act
        var result = InvokeParseBatchResult(responseText);

        // Assert
        Assert.Equal(10, GetEmailsProcessed(result));
        Assert.Equal(3, GetMovedToJunk(result));
        Assert.Equal(2, GetCandidatesRecorded(result));
    }

    /// <summary>
    /// Tests that ExtractCountFromPattern extracts numeric values.
    /// </summary>
    [Fact]
    public void ExtractCountFromPatternExtractsNumericValues()
    {
        // Arrange
        var method = typeof(SpamFilterAgent).GetMethod(
            "ExtractCountFromPattern",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act
        var result = method.Invoke(null, ["5 EMAILS PROCESSED", @"(\d+)\s*(?:EMAILS?\s*)?PROCESSED|PROCESSED\s*(\d+)"]);

        // Assert
        Assert.Equal(5, result);
    }

    /// <summary>
    /// Tests that ExtractCountFromPattern returns zero when no match.
    /// </summary>
    [Fact]
    public void ExtractCountFromPatternReturnsZeroWhenNoMatch()
    {
        // Arrange
        var method = typeof(SpamFilterAgent).GetMethod(
            "ExtractCountFromPattern",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act
        var result = method.Invoke(null, ["no numbers here", @"(\d+)"]);

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Tests that Truncate returns original when shorter than max.
    /// </summary>
    [Fact]
    public void TruncateReturnsOriginalWhenShorterThanMax()
    {
        // Act
        var result = InvokeTruncate("Short", 10);

        // Assert
        Assert.Equal("Short", result);
    }

    /// <summary>
    /// Tests that Truncate truncates and adds ellipsis.
    /// </summary>
    [Fact]
    public void TruncateTruncatesAndAddsEllipsis()
    {
        // Act
        var result = InvokeTruncate("This is a very long string", 10);

        // Assert
        Assert.Equal(10, result.Length);
        Assert.EndsWith("…", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that ExtractDomain extracts domain from email.
    /// </summary>
    [Fact]
    public void ExtractDomainExtractsDomainFromEmail()
    {
        // Act
        var result = InvokeExtractDomain("user@example.com");

        // Assert
        Assert.Equal("EXAMPLE.COM", result);
    }

    /// <summary>
    /// Tests that ExtractDomain handles email without @.
    /// </summary>
    [Fact]
    public void ExtractDomainHandlesEmailWithoutAt()
    {
        // Act
        var result = InvokeExtractDomain("nodomain");

        // Assert
        Assert.Equal("NODOMAIN", result);
    }

    /// <summary>
    /// Tests that BatchResult initializes with zero values.
    /// </summary>
    [Fact]
    public void BatchResultInitializesWithZeroValues()
    {
        // Arrange
        var batchResultType = typeof(SpamFilterAgent).GetNestedType("BatchResult", BindingFlags.NonPublic);
        Assert.NotNull(batchResultType);

        // Act
        var instance = Activator.CreateInstance(batchResultType);
        Assert.NotNull(instance);

        // Assert
        Assert.Equal(0, batchResultType.GetProperty("EmailsProcessed")?.GetValue(instance));
        Assert.Equal(0, batchResultType.GetProperty("MovedToJunk")?.GetValue(instance));
        Assert.Equal(0, batchResultType.GetProperty("CandidatesRecorded")?.GetValue(instance));
        Assert.False((bool)(batchResultType.GetProperty("InboxWasEmpty")?.GetValue(instance) ?? true));
    }

    /// <summary>
    /// Tests that BatchResult properties can be set.
    /// </summary>
    [Fact]
    public void BatchResultPropertiesCanBeSet()
    {
        // Arrange
        var batchResultType = typeof(SpamFilterAgent).GetNestedType("BatchResult", BindingFlags.NonPublic);
        Assert.NotNull(batchResultType);

        var instance = Activator.CreateInstance(batchResultType);
        Assert.NotNull(instance);

        // Act
        batchResultType.GetProperty("EmailsProcessed")?.SetValue(instance, 15);
        batchResultType.GetProperty("MovedToJunk")?.SetValue(instance, 5);
        batchResultType.GetProperty("CandidatesRecorded")?.SetValue(instance, 3);
        batchResultType.GetProperty("InboxWasEmpty")?.SetValue(instance, true);

        // Assert
        Assert.Equal(15, batchResultType.GetProperty("EmailsProcessed")?.GetValue(instance));
        Assert.Equal(5, batchResultType.GetProperty("MovedToJunk")?.GetValue(instance));
        Assert.Equal(3, batchResultType.GetProperty("CandidatesRecorded")?.GetValue(instance));
        Assert.True((bool)(batchResultType.GetProperty("InboxWasEmpty")?.GetValue(instance) ?? false));
    }

    /// <summary>
    /// Tests that RunStats initializes with zero values.
    /// </summary>
    [Fact]
    public void RunStatsInitializesWithZeroValues()
    {
        // Arrange
        var runStatsType = typeof(SpamFilterAgent).GetNestedType("RunStats", BindingFlags.NonPublic);
        Assert.NotNull(runStatsType);

        // Act
        var instance = Activator.CreateInstance(runStatsType);
        Assert.NotNull(instance);

        // Assert
        Assert.Equal(0, runStatsType.GetProperty("Iteration")?.GetValue(instance));
        Assert.Equal(0, runStatsType.GetProperty("TotalProcessed")?.GetValue(instance));
        Assert.Equal(0, runStatsType.GetProperty("TotalMovedToJunk")?.GetValue(instance));
        Assert.Equal(0, runStatsType.GetProperty("TotalCandidates")?.GetValue(instance));
    }

    /// <summary>
    /// Tests that RunStats properties can be set.
    /// </summary>
    [Fact]
    public void RunStatsPropertiesCanBeSet()
    {
        // Arrange
        var runStatsType = typeof(SpamFilterAgent).GetNestedType("RunStats", BindingFlags.NonPublic);
        Assert.NotNull(runStatsType);

        var instance = Activator.CreateInstance(runStatsType);
        Assert.NotNull(instance);

        // Act
        runStatsType.GetProperty("Iteration")?.SetValue(instance, 5);
        runStatsType.GetProperty("TotalProcessed")?.SetValue(instance, 50);
        runStatsType.GetProperty("TotalMovedToJunk")?.SetValue(instance, 20);
        runStatsType.GetProperty("TotalCandidates")?.SetValue(instance, 10);

        // Assert
        Assert.Equal(5, runStatsType.GetProperty("Iteration")?.GetValue(instance));
        Assert.Equal(50, runStatsType.GetProperty("TotalProcessed")?.GetValue(instance));
        Assert.Equal(20, runStatsType.GetProperty("TotalMovedToJunk")?.GetValue(instance));
        Assert.Equal(10, runStatsType.GetProperty("TotalCandidates")?.GetValue(instance));
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

    private static object InvokeParseBatchResult(string responseText)
    {
        var method = typeof(SpamFilterAgent).GetMethod(
            "ParseBatchResult",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [responseText]);
        Assert.NotNull(result);

        return result;
    }

    private static bool GetInboxWasEmpty(object batchResult)
    {
        var prop = batchResult.GetType().GetProperty("InboxWasEmpty");
        Assert.NotNull(prop);
        return (bool)(prop.GetValue(batchResult) ?? false);
    }

    private static int GetEmailsProcessed(object batchResult)
    {
        var prop = batchResult.GetType().GetProperty("EmailsProcessed");
        Assert.NotNull(prop);
        return (int)(prop.GetValue(batchResult) ?? 0);
    }

    private static int GetMovedToJunk(object batchResult)
    {
        var prop = batchResult.GetType().GetProperty("MovedToJunk");
        Assert.NotNull(prop);
        return (int)(prop.GetValue(batchResult) ?? 0);
    }

    private static int GetCandidatesRecorded(object batchResult)
    {
        var prop = batchResult.GetType().GetProperty("CandidatesRecorded");
        Assert.NotNull(prop);
        return (int)(prop.GetValue(batchResult) ?? 0);
    }

    private static string InvokeTruncate(string text, int maxLength)
    {
        var method = typeof(SpamFilterAgent).GetMethod(
            "Truncate",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [text, maxLength]);
        Assert.NotNull(result);

        return (string)result;
    }

    private static string InvokeExtractDomain(string email)
    {
        var method = typeof(SpamFilterAgent).GetMethod(
            "ExtractDomain",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method.Invoke(null, [email]);
        Assert.NotNull(result);

        return (string)result;
    }
}
