// <copyright file="SpamCleanupAgent.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Agents;

using System.Diagnostics;

using HemSoft.PowerAI.Console.Configuration;
using HemSoft.PowerAI.Console.Services;

using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;

using Spectre.Console;

/// <summary>
/// Agent that moves emails from blocked domains to junk.
/// </summary>
/// <param name="settings">The spam filter settings.</param>
internal sealed class SpamCleanupAgent(SpamFilterSettings settings) : IDisposable
{
    private const string FolderInbox = "inbox";
    private const string FolderJunk = "junkemail";

    private static readonly ActivitySource GraphApiActivitySource = new("HemSoft.PowerAI.GraphApi", "1.0.0");

    private readonly SpamStorageService storageService = new(settings);
    private bool disposed;

    /// <summary>
    /// Runs the spam cleanup workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        DisplayHeader();

        var blockedDomains = this.storageService.GetSpamDomains();
        if (blockedDomains.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No blocked domains in spam list.[/]");
            AnsiConsole.MarkupLine("[dim]Run /spam-scan and /spam-review to identify spam domains.[/]");
            return;
        }

        var client = SharedGraphClient.GetClient();
        if (client is null)
        {
            AnsiConsole.Write(new Panel($"[red]Set {SharedGraphClient.ClientIdEnvVar} environment variable.[/]")
                .Header("[red]Configuration Error[/]")
                .Border(BoxBorder.Rounded));
            return;
        }

        AnsiConsole.MarkupLine($"[cyan]Processing {blockedDomains.Count} blocked domain(s)...[/]\n");

        var domainList = blockedDomains.Select(d => d.Domain).ToList();
        var stats = await ExecuteCleanupAsync(client, domainList, cancellationToken).ConfigureAwait(false);

