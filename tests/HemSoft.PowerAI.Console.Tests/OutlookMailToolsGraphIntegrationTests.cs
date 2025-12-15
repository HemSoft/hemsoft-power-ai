// <copyright file="OutlookMailToolsGraphIntegrationTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using System.Net;

using HemSoft.PowerAI.Console.Configuration;
using HemSoft.PowerAI.Console.Services;
using HemSoft.PowerAI.Console.Tools;

/// <summary>
/// Integration tests for <see cref="OutlookMailTools"/> that use mocked Graph API responses.
/// </summary>
[Collection("EnvironmentVariableTests")]
public class OutlookMailToolsGraphIntegrationTests : IDisposable
{
    private readonly string testDirectory;
    private readonly SpamFilterSettings settings;
    private readonly SpamStorageService storageService;
    private readonly MockHttpMessageHandler mockHandler;
    private readonly TestGraphClientProvider testProvider;
    private readonly OutlookMailTools sut;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookMailToolsGraphIntegrationTests"/> class.
    /// </summary>
    public OutlookMailToolsGraphIntegrationTests()
    {
        this.testDirectory = Path.Combine(Path.GetTempPath(), "OutlookMailToolsGraphTests_" + Guid.NewGuid().ToString("N")[..8]);
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
        this.sut = new OutlookMailTools(this.testProvider, this.storageService);
    }

    /// <summary>
    /// Tests that inbox mode returns messages when Graph API responds successfully.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task InboxModeReturnsMessagesWhenGraphApiSucceeds()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders", GraphApiTestResponses.MessagesResponse);

        // Act
        var result = await this.sut.MailAsync("inbox");

