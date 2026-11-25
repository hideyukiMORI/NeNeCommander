using System;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Bookmarks;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.App.Views;

/// <summary>
/// Applies the bookmark manager's mapped keys and pressed key hints (ADR-0080). It remembers which
/// region holds focus, asks <see cref="BookmarkManagerKeyboard"/> what one action does there, and
/// applies the answer: an existing intent goes to the session, movement and focus go to the browse
/// controls. The key band is the region's projection from <see cref="BookmarkManagerKeyHintPresenter"/>.
/// </summary>
internal sealed class BookmarkKeyboardView
{
    private readonly BookmarkBrowseView _browse;
    private readonly BookmarkKeyHintView _hints;
    private readonly Action<UserIntent> _forward;
    private BookmarkManagerRegion _region = BookmarkManagerRegion.Bookmarks;

    internal BookmarkKeyboardView(BookmarkBrowseView browse, BookmarkKeyHintView hints, Action<UserIntent> forward)
    {
        ArgumentNullException.ThrowIfNull(browse);
        ArgumentNullException.ThrowIfNull(hints);
        ArgumentNullException.ThrowIfNull(forward);
        _browse = browse;
        _hints = hints;
        _forward = forward;
        _browse.RegionFocused += (_, region) => ShowRegion(region);
        _browse.Search.GotFocus += (_, _) => _hints.RenderSearch();
        RenderHints();
    }

    /// <summary>Returns the keyboard context the manager owns for the rendered substate.</summary>
    /// <param name="details">Rendered manager substate.</param>
    /// <returns>The owned context, or <see langword="null"/> when the shared modal context applies.</returns>
    internal KeyboardContext? ContextFor(BookmarkEditorDetails details)
    {
        return BookmarkManagerKeyboard.ContextFor(
            details,
            _browse.SearchHasFocus ? BookmarkManagerFocusTarget.Search : BookmarkManagerFocusTarget.BookmarkList);
    }

    /// <summary>Applies one mapped bookmark-manager action in the focused region.</summary>
    /// <param name="action">Action the canonical bookmark table produced.</param>
    internal void Forward(BookmarkManagerKeyAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Apply(BookmarkManagerKeyboard.Resolve(_region, action, _browse.SelectedRow, _browse.SelectedCategory));
    }

    /// <summary>Applies the action of one pressed key hint exactly as its key would.</summary>
    /// <param name="hint">Pressed hint.</param>
    internal void Invoke(BookmarkManagerKeyHint hint)
    {
        ArgumentNullException.ThrowIfNull(hint);
        if (hint.IsInvocable)
        {
            Forward(hint.Action);
        }
    }

    /// <summary>Starts browsing with focus on the bookmark list, as the manager does when it opens.</summary>
    internal void FocusBookmarkList()
    {
        ShowRegion(BookmarkManagerRegion.Bookmarks);
        _browse.Focus(BookmarkManagerRegion.Bookmarks);
    }

    private void Apply(BookmarkManagerCommand command)
    {
        if (command.Intent is UserIntent intent)
        {
            _forward(intent);
        }
        if (command.Step != 0)
        {
            _browse.Move(_region, command.Step);
        }
        if (command.FocusRegion is BookmarkManagerRegion region)
        {
            ShowRegion(region);
            _browse.Focus(region);
        }
        if (command.FocusesSearch)
        {
            _browse.FocusSearch();
        }
    }

    private void ShowRegion(BookmarkManagerRegion region)
    {
        _region = region;
        RenderHints();
    }

    private void RenderHints()
    {
        if (_browse.SearchHasFocus)
        {
            _hints.RenderSearch();
            return;
        }
        _hints.Render(_region);
    }
}
