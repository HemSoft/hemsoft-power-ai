// <copyright file="MailAgent.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Agents;

using System.Diagnostics.CodeAnalysis;

using HemSoft.PowerAI.Console.Agents.Infrastructure;
using HemSoft.PowerAI.Console.Services;
using HemSoft.PowerAI.Console.Tools;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

/// <summary>
/// A mail specialist agent that handles email operations through Outlook/Hotmail.
/// Encapsulates OutlookMailTools as an internal tool for domain-specific reasoning.
/// Uses the MS Agent Framework AIAgent pattern.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Agent requires OpenRouter API")]
internal static class MailAgent
{
    private const string ModelId = "x-ai/grok-4.1-fast";

    private const string Instructions = """
        You are an email management specialist agent. Your job is to handle email operations
        for Outlook/Hotmail mailboxes efficiently and intelligently.

        ## Your capabilities:
        1. List and read emails from inbox and other folders
        2. Send emails to recipients
        3. Search emails by query
        4. Delete and move emails between folders
        5. Manage spam domain blocklist

        ## Your workflow:
        1. Analyze the email task to understand what's needed
        2. Use the appropriate mail operation (inbox, read, send, search, delete, move, junk)
        3. For spam operations, use blocklist, blockadd, or blockcheck modes
        4. Provide clear summaries of actions taken

        ## Available mail modes:
        - inbox: List recent inbox messages
        - junk: List junk folder OR move a message to junk (provide ID)
        - folder: List messages in a specific folder
        - read: Read full message content by ID
        - send: Send email (to, subject, body)
        - search: Search emails by query
        - delete: Delete a message by ID
        - batchdelete: Delete multiple messages (comma-separated IDs)
        - move: Move message to folder (inbox, archive, deleteditems, junkemail)
        - count: Get folder statistics
        - blocklist: List blocked spam domains
        - blockadd: Add domain to blocklist
        - blockcheck: Check if domain is blocked

        ## Guidelines:
        - Always confirm actions that modify mailbox state
        - When deleting multiple emails, use batchdelete for efficiency
        - Summarize email content concisely when reading
        - For suspicious emails, suggest using junk mode or blockadd

        ## Output format:
        - **Action**: What operation was performed
        - **Result**: Outcome or data retrieved
        - **Summary**: Brief explanation if needed
        """;

    /// <summary>
    /// Creates a new MailAgent as an AIAgent.
    /// The agent encapsulates OutlookMailTools for intelligent email handling.
    /// Can be used directly or passed as a tool to other agents.
    /// </summary>
    /// <param name="graphClientProvider">The Graph client provider for API access.</param>
    /// <param name="spamStorage">Optional spam storage service for domain management.</param>
    /// <returns>An AIAgent configured for email tasks.</returns>
    public static AIAgent Create(IGraphClientProvider graphClientProvider, SpamStorageService? spamStorage = null)
    {
        var outlookMailTools = new OutlookMailTools(graphClientProvider, spamStorage);

        IList<AITool> tools =
        [
            AIFunctionFactory.Create(outlookMailTools.MailAsync),
        ];

        return AgentFactory.CreateAgent(
            modelId: ModelId,
            name: "MailAgent",
            instructions: Instructions,
            description: "Email management specialist for Outlook/Hotmail mailboxes",
            tools: tools);
    }
}
