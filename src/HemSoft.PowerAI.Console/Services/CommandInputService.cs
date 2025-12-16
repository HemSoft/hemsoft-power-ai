// <copyright file="CommandInputService.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Services;

using System.Diagnostics.CodeAnalysis;

using Spectre.Console;

/// <summary>
/// Provides interactive command input with agent menu support.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Interactive console input cannot be unit tested")]
internal static class CommandInputService
{
    private const string InputPrompt = "[cyan]❯[/] ";

    /// <summary>
    /// Available menu options invoked via /.
    /// </summary>
    private static readonly List<AgentChoice> Agents =
    [
        new("Model", "Change the AI model"),
        new("Agents", "Run autonomous agent tasks"),
    ];

    /// <summary>
    /// Reads user input with / agent menu support.
    /// </summary>
    /// <returns>The user's input string, or an agent command like "/coordinate".</returns>
    public static string ReadInput()
    {
        AnsiConsole.Markup(InputPrompt);
        var state = new InputState
        {
            StartLeft = System.Console.CursorLeft,
            StartTop = System.Console.CursorTop,
        };

        while (true)
        {
            var key = System.Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter || state.SubmitImmediately)
            {
                System.Console.WriteLine();
                return new string([.. state.Input]);
            }

            ProcessKey(key, state);

            if (state.SubmitImmediately)
            {
                System.Console.WriteLine();
                return new string([.. state.Input]);
            }
        }
    }

    /// <summary>
    /// Shows the agent selection menu and returns the selected agent command.
    /// </summary>
    /// <returns>The command for the selected agent, or null if cancelled.</returns>
    public static string? ShowAgentMenu()
    {
        var choices = Agents.ConvertAll(a => $"[cyan]{a.Name,-14}[/] [dim]{a.Description}[/]");
        choices.Add("[dim]Cancel[/]");

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[blue]Select Agent[/]")
                .PageSize(10)
                .HighlightStyle(new Style(Color.Black, Color.Cyan))
                .AddChoices(choices));

        if (selected.StartsWith("[dim]Cancel", StringComparison.Ordinal))
        {
            return null;
        }

        // Extract agent name between [cyan] and [/]
        const string startTag = "[cyan]";
        const string endTag = "[/]";
        var startIdx = selected.IndexOf(startTag, StringComparison.Ordinal) + startTag.Length;
        var endIdx = selected.IndexOf(endTag, startIdx, StringComparison.Ordinal);
        var agentName = selected[startIdx..endIdx].Trim();

        return agentName switch
        {
            "Model" => "/model",
            "Agents" => "/agents",
            _ => null,
        };
    }

    private static void ProcessKey(ConsoleKeyInfo key, InputState state)
    {
        switch (key.Key)
        {
            case ConsoleKey.Backspace:
                HandleBackspace(state);
                break;

            case ConsoleKey.Delete:
                HandleDelete(state);
                break;

            case ConsoleKey.LeftArrow:
                MoveCursorLeft(state);
                break;

            case ConsoleKey.RightArrow:
                MoveCursorRight(state);
                break;

            case ConsoleKey.Home:
                state.CursorPos = 0;
                SetCursor(state.StartLeft, state.StartTop);
                break;

            case ConsoleKey.End:
                state.CursorPos = state.Input.Count;
                SetCursor(state.StartLeft + state.CursorPos, state.StartTop);
                break;

            default:
                HandleCharacterInput(key, state);
                break;
        }
    }

    private static void HandleBackspace(InputState state)
    {
        if (state.CursorPos > 0)
        {
            state.Input.RemoveAt(state.CursorPos - 1);
            state.CursorPos--;
            RedrawInput(state);
        }
    }

    private static void HandleDelete(InputState state)
    {
        if (state.CursorPos < state.Input.Count)
        {
            state.Input.RemoveAt(state.CursorPos);
            RedrawInput(state);
        }
    }

    private static void MoveCursorLeft(InputState state)
    {
        if (state.CursorPos > 0)
        {
            state.CursorPos--;
            SetCursor(state.StartLeft + state.CursorPos, state.StartTop);
        }
    }

    private static void MoveCursorRight(InputState state)
    {
        if (state.CursorPos < state.Input.Count)
        {
            state.CursorPos++;
            SetCursor(state.StartLeft + state.CursorPos, state.StartTop);
        }
    }

    private static void HandleCharacterInput(ConsoleKeyInfo key, InputState state)
    {
        if (key.KeyChar != '\0' && !char.IsControl(key.KeyChar))
        {
            state.Input.Insert(state.CursorPos, key.KeyChar);
            state.CursorPos++;
            RedrawInput(state);

            // Show agent menu immediately when user types just '/'
            if (state.Input.Count == 1 && state.Input[0] == '/')
            {
                ShowAgentMenuInline(state);
            }
        }
    }

    private static void ShowAgentMenuInline(InputState state)
    {
        System.Console.WriteLine();
        var selected = ShowAgentMenu();
        if (selected is not null)
        {
            state.Input.Clear();
            state.Input.AddRange(selected);
            state.CursorPos = state.Input.Count;
            state.SubmitImmediately = true;
        }
        else
        {
            // User cancelled, clear the '/'
            state.Input.Clear();
            state.CursorPos = 0;
        }

        // Redraw prompt and input
        AnsiConsole.Markup(InputPrompt);
        state.StartTop = System.Console.CursorTop;
        RedrawInput(state);
    }

    private static void SetCursor(int left, int top)
    {
        try
        {
            System.Console.SetCursorPosition(Math.Max(0, left), Math.Max(0, top));
        }
        catch (ArgumentOutOfRangeException)
        {
            // Ignore
        }
    }

    private static void RedrawInput(InputState state)
    {
        SetCursor(state.StartLeft, state.StartTop);
        var text = new string([.. state.Input]);
        var clearLen = Math.Max(0, System.Console.BufferWidth - state.StartLeft - text.Length - 1);
        System.Console.Write(text + new string(' ', clearLen));
        SetCursor(state.StartLeft + state.CursorPos, state.StartTop);
    }

    private sealed class InputState
    {
        public List<char> Input { get; } = [];

        public int CursorPos { get; set; }

        public int StartLeft { get; set; }

        public int StartTop { get; set; }

        public bool SubmitImmediately { get; set; }
    }

    /// <summary>
    /// Represents an agent choice in the menu.
    /// </summary>
    /// <param name="Name">The agent's display name.</param>
    /// <param name="Description">A brief description of what the agent does.</param>
    private sealed record AgentChoice(string Name, string Description);
}
