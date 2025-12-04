// <copyright file="SpamReviewAgent.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Agents;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using HemSoft.PowerAI.Console.Configuration;
using HemSoft.PowerAI.Console.Models;
using HemSoft.PowerAI.Console.Services;

using Spectre.Console;

/// <summary>
/// Agent for human review of domains flagged as potential spam.
/// Processes domains in batches and adds confirmed spam domains to the blocklist.
/// </summary>
/// <param name="settings">The spam filter settings.</param>
[ExcludeFromCodeCoverage(Justification = "Agent requires interactive console and file I/O")]
internal sealed class SpamReviewAgent(SpamFilterSettings settings) : IDisposable
{
    private readonly HumanReviewService humanReviewService = new(settings);
    private readonly SpamStorageService storageService = new(settings);
    private bool disposed;

    /// <summary>
    /// Runs the human review workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        DisplayHeader();

        var totalDomains = this.humanReviewService.GetPendingCount();

        // Show menu with options
        var prompt = new SelectionPrompt<string>()
            .Title("[cyan]What would you like to do?[/]")
            .AddChoices(
                $"Review pending domains ({totalDomains} pending)",
                "Add domain to blocklist manually",
                "View current blocklist");

        var choice = await prompt.ShowAsync(AnsiConsole.Console, cancellationToken).ConfigureAwait(false);

        if (choice.StartsWith("Add domain", StringComparison.Ordinal))
        {
            await this.AddDomainManuallyAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (choice.StartsWith("View current", StringComparison.Ordinal))
        {
            this.DisplayBlocklist();
            return;
        }

        // Review pending domains
        if (totalDomains == 0)
        {
            AnsiConsole.MarkupLine("[green]No domains pending review.[/]");
            AnsiConsole.MarkupLine("[dim]Run /spam-scan to identify suspicious domains.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[cyan]Found {totalDomains} domain(s) pending review.[/]\n");

        var stats = new ReviewStats();
        var batchNum = 0;

        while (true)
        {
            var pendingDomains = this.humanReviewService.GetPendingDomains();
            if (pendingDomains.Count == 0)
            {
                break;
            }

            batchNum++;
            var batch = pendingDomains.Take(settings.ReviewBatchSize).ToList();

            AnsiConsole.MarkupLine($"\n[blue]═══ Review Batch {batchNum} ({batch.Count} domains) ═══[/]");

            await this.ProcessBatchAsync(batch, stats).ConfigureAwait(false);

            var remaining = this.humanReviewService.GetPendingCount();
            if (remaining == 0)
            {
                break;
            }

            var continueReview = await AnsiConsole.ConfirmAsync(
                $"[yellow]{stats.DomainsBlocked} domain(s) blocked. {remaining} remaining. Continue?[/]",
                defaultValue: true,
                cancellationToken).ConfigureAwait(false);

            if (!continueReview)
            {
                break;
            }
        }

        DisplaySummary(stats);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
    }

    private static void DisplayHeader()
    {
        AnsiConsole.Write(new FigletText("Spam Review").Color(Color.Yellow));
        AnsiConsole.MarkupLine("[dim]Review flagged domains and add confirmed spam to blocklist.[/]\n");
    }

    private static void DisplaySummary(ReviewStats stats)
    {
        AnsiConsole.MarkupLine($"\n[green]═══ Review Complete ═══[/]");
        AnsiConsole.MarkupLine($"[green]Domains reviewed: {stats.DomainsReviewed}[/]");
        AnsiConsole.MarkupLine($"[red]Added to blocklist: {stats.DomainsBlocked}[/]");
        AnsiConsole.MarkupLine($"[green]Marked legitimate: {stats.DomainsLegitimate}[/]");

        if (stats.DomainsBlocked > 0)
        {
            AnsiConsole.MarkupLine($"\n[yellow]Run /spam-cleanup to move blocked emails to junk.[/]");
        }
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 1)] + "…";

    private async Task AddDomainManuallyAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("\n[cyan]═══ Add Domain to Blocklist ═══[/]\n");

        var domain = await AnsiConsole.AskAsync<string>(
            "[yellow]Enter domain to block (e.g., spammer.com):[/]",
            cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(domain))
        {
            AnsiConsole.MarkupLine("[yellow]No domain entered.[/]");
            return;
        }

        // Normalize the domain
        domain = domain.Trim().ToUpperInvariant();

        // Remove any protocol or path if accidentally included
        if (domain.Contains("://", StringComparison.Ordinal))
        {
            domain = new Uri(domain).Host.ToUpperInvariant();
        }

