// <copyright file="SpamScanToolsGraphIntegrationTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using System.Net;

using HemSoft.PowerAI.Console.Agents;
using HemSoft.PowerAI.Console.Configuration;
using HemSoft.PowerAI.Console.Services;

/// <summary>
/// Integration tests for <see cref="SpamScanTools"/> that use mocked Graph API responses.
/// </summary>
[Collection("EnvironmentVariableTests")]
public class SpamScanToolsGraphIntegrationTests : IDisposable
{
    private readonly string testDirectory;
    private readonly SpamFilterSettings settings;
    private readonly SpamStorageService storageService;
    private readonly HumanReviewService humanReviewService;
    private readonly MockHttpMessageHandler mockHandler;
    private readonly TestGraphClientProvider testProvider;
    private readonly SpamScanTools sut;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpamScanToolsGraphIntegrationTests"/> class.
    /// </summary>
    public SpamScanToolsGraphIntegrationTests()
    {
        this.testDirectory = Path.Combine(
            Path.GetTempPath(),
            "SpamScanToolsGraphTests_" + Guid.NewGuid().ToString("N")[..8]);
        _ = Directory.CreateDirectory(this.testDirectory);

        this.settings = new SpamFilterSettings
        {
            HumanReviewFilePath = Path.Combine(this.testDirectory, "human_review.json"),
            SpamDomainsFilePath = "spam_domains.json",
            SpamCandidatesFilePath = "spam_candidates.json",
        };

        this.storageService = new SpamStorageService(this.settings, this.testDirectory);
        this.humanReviewService = new HumanReviewService(this.settings);
        this.mockHandler = new MockHttpMessageHandler();
        this.testProvider = new TestGraphClientProvider(this.mockHandler);
        this.sut = new SpamScanTools(this.storageService, this.humanReviewService, this.testProvider);
    }

    /// <summary>
    /// Tests that GetInboxEmailsAsync returns emails when Graph API responds.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetInboxEmailsAsyncReturnsEmailsWhenGraphApiSucceeds()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders", GraphApiTestResponses.MessagesResponse);

        // Act
        var result = await this.sut.GetInboxEmailsAsync(10);

        // Assert
        Assert.Contains("AAMkAGVmMessage1", result, StringComparison.Ordinal);
        Assert.Contains("john@example.com", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that GetInboxEmailsAsync returns empty array when no messages.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetInboxEmailsAsyncReturnsEmptyWhenNoMessages()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders", GraphApiTestResponses.EmptyMessagesResponse);

        // Act
        var result = await this.sut.GetInboxEmailsAsync(10);

        // Assert
        Assert.Contains("[]", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that GetInboxEmailsAsync handles API errors gracefully.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetInboxEmailsAsyncHandlesApiErrorGracefully()
    {
        // Arrange
        this.mockHandler.SetupResponse(
            "mailFolders",
            HttpStatusCode.InternalServerError,
            GraphApiTestResponses.ErrorResponse("ServerError", "Internal server error"));

        // Act
        var result = await this.sut.GetInboxEmailsAsync(10);

        // Assert
        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that ReadEmailAsync returns email details when found.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ReadEmailAsyncReturnsDetailsWhenFound()
    {
        // Arrange
        this.mockHandler.SetupResponse("messages", GraphApiTestResponses.SingleMessageResponse);

        // Act
        var result = await this.sut.ReadEmailAsync("AAMkAGVmMessage1");

        // Assert
        Assert.Contains("Test Email Subject", result, StringComparison.Ordinal);
        Assert.Contains("sender@example.com", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that ReadEmailAsync handles message not found.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ReadEmailAsyncHandlesMessageNotFound()
    {
        // Arrange
        this.mockHandler.SetupResponse(
            "messages",
            HttpStatusCode.NotFound,
            GraphApiTestResponses.ErrorResponse("ErrorItemNotFound", "Not found"));

        // Act
        var result = await this.sut.ReadEmailAsync("nonexistent");

        // Assert
        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that ReadEmailAsync handles null body gracefully.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ReadEmailAsyncHandlesNullBodyGracefully()
    {
        // Arrange
        this.mockHandler.SetupResponse("messages", GraphApiTestResponses.MinimalMessageResponse);

        // Act
        var result = await this.sut.ReadEmailAsync("AAMkMinimal");

        // Assert
        Assert.Contains("AAMkMinimal", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that GetInboxEmailsAsync filters processed messages.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetInboxEmailsAsyncFiltersProcessedMessages()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders", GraphApiTestResponses.MessagesResponse);

        // Act - first call processes messages
        var result1 = await this.sut.GetInboxEmailsAsync(10);
        Assert.Contains("AAMkAGVmMessage1", result1, StringComparison.Ordinal);

        // Act - second call returns empty
        var result2 = await this.sut.GetInboxEmailsAsync(10);

        // Assert
        Assert.Contains("[]", result2, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that GetPendingReviewCount returns zero when no pending reviews.
    /// </summary>
    [Fact]
    public void GetPendingReviewCountReturnsZeroWhenNoPending()
    {
        // Act
        var count = this.sut.GetPendingReviewCount();

        // Assert
        Assert.Equal(0, count);
    }

    /// <summary>
    /// Tests that GetKnownSpamDomains returns empty when no domains.
    /// </summary>
    [Fact]
    public void GetKnownSpamDomainsReturnsEmptyWhenNoDomains()
    {
        // Act
        var result = this.sut.GetKnownSpamDomains();

        // Assert
        Assert.Equal("[]", result);
    }

    /// <summary>
    /// Tests that GetPendingReviewDomains returns empty when no pending.
    /// </summary>
    [Fact]
    public void GetPendingReviewDomainsReturnsEmptyWhenNoPending()
    {
        // Act
        var result = this.sut.GetPendingReviewDomains();

        // Assert
        Assert.Equal("[]", result);
    }

    /// <summary>
    /// Tests that SetResultCallback sets callback successfully.
    /// </summary>
    [Fact]
    public void SetResultCallbackSetsCallback()
    {
        // Arrange
        SpamScanAgent.ScanResult? capturedResult = null;
        void Callback(SpamScanAgent.ScanResult r) => capturedResult = r;

        // Act
        this.sut.SetResultCallback(Callback);

        // Assert - no exception thrown
        Assert.Null(capturedResult);
    }

    /// <summary>
    /// Tests reading email with HTML body truncation.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ReadEmailAsyncTruncatesHtmlBody()
    {
        // Arrange
        const string htmlResponse = """
            {
                "id": "AAMkHtml",
                "subject": "HTML Email",
                "from": {"emailAddress": {"address": "html@example.com"}},
                "receivedDateTime": "2025-12-14T10:30:00Z",
                "bodyPreview": "Preview text",
                "body": {
                    "contentType": "html",
                    "content": "<html><body><p>This is <strong>HTML</strong> content</p></body></html>"
                }
            }
            """;
        this.mockHandler.SetupResponse("messages", htmlResponse);

        // Act
        var result = await this.sut.ReadEmailAsync("AAMkHtml");

        // Assert - HTML tags should be stripped
        Assert.DoesNotContain("<html>", result, StringComparison.Ordinal);
        Assert.DoesNotContain("<strong>", result, StringComparison.Ordinal);
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
            this.sut.Dispose();
            this.testProvider.Dispose();
            this.mockHandler.Dispose();

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
