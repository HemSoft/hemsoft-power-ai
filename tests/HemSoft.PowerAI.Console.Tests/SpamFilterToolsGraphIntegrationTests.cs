// <copyright file="SpamFilterToolsGraphIntegrationTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using System.Net;

using HemSoft.PowerAI.Console.Configuration;
using HemSoft.PowerAI.Console.Models;
using HemSoft.PowerAI.Console.Services;
using HemSoft.PowerAI.Console.Tools;

/// <summary>
/// Integration tests for <see cref="SpamFilterTools"/> that use mocked Graph API responses.
/// </summary>
[Collection("EnvironmentVariableTests")]
public class SpamFilterToolsGraphIntegrationTests : IDisposable
{
    private readonly string testDirectory;
    private readonly SpamFilterSettings settings;
    private readonly SpamStorageService storageService;
    private readonly MockHttpMessageHandler mockHandler;
    private readonly TestGraphClientProvider testProvider;
    private readonly SpamFilterTools sut;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpamFilterToolsGraphIntegrationTests"/> class.
    /// </summary>
    public SpamFilterToolsGraphIntegrationTests()
    {
        this.testDirectory = Path.Combine(
            Path.GetTempPath(),
            "SpamFilterToolsGraphTests_" + Guid.NewGuid().ToString("N")[..8]);
        _ = Directory.CreateDirectory(this.testDirectory);

        this.settings = new SpamFilterSettings
        {
            HumanReviewFilePath = Path.Combine(this.testDirectory, "human_review.json"),
            SpamDomainsFilePath = "spam_domains.json",
            SpamCandidatesFilePath = "spam_candidates.json",
        };

        this.storageService = new SpamStorageService(this.settings, this.testDirectory);
        this.mockHandler = new MockHttpMessageHandler();
        this.testProvider = new TestGraphClientProvider(this.mockHandler);
        this.sut = new SpamFilterTools(this.storageService, this.testProvider);
    }

    /// <summary>
    /// Tests that GetInboxEmailsAsync returns emails when Graph API responds successfully.
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
        Assert.Contains("Test Subject 1", result, StringComparison.Ordinal);
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

        // Assert - should return error message rather than throwing
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
        Assert.Contains("test email body", result, StringComparison.OrdinalIgnoreCase);
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
            GraphApiTestResponses.ErrorResponse("ErrorItemNotFound", "The specified object was not found"));

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
    /// Tests that MoveToJunkAsync moves message successfully.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task MoveToJunkAsyncMovesMessageSuccessfully()
    {
        // Arrange
        this.mockHandler.SetupResponse("move", GraphApiTestResponses.MovedMessageResponse);

        // Act
        var result = await this.sut.MoveToJunkAsync("AAMkAGVmMessage1");

        // Assert
        Assert.Contains("Successfully", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that MoveToJunkAsync handles API error.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task MoveToJunkAsyncHandlesApiError()
    {
        // Arrange
        this.mockHandler.SetupResponse(
            "move",
            HttpStatusCode.NotFound,
            GraphApiTestResponses.ErrorResponse("ErrorItemNotFound", "Message not found"));

        // Act
        var result = await this.sut.MoveToJunkAsync("nonexistent");

        // Assert
        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that MoveToJunkAsync returns error for empty message ID.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task MoveToJunkAsyncReturnsErrorForEmptyMessageId()
    {
        // Act
        var result = await this.sut.MoveToJunkAsync(string.Empty);

        // Assert
        Assert.Contains("messageId is required", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that GetInboxEmailsAsync filters already processed messages.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task GetInboxEmailsAsyncFiltersProcessedMessages()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders", GraphApiTestResponses.MessagesResponse);

        // Act - first call processes all messages
        var result1 = await this.sut.GetInboxEmailsAsync(10);
        Assert.Contains("AAMkAGVmMessage1", result1, StringComparison.Ordinal);

        // Act - second call should return empty because messages are already processed
        var result2 = await this.sut.GetInboxEmailsAsync(10);

        // Assert - second call returns empty array (no new unprocessed messages)
        Assert.Contains("[]", result2, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that ProcessApprovedSpamDomainAsync adds domain to spam list.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ProcessApprovedSpamDomainAsyncAddsDomain()
    {
        // Arrange
        this.mockHandler.SetupResponse("move", GraphApiTestResponses.MovedMessageResponse);

        // Act
        var result = await this.sut.ProcessApprovedSpamDomainAsync("EXAMPLE.COM");

        // Assert
        Assert.Contains("Processed domain", result, StringComparison.OrdinalIgnoreCase);
        Assert.True(this.storageService.IsKnownSpamDomain("EXAMPLE.COM"));
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
                    "content": "<html><body><p>This is <strong>HTML</strong> content with tags</p></body></html>"
                }
            }
            """;
        this.mockHandler.SetupResponse("messages", htmlResponse);

        // Act
        var result = await this.sut.ReadEmailAsync("AAMkHtml");

        // Assert - HTML tags should be stripped
        Assert.DoesNotContain("<html>", result, StringComparison.Ordinal);
        Assert.DoesNotContain("<strong>", result, StringComparison.Ordinal);
        Assert.Contains("HTML content", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that ProcessApprovedSpamDomainAsync handles move errors correctly.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ProcessApprovedSpamDomainAsyncHandlesMovementErrors()
    {
        // Arrange - Add a candidate to spam storage first
        const string testDomain = "movetest.com";
        const string testMessageId = "AAMkMoveFail123456";

        // Add candidate directly to storage for testing
        var candidate = new SpamCandidate
        {
            MessageId = testMessageId,
            SenderEmail = $"user@{testDomain}",
            SenderDomain = testDomain,
            Subject = "Test Subject",
            ConfidenceScore = 0.8,
            SpamReason = "Test reason",
            ReceivedAt = DateTimeOffset.UtcNow,
            IdentifiedAt = DateTimeOffset.UtcNow,
        };
        _ = this.storageService.AddSpamCandidate(candidate);

        // Set up the Graph mock to return an error for ALL requests
        // by setting the default response to 404
        this.mockHandler.DefaultStatusCode = HttpStatusCode.NotFound;
        this.mockHandler.DefaultContent =
            """{"error": {"code": "ErrorItemNotFound", "message": "The specified message could not be found."}}""";

        // Act
        var result = await this.sut.ProcessApprovedSpamDomainAsync(testDomain);

        // Assert - Should report errors but still mark domain as spam
        Assert.Contains("Processed domain", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Errors: 1", result, StringComparison.Ordinal);
        Assert.True(this.storageService.IsKnownSpamDomain(testDomain));
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
