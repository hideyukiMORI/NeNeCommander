using System;
using System.Globalization;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Presentation.WinUI.Panes;

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
    /// <summary>Gets the category-column mark: the file list's focus mark for the selected filter, otherwise unmarked.</summary>
    public PaneRowMark Mark => IsSelected ? SelectionMark : PaneRowMark.Unmarked;

    internal PaneRowMark SelectionMark { get; init; } = PaneRowMark.FocusInActivePane;
    /// <summary>
    /// Gets the number of bookmarks in this filter regardless of the search text: every bookmark for
    /// All, those without a category for Uncategorized, and the category's entries otherwise.
    /// </summary>
    public int EntryCount { get; internal init; }
    /// <summary>Gets the invariant digits of <see cref="EntryCount"/> shown in the category column.</summary>
    public string EntryCountText => EntryCount.ToString(CultureInfo.InvariantCulture);
    /// <summary>Gets the automation identifier of the category-column container.</summary>
    public string AutomationId => "BookmarkCategory_" + DisplayText;
}
