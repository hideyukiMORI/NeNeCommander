using System;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>One unassigned or canonical fixed shortcut choice shown in a bookmark draft.</summary>
public sealed record BookmarkShortcutOption
{
    internal BookmarkShortcutOption(
        BookmarkShortcutSlot? slot,
        string labelResourceKey,
        BookmarkOptionSelection optionSelection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(labelResourceKey);
        ArgumentNullException.ThrowIfNull(optionSelection);
        Slot = slot;
        LabelResourceKey = labelResourceKey;
        IsSelected = optionSelection == BookmarkOptionSelection.Selected;
    }

    /// <summary>Gets the fixed shortcut slot, or null for no shortcut.</summary>
    public BookmarkShortcutSlot? Slot { get; }
    /// <summary>Gets the canonical localized key-label resource.</summary>
    public string LabelResourceKey { get; }
    /// <summary>Gets whether this option matches the session-owned draft.</summary>
    public bool IsSelected { get; }
}