        // Check if already blocked
        if (this.storageService.IsKnownSpamDomain(domain))
        {
            AnsiConsole.MarkupLine($"[yellow]{domain}[/] is already in the blocklist.");
            return;
        }

        var reason = await AnsiConsole.AskAsync<string>(
            "[dim]Reason for blocking (optional, press Enter to skip):[/]",
            cancellationToken).ConfigureAwait(false);

        var reasonText = string.IsNullOrWhiteSpace(reason) ? "Manually added by user" : reason;

        if (this.storageService.AddSpamDomain(domain, reasonText))
        {
            AnsiConsole.MarkupLine($"\n[green]✓[/] [red]{domain}[/] added to blocklist.");
            AnsiConsole.MarkupLine("[dim]Run /spam-cleanup to move existing emails from this domain to junk.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]Failed to add {domain} to blocklist.[/]");
        }
    }

    private void DisplayBlocklist()
    {
        var domains = this.storageService.GetSpamDomains();

        AnsiConsole.MarkupLine("\n[cyan]═══ Current Blocklist ═══[/]\n");

        if (domains.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No domains in blocklist.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[blue]#[/]").Width(4))
            .AddColumn(new TableColumn("[blue]Domain[/]").Width(30))
            .AddColumn(new TableColumn("[blue]Added[/]").Width(12))
            .AddColumn(new TableColumn("[blue]Reason[/]").Width(40));

        var index = 1;
        foreach (var domain in domains.OrderBy(d => d.Domain, StringComparer.OrdinalIgnoreCase))
        {
            table.AddRow(
                $"[cyan]{index++}[/]",
                Markup.Escape(domain.Domain),
                domain.AddedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Markup.Escape(Truncate(domain.Reason ?? "-", 38)));
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {domains.Count} domain(s)[/]");
    }

    private async Task ProcessBatchAsync(List<HumanReviewDomain> batch, ReviewStats stats)
    {
        _ = await Task.FromResult(0).ConfigureAwait(false);

        // Display the batch table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[blue]#[/]").Width(3))
            .AddColumn(new TableColumn("[blue]Domain[/]").Width(28))
            .AddColumn(new TableColumn("[blue]Emails[/]").Width(8))
            .AddColumn(new TableColumn("[blue]Sample Subject[/]").Width(35))
            .AddColumn(new TableColumn("[blue]Reason[/]").Width(24));

        for (var i = 0; i < batch.Count; i++)
        {
            var domain = batch[i];
            var sample = domain.Samples.FirstOrDefault();
            var subject = sample?.Subject ?? "(no sample)";
            var reason = sample?.Reason ?? "-";

            table.AddRow(
                $"[cyan]{i + 1}[/]",
                Markup.Escape(domain.Domain),
                domain.EmailCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Markup.Escape(Truncate(subject, 33)),
                Markup.Escape(Truncate(reason, 22)));
        }

        AnsiConsole.Write(table);

        // Ask which domains are NOT spam (legitimate)
        AnsiConsole.MarkupLine("\n[yellow]Which domains are NOT spam (legitimate)?[/]");
        var input = await AnsiConsole.AskAsync<string>(
            "[dim]Enter numbers separated by commas (e.g., 3,7,12) or press Enter if all are spam:[/]").ConfigureAwait(false);

        // Parse legitimate selections
        HashSet<int> legitIndices = [];
        if (!string.IsNullOrWhiteSpace(input))
        {
            legitIndices = [.. input
                .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), CultureInfo.InvariantCulture, out var n) ? n : 0)
                .Where(n => n > 0 && n <= batch.Count)];
        }

        // Process each domain
        List<string> domainsToRemove = [];
        foreach (var (domain, index) in batch.Select((d, i) => (d, i + 1)))
        {
            stats.DomainsReviewed++;

            if (legitIndices.Contains(index))
            {
                // User marked as legitimate - just remove from review queue
                AnsiConsole.MarkupLine($"[green]#{index} {domain.Domain}[/] → Marked legitimate");
                stats.DomainsLegitimate++;
            }
            else
            {
                // Add to spam blocklist
                this.storageService.AddSpamDomain(domain.Domain, "Confirmed spam by user review");
                AnsiConsole.MarkupLine($"[red]#{index} {domain.Domain}[/] → Added to blocklist");
                stats.DomainsBlocked++;
            }

            domainsToRemove.Add(domain.Domain);
        }

        // Remove processed domains from review queue
        this.humanReviewService.RemoveDomains(domainsToRemove);
    }

    private sealed class ReviewStats
    {
        public int DomainsReviewed { get; set; }

        public int DomainsBlocked { get; set; }

        public int DomainsLegitimate { get; set; }
    }
}
