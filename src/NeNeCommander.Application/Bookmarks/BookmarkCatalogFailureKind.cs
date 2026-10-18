namespace NeNeCommander.Application.Bookmarks;

/// <summary>Identifies one closed reason a complete bookmark catalog was rejected.</summary>
public abstract record BookmarkCatalogFailureKind
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the failure for more than 32 user categories.</summary>
    public static BookmarkCatalogFailureKind TooManyCategories { get; } =
        new TooManyCategoriesFailure();

    /// <summary>Gets the failure for more than 128 bookmarks.</summary>
    public static BookmarkCatalogFailureKind TooManyBookmarks { get; } =
        new TooManyBookmarksFailure();

    /// <summary>Gets the failure for category names equal under the catalog comparer.</summary>
    public static BookmarkCatalogFailureKind DuplicateCategory { get; } =
        new DuplicateCategoryFailure();

    /// <summary>Gets the failure for a bookmark that references no exact declared category.</summary>
    public static BookmarkCatalogFailureKind InvalidCategoryReference { get; } =
        new InvalidCategoryReferenceFailure();

    /// <summary>Gets the failure for duplicate bookmark names within one category.</summary>
    public static BookmarkCatalogFailureKind DuplicateBookmark { get; } =
        new DuplicateBookmarkFailure();

    /// <summary>Gets the failure for a slot assigned to more than one bookmark.</summary>
    public static BookmarkCatalogFailureKind DuplicateShortcutSlot { get; } =
        new DuplicateShortcutSlotFailure();

    /// <summary>Gets the failure for a null collection element.</summary>
    public static BookmarkCatalogFailureKind InvalidElement { get; } = new InvalidElementFailure();

    /// <summary>Gets the failure for a selection that no longer matches the current catalog.</summary>
    public static BookmarkCatalogFailureKind StaleSelection { get; } = new StaleSelectionFailure();

    private BookmarkCatalogFailureKind()
    {
    }

    private sealed record TooManyCategoriesFailure : BookmarkCatalogFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record TooManyBookmarksFailure : BookmarkCatalogFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record DuplicateCategoryFailure : BookmarkCatalogFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record InvalidCategoryReferenceFailure : BookmarkCatalogFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record DuplicateBookmarkFailure : BookmarkCatalogFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record DuplicateShortcutSlotFailure : BookmarkCatalogFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record InvalidElementFailure : BookmarkCatalogFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record StaleSelectionFailure : BookmarkCatalogFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
