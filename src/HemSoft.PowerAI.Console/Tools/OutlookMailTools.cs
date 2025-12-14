// <copyright file="OutlookMailTools.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tools;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;

using Azure.Identity;

using HemSoft.PowerAI.Console.Configuration;
using HemSoft.PowerAI.Console.Extensions;
using HemSoft.PowerAI.Console.Services;

using Microsoft.Graph;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Identity.Client;

/// <summary>
/// Provides Outlook/Hotmail mail tools for the AI agent using Microsoft Graph.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Graph API operations require authentication")]
internal static class OutlookMailTools
{
    private const string AuthError = "authentication";
    private const int DefaultMaxResults = 10;

    /// <summary>
    /// Graph API max per batch request.
    /// </summary>
    private const int BatchSize = 20;

    private const int MaxRetries = 3;
    private const int BaseDelayMs = 1000;
    private const int SearchTimeoutSeconds = 120;
    private const string FolderInbox = "inbox";
    private const string FolderJunk = "junkemail";
    private const string FolderSent = "sentitems";
    private const string FolderDrafts = "drafts";
    private const string FolderDeleted = "deleteditems";
    private const string FolderArchive = "archive";

    private static string? lastOperation;
    private static SpamStorageService? spamStorage;

    /// <summary>
    /// Accesses Outlook/Hotmail mailbox. Supports personal Microsoft accounts (hotmail.com, outlook.com).
    /// </summary>
    /// <param name="mode">Operation: 'inbox', 'spam', 'folder', 'read', 'send', 'search', 'delete', 'batchdelete', 'move', 'junk', 'count', 'spamlist', 'spamadd', 'spamcheck'.</param>
    /// <param name="param1">Context-dependent: message ID for read/delete/move/junk, recipient for send, query for search, folder name for 'folder'/'count', comma-separated IDs for 'batchdelete', domain for 'blockadd'/'blockcheck'.</param>
    /// <param name="param2">For 'send': subject. For 'move': destination folder (inbox, archive, deleteditems, junkemail). For 'blockadd': reason.</param>
    /// <param name="param3">For 'send': body.</param>
    /// <param name="maxResults">Max results for inbox/search (default: 10).</param>
    /// <returns>Result message or error.</returns>
    [Description(
        "Access Outlook/Hotmail mailbox. Modes: 'inbox' (list), 'junk' (list junk folder OR mark message as junk with id), " +
        "'folder' (list by name), 'read' (id), 'send' (to,subject,body), 'search' (query), 'delete' (id), " +
        "'batchdelete' (comma-separated ids), 'move' (id,folder), 'count' (folder stats), " +
        "'blocklist' (list blocked domains), 'blockadd' (domain,reason), 'blockcheck' (domain).")]
    public static async Task<string> MailAsync(
        string mode,
        string? param1 = null,
        string? param2 = null,
        string? param3 = null,
        int maxResults = DefaultMaxResults)
    {
        TrackOperation(mode);

        var client = SharedGraphClient.GetClient();
        if (client is null)
        {
            return $"Error: Set {SharedGraphClient.ClientIdEnvVar} environment variable. " +
                "Register app at https://entra.microsoft.com with 'Personal Microsoft accounts' support.";
        }

        maxResults = Math.Clamp(maxResults, 1, 50);

        return mode?.ToUpperInvariant() switch
        {
            "INBOX" => await ListFolderAsync(client, FolderInbox, maxResults).ConfigureAwait(false),
            "JUNK" or "JUNKMAIL" or "SPAM" => param1 is null
                ? await ListFolderAsync(client, FolderJunk, maxResults).ConfigureAwait(false)
                : await MoveMessageAsync(client, param1, FolderJunk).ConfigureAwait(false),
            "FOLDER" => await ListFolderAsync(client, param1 ?? FolderInbox, maxResults).ConfigureAwait(false),
            "READ" => await ReadMessageAsync(client, param1).ConfigureAwait(false),
            "SEND" => await SendMailAsync(client, param1, param2, param3).ConfigureAwait(false),
            "SEARCH" => await SearchMailAsync(client, param1, maxResults).ConfigureAwait(false),
            "DELETE" => await DeleteMessageAsync(client, param1).ConfigureAwait(false),
            "BATCHDELETE" => await BatchDeleteMessagesAsync(client, param1).ConfigureAwait(false),
            "MOVE" => await MoveMessageAsync(client, param1, param2).ConfigureAwait(false),
            "COUNT" => await CountFolderAsync(client, param1).ConfigureAwait(false),
            "BLOCKLIST" => GetSpamDomainList(),
            "BLOCKADD" => AddToSpamDomainList(param1, param2),
            "BLOCKCHECK" => CheckSpamDomain(param1),
            _ => "Unknown mode. Use: inbox, junk, folder, read, send, search, delete, " +
                "batchdelete, move, count, blocklist, blockadd, blockcheck",
        };
    }

