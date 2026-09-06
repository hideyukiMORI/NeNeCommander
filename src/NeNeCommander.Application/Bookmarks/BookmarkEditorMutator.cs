namespace NeNeCommander.Application.Bookmarks;

/// <summary>Validates one complete draft or stale-safe metadata mutation without owning state.</summary>
internal static class BookmarkEditorMutator
{
    internal static BookmarkEditorMutationResult SaveBookmark(
        BookmarkDrafting state,
        BookmarkCatalog catalog)
    {
        BookmarkEntry? entry = ValidateBookmark(
            state.Draft,
            catalog,
            out BookmarkEditorProblem? problem);
        if (entry is null)
        {
            return StateOnly(new BookmarkDrafting(
                state.ReturnContext,
                state.Original,
                state.Draft,
                problem));
        }
        BookmarkCatalogMutationOutcome outcome = state.Original is null
            ? catalog.AddBookmark(entry)
            : catalog.ReplaceBookmark(state.Original, entry);
        return Complete(state.ReturnContext, state, outcome);
    }

    internal static BookmarkEditorMutationResult SaveCategory(
        BookmarkCategoryDrafting state,
        BookmarkCatalog catalog)
    {
        BookmarkCategoryNameParseOutcome parsed = BookmarkCategoryName.Parse(state.Name);
        if (parsed is not BookmarkCategoryNameAccepted accepted)
        {
            return StateOnly(new BookmarkCategoryDrafting(
                state.ReturnContext,
                state.Original,
                state.Name,
                BookmarkEditorProblem.InvalidName));
        }
        BookmarkCatalogMutationOutcome outcome = state.Original is null
            ? catalog.AddCategory(accepted.Name)
            : catalog.RenameCategory(state.Original, accepted.Name);
        BookmarkBrowseContext context = state.Original is null
            ? state.ReturnContext
            : RebindRenamedCategory(state.ReturnContext, state.Original.Category, accepted.Name);
        return Complete(context, state, outcome);
    }

    internal static BookmarkEditorMutationResult DeleteBookmark(
        BookmarkBrowseContext context,
        BookmarkSelection selection,
        BookmarkCatalog catalog)
    {
        return Complete(context, Browse(context), catalog.DeleteBookmark(selection));
    }

    internal static BookmarkEditorMutationResult DeleteCategory(
        BookmarkCategoryDeleteConfirmation confirmation,
        BookmarkCatalog catalog)
    {
        BookmarkCatalogMutationOutcome outcome = catalog.DeleteCategory(confirmation.Selection);
        BookmarkBrowseContext context = MoveDeletedCategoryToUncategorized(
            confirmation.ReturnContext,
            confirmation.Selection.Category);
        return outcome is BookmarkCatalogChangeRejected rejected &&
            rejected.Kind == BookmarkCatalogFailureKind.DuplicateBookmark
            ? StateOnly(Browse(
                confirmation.ReturnContext,
                BookmarkEditorProblem.CategoryDeleteCollision))
            : Complete(context, confirmation, outcome);
    }

    private static BookmarkEditorMutationResult Complete(
        BookmarkBrowseContext context,
        BookmarksEditorState failureState,
        BookmarkCatalogMutationOutcome outcome)
    {
        if (outcome is BookmarkCatalogChanged changed)
        {
            BookmarksBrowsing browsing = Browse(new BookmarkBrowseContext(
                context.SearchText,
                context.Filter,
                null));
            return new BookmarkEditorMutationResult(
                browsing,
                new BookmarkEditorTransition.CatalogChanged(changed.Catalog));
        }
        BookmarkCatalogFailureKind failure = ((BookmarkCatalogChangeRejected)outcome).Kind;
        BookmarkEditorProblem problem = MapFailure(failure);
        BookmarksEditorState rejectedState = failureState switch
        {
            BookmarkDrafting draft => new BookmarkDrafting(
                draft.ReturnContext,
                draft.Original,
                draft.Draft,
                problem),
            BookmarkCategoryDrafting category => new BookmarkCategoryDrafting(
                category.ReturnContext,
                category.Original,
                category.Name,
                problem),
            _ => Browse(context, problem),
        };
        return StateOnly(rejectedState);
    }

