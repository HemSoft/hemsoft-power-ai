// <copyright file="SpamFilterTools.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace AgentDemo.Console.Tools;

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

using AgentDemo.Console.Models;
using AgentDemo.Console.Services;

using Azure.Identity;

using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;

/// <summary>
/// Callback invoked when an email is evaluated.
/// </summary>
/// <param name="evaluation">The email evaluation result.</param>
internal delegate void EmailEvaluatedCallback(EmailEvaluation evaluation);

/// <summary>
/// Provides spam filtering tools for the AI agent.
/// </summary>
internal sealed class SpamFilterTools(SpamStorageService storageService) : IDisposable
{
    private const string ClientIdEnvVar = "GRAPH_CLIENT_ID";
    private const string TenantIdEnvVar = "GRAPH_TENANT_ID";
    private const string FolderInbox = "inbox";
    private const string FolderJunk = "junkemail";

    private static readonly ActivitySource GraphApiActivitySource = new("AgentDemo.GraphApi", "1.0.0");
    private static readonly string[] Scopes = ["User.Read", "Mail.Read", "Mail.ReadWrite"];

    private static readonly string AuthRecordPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AgentDemo",
        "graph_auth_record.json");

    private readonly HashSet<string> processedMessageIds = new(StringComparer.Ordinal);
    private GraphServiceClient? graphClient;
    private EmailEvaluatedCallback? onEmailEvaluated;
    private bool disposed;

    /// <summary>
    /// Sets the callback to be invoked when an email is evaluated.
    /// </summary>
    /// <param name="callback">The callback to invoke.</param>
    public void SetEvaluationCallback(EmailEvaluatedCallback? callback) => this.onEmailEvaluated = callback;

    /// <summary>
    /// Gets the list of known spam domains.
    /// </summary>
    /// <returns>JSON array of known spam domains.</returns>
    [Description("Gets the list of all known spam domains that should be immediately moved to junk.")]
    public string GetKnownSpamDomains()
    {
        var domains = storageService.GetSpamDomains();
        return JsonSerializer.Serialize(domains.Select(d => d.Domain));
    }

    /// <summary>
    /// Checks if a specific domain is in the known spam list.
    /// </summary>
    /// <param name="domain">The domain to check.</param>
    /// <returns>True if known spam, false otherwise.</returns>
    [Description("Checks if a specific email domain is in the known spam list.")]
    public bool IsKnownSpamDomain(string domain) => storageService.IsKnownSpamDomain(domain);

    /// <summary>
    /// Reports an email evaluation for display purposes.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="senderEmail">The sender's email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="verdict">The verdict (Legitimate, Known Spam, Candidate, Junked).</param>
    /// <param name="reason">Brief reason for the verdict.</param>
    /// <returns>Confirmation message.</returns>
    [Description("Reports the evaluation result for an email. REQUIRED parameters: messageId (the 'id' from GetInboxEmailsAsync), senderEmail, subject, verdict ('Legitimate'/'Junked'/'Candidate'), reason.")]
    public string ReportEmailEvaluation(string messageId, string senderEmail, string subject, string verdict, string? reason = null)
    {
        this.onEmailEvaluated?.Invoke(new EmailEvaluation
        {
            MessageId = messageId ?? string.Empty,
            Sender = senderEmail ?? string.Empty,
            Subject = subject ?? string.Empty,
            Verdict = verdict ?? "Unknown",
            Reason = reason,
        });

        return "Evaluation recorded.";
    }

    /// <summary>
    /// Fetches a batch of emails from the inbox that haven't been processed yet.
    /// </summary>
    /// <param name="batchSize">Number of emails to fetch.</param>
    /// <returns>JSON array of email summaries with id, subject, sender, and receivedDateTime.</returns>
    [Description("Fetches a batch of unprocessed emails from the inbox. Returns JSON array with id, subject, senderEmail, senderDomain, and receivedDateTime for each email. Returns empty array when all emails have been processed.")]
    public async Task<string> GetInboxEmailsAsync(int batchSize = 10)
    {
        using var activity = GraphApiActivitySource.StartActivity("GetInboxEmails");
        activity?.SetTag("batch.size", batchSize);

        var client = this.GetOrCreateClient();
        if (client is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Missing client ID");
            return $"Error: Set {ClientIdEnvVar} environment variable.";
        }

        try
        {
            // Fetch more than needed to account for already-processed emails
            var fetchSize = Math.Min(batchSize * 3, 50);
            var messages = await client.Me.MailFolders[FolderInbox].Messages.GetAsync(config =>
            {
                config.QueryParameters.Top = fetchSize;
                config.QueryParameters.Select = ["id", "subject", "from", "receivedDateTime", "isRead"];
                config.QueryParameters.Orderby = ["receivedDateTime desc"];
            }).ConfigureAwait(false);

            if (messages?.Value is not { Count: > 0 })
            {
                activity?.SetTag("emails.fetched", 0);
                return "[]";
            }

            // Filter out already-processed emails and take only the requested batch size
            var unprocessedEmails = messages.Value
                .Where(m => m.Id is not null && !this.processedMessageIds.Contains(m.Id))
                .Take(batchSize)
                .ToList();

            activity?.SetTag("emails.fetched", unprocessedEmails.Count);

            if (unprocessedEmails.Count == 0)
            {
                return "[]";
            }

            // Mark these as processed so we don't fetch them again
            foreach (var email in unprocessedEmails)
            {
                this.processedMessageIds.Add(email.Id!);
            }

            var result = unprocessedEmails.Select(m =>
            {
                var senderEmail = m.From?.EmailAddress?.Address ?? "unknown";
                var senderDomain = ExtractDomain(senderEmail);
                return new
                {
                    id = m.Id,
                    subject = m.Subject,
                    senderEmail,
                    senderDomain,
                    receivedDateTime = m.ReceivedDateTime?.ToString("o", CultureInfo.InvariantCulture),
                    isRead = m.IsRead,
                };
            });

            return JsonSerializer.Serialize(result);
        }
        catch (ODataError ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Error?.Message ?? ex.Message);
            return $"Error fetching emails: {ex.Error?.Message ?? ex.Message}";
        }
    }

    /// <summary>
    /// Reads the full content of an email by its ID.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <returns>Full email details including body.</returns>
    [Description("Reads the full content of an email by its ID. Returns subject, sender, body preview, and other details for spam analysis.")]
    public async Task<string> ReadEmailAsync(string messageId)
    {
        using var activity = GraphApiActivitySource.StartActivity("ReadEmail");
        activity?.SetTag("message.id", messageId);

        var client = this.GetOrCreateClient();
        if (client is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Missing client ID");
            return $"Error: Set {ClientIdEnvVar} environment variable.";
        }

        try
        {
            // Use MailFolders[inbox].Messages to match how we fetched the IDs in GetInboxEmailsAsync
            var message = await client.Me.MailFolders[FolderInbox].Messages[messageId].GetAsync(config =>
                config.QueryParameters.Select = ["id", "subject", "from", "receivedDateTime", "bodyPreview", "body"]).ConfigureAwait(false);

            if (message is null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Message not found");
                return "Message not found";
            }

            var senderEmail = message.From?.EmailAddress?.Address ?? "unknown";
            activity?.SetTag("sender.domain", ExtractDomain(senderEmail));

            var result = new
            {
                id = message.Id,
                subject = message.Subject,
                senderEmail,
                senderDomain = ExtractDomain(senderEmail),
                receivedDateTime = message.ReceivedDateTime?.ToString("o", CultureInfo.InvariantCulture),
                bodyPreview = message.BodyPreview,
                bodySnippet = TruncateBody(message.Body?.Content, 500),
            };

            return JsonSerializer.Serialize(result);
        }
        catch (ODataError ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Error?.Message ?? ex.Message);
            return $"Error reading email: {ex.Error?.Message ?? ex.Message}";
        }
    }

    /// <summary>
    /// Moves an email to the junk folder.
    /// </summary>
    /// <param name="messageId">The message ID to move.</param>
    /// <returns>Success or error message.</returns>
    [Description("Moves an email to the junk folder immediately. You MUST call this for emails from known spam domains BEFORE calling ReportEmailEvaluation.")]
    public async Task<string> MoveToJunkAsync(string messageId)
    {
        using var activity = GraphApiActivitySource.StartActivity("MoveToJunk");
        activity?.SetTag("message.id", messageId);

        var client = this.GetOrCreateClient();
        if (client is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Missing client ID");
            return $"Error: Set {ClientIdEnvVar} environment variable.";
        }

        if (string.IsNullOrEmpty(messageId))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Missing message ID");
            return "Error: messageId is required.";
        }

        try
        {
            // Use MailFolders[inbox].Messages to match how we fetched the IDs in GetInboxEmailsAsync
            await client.Me.MailFolders[FolderInbox].Messages[messageId].Move.PostAsync(
                new Microsoft.Graph.Me.MailFolders.Item.Messages.Item.Move.MovePostRequestBody
                {
                    DestinationId = FolderJunk,
                }).ConfigureAwait(false);

            // Track as processed so we don't see it again
            this.processedMessageIds.Add(messageId);

            activity?.SetTag("move.success", true);
            return "Successfully moved message to junk folder.";
        }
        catch (ODataError ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Error?.Message ?? ex.Message);
            return $"Error moving to junk: {ex.Error?.Message ?? ex.Message}";
        }
    }

    /// <summary>
    /// Records a spam candidate for later human review.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="senderEmail">The sender's email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="spamReason">The AI's reasoning for flagging this as spam.</param>
    /// <param name="confidenceScore">Confidence score from 0.0 to 1.0.</param>
    /// <returns>Success or error message.</returns>
    [Description("Records an email as a spam candidate for later human review. Use this when AI analysis suggests the email is spam but the domain is not in the known list.")]
    public string RecordSpamCandidate(
        string messageId,
        string senderEmail,
        string subject,
        string spamReason,
        double confidenceScore)
    {
        var candidate = new SpamCandidate
        {
            MessageId = messageId,
            SenderEmail = senderEmail,
            SenderDomain = ExtractDomain(senderEmail),
            Subject = subject,
            SpamReason = spamReason,
            ConfidenceScore = Math.Clamp(confidenceScore, 0.0, 1.0),
            IdentifiedAt = DateTime.UtcNow,
        };

        var added = storageService.AddSpamCandidate(candidate);
        return added
            ? $"Recorded spam candidate from {candidate.SenderDomain}"
            : $"Candidate already recorded for message {messageId}";
    }

    /// <summary>
    /// Adds a domain to the known spam list.
    /// </summary>
    /// <param name="domain">The domain to add.</param>
    /// <param name="reason">Reason for marking as spam.</param>
    /// <returns>Success or error message.</returns>
    [Description("Adds a domain to the known spam list. Future emails from this domain will be moved to junk immediately.")]
    public string AddToSpamDomainList(string domain, string reason)
    {
        var added = storageService.AddSpamDomain(domain, reason);
        return added
            ? $"Added {domain} to spam domain list"
            : $"Domain {domain} already in spam list";
    }

    /// <summary>
    /// Gets all pending spam candidates grouped by domain for human review.
    /// </summary>
    /// <returns>JSON object with domains as keys and candidate arrays as values.</returns>
    [Description("Gets all pending spam candidates grouped by sender domain for human review.")]
    public string GetPendingSpamCandidatesByDomain()
    {
        var grouped = storageService.GetCandidatesGroupedByDomain();
        var result = grouped.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(c => new
            {
                c.MessageId,
                c.SenderEmail,
                c.Subject,
                c.SpamReason,
                c.ConfidenceScore,
            }));

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Processes approved domains by adding them to the spam list and moving their emails to junk.
    /// </summary>
    /// <param name="domain">The domain that was approved as spam.</param>
    /// <returns>Summary of actions taken.</returns>
    [Description("Processes a domain after human approval - adds to spam list and moves all pending emails from that domain to junk.")]
    public async Task<string> ProcessApprovedSpamDomainAsync(string domain)
    {
        using var activity = GraphApiActivitySource.StartActivity("ProcessApprovedSpamDomain");
        activity?.SetTag("domain", domain);

        // Add to spam list
        storageService.AddSpamDomain(domain, "Approved by user");

        // Get all candidates from this domain
        var candidates = storageService.GetSpamCandidates()
            .Where(c => c.SenderDomain.Equals(domain, StringComparison.OrdinalIgnoreCase))
            .ToList();

        activity?.SetTag("candidates.count", candidates.Count);

        var movedCount = 0;
        var errors = new List<string>();

        foreach (var candidate in candidates)
        {
            var result = await this.MoveToJunkAsync(candidate.MessageId).ConfigureAwait(false);
            if (result.StartsWith("Successfully", StringComparison.Ordinal))
            {
                storageService.RemoveSpamCandidate(candidate.MessageId);
                movedCount++;
            }
            else
            {
                errors.Add($"{candidate.SenderEmail}: {result}");
            }
        }

        activity?.SetTag("emails.moved", movedCount);
        activity?.SetTag("errors.count", errors.Count);

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"Processed domain {domain}: ");
        sb.Append(CultureInfo.InvariantCulture, $"Added to spam list, moved {movedCount} emails to junk.");

        if (errors.Count > 0)
        {
            sb.Append(CultureInfo.InvariantCulture, $" Errors: {errors.Count}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Clears all processed spam candidates.
    /// </summary>
    /// <returns>Confirmation message.</returns>
    [Description("Clears all spam candidates that have been processed.")]
    public string ClearProcessedCandidates()
    {
        storageService.ClearSpamCandidates();
        return "Cleared all spam candidates.";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.graphClient?.Dispose();
        this.disposed = true;
    }

    private static string ExtractDomain(string email)
    {
        var atIndex = email.LastIndexOf('@');
        return atIndex >= 0 && atIndex < email.Length - 1
            ? email[(atIndex + 1)..].ToUpperInvariant()
            : email.ToUpperInvariant();
    }

    private static string TruncateBody(string? body, int maxLength)
    {
        if (string.IsNullOrEmpty(body))
        {
            return string.Empty;
        }

        // Strip HTML tags for a cleaner preview
        var text = System.Text.RegularExpressions.Regex.Replace(body, "<[^>]+>", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

        return text.Length <= maxLength ? text : text[..maxLength] + "...";
    }

    private GraphServiceClient? GetOrCreateClient()
    {
        if (this.graphClient is not null)
        {
            return this.graphClient;
        }

        var clientId = Environment.GetEnvironmentVariable(ClientIdEnvVar);
        if (string.IsNullOrEmpty(clientId))
        {
            return null;
        }

        var tenantId = Environment.GetEnvironmentVariable(TenantIdEnvVar) ?? "consumers";

        var cacheOptions = new TokenCachePersistenceOptions
        {
            Name = "AgentDemo.Graph",
            UnsafeAllowUnencryptedStorage = true,
        };

        AuthenticationRecord? authRecord = null;

        if (File.Exists(AuthRecordPath))
        {
            try
            {
                using var stream = File.OpenRead(AuthRecordPath);
                authRecord = AuthenticationRecord.Deserialize(stream);
            }
            catch (IOException)
            {
                // Ignore errors loading auth record
            }
        }

        var credentialOptions = new DeviceCodeCredentialOptions
        {
            ClientId = clientId,
            TenantId = tenantId,
            TokenCachePersistenceOptions = cacheOptions,
            AuthenticationRecord = authRecord,
            DeviceCodeCallback = (code, _) =>
            {
                System.Console.WriteLine(code.Message);
                return Task.CompletedTask;
            },
        };

        var credential = new DeviceCodeCredential(credentialOptions);
        this.graphClient = new GraphServiceClient(credential, Scopes);

        if (authRecord is null)
        {
            try
            {
                var context = new Azure.Core.TokenRequestContext(Scopes);
                _ = credential.GetToken(context, default);

                authRecord = credential.AuthenticateAsync(context).GetAwaiter().GetResult();
                Directory.CreateDirectory(Path.GetDirectoryName(AuthRecordPath)!);
                using var stream = File.Create(AuthRecordPath);
                authRecord.Serialize(stream);
            }
            catch (AuthenticationFailedException)
            {
                // Auth failed - will prompt again next time
            }
        }

        return this.graphClient;
    }
}