    /// <summary>
    /// Initializes the spam storage service for spam domain management.
    /// </summary>
    /// <param name="settings">The spam filter settings.</param>
    internal static void InitializeSpamStorage(SpamFilterSettings settings) =>
        spamStorage = new SpamStorageService(settings);

    /// <summary>
    /// Resets the operation tracking state.
    /// </summary>
    internal static void ResetOperationTracking() => lastOperation = null;

    private static string ResolveFolderName(string? folder, string defaultFolder = FolderInbox) =>
        folder?.ToUpperInvariant() switch
        {
            "INBOX" => FolderInbox,
            "JUNK" or "JUNKEMAIL" or "SPAM" => FolderJunk,
            "SENT" or "SENTITEMS" => FolderSent,
            "DRAFTS" => FolderDrafts,
            "DELETED" or "DELETEDITEMS" or "TRASH" => FolderDeleted,
            "ARCHIVE" => FolderArchive,
            null or "" => defaultFolder,
            _ => folder,
        };

    private static async Task<string> BatchDeleteMessagesAsync(GraphServiceClient client, string? messageIds)
    {
        if (string.IsNullOrWhiteSpace(messageIds))
        {
            return "Error: Message IDs required (comma-separated)";
        }

        var ids = messageIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (ids.Count == 0)
        {
            return "Error: No valid message IDs provided";
        }

        var (deleted, failed, retryCount) = await ExecuteBatchDeleteAsync(client, ids).ConfigureAwait(false);

        return FormatBatchResult(ids.Count, deleted, failed, retryCount);
    }

    private static async Task<string> CountFolderAsync(GraphServiceClient client, string? folder)
    {
        var folderName = ResolveFolderName(folder);

        try
        {
            var folderInfo = await client.Me.MailFolders[folderName].GetAsync(config =>
                config.QueryParameters.Select = ["displayName", "totalItemCount", "unreadItemCount"]).ConfigureAwait(false);

            if (folderInfo is null)
            {
                return $"Folder '{folder}' not found";
            }

            var displayName = folderInfo.DisplayName ?? folderName;
            var total = folderInfo.TotalItemCount ?? 0;
            var unread = folderInfo.UnreadItemCount ?? 0;

            return $"{displayName}: {total.ToInvariant()} emails ({unread.ToInvariant()} unread)";
        }
        catch (ODataError ex)
        {
            return FormatError($"counting {folder}", ex);
        }
        catch (AuthenticationFailedException ex)
        {
            return FormatError(AuthError, ex);
        }
        catch (MsalServiceException ex)
        {
            return FormatError(AuthError, ex);
        }
    }

    private static void TrackOperation(string mode)
    {
        var normalizedMode = mode?.ToUpperInvariant() ?? "UNKNOWN";

        if (!string.Equals(normalizedMode, lastOperation, StringComparison.Ordinal))
        {
            // New operation type - print it once
            lastOperation = normalizedMode;
            System.Console.WriteLine($"        Mail: {mode}");
        }
    }

