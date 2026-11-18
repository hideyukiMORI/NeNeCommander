using System;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>
/// Decides the bookmark manager's keyboard behavior (ADR-0080): which context owns input, what one
/// action does in the focused region, and where movement lands. The row and category commands
/// submit exactly the existing actions of the buttons they replace and stay unavailable where
/// those buttons were disabled, so no second route to an operation exists.
/// </summary>
public static class BookmarkManagerKeyboard
{
    /// <summary>Returns the context that owns input while the manager is shown.</summary>
    /// <param name="details">Current manager substate projection.</param>
    /// <param name="focused">
    /// <see cref="BookmarkManagerFocusTarget.Search"/> while the search field holds focus, otherwise
    /// <see cref="BookmarkManagerFocusTarget.BookmarkList"/>.
    /// </param>
    /// <returns>
    /// <see cref="KeyboardContext.BookmarkSearch"/> or <see cref="KeyboardContext.Bookmarks"/> while
    /// browsing, or <see langword="null"/> when a draft, confirmation or navigation keeps the
    /// shared modal context.
    /// </returns>
    public static KeyboardContext? ContextFor(BookmarkEditorDetails details, BookmarkManagerFocusTarget focused)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(focused);
        return !details.IsBrowsing
            ? null
            : focused == BookmarkManagerFocusTarget.Search ? KeyboardContext.BookmarkSearch : KeyboardContext.Bookmarks;
    }

    /// <summary>Resolves one action in the focused region against the rendered selections.</summary>
    /// <param name="region">Region that holds focus.</param>
    /// <param name="action">Action the key map or a key hint produced.</param>
    /// <param name="row">Selected bookmark row, when one is selected.</param>
    /// <param name="category">Selected category option, when one is selected.</param>
    /// <returns>The one effect the view applies.</returns>
    public static BookmarkManagerCommand Resolve(
        BookmarkManagerRegion region,
        BookmarkManagerKeyAction action,
        BookmarkRow? row,
        BookmarkCategoryOption? category)
    {
        ArgumentNullException.ThrowIfNull(region);
        ArgumentNullException.ThrowIfNull(action);
        return action switch
        {
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.MoveNext =>
                BookmarkManagerCommand.Move(1),
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.MovePrevious =>
                BookmarkManagerCommand.Move(-1),
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.SwitchRegion =>
                BookmarkManagerCommand.Focus(region.Other),
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.FocusSearch =>
                BookmarkManagerCommand.Search,
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.LeaveSearch =>
                BookmarkManagerCommand.Focus(BookmarkManagerRegion.Bookmarks),
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Close =>
                BookmarkManagerCommand.Forward(UserIntent.Escape),
            _ when region == BookmarkManagerRegion.Categories => ResolveCategory(action, category),
            _ => ResolveBookmark(action, row),
        };
    }

    /// <summary>Returns the index to select after one movement step; movement stops at both ends.</summary>
    /// <param name="current">Selected index, or a negative value when nothing is selected.</param>
    /// <param name="count">Number of items in the region.</param>
    /// <param name="step">One for the next item, minus one for the previous item.</param>
    /// <returns>The index to select, or a negative value when the region is empty.</returns>
    public static int Move(int current, int count, int step)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return count == 0
            ? -1
            : current < 0 || current >= count
                ? 0
                : Math.Clamp(current + Math.Sign(step), 0, count - 1);
    }

    private static BookmarkManagerCommand ResolveBookmark(BookmarkManagerKeyAction action, BookmarkRow? row)
    {
        return action == BookmarkManagerKeyAction.Add
            ? Submit(BookmarkEditorAction.BeginAddBookmark)
            : row is null ? BookmarkManagerCommand.None : ResolveSelectedBookmark(action, row.Selection);
    }

    private static BookmarkManagerCommand ResolveSelectedBookmark(
        BookmarkManagerKeyAction action,
        BookmarkSelection selection)
    {
        return action switch
        {
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Activate =>
                BookmarkManagerCommand.Forward(UserIntent.NavigateBookmark(selection)),
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Edit =>
                Submit(BookmarkEditorAction.BeginEditBookmark(selection)),
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Delete =>
                Submit(BookmarkEditorAction.BeginDeleteBookmark(selection)),
            _ => BookmarkManagerCommand.None,
        };
    }

    private static BookmarkManagerCommand ResolveCategory(
        BookmarkManagerKeyAction action,
        BookmarkCategoryOption? category)
    {
        return action == BookmarkManagerKeyAction.Activate
            ? BookmarkManagerCommand.Focus(BookmarkManagerRegion.Bookmarks)
            : action == BookmarkManagerKeyAction.Add
                ? Submit(BookmarkEditorAction.BeginAddCategory)
                : category?.Selection is BookmarkCategorySelection selection
                    ? ResolveSelectedCategory(action, selection)
                    : BookmarkManagerCommand.None;
    }

    private static BookmarkManagerCommand ResolveSelectedCategory(
        BookmarkManagerKeyAction action,
        BookmarkCategorySelection selection)
    {
        return action switch
        {
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Edit =>
                Submit(BookmarkEditorAction.BeginRenameCategory(selection)),
            BookmarkManagerKeyAction value when value == BookmarkManagerKeyAction.Delete =>
                Submit(BookmarkEditorAction.BeginDeleteCategory(selection)),
            _ => BookmarkManagerCommand.None,
        };
    }

    private static BookmarkManagerCommand Submit(BookmarkEditorAction action)
    {
        return BookmarkManagerCommand.Forward(UserIntent.ManageBookmarks(action));
    }
}
