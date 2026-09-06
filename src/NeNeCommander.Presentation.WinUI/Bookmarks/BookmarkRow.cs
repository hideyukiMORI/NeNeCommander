using System;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>One ordered bookmark row with its immutable stale-safe selection.</summary>
public sealed record BookmarkRow
{
    internal BookmarkRow(
        BookmarkSelection selection,
        string categoryText,
        string shortcutLabelResourceKey,
        BookmarkOptionSelection optionSelection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryText);
        ArgumentNullException.ThrowIfNull(shortcutLabelResourceKey);
        ArgumentNullException.ThrowIfNull(optionSelection);
        Selection = selection;
        CategoryText = categoryText;
        ShortcutLabelResourceKey = shortcutLabelResourceKey;
        IsSelected = optionSelection == BookmarkOptionSelection.Selected;
    }

    /// <summary>Gets the complete immutable selection submitted by row actions.</summary>
    public BookmarkSelection Selection { get; }
    /// <summary>Gets the bookmark display name.</summary>
    public string NameText => Selection.Entry.Name.Value;
    /// <summary>Gets the canonical path text.</summary>
    public string PathText => Selection.Entry.Path.Value.CanonicalText;
    /// <summary>Gets the localized or user-authored category text.</summary>
    public string CategoryText { get; }
    /// <summary>Gets the canonical assigned-shortcut label, or the empty label resource.</summary>
    public string ShortcutLabelResourceKey { get; }
    /// <summary>Gets whether this row matches the session-owned selection.</summary>
    public bool IsSelected { get; }
}
