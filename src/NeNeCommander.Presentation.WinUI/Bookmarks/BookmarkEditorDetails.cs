using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>Render-ready details for the one closed bookmark-manager substate.</summary>
public sealed record BookmarkEditorDetails
{
    private readonly ReadOnlyCollection<BookmarkCategoryOption> _draftCategories;
    private readonly ReadOnlyCollection<BookmarkShortcutOption> _shortcuts;
    private readonly BookmarksEditorState _state;

    internal BookmarkEditorDetails(
        BookmarksEditorState state,
        IReadOnlyList<BookmarkCategoryOption> draftCategories,
        IReadOnlyList<BookmarkShortcutOption> shortcuts,
        string statusResourceKey)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(draftCategories);
        ArgumentNullException.ThrowIfNull(shortcuts);
        ArgumentException.ThrowIfNullOrWhiteSpace(statusResourceKey);
        _state = state;
        _draftCategories = new List<BookmarkCategoryOption>(draftCategories).AsReadOnly();
        _shortcuts = new List<BookmarkShortcutOption>(shortcuts).AsReadOnly();
        StatusResourceKey = statusResourceKey;
    }

    /// <summary>Gets whether normal browse controls own input.</summary>
    public bool IsBrowsing => _state is BookmarksBrowsing;
    /// <summary>Gets whether a bookmark draft owns input.</summary>
    public bool IsBookmarkDrafting => _state is BookmarkDrafting;
    /// <summary>Gets whether a category draft owns input.</summary>
    public bool IsCategoryDrafting => _state is BookmarkCategoryDrafting;
    /// <summary>Gets whether category deletion awaits explicit confirmation.</summary>
    public bool IsCategoryDeleteConfirmation => _state is BookmarkCategoryDeleteConfirmation;
    /// <summary>Gets whether canonical pane navigation is pending.</summary>
    public bool IsNavigationPending => _state is BookmarkNavigationPending;
    /// <summary>Gets whether canonical pane navigation failed.</summary>
    public bool IsNavigationFailed => _state is BookmarkNavigationFailed;
    /// <summary>Gets whether all manager inputs must remain frozen.</summary>
    public bool IsInputFrozen => IsNavigationPending;
    /// <summary>Gets whether the bookmark draft registers a new entry.</summary>
    public bool IsAddingBookmark => _state is BookmarkDrafting { Original: null };
    /// <summary>Gets whether the category draft creates a new category.</summary>
    public bool IsAddingCategory => _state is BookmarkCategoryDrafting { Original: null };
    /// <summary>Gets the complete untrusted bookmark draft, when active.</summary>
    public BookmarkDraft? BookmarkDraft => (_state as BookmarkDrafting)?.Draft;
    /// <summary>Gets the selected draft category option.</summary>
    public BookmarkCategoryOption? SelectedDraftCategory =>
        _draftCategories.FirstOrDefault(option => option.IsSelected);
    /// <summary>Gets the selected shortcut option.</summary>
    public BookmarkShortcutOption? SelectedShortcut =>
        _shortcuts.FirstOrDefault(option => option.IsSelected);
    /// <summary>Gets draft category choices without the All filter.</summary>
    public IReadOnlyList<BookmarkCategoryOption> DraftCategories => _draftCategories;
    /// <summary>Gets the unassigned choice followed by fixed slots 1 through 9.</summary>
    public IReadOnlyList<BookmarkShortcutOption> Shortcuts => _shortcuts;
    /// <summary>Gets the untrusted category draft text.</summary>
    public string CategoryDraftName => (_state as BookmarkCategoryDrafting)?.Name ?? string.Empty;
    /// <summary>Gets the category captured for rename or deletion.</summary>
    public BookmarkCategorySelection? CategorySelection =>
        (_state as BookmarkCategoryDrafting)?.Original ??
        (_state as BookmarkCategoryDeleteConfirmation)?.Selection;
    /// <summary>Gets the affected bookmark count shown by category deletion confirmation.</summary>
    public int CategoryDeleteCount =>
        (_state as BookmarkCategoryDeleteConfirmation)?.Selection.Entries.Count ?? 0;
    /// <summary>Gets the immutable selection for pending, failed, or retry navigation.</summary>
    public BookmarkSelection? NavigationSelection =>
        (_state as BookmarkNavigationPending)?.Selection ??
        (_state as BookmarkNavigationFailed)?.Selection;
    /// <summary>Gets the native control that receives focus on a new substate.</summary>
    public BookmarkManagerFocusTarget InitialFocus => _state switch
    {
        BookmarkDrafting => BookmarkManagerFocusTarget.BookmarkName,
        BookmarkCategoryDrafting => BookmarkManagerFocusTarget.CategoryName,
        BookmarkCategoryDeleteConfirmation => BookmarkManagerFocusTarget.CancelCategoryDelete,
        BookmarkNavigationFailed => BookmarkManagerFocusTarget.RetryNavigation,
        _ => BookmarkManagerFocusTarget.Search,
    };
    /// <summary>Gets the localized status or problem resource.</summary>
    public string StatusResourceKey { get; }
}
