using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Owns bookmark-manager interaction state and delegates validated catalog mutations.</summary>
internal sealed class BookmarkEditorSession
{
    internal BookmarksEditorState Current { get; private set; } = BookmarksEditorState.Closed;

    internal void Open()
    {
        Current = Browse(new BookmarkBrowseContext(string.Empty, BookmarkCategoryFilter.All, null));
    }

    internal void Close()
    {
        Current = BookmarksEditorState.Closed;
    }

    internal BookmarkEditorTransition Apply(
        BookmarkEditorAction action,
        BookmarkCatalog catalog,
        BookmarkRegistrationDefaults defaults)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(defaults);
        return action switch
        {
            BookmarkEditorAction.SearchAction search => Search(search.Text),
            BookmarkEditorAction.FilterAction filter => Filter(filter.SelectedFilter, catalog),
            BookmarkEditorAction.SelectAction select => Select(select.Selection, catalog),
            BookmarkEditorAction.BeginAddBookmarkAction => BeginAddBookmark(defaults),
            BookmarkEditorAction.BeginEditBookmarkAction edit =>
                BeginEditBookmark(edit.Selection, catalog),
            BookmarkEditorAction.UpdateBookmarkAction update => UpdateBookmark(update.Draft),
            BookmarkEditorAction.SaveAction => Save(catalog),
            BookmarkEditorAction.BeginAddCategoryAction => BeginAddCategory(),
            BookmarkEditorAction.BeginRenameCategoryAction rename =>
                BeginRenameCategory(rename.Selection, catalog),
            BookmarkEditorAction.UpdateCategoryAction update => UpdateCategory(update.Name),
            BookmarkEditorAction.DeleteBookmarkAction delete =>
                DeleteBookmark(delete.Selection, catalog),
            BookmarkEditorAction.BeginDeleteCategoryAction delete =>
                BeginDeleteCategory(delete.Selection, catalog),
            BookmarkEditorAction.ConfirmDeleteCategoryAction => ConfirmDeleteCategory(catalog),
            BookmarkEditorAction.CancelAction => Cancel(),
            _ => Changed(),
        };
    }

    internal bool BeginNavigation(BookmarkSelection selection, BookmarkCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(catalog);
        BookmarkBrowseContext? context = CurrentBrowseContext();
        if (context is null || !catalog.Matches(selection))
        {
            if (context is not null)
            {
                Current = Browse(context, BookmarkEditorProblem.StaleSelection);
            }
            return false;
        }
        Current = new BookmarkNavigationPending(context, selection);
        return true;
    }

    internal void FinishNavigationSucceeded()
    {
        if (Current is BookmarkNavigationPending)
        {
            Current = BookmarksEditorState.Closed;
        }
    }

    internal void FinishNavigationFailed()
    {
        if (Current is BookmarkNavigationPending pending)
        {
            Current = new BookmarkNavigationFailed(pending.ReturnContext, pending.Selection);
        }
    }

    private BookmarkEditorTransition.StateChanged Search(string text)
    {
        if (Current is BookmarksBrowsing browsing)
        {
            Current = Browse(new BookmarkBrowseContext(text, browsing.Context.Filter, null));
        }
        return Changed();
    }

    private BookmarkEditorTransition.StateChanged Filter(
        BookmarkCategoryFilter filter,
        BookmarkCatalog catalog)
    {
        if (Current is BookmarksBrowsing browsing)
        {
            BookmarkCategoryFilter? current = ResolveFilter(filter, catalog);
            Current = current is not null
                ? Browse(new BookmarkBrowseContext(browsing.Context.SearchText, current, null))
                : Browse(browsing.Context, BookmarkEditorProblem.InvalidCategory);
        }
        return Changed();
    }

    private BookmarkEditorTransition.StateChanged Select(
        BookmarkSelection? selection,
        BookmarkCatalog catalog)
    {
        if (Current is BookmarksBrowsing browsing)
        {
            BookmarkSelection? current = selection is null ? null : catalog.Select(selection.Key);
            Current = selection is null || (current is not null && catalog.Matches(selection))
                ? Browse(new BookmarkBrowseContext(
                    browsing.Context.SearchText,
                    browsing.Context.Filter,
                    current))
                : Browse(browsing.Context, BookmarkEditorProblem.StaleSelection);
        }
        return Changed();
    }

    private BookmarkEditorTransition.StateChanged BeginAddBookmark(
        BookmarkRegistrationDefaults defaults)
    {
        if (Current is BookmarksBrowsing browsing)
        {
            BookmarkCategoryFilter category = browsing.Context.Filter is BookmarkUserCategoryFilter
                or BookmarkUncategorizedCategoryFilter
                ? browsing.Context.Filter
                : BookmarkCategoryFilter.Uncategorized;
            Current = new BookmarkDrafting(
                browsing.Context,
                null,
                new BookmarkDraft(defaults.Name, defaults.Path, category, null),
                null);
        }
        return Changed();
    }

    private BookmarkEditorTransition.StateChanged BeginEditBookmark(
        BookmarkSelection selection,
        BookmarkCatalog catalog)
    {
        if (Current is BookmarksBrowsing browsing)
        {
            BookmarkSelection? current = catalog.Select(selection.Key);
            Current = current is not null && catalog.Matches(selection)
                ? CreateEditDraft(browsing.Context, current)
                : Browse(browsing.Context, BookmarkEditorProblem.StaleSelection);
        }
        return Changed();
    }

    private BookmarkEditorTransition.StateChanged UpdateBookmark(BookmarkDraft draft)
    {
        if (Current is BookmarkDrafting current)
        {
            Current = new BookmarkDrafting(
                current.ReturnContext,
                current.Original,
                draft,
                null);
        }
        return Changed();
    }

    private BookmarkEditorTransition Save(BookmarkCatalog catalog)
    {
        return Current switch
        {
            BookmarkDrafting bookmark => ApplyMutationResult(
                BookmarkEditorMutator.SaveBookmark(bookmark, catalog)),
            BookmarkCategoryDrafting category => ApplyMutationResult(
                BookmarkEditorMutator.SaveCategory(category, catalog)),
            _ => Changed(),
        };
    }

    private BookmarkEditorTransition.StateChanged BeginAddCategory()
    {
        if (Current is BookmarksBrowsing browsing)
        {
            Current = new BookmarkCategoryDrafting(browsing.Context, null, string.Empty, null);
        }
        return Changed();
    }

    private BookmarkEditorTransition.StateChanged BeginRenameCategory(
        BookmarkCategorySelection selection,
        BookmarkCatalog catalog)
    {
        if (Current is BookmarksBrowsing browsing)
        {
            BookmarkCategorySelection? current = catalog.Select(selection.Category);
            Current = current is not null && catalog.Matches(selection)
                ? new BookmarkCategoryDrafting(
                    browsing.Context,
                    current,
                    current.Category.Value,
                    null)
                : Browse(browsing.Context, BookmarkEditorProblem.StaleSelection);
        }
        return Changed();
    }

    private BookmarkEditorTransition.StateChanged UpdateCategory(string name)
    {
        if (Current is BookmarkCategoryDrafting current)
        {
            Current = new BookmarkCategoryDrafting(
                current.ReturnContext,
                current.Original,
                name,
                null);
        }
        return Changed();
    }

    private BookmarkEditorTransition DeleteBookmark(
        BookmarkSelection selection,
        BookmarkCatalog catalog)
    {
        BookmarkBrowseContext? context = CurrentBrowseContext();
        return context is null
            ? Changed()
            : ApplyMutationResult(BookmarkEditorMutator.DeleteBookmark(context, selection, catalog));
    }

    private BookmarkEditorTransition.StateChanged BeginDeleteCategory(
        BookmarkCategorySelection selection,
        BookmarkCatalog catalog)
    {
        if (Current is BookmarksBrowsing browsing)
        {
            BookmarkCategorySelection? current = catalog.Select(selection.Category);
            Current = current is not null && catalog.Matches(selection)
                ? new BookmarkCategoryDeleteConfirmation(browsing.Context, current)
                : Browse(browsing.Context, BookmarkEditorProblem.StaleSelection);
        }
        return Changed();
    }

    private BookmarkEditorTransition ConfirmDeleteCategory(BookmarkCatalog catalog)
    {
        return Current is BookmarkCategoryDeleteConfirmation confirmation
            ? ApplyMutationResult(BookmarkEditorMutator.DeleteCategory(confirmation, catalog))
            : Changed();
    }

    private BookmarkEditorTransition Cancel()
    {
        switch (Current)
        {
            case BookmarkDrafting draft:
                Current = Browse(draft.ReturnContext);
                return Changed();
            case BookmarkCategoryDrafting category:
                Current = Browse(category.ReturnContext);
                return Changed();
            case BookmarkCategoryDeleteConfirmation confirmation:
                Current = Browse(confirmation.ReturnContext);
                return Changed();
            case BookmarkNavigationFailed failed:
                Current = Browse(failed.ReturnContext);
                return Changed();
            case BookmarksBrowsing:
                return new BookmarkEditorTransition.CloseRequested();
            default:
                return Changed();
        }
    }

    private BookmarkEditorTransition ApplyMutationResult(BookmarkEditorMutationResult result)
    {
        Current = result.State;
        return result.Transition;
    }

    private BookmarkBrowseContext? CurrentBrowseContext()
    {
        return Current switch
        {
            BookmarksBrowsing browsing => browsing.Context,
            BookmarkNavigationFailed failed => failed.ReturnContext,
            _ => null,
        };
    }

    private static BookmarkCategoryFilter? ResolveFilter(
        BookmarkCategoryFilter filter,
        BookmarkCatalog catalog)
    {
        if (filter is BookmarkAllCategoryFilter or BookmarkUncategorizedCategoryFilter)
        {
            return filter;
        }
        BookmarkCategorySelection? category = filter is BookmarkUserCategoryFilter user
            ? catalog.Select(user.Category)
            : null;
        return category is null ? null : BookmarkCategoryFilter.For(category.Category);
    }

    private static BookmarksBrowsing Browse(BookmarkBrowseContext context)
    {
        return new BookmarksBrowsing(context, null);
    }

    private static BookmarksBrowsing Browse(
        BookmarkBrowseContext context,
        BookmarkEditorProblem problem)
    {
        return new BookmarksBrowsing(context, problem);
    }

    private static BookmarkDrafting CreateEditDraft(
        BookmarkBrowseContext context,
        BookmarkSelection selection)
    {
        BookmarkEntry entry = selection.Entry;
        BookmarkCategoryFilter category = entry.Category is null
            ? BookmarkCategoryFilter.Uncategorized
            : BookmarkCategoryFilter.For(entry.Category);
        return new BookmarkDrafting(
            context,
            selection,
            new BookmarkDraft(
                entry.Name.Value,
                entry.Path.Value.CanonicalText,
                category,
                entry.ShortcutSlot),
            null);
    }

    private static BookmarkEditorTransition.StateChanged Changed()
    {
        return new BookmarkEditorTransition.StateChanged();
    }
}