    private static async Task<string> ListFolderAsync(GraphServiceClient client, string folderName, int maxResults)
    {
        try
        {
            var wellKnownFolder = ResolveFolderName(folderName);

            var messages = await client.Me.MailFolders[wellKnownFolder].Messages.GetAsync(config =>
            {
                config.Headers.Add("Prefer", "IdType=\"ImmutableId\"");
                config.QueryParameters.Top = maxResults;
                config.QueryParameters.Select = ["id", "subject", "from", "receivedDateTime", "isRead", "parentFolderId"];
                config.QueryParameters.Orderby = ["receivedDateTime desc"];
            }).ConfigureAwait(false);

            return messages?.Value is { Count: > 0 }
                ? FormatMessages(messages.Value, wellKnownFolder)
                : $"{folderName} folder is empty";
        }
        catch (ODataError ex)
        {
            return FormatError($"listing {folderName}", ex);
        }
        catch (AuthenticationFailedException ex)
        {
            return FormatError(AuthError, ex);
        }
        catch (MsalServiceException ex)
        {
            return FormatError(AuthError, ex);
        }
    }

    private static async Task<string> ReadMessageAsync(GraphServiceClient client, string? messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return "Error: Message ID required";
        }

        try
        {
            var message = await client.Me.Messages[messageId].GetAsync(config =>
                config.QueryParameters.Select = ["subject", "from", "toRecipients", "receivedDateTime", "body"]).ConfigureAwait(false);

            return message is null ? "Message not found" : FormatFullMessage(message);
        }
        catch (ODataError ex)
        {
            return FormatError("reading message", ex);
        }
        catch (AuthenticationFailedException ex)
        {
            return FormatError(AuthError, ex);
        }
        catch (MsalServiceException ex)
        {
            return FormatError(AuthError, ex);
        }
    }

    private static async Task<string> SendMailAsync(GraphServiceClient client, string? to, string? subject, string? body)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            return "Error: Recipient email required";
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            return "Error: Subject required";
        }

        try
        {
            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody { ContentType = BodyType.Text, Content = body ?? string.Empty },
                ToRecipients = [new Recipient { EmailAddress = new EmailAddress { Address = to } }],
            };

            await client.Me.SendMail.PostAsync(new SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = true,
            }).ConfigureAwait(false);

            return $"Email sent to {to}";
        }
        catch (ODataError ex)
        {
            return FormatError("sending email", ex);
        }
        catch (AuthenticationFailedException ex)
        {
            return FormatError(AuthError, ex);
        }
        catch (MsalServiceException ex)
        {
            return FormatError(AuthError, ex);
        }
    }

    private static async Task<string> SearchMailAsync(GraphServiceClient client, string? query, int maxResults)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "Error: Search query required";
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(SearchTimeoutSeconds));
            var messages = await client.Me.Messages.GetAsync(
                config =>
                {
                    config.Headers.Add("Prefer", "IdType=\"ImmutableId\"");
                    config.QueryParameters.Top = maxResults;
                    config.QueryParameters.Search = $"\"{query}\"";
                    config.QueryParameters.Select = ["id", "subject", "from", "receivedDateTime", "parentFolderId"];
                },
                cts.Token).ConfigureAwait(false);

            return messages?.Value is { Count: > 0 }
                ? FormatMessages(messages.Value)
                : "No messages found";
        }
        catch (OperationCanceledException)
        {
            return "Error: Search timed out after 2 minutes. Try a more specific query.";
        }
        catch (ODataError ex)
        {
            return FormatError("searching mail", ex);
        }
        catch (AuthenticationFailedException ex)
        {
            return FormatError(AuthError, ex);
        }
        catch (MsalServiceException ex)
        {
            return FormatError(AuthError, ex);
        }
    }

    private static async Task<string> DeleteMessageAsync(GraphServiceClient client, string? messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return "Error: Message ID required";
        }

        try
        {
            // Try root messages path first (works for messages from any folder, returned by search)
            // Fall back to folder-scoped path if root fails
            try
            {
                await client.Me.Messages[messageId].DeleteAsync().ConfigureAwait(false);
                return $"Deleted message {messageId[..8]}";
            }
            catch (ODataError)
            {
                // Message might need folder-specific path, try inbox
                await client.Me.MailFolders[FolderInbox].Messages[messageId].DeleteAsync().ConfigureAwait(false);
                return $"Deleted message {messageId[..8]}";
            }
        }
        catch (ODataError ex)
        {
            return FormatError("deleting message", ex);
        }
        catch (AuthenticationFailedException ex)
        {
            return FormatError(AuthError, ex);
        }
        catch (MsalServiceException ex)
        {
            return FormatError(AuthError, ex);
        }
    }

    private static async Task<string> MoveMessageAsync(GraphServiceClient client, string? messageId, string? folder)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return "Error: Message ID required";
        }

        var destination = ResolveFolderName(folder, FolderDeleted);

        try
        {
            // Try root messages path first (works for messages from any folder, returned by search)
            // Fall back to folder-scoped path if root fails
            try
            {
                _ = await client.Me.Messages[messageId].Move.PostAsync(
                    new Microsoft.Graph.Me.Messages.Item.Move.MovePostRequestBody
                    {
                        DestinationId = destination,
                    }).ConfigureAwait(false);
                return $"Moved message {messageId[..8]} to {destination}";
            }
            catch (ODataError)
            {
                // Message might need folder-specific path, try inbox
                _ = await client.Me.MailFolders[FolderInbox].Messages[messageId].Move.PostAsync(
                    new Microsoft.Graph.Me.MailFolders.Item.Messages.Item.Move.MovePostRequestBody
                    {
                        DestinationId = destination,
                    }).ConfigureAwait(false);
                return $"Moved message {messageId[..8]} to {destination}";
            }
        }
        catch (ODataError ex)
        {
            return FormatError($"moving to {destination}", ex);
        }
        catch (AuthenticationFailedException ex)
        {
            return FormatError(AuthError, ex);
        }
        catch (MsalServiceException ex)
        {
            return FormatError(AuthError, ex);
        }
    }

    private static string FormatMessages(IList<Message> messages, string? folderName = null)
    {
        var sb = new StringBuilder();
        if (folderName is not null)
        {
            _ = sb.Append(CultureInfo.InvariantCulture, $"[{folderName}] ").AppendLine();
        }

        var index = 1;
        foreach (var msg in messages)
        {
            var read = msg.IsRead == true ? "✓" : "●";
            var from = msg.From?.EmailAddress?.Address ?? "Unknown";
            var date = msg.ReceivedDateTime?.ToString("g", CultureInfo.InvariantCulture) ?? "Unknown";
            _ = sb.Append(CultureInfo.InvariantCulture, $"{index}. {read} {date} | {from}").AppendLine();
            _ = sb.Append(CultureInfo.InvariantCulture, $"   {msg.Subject}").AppendLine();
            _ = sb.Append(CultureInfo.InvariantCulture, $"   ID: {msg.Id}").AppendLine();
            index++;
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatFullMessage(Message msg)
    {
        var sb = new StringBuilder();
        var toAddresses = string.Join(", ", msg.ToRecipients?.Select(r => r.EmailAddress?.Address) ?? []);
        _ = sb.Append(CultureInfo.InvariantCulture, $"Subject: {msg.Subject}").AppendLine()
            .Append(CultureInfo.InvariantCulture, $"From: {msg.From?.EmailAddress?.Address}").AppendLine()
            .Append(CultureInfo.InvariantCulture, $"Date: {msg.ReceivedDateTime?.ToString("g", CultureInfo.InvariantCulture)}").AppendLine()
            .Append(CultureInfo.InvariantCulture, $"To: {toAddresses}").AppendLine()
            .AppendLine()
            .AppendLine(msg.Body?.Content ?? "(No content)");
        return sb.ToString().TrimEnd();
    }

    private static string FormatError(string operation, ODataError ex) =>
        $"Error {operation}: {ex.Error?.Message ?? ex.Message}";

    private static string FormatError(string operation, AuthenticationFailedException ex) =>
        $"Error {operation}: {ex.Message}";

    private static string FormatError(string operation, MsalServiceException ex) =>
        $"Error {operation}: {ex.Message}";

    private static async Task<(int Deleted, int Failed, int RetryCount)> ExecuteBatchDeleteAsync(
        GraphServiceClient client,
        List<string> ids)
    {
        var deleted = 0;
        var failed = 0;
        var retryCount = 0;

        var batches = ids.Chunk(BatchSize).ToList();
        for (var i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            var batchTasks = batch.Select(id => DeleteSingleMessageWithRetryAsync(client, id));
            var results = await Task.WhenAll(batchTasks).ConfigureAwait(false);

            deleted += results.Count(r => r.Success);
            failed += results.Count(r => !r.Success);
            retryCount += results.Sum(r => r.Retries);

            // Small delay between batches to be nice to the API
            if (i < batches.Count - 1)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        return (deleted, failed, retryCount);
    }

    private static async Task<(bool Success, int Retries)> DeleteSingleMessageWithRetryAsync(
        GraphServiceClient client,
        string messageId)
    {
        var retries = 0;

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                await DeleteFromInboxOrRootAsync(client, messageId).ConfigureAwait(false);
                return (true, retries);
            }
            catch (ODataError ex) when (ex.ResponseStatusCode == 429)
            {
                // Rate limited - wait with exponential backoff
                var delay = BaseDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay).ConfigureAwait(false);
                retries++;
            }
            catch (ODataError)
            {
                // Other error - don't retry
                return (false, retries);
            }
        }

        return (false, retries);
    }

    private static async Task DeleteFromInboxOrRootAsync(GraphServiceClient client, string messageId)
    {
        // Try root messages path first (works for messages from any folder, returned by search)
        // Fall back to folder-scoped path if root fails
        try
        {
            await client.Me.Messages[messageId].DeleteAsync().ConfigureAwait(false);
        }
        catch (ODataError)
        {
            await client.Me.MailFolders[FolderInbox].Messages[messageId].DeleteAsync().ConfigureAwait(false);
        }
    }

    private static string FormatBatchResult(int total, int deleted, int failed, int retryCount)
    {
        var result = $"Deleted {deleted.ToInvariant()}/{total.ToInvariant()} emails";

        if (failed > 0)
        {
            result += $" ({failed.ToInvariant()} failed)";
        }

        if (retryCount > 0)
        {
            result += $" [retried {retryCount.ToInvariant()}x due to rate limits]";
        }

        return result;
    }

    private static string GetSpamDomainList()
    {
        if (spamStorage is null)
        {
            return "Error: Spam storage not initialized. Run with /spam first or restart the app.";
        }

        var domains = spamStorage.GetSpamDomains();
        return domains.Count == 0
            ? "No domains in spam registry."
            : JsonSerializer.Serialize(domains.Select(d => d.Domain));
    }

    private static string AddToSpamDomainList(string? domain, string? reason)
    {
        if (spamStorage is null)
        {
            return "Error: Spam storage not initialized. Run with /spam first or restart the app.";
        }

        if (string.IsNullOrWhiteSpace(domain))
        {
            return "Error: Domain required.";
        }

        var added = spamStorage.AddSpamDomain(domain, reason);
        return added
            ? $"Added {domain.ToUpperInvariant()} to spam registry."
            : $"Domain {domain.ToUpperInvariant()} already in spam registry.";
    }

    private static string CheckSpamDomain(string? domain)
    {
        if (spamStorage is null)
        {
            return "Error: Spam storage not initialized. Run with /spam first or restart the app.";
        }

        if (string.IsNullOrWhiteSpace(domain))
        {
            return "Error: Domain required.";
        }

        var isSpam = spamStorage.IsKnownSpamDomain(domain);
        return isSpam
            ? $"{domain.ToUpperInvariant()} IS in the spam registry."
            : $"{domain.ToUpperInvariant()} is NOT in the spam registry.";
    }
}
