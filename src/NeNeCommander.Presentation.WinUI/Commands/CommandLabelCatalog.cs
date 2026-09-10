using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NeNeCommander.Application.Input;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Owns the sole Presentation correspondence from canonical intents to localized labels.</summary>
public static class CommandLabelCatalog
{
    private static readonly IReadOnlyList<CommandLabel> DeclaredLabels = new ReadOnlyCollection<CommandLabel>(
    [
        new(UserIntent.OpenFocused, "IntentLabelOpenFocused"),
        new(UserIntent.NavigateParent, "IntentLabelNavigateParent"),
        new(UserIntent.NavigateBack, "IntentLabelNavigateBack"),
        new(UserIntent.NavigateForward, "IntentLabelNavigateForward"),
        new(UserIntent.Refresh, "IntentLabelRefresh"),
        new(UserIntent.FocusAddress, "IntentLabelFocusAddress"),
        new(UserIntent.Rename, "IntentLabelRename"),
        new(UserIntent.Copy, "IntentLabelCopy"),
        new(UserIntent.Move, "IntentLabelMove"),
        new(UserIntent.CreateDirectory, "IntentLabelCreateDirectory"),
        new(UserIntent.Delete, "IntentLabelDelete"),
        new(UserIntent.ToggleSelection, "IntentLabelToggleSelection"),
        new(UserIntent.ToggleHiddenItems, "IntentLabelToggleHiddenItems"),
        new(UserIntent.ActivateOtherPane, "IntentLabelActivateOtherPane"),
        new(UserIntent.OpenSettings, "IntentLabelOpenSettings"),
        new(UserIntent.OpenCommandPalette, "IntentLabelOpenCommandPalette"),
        new(UserIntent.OpenBookmarks, "IntentLabelOpenBookmarks"),
        new(UserIntent.Escape, "IntentLabelEscape"),
        new(UserIntent.Confirm, "IntentLabelConfirm"),
    ]);

    /// <summary>Gets the label declared for an intent.</summary>
    public static CommandLabel LabelFor(UserIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        return DeclaredLabels.Single(label => label.Intent == intent);
    }
}