        DisplaySummary(stats);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        // SharedGraphClient owns the client - don't dispose it here
        this.disposed = true;
    }

    private static async Task<CleanupStats> ExecuteCleanupAsync(
        GraphServiceClient client,
        List<string> domainList,
        CancellationToken cancellationToken)
    {
        var stats = new CleanupStats();

        await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                await ProcessInboxPhaseAsync(ctx, client, domainList, stats, cancellationToken).ConfigureAwait(false);
                await ProcessJunkPhaseAsync(ctx, client, domainList, stats, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);

        return stats;
    }

    private static async Task ProcessInboxPhaseAsync(
        ProgressContext ctx,
        GraphServiceClient client,
        List<string> domainList,
        CleanupStats stats,
        CancellationToken cancellationToken)
    {
        var inboxTask = ctx.AddTask("[cyan]Checking inbox...[/]", maxValue: domainList.Count);

        foreach (var domainName in domainList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            inboxTask.Description = $"[cyan]Inbox: {domainName}[/]";
            var movedCount = await ProcessDomainInboxAsync(client, domainName, cancellationToken).ConfigureAwait(false);

            if (movedCount > 0)
            {
                stats.TotalEmailsMoved += movedCount;
                AnsiConsole.MarkupLine($"  [yellow]{domainName}[/]: Moved {movedCount} email(s) to junk");
            }

            stats.DomainsProcessed++;
            inboxTask.Increment(1);
        }
    }

    private static async Task ProcessJunkPhaseAsync(
        ProgressContext ctx,
        GraphServiceClient client,
        List<string> domainList,
        CleanupStats stats,
        CancellationToken cancellationToken)
    {
        var junkTask = ctx.AddTask("[red]Cleaning junk folder...[/]", maxValue: domainList.Count);

        foreach (var domainName in domainList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            junkTask.Description = $"[red]Junk: {domainName}[/]";
            var deletedCount = await ProcessDomainJunkAsync(client, domainName, cancellationToken).ConfigureAwait(false);

            if (deletedCount > 0)
            {
                stats.TotalEmailsDeleted += deletedCount;
                AnsiConsole.MarkupLine($"  [red]{domainName}[/]: Deleted {deletedCount} email(s) from junk");
            }

            junkTask.Increment(1);
        }
    }

    private static void DisplayHeader()
    {
        AnsiConsole.Write(new FigletText("Spam Cleanup").Color(Color.Red));
        AnsiConsole.MarkupLine("[dim]Move emails from blocked domains to junk.[/]\n");
    }

    private static void DisplaySummary(CleanupStats stats)
    {
        AnsiConsole.MarkupLine($"\n[green]═══ Cleanup Complete ═══[/]");
        AnsiConsole.MarkupLine($"[green]Domains checked: {stats.DomainsProcessed}[/]");
        AnsiConsole.MarkupLine($"[yellow]Emails moved from inbox to junk: {stats.TotalEmailsMoved}[/]");
        AnsiConsole.MarkupLine($"[red]Emails deleted from junk: {stats.TotalEmailsDeleted}[/]");
    }

    private static async Task<int> ProcessDomainInboxAsync(
        GraphServiceClient client,
        string domain,
        CancellationToken cancellationToken)
    {
        using var activity = GraphApiActivitySource.StartActivity("ProcessDomainInbox");
        activity?.SetTag("domain", domain);

        var movedCount = 0;

        try
        {
            var domainFilter = domain.ToUpperInvariant();
            var messages = await client.Me.MailFolders[FolderInbox].Messages.GetAsync(
                config =>
                {
                    config.QueryParameters.Filter = $"contains(from/emailAddress/address, '{domainFilter}')";
                    config.QueryParameters.Select = ["id", "subject", "from"];
                    config.QueryParameters.Top = 100;
                },
                cancellationToken).ConfigureAwait(false);

            if (messages?.Value is null || messages.Value.Count == 0)
            {
                return 0;
            }

            activity?.SetTag("emails.found", messages.Value.Count);

            foreach (var message in messages.Value)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await client.Me.MailFolders[FolderInbox].Messages[message.Id].Move.PostAsync(
                        new Microsoft.Graph.Me.MailFolders.Item.Messages.Item.Move.MovePostRequestBody
                        {
                            DestinationId = FolderJunk,
                        },
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    movedCount++;
                }
                catch (ODataError ex)
                {
                    AnsiConsole.MarkupLine($"  [yellow]Error moving message: {Markup.Escape(ex.Error?.Message ?? ex.Message)}[/]");
                }
            }

            activity?.SetTag("emails.moved", movedCount);
        }
        catch (ODataError ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Error?.Message ?? ex.Message);
            AnsiConsole.MarkupLine($"  [yellow]Error searching inbox for {domain}: {Markup.Escape(ex.Error?.Message ?? ex.Message)}[/]");
        }

        return movedCount;
    }

    private static async Task<int> ProcessDomainJunkAsync(
        GraphServiceClient client,
        string domain,
        CancellationToken cancellationToken)
    {
        using var activity = GraphApiActivitySource.StartActivity("ProcessDomainJunk");
        activity?.SetTag("domain", domain);

        var deletedCount = 0;

        try
        {
            var domainFilter = domain.ToUpperInvariant();
            var messages = await client.Me.MailFolders[FolderJunk].Messages.GetAsync(
                config =>
                {
                    config.QueryParameters.Filter = $"contains(from/emailAddress/address, '{domainFilter}')";
                    config.QueryParameters.Select = ["id", "subject", "from"];
                    config.QueryParameters.Top = 100;
                },
                cancellationToken).ConfigureAwait(false);

            if (messages?.Value is null || messages.Value.Count == 0)
            {
                return 0;
            }

            activity?.SetTag("emails.found", messages.Value.Count);

            foreach (var message in messages.Value)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await client.Me.MailFolders[FolderJunk].Messages[message.Id]
                        .DeleteAsync(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    deletedCount++;
                }
                catch (ODataError ex)
                {
                    AnsiConsole.MarkupLine($"  [yellow]Error deleting message: {Markup.Escape(ex.Error?.Message ?? ex.Message)}[/]");
                }
            }

            activity?.SetTag("emails.deleted", deletedCount);
        }
        catch (ODataError ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Error?.Message ?? ex.Message);
            AnsiConsole.MarkupLine($"  [yellow]Error searching junk for {domain}: {Markup.Escape(ex.Error?.Message ?? ex.Message)}[/]");
        }

        return deletedCount;
    }

    private sealed class CleanupStats
    {
        public int DomainsProcessed { get; set; }

        public int TotalEmailsMoved { get; set; }

        public int TotalEmailsDeleted { get; set; }
    }
}