    private static BookmarkEntry? ValidateBookmark(
        BookmarkDraft draft,
        BookmarkCatalog catalog,
        out BookmarkEditorProblem? problem)
    {
        problem = null;
        BookmarkDisplayNameParseOutcome name = BookmarkDisplayName.Parse(draft.Name);
        if (name is not BookmarkDisplayNameAccepted acceptedName)
        {
            problem = BookmarkEditorProblem.InvalidName;
            return null;
        }
        BookmarkPathParseOutcome path = BookmarkPath.Parse(draft.Path);
        if (path is not BookmarkPathAccepted acceptedPath)
        {
            problem = BookmarkEditorProblem.InvalidPath;
            return null;
        }
        BookmarkCategoryName? category = ResolveDraftCategory(draft.Category, catalog);
        if (draft.Category is BookmarkAllCategoryFilter ||
            (draft.Category is BookmarkUserCategoryFilter && category is null))
        {
            problem = BookmarkEditorProblem.InvalidCategory;
            return null;
        }
        return BookmarkEntry.Create(acceptedName.Name, acceptedPath.Path, category, draft.Shortcut);
    }

    private static BookmarkCategoryName? ResolveDraftCategory(
        BookmarkCategoryFilter filter,
        BookmarkCatalog catalog)
    {
        if (filter is not BookmarkUserCategoryFilter user)
        {
            return null;
        }
        BookmarkCategorySelection? current = catalog.Select(user.Category);
        return current?.Category;
    }

    private static BookmarkEditorProblem MapFailure(BookmarkCatalogFailureKind failure)
    {
        return failure == BookmarkCatalogFailureKind.TooManyCategories
            ? BookmarkEditorProblem.CategoryLimit
            : failure == BookmarkCatalogFailureKind.TooManyBookmarks
                ? BookmarkEditorProblem.BookmarkLimit
                : failure == BookmarkCatalogFailureKind.DuplicateCategory
                    ? BookmarkEditorProblem.DuplicateCategory
                    : failure == BookmarkCatalogFailureKind.DuplicateShortcutSlot
                        ? BookmarkEditorProblem.DuplicateShortcut
                        : failure == BookmarkCatalogFailureKind.DuplicateBookmark
                            ? BookmarkEditorProblem.DuplicateBookmark
                            : failure == BookmarkCatalogFailureKind.StaleSelection
                                ? BookmarkEditorProblem.StaleSelection
                                : BookmarkEditorProblem.InvalidCategory;
    }

    private static BookmarkBrowseContext RebindRenamedCategory(
        BookmarkBrowseContext context,
        BookmarkCategoryName original,
        BookmarkCategoryName replacement)
    {
        BookmarkCategoryFilter filter = context.Filter is BookmarkUserCategoryFilter user &&
            BookmarkKey.CategoryEquals(user.Category, original)
            ? BookmarkCategoryFilter.For(replacement)
            : context.Filter;
        return new BookmarkBrowseContext(context.SearchText, filter, null);
    }

    private static BookmarkBrowseContext MoveDeletedCategoryToUncategorized(
        BookmarkBrowseContext context,
        BookmarkCategoryName deleted)
    {
        BookmarkCategoryFilter filter = context.Filter is BookmarkUserCategoryFilter user &&
            BookmarkKey.CategoryEquals(user.Category, deleted)
            ? BookmarkCategoryFilter.Uncategorized
            : context.Filter;
        return new BookmarkBrowseContext(context.SearchText, filter, null);
    }

    private static BookmarkEditorMutationResult StateOnly(BookmarksEditorState state)
    {
        return new BookmarkEditorMutationResult(
            state,
            new BookmarkEditorTransition.StateChanged());
    }

    private static BookmarksBrowsing Browse(
        BookmarkBrowseContext context,
        BookmarkEditorProblem? problem = null)
    {
        return new BookmarksBrowsing(context, problem);
    }
}
