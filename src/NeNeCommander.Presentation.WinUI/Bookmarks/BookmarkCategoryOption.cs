using System;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>One rendered category filter and its stale-safe category selection, when applicable.</summary>
public sealed record BookmarkCategoryOption
{
    internal BookmarkCategoryOption(
        BookmarkCategoryFilter filter,
        string displayText,
        BookmarkCategorySelection? selection,
        BookmarkOptionSelection optionSelection)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayText);
        ArgumentNullException.ThrowIfNull(optionSelection);
        Filter = filter;
        DisplayText = displayText;
        Selection = selection;
        IsSelected = optionSelection == BookmarkOptionSelection.Selected;
    }

    /// <summary>Gets the typed filter submitted when this option is selected.</summary>
    public BookmarkCategoryFilter Filter { get; }
    /// <summary>Gets the localized or user-authored category text.</summary>
    public string DisplayText { get; }
    /// <summary>Gets the stale-safe user-category selection for category actions.</summary>
    public BookmarkCategorySelection? Selection { get; }
    /// <summary>Gets whether this option matches the session-owned filter.</summary>
    public bool IsSelected { get; }
}