        // Assert
        Assert.Contains("Test Subject 1", result, StringComparison.Ordinal);
        Assert.Contains("john@example.com", result, StringComparison.Ordinal);
        Assert.Contains("Test Subject 2", result, StringComparison.Ordinal);
        Assert.Contains("jane@example.com", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that inbox mode returns empty message when folder is empty.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task InboxModeReturnsEmptyMessageWhenFolderEmpty()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders", GraphApiTestResponses.EmptyMessagesResponse);

        // Act
        var result = await this.sut.MailAsync("inbox");

        // Assert
        Assert.Contains("folder is empty", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that read mode returns message content successfully.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ReadModeReturnsMessageContent()
    {
        // Arrange
        this.mockHandler.SetupResponse("messages/AAMkTestMessage", GraphApiTestResponses.SingleMessageResponse);

        // Act
        var result = await this.sut.MailAsync("read", "AAMkTestMessage");

        // Assert
        Assert.Contains("Test Email Subject", result, StringComparison.Ordinal);
        Assert.Contains("sender@example.com", result, StringComparison.Ordinal);
        Assert.Contains("This is the test email body content", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that read mode returns error for missing message ID.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ReadModeReturnsErrorForMissingMessageId()
    {
        // Act
        var result = await this.sut.MailAsync("read");

        // Assert
        Assert.Contains("Message ID required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that read mode handles message not found error.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ReadModeHandlesNotFoundError()
    {
        // Arrange
        this.mockHandler.SetupError(
            "messages/NotFoundId",
            HttpStatusCode.NotFound,
            "ErrorItemNotFound",
            "The specified object was not found.");

        // Act
        var result = await this.sut.MailAsync("read", "NotFoundId");

        // Assert
        Assert.Contains("Error reading message", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that count mode returns folder statistics.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task CountModeReturnsFolderStatistics()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders/inbox", GraphApiTestResponses.FolderInfoResponse);

        // Act
        var result = await this.sut.MailAsync("count", "inbox");

        // Assert
        Assert.Contains("Inbox", result, StringComparison.Ordinal);
        Assert.Contains("42", result, StringComparison.Ordinal);
        Assert.Contains("5 unread", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that count mode handles null values gracefully.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task CountModeHandlesNullValues()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders/customfolder", GraphApiTestResponses.FolderInfoNullValuesResponse);

        // Act
        var result = await this.sut.MailAsync("count", "customfolder");

        // Assert
        Assert.Contains("customfolder", result, StringComparison.Ordinal);
        Assert.Contains("0 emails", result, StringComparison.Ordinal);
        Assert.Contains("0 unread", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that send mode validates required parameters.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task SendModeValidatesRecipient()
    {
        // Act
        var result = await this.sut.MailAsync("send");

        // Assert
        Assert.Contains("Recipient email required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that send mode validates subject.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task SendModeValidatesSubject()
    {
        // Act
        var result = await this.sut.MailAsync("send", "test@example.com");

        // Assert
        Assert.Contains("Subject required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that send mode sends email successfully.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task SendModeSendsEmailSuccessfully()
    {
        // Arrange
        this.mockHandler.SetupResponse("sendMail", HttpStatusCode.Accepted, "{}");

        // Act
        var result = await this.sut.MailAsync(
            "send",
            "recipient@example.com",
            "Test Subject",
            "Test Body");

        // Assert
        Assert.Contains("Email sent to recipient@example.com", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that delete mode validates message ID.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task DeleteModeValidatesMessageId()
    {
        // Act
        var result = await this.sut.MailAsync("delete");

        // Assert
        Assert.Contains("Message ID required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that delete mode deletes message successfully.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task DeleteModeDeletesMessageSuccessfully()
    {
        // Arrange
        this.mockHandler.SetupResponse("messages/TestMsgId", HttpStatusCode.NoContent, string.Empty);

        // Act
        var result = await this.sut.MailAsync("delete", "TestMsgId12345678");

        // Assert
        Assert.Contains("Deleted message TestMsgI", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that move mode validates message ID.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task MoveModeValidatesMessageId()
    {
        // Act
        var result = await this.sut.MailAsync("move");

        // Assert
        Assert.Contains("Message ID required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that move mode moves message successfully.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task MoveModeMovesMessageSuccessfully()
    {
        // Arrange
        this.mockHandler.SetupResponse("messages/TestMsgId", GraphApiTestResponses.MovedMessageResponse);

        // Act
        var result = await this.sut.MailAsync("move", "TestMsgId12345678", "junkemail");

        // Assert
        Assert.Contains("Moved message TestMsgI", result, StringComparison.Ordinal);
        Assert.Contains("junkemail", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that search mode validates query.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task SearchModeValidatesQuery()
    {
        // Act
        var result = await this.sut.MailAsync("search");

        // Assert
        Assert.Contains("Search query required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that search mode returns results.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task SearchModeReturnsResults()
    {
        // Arrange
        this.mockHandler.SetupResponse("messages", GraphApiTestResponses.SearchResultsResponse);

        // Act
        var result = await this.sut.MailAsync("search", "test query");

        // Assert
        Assert.Contains("Search Match Subject", result, StringComparison.Ordinal);
        Assert.Contains("search@example.com", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that search mode returns no results message.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task SearchModeReturnsNoResultsMessage()
    {
        // Arrange
        this.mockHandler.SetupResponse("messages", GraphApiTestResponses.EmptyMessagesResponse);

        // Act
        var result = await this.sut.MailAsync("search", "nonexistent query");

        // Assert
        Assert.Contains("No messages found", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that batchdelete mode validates message IDs.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task BatchdeleteModeValidatesMessageIds()
    {
        // Act
        var result = await this.sut.MailAsync("batchdelete");

        // Assert
        Assert.Contains("Message IDs required", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that batchdelete mode handles empty IDs.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task BatchdeleteModeHandlesEmptyIds()
    {
        // Act
        var result = await this.sut.MailAsync("batchdelete", "   ,  ,   ");

        // Assert
        Assert.Contains("No valid message IDs", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that batchdelete mode deletes multiple messages.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task BatchdeleteModeDeletesMultipleMessages()
    {
        // Arrange
        this.mockHandler.SetupResponse("messages", HttpStatusCode.NoContent, string.Empty);

        // Act
        var result = await this.sut.MailAsync("batchdelete", "msg1,msg2,msg3");

        // Assert
        Assert.Contains("Deleted", result, StringComparison.Ordinal);
        Assert.Contains("/3", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that batchdelete mode handles partial failures.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task BatchdeleteModeHandlesPartialFailures()
    {
        // Arrange - setup one success and one failure
        this.mockHandler.SetupResponse("messages/msg1", HttpStatusCode.NoContent, string.Empty);
        this.mockHandler.SetupError("messages/msg2", HttpStatusCode.NotFound, "ErrorItemNotFound", "Not found");

        // Act
        var result = await this.sut.MailAsync("batchdelete", "msg1,msg2");

        // Assert - should show partial success
        Assert.Contains("Deleted", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that folder mode uses custom folder name.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task FolderModeUsesCustomFolderName()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders/customfolder", GraphApiTestResponses.EmptyMessagesResponse);

        // Act
        var result = await this.sut.MailAsync("folder", "customfolder");

        // Assert
        Assert.Contains("folder is empty", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that junk mode lists junk folder when no message ID provided.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task JunkModeListsJunkFolderWhenNoMessageId()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders/junkemail", GraphApiTestResponses.MessagesResponse);

        // Act
        var result = await this.sut.MailAsync("junk");

        // Assert
        Assert.Contains("Test Subject 1", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that junk mode moves message when message ID provided.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task JunkModeMoveMessageWhenMessageIdProvided()
    {
        // Arrange
        this.mockHandler.SetupResponse("messages/MsgToMove12", GraphApiTestResponses.MovedMessageResponse);

        // Act
        var result = await this.sut.MailAsync("junk", "MsgToMove12345678");

        // Assert
        Assert.Contains("Moved message MsgToMov", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that blocklist mode returns spam domains via Graph client path.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task BlocklistModeReturnsSpamDomains()
    {
        // Arrange
        _ = this.storageService.AddSpamDomain("test.com", "Test reason");

        // Act
        var result = await this.sut.MailAsync("blocklist");

        // Assert
        Assert.Contains("TEST.COM", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that blockadd mode adds spam domain via Graph client path.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task BlockaddModeAddsSpamDomain()
    {
        // Act
        var result = await this.sut.MailAsync("blockadd", "newspam.com", "Testing");

        // Assert
        Assert.Contains("Added", result, StringComparison.Ordinal);
        Assert.Contains("NEWSPAM.COM", result, StringComparison.Ordinal);
        Assert.True(this.storageService.IsKnownSpamDomain("newspam.com"));
    }

    /// <summary>
    /// Tests that blockcheck mode checks spam domain via Graph client path.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task BlockcheckModeChecksSpamDomain()
    {
        // Arrange
        _ = this.storageService.AddSpamDomain("known.com", "Known spam");

        // Act
        var result = await this.sut.MailAsync("blockcheck", "known.com");

        // Assert
        Assert.Contains("IS in the spam registry", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that unknown mode returns error message.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task UnknownModeReturnsErrorMessage()
    {
        // Act
        var result = await this.sut.MailAsync("unknownmode");

        // Assert
        Assert.Contains("Unknown mode", result, StringComparison.Ordinal);
        Assert.Contains("inbox", result, StringComparison.Ordinal);
        Assert.Contains("send", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that read mode handles null message response.
    /// </summary>
    /// <remarks>
    /// When the Graph API returns "null", the SDK deserializes it as an empty message object
    /// with null fields, not as a null message reference.
    /// </remarks>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ReadModeHandlesNullMessageResponse()
    {
        // Arrange - return null instead of JSON - SDK deserializes to empty message object
        this.mockHandler.SetupResponse("messages/NullMsg", HttpStatusCode.OK, "null");

        // Act
        var result = await this.sut.MailAsync("read", "NullMsg");

        // Assert - empty message object is formatted with empty fields
        Assert.Contains("Subject:", result, StringComparison.Ordinal);
        Assert.Contains("From:", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that read mode handles message with null body.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ReadModeHandlesMessageWithNullBody()
    {
        // Arrange
        this.mockHandler.SetupResponse("messages/EmptyBody", GraphApiTestResponses.MessageWithEmptyBodyResponse);

        // Act
        var result = await this.sut.MailAsync("read", "EmptyBody");

        // Assert
        Assert.Contains("Subject Only", result, StringComparison.Ordinal);
        Assert.Contains("(No content)", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that read mode handles message with minimal fields.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ReadModeHandlesMessageWithMinimalFields()
    {
        // Arrange
        this.mockHandler.SetupResponse("messages/Minimal", GraphApiTestResponses.MinimalMessageResponse);

        // Act
        var result = await this.sut.MailAsync("read", "Minimal");

        // Assert
        Assert.Contains("Subject:", result, StringComparison.Ordinal);
        Assert.Contains("From:", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that JUNKMAIL mode alias lists junk folder.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task JunkmailModeListsJunkFolder()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders/junkemail", GraphApiTestResponses.MessagesResponse);

        // Act
        var result = await this.sut.MailAsync("junkmail");

        // Assert
        Assert.Contains("Test Subject 1", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that SPAM mode alias lists junk folder.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task SpamModeListsJunkFolder()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders/junkemail", GraphApiTestResponses.MessagesResponse);

        // Act
        var result = await this.sut.MailAsync("spam");

        // Assert
        Assert.Contains("Test Subject 1", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that folder mode returns inbox when param1 is null.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task FolderModeUsesInboxWhenParam1Null()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders/inbox", GraphApiTestResponses.MessagesResponse);

        // Act
        var result = await this.sut.MailAsync("folder");

        // Assert
        Assert.Contains("Test Subject 1", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that count mode handles OData error.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task CountModeHandlesODataError()
    {
        // Arrange
        this.mockHandler.SetupError(
            "mailFolders/badFolder",
            HttpStatusCode.NotFound,
            "ErrorItemNotFound",
            "The folder was not found.");

        // Act
        var result = await this.sut.MailAsync("count", "badFolder");

        // Assert
        Assert.Contains("Error counting", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that inbox mode handles OData error.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task InboxModeHandlesODataError()
    {
        // Arrange
        this.mockHandler.SetupError(
            "mailFolders/inbox",
            HttpStatusCode.Forbidden,
            "ErrorAccessDenied",
            "Access denied.");

        // Act
        var result = await this.sut.MailAsync("inbox");

        // Assert
        Assert.Contains("Error listing", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that delete mode handles OData error gracefully.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task DeleteModeHandlesODataError()
    {
        // Arrange
        this.mockHandler.SetupError(
            "messages/FailDelete",
            HttpStatusCode.NotFound,
            "ErrorItemNotFound",
            "Message not found.");

        // Act
        var result = await this.sut.MailAsync("delete", "FailDelete1234567");

        // Assert
        Assert.Contains("Error deleting", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that move mode handles OData error gracefully.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task MoveModeHandlesODataError()
    {
        // Arrange
        this.mockHandler.SetupError(
            "messages/FailMove",
            HttpStatusCode.NotFound,
            "ErrorItemNotFound",
            "Message not found.");

        // Act
        var result = await this.sut.MailAsync("move", "FailMove12345678", "archive");

        // Assert
        Assert.Contains("Error moving", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that send mode handles OData error gracefully.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task SendModeHandlesODataError()
    {
        // Arrange
        this.mockHandler.SetupError(
            "sendMail",
            HttpStatusCode.BadRequest,
            "ErrorInvalidRecipients",
            "Invalid recipient.");

        // Act
        var result = await this.sut.MailAsync("send", "bad@invalid", "Subject", "Body");

        // Assert
        Assert.Contains("Error sending", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that search mode handles OData error gracefully.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task SearchModeHandlesODataError()
    {
        // Arrange
        this.mockHandler.SetupError(
            "messages",
            HttpStatusCode.BadRequest,
            "ErrorSearchQueryInvalid",
            "Invalid search query.");

        // Act
        var result = await this.sut.MailAsync("search", "bad query []");

        // Assert
        Assert.Contains("Error searching", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that count mode returns folder not found for null folder info.
    /// </summary>
    /// <remarks>
    /// When the Graph API returns null for a folder, the SDK deserializes it as empty object
    /// with default values (0 items), not as null reference.
    /// </remarks>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task CountModeReturnsDefaultValuesForNullFolderInfo()
    {
        // Arrange - return null JSON - SDK deserializes as empty folder object with defaults
        this.mockHandler.SetupResponse("mailFolders/nullfolder", HttpStatusCode.OK, "null");

        // Act
        var result = await this.sut.MailAsync("count", "nullfolder");

        // Assert - defaults to folder name and 0 counts
        Assert.Contains("0 emails", result, StringComparison.Ordinal);
        Assert.Contains("0 unread", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that count mode works with drafts folder.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task CountModeWorkWithDraftsFolder()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders/drafts", GraphApiTestResponses.FolderInfoResponse);

        // Act
        var result = await this.sut.MailAsync("count", "drafts");

        // Assert - This exercises the "DRAFTS" => FolderDrafts branch
        Assert.Contains("emails", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that folder mode works with sent items folder.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task FolderModeWorksWithSentItemsFolder()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders/sentitems", GraphApiTestResponses.MessagesResponse);

        // Act
        var result = await this.sut.MailAsync("folder", "sent");

        // Assert
        Assert.Contains("Test Subject 1", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that folder mode works with deleted items folder.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task FolderModeWorksWithDeletedItemsFolder()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders/deleteditems", GraphApiTestResponses.MessagesResponse);

        // Act
        var result = await this.sut.MailAsync("folder", "trash");

        // Assert
        Assert.Contains("Test Subject 1", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that folder mode works with archive folder.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task FolderModeWorksWithArchiveFolder()
    {
        // Arrange
        this.mockHandler.SetupResponse("mailFolders/archive", GraphApiTestResponses.EmptyMessagesResponse);

        // Act
        var result = await this.sut.MailAsync("folder", "archive");

        // Assert
        Assert.Contains("folder is empty", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that read mode handles empty message ID.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ReadModeHandlesEmptyMessageId()
    {
        // Act - param1 is the messageId for read mode
        var result = await this.sut.MailAsync("read", string.Empty);

        // Assert
        Assert.Contains("Message ID required", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that delete mode handles null message ID.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task DeleteModeHandlesNullMessageId()
    {
        // Act - param1 is the messageId for delete mode
        var result = await this.sut.MailAsync("delete", param1: null);

        // Assert
        Assert.Contains("Message ID required", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that search mode handles empty query.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task SearchModeHandlesEmptyQuery()
    {
        // Act - param1 is the query for search mode
        var result = await this.sut.MailAsync("search", string.Empty);

        // Assert
        Assert.Contains("query required", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that batch delete mode handles empty message IDs.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task BatchDeleteModeHandlesEmptyMessageIds()
    {
        // Act - param1 is comma-separated message IDs for batchdelete mode
        var result = await this.sut.MailAsync("batchdelete", string.Empty);

        // Assert
        Assert.Contains("Message IDs required", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that send mode handles missing recipients.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task SendModeHandlesMissingRecipients()
    {
        // Act - param1=to, param2=subject, param3=body for send mode
        var result = await this.sut.MailAsync("send", string.Empty, "Test Subject");

        // Assert
        Assert.Contains("Recipient email required", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that delete mode falls back to inbox path when root message path fails.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task DeleteModeFallsBackToInboxPath()
    {
        // Arrange - root path fails, inbox path succeeds
        this.mockHandler.SetupError("me/messages/msg123456", HttpStatusCode.NotFound, "ErrorItemNotFound", "Not found");
        this.mockHandler.SetupResponse("mailFolders/inbox/messages/msg123456", HttpStatusCode.NoContent, string.Empty);

        // Act
        var result = await this.sut.MailAsync("delete", "msg12345678901234");

        // Assert - should succeed via fallback
        Assert.Contains("Deleted message", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that move mode falls back to inbox path when root message path fails.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task MoveModeFallsBackToInboxPath()
    {
        // Arrange - root path fails, inbox path succeeds
        this.mockHandler.SetupError("me/messages/msg123456/move", HttpStatusCode.NotFound, "ErrorItemNotFound", "Not found");
        this.mockHandler.SetupResponse("mailFolders/inbox/messages/msg123456/move", GraphApiTestResponses.MovedMessageResponse);

        // Act
        var result = await this.sut.MailAsync("move", "msg12345678901234", "archive");

        // Assert - should succeed via fallback
        Assert.Contains("Moved message", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that search mode handles timeout.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task SearchModeHandlesTimeout()
    {
        // Arrange - set up a delayed response that will time out
        this.mockHandler.SetupError("messages", HttpStatusCode.RequestTimeout, "Timeout", "Request timeout");

        // Act
        var result = await this.sut.MailAsync("search", "test query");

        // Assert
        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
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
