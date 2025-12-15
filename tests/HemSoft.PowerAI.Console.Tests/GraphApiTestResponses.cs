// <copyright file="GraphApiTestResponses.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

/// <summary>
/// Provides sample Graph API JSON responses for testing.
/// </summary>
internal static class GraphApiTestResponses
{
    /// <summary>
    /// Gets a sample messages response with multiple messages.
    /// </summary>
    public static string MessagesResponse => """
        {
            "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#me/mailFolders('inbox')/messages",
            "value": [
                {
                    "id": "AAMkAGVmMessage1",
                    "subject": "Test Subject 1",
                    "from": {
                        "emailAddress": {
                            "name": "John Doe",
                            "address": "john@example.com"
                        }
                    },
                    "receivedDateTime": "2025-12-14T10:30:00Z",
                    "isRead": true,
                    "parentFolderId": "inbox"
                },
                {
                    "id": "AAMkAGVmMessage2",
                    "subject": "Test Subject 2",
                    "from": {
                        "emailAddress": {
                            "name": "Jane Smith",
                            "address": "jane@example.com"
                        }
                    },
                    "receivedDateTime": "2025-12-14T09:15:00Z",
                    "isRead": false,
                    "parentFolderId": "inbox"
                }
            ]
        }
        """;

    /// <summary>
    /// Gets a sample empty messages response.
    /// </summary>
    public static string EmptyMessagesResponse => """
        {
            "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#me/mailFolders('inbox')/messages",
            "value": []
        }
        """;

    /// <summary>
    /// Gets a sample single message response for read operations.
    /// </summary>
    public static string SingleMessageResponse => """
        {
            "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#me/messages/$entity",
            "id": "AAMkAGVmMessage1",
            "subject": "Test Email Subject",
            "from": {
                "emailAddress": {
                    "name": "Sender Name",
                    "address": "sender@example.com"
                }
            },
            "toRecipients": [
                {
                    "emailAddress": {
                        "name": "Recipient Name",
                        "address": "recipient@example.com"
                    }
                }
            ],
            "receivedDateTime": "2025-12-14T10:30:00Z",
            "body": {
                "contentType": "text",
                "content": "This is the test email body content."
            }
        }
        """;

    /// <summary>
    /// Gets a sample folder info response.
    /// </summary>
    public static string FolderInfoResponse => """
        {
            "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#me/mailFolders/$entity",
            "id": "inbox",
            "displayName": "Inbox",
            "totalItemCount": 42,
            "unreadItemCount": 5
        }
        """;

    /// <summary>
    /// Gets a sample junk folder info response.
    /// </summary>
    public static string JunkFolderInfoResponse => """
        {
            "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#me/mailFolders/$entity",
            "id": "junkemail",
            "displayName": "Junk Email",
            "totalItemCount": 15,
            "unreadItemCount": 15
        }
        """;

    /// <summary>
    /// Gets a sample moved message response.
    /// </summary>
    public static string MovedMessageResponse => """
        {
            "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#me/messages/$entity",
            "id": "AAMkAGVmMessage1New",
            "subject": "Test Email Subject",
            "parentFolderId": "junkemail"
        }
        """;

    /// <summary>
    /// Gets a sample search results response.
    /// </summary>
    public static string SearchResultsResponse => """
        {
            "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#me/messages",
            "value": [
                {
                    "id": "AAMkSearchResult1",
                    "subject": "Search Match Subject",
                    "from": {
                        "emailAddress": {
                            "name": "Search Sender",
                            "address": "search@example.com"
                        }
                    },
                    "receivedDateTime": "2025-12-13T15:00:00Z",
                    "parentFolderId": "inbox"
                }
            ]
        }
        """;

    /// <summary>
    /// Gets a rate limit (429) error response.
    /// </summary>
    public static string RateLimitErrorResponse => """
        {
            "error": {
                "code": "TooManyRequests",
                "message": "Too many requests. Please retry after some time."
            }
        }
        """;

    /// <summary>
    /// Gets a folder info response with null values for edge case testing.
    /// </summary>
    public static string FolderInfoNullValuesResponse => """
        {
            "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#me/mailFolders/$entity",
            "id": "customfolder"
        }
        """;

    /// <summary>
    /// Gets a message response with minimal/null fields for edge case testing.
    /// </summary>
    public static string MinimalMessageResponse => """
        {
            "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#me/messages/$entity",
            "id": "AAMkMinimal",
            "subject": null,
            "from": null,
            "toRecipients": null,
            "receivedDateTime": null,
            "body": null
        }
        """;

    /// <summary>
    /// Gets a message response with empty body.
    /// </summary>
    public static string MessageWithEmptyBodyResponse => """
        {
            "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#me/messages/$entity",
            "id": "AAMkEmptyBody",
            "subject": "Subject Only",
            "from": {
                "emailAddress": {
                    "name": "Test",
                    "address": "test@example.com"
                }
            },
            "toRecipients": [],
            "receivedDateTime": "2025-12-14T10:30:00Z",
            "body": {
                "contentType": "text",
                "content": null
            }
        }
        """;

    /// <summary>
    /// Gets an OData error response for not found.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>JSON error response.</returns>
    public static string ErrorResponse(string code, string message) =>
        $"{{\"error\": {{\"code\": \"{code}\", \"message\": \"{message}\"}}}}";
}
