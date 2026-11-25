using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Projects the three existing command entries without introducing an execution route.</summary>
public static class CommandEntryPresenter
{
    private static readonly IReadOnlyList<CommandEntryButton> Buttons = Array.AsReadOnly(
    [
        Button(UserIntent.OpenBookmarks),
        Button(UserIntent.OpenSettings),
        Button(UserIntent.OpenHelp),
    ]);

    /// <summary>Projects the current input owner into enabled appearance and immutable command labels.</summary>
    public static CommandEntryPresentation Present(CommanderSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new CommandEntryPresentation(Buttons, snapshot);
    }

    private static CommandEntryButton Button(UserIntent intent)
    {
        KeyBinding binding = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList).First(value => value.Intent == intent);
        return new CommandEntryButton(intent, binding);
    }

    /// <summary>Formats a localized label and canonical shortcut without host-owned wording.</summary>
    public static string ToolTipFor(CommandEntryButton button, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(button);
        ArgumentNullException.ThrowIfNull(localize);
        return string.Format(CultureInfo.InvariantCulture, localize("CommandEntryToolTipFormat"),
            localize(button.LabelResourceKey), localize(button.ShortcutResourceKey));
    }
}
