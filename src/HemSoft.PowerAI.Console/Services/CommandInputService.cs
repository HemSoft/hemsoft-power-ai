// <copyright file="CommandInputService.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Services;

using System.Diagnostics.CodeAnalysis;

using Spectre.Console;

/// <summary>
/// Provides interactive command input with autocomplete suggestions.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Interactive console input cannot be unit tested")]
internal static class CommandInputService
{
    private const string InputPrompt = "[yellow]You:[/] ";

    private static readonly Dictionary<string, string> Commands = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/clear"] = "Clear history",
        ["/usage"] = "Token usage",
        ["/spam"] = "Spam filter",
        ["/spam-scan"] = "Scan inbox",
        ["/spam-review"] = "Review domains",
        ["/spam-cleanup"] = "Move to junk",
        ["/coordinate"] = "Multi-agent mode",
        ["exit"] = "Exit",
    };

    private static readonly Dictionary<string, (string Description, string PromptText)> Prompts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["#list-5-spam-emails"] = ("List 5 spam emails", "List the first 5 spam emails in my inbox."),
    };

    /// <summary>
    /// Reads user input with command autocomplete support.
    /// </summary>
    /// <returns>The user's input string.</returns>
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

            // Check if a prompt was selected and should be submitted immediately
            if (state.SubmitImmediately)
            {
                System.Console.WriteLine();
                return new string([.. state.Input]);
            }
        }
    }

    private static void ProcessKey(ConsoleKeyInfo key, InputState state)
    {
        switch (key.Key)
        {
            case ConsoleKey.Tab:
                HandleTabCompletion(state);
                break;

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

    private static void HandleTabCompletion(InputState state)
    {
        if (state.Input.Count == 0)
        {
            return;
        }

        var firstChar = state.Input[0];
        if (firstChar == '/')
        {
            HandleCommandTabCompletion(state);
        }
        else if (firstChar == '#')
        {
            HandlePromptTabCompletion(state);
        }
    }

    private static void HandleCommandTabCompletion(InputState state)
    {
        var currentText = new string([.. state.Input]);
        var matches = Commands.Keys
            .Where(c => c.StartsWith(currentText, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return;
        }

        if (matches.Count == 1)
        {
            ApplyCompletion(state, matches[0]);
            return;
        }

        // Multiple matches - show picker
        System.Console.WriteLine();
        var selected = ShowCommandPicker(matches);
        if (selected is not null)
        {
            state.Input.Clear();
            state.Input.AddRange(selected);
            state.CursorPos = state.Input.Count;
        }

        // Redraw prompt and input
        AnsiConsole.Markup(InputPrompt);
        state.StartTop = System.Console.CursorTop;
        RedrawInput(state);
    }

    private static void HandlePromptTabCompletion(InputState state)
    {
        var currentText = new string([.. state.Input]);
        var matches = Prompts.Keys
            .Where(p => p.StartsWith(currentText, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return;
        }

        if (matches.Count == 1)
        {
            ApplyPromptCompletion(state, matches[0]);
            return;
        }

        // Multiple matches - show picker
        System.Console.WriteLine();
        var selected = ShowPromptPicker(matches);
        if (selected is not null)
        {
            ApplyPromptCompletion(state, selected);
        }
        else
        {
            // Redraw prompt and input without changes
            AnsiConsole.Markup(InputPrompt);
            state.StartTop = System.Console.CursorTop;
            RedrawInput(state);
        }
    }

    private static void ApplyCompletion(InputState state, string command)
    {
        state.Input.Clear();
        state.Input.AddRange(command);
        state.CursorPos = state.Input.Count;
        RedrawInput(state);
    }

    private static void ApplyPromptCompletion(InputState state, string promptKey)
    {
        var promptText = Prompts[promptKey].PromptText;
        state.Input.Clear();
        state.Input.AddRange(promptText);
        state.CursorPos = state.Input.Count;
        state.SubmitImmediately = true;

        // Show the prompt text on one line
        AnsiConsole.Markup(InputPrompt);
        System.Console.Write(promptText);
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

            // Show command picker immediately when user types just '/'
            if (state.Input.Count == 1 && state.Input[0] == '/')
            {
                ShowCommandPickerInline(state);
            }
            else if (state.Input.Count == 1 && state.Input[0] == '#')
            {
                ShowPromptPickerInline(state);
            }
        }
    }

    private static void ShowCommandPickerInline(InputState state)
    {
        System.Console.WriteLine();
        var selected = ShowCommandPicker([.. Commands.Keys]);
        if (selected is not null)
        {
            state.Input.Clear();
            state.Input.AddRange(selected);
            state.CursorPos = state.Input.Count;
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

    private static void ShowPromptPickerInline(InputState state)
    {
        System.Console.WriteLine();
        var selected = ShowPromptPicker([.. Prompts.Keys]);
        if (selected is not null)
        {
            // Replace with the actual prompt text and submit immediately
            var promptText = Prompts[selected].PromptText;
            state.Input.Clear();
            state.Input.AddRange(promptText);
            state.CursorPos = state.Input.Count;
            state.SubmitImmediately = true;

            // Show the prompt text on one line
            AnsiConsole.Markup(InputPrompt);
            System.Console.Write(promptText);
            return;
        }

        // User cancelled, clear the '#'
        state.Input.Clear();
        state.CursorPos = 0;

        // Redraw prompt and input
        AnsiConsole.Markup(InputPrompt);
        state.StartTop = System.Console.CursorTop;
        RedrawInput(state);
    }

    private static string? ShowCommandPicker(List<string> commands)
    {
        var choices = commands.ConvertAll(c => $"{c,-14} [dim]{Commands[c]}[/]");
        choices.Add("[dim]Cancel[/]");

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[dim]Commands[/]")
                .PageSize(8)
                .HighlightStyle(new Style(Color.Black, Color.Cyan))
                .AddChoices(choices));

        if (selected.StartsWith("[dim]Cancel", StringComparison.Ordinal))
        {
            return null;
        }

        // Extract the command (first word)
        var spaceIndex = selected.IndexOf(' ', StringComparison.Ordinal);
        return spaceIndex > 0 ? selected[..spaceIndex] : selected;
    }

    private static string? ShowPromptPicker(List<string> prompts)
    {
        var choices = prompts.ConvertAll(p => $"{p,-20} [dim]{Prompts[p].Description}[/]");
        choices.Add("[dim]Cancel[/]");

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[dim]Prompts[/]")
                .PageSize(8)
                .HighlightStyle(new Style(Color.Black, Color.Magenta))
                .AddChoices(choices));

        if (selected.StartsWith("[dim]Cancel", StringComparison.Ordinal))
        {
            return null;
        }

        // Extract the prompt key (first word)
        var spaceIndex = selected.IndexOf(' ', StringComparison.Ordinal);
        return spaceIndex > 0 ? selected[..spaceIndex] : selected;
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
}
