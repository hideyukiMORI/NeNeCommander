namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents the closed category filter choices owned by the bookmark manager.</summary>
public abstract record BookmarkCategoryFilter
{
    /// <summary>Gets the filter that includes every registered bookmark.</summary>
    public static BookmarkCategoryFilter All { get; } = new BookmarkAllCategoryFilter();

    /// <summary>Gets the filter for bookmarks without a user category.</summary>
    public static BookmarkCategoryFilter Uncategorized { get; } =
        new BookmarkUncategorizedCategoryFilter();

    private protected BookmarkCategoryFilter()
    {
    }

    /// <summary>Creates a filter for one typed user category.</summary>
    public static BookmarkCategoryFilter For(BookmarkCategoryName category)
    {
        return new BookmarkUserCategoryFilter(category);
    }
}
