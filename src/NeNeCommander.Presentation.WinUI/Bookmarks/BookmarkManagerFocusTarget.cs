namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>Closed native-control focus target for each newly rendered manager substate.</summary>
public abstract record BookmarkManagerFocusTarget
{
    /// <summary>Gets the browse search target.</summary>
    public static BookmarkManagerFocusTarget Search { get; } = new SearchTarget();
    /// <summary>Gets the bookmark-name draft target.</summary>
    public static BookmarkManagerFocusTarget BookmarkName { get; } = new BookmarkNameTarget();
    /// <summary>Gets the category-name draft target.</summary>
    public static BookmarkManagerFocusTarget CategoryName { get; } = new CategoryNameTarget();
    /// <summary>Gets the default-safe category-delete Cancel target.</summary>
    public static BookmarkManagerFocusTarget CancelCategoryDelete { get; } =
        new CancelCategoryDeleteTarget();
    /// <summary>Gets the failed-navigation Retry target.</summary>
    public static BookmarkManagerFocusTarget RetryNavigation { get; } = new RetryNavigationTarget();

    private BookmarkManagerFocusTarget()
    {
    }

    private sealed record SearchTarget : BookmarkManagerFocusTarget;
    private sealed record BookmarkNameTarget : BookmarkManagerFocusTarget;
    private sealed record CategoryNameTarget : BookmarkManagerFocusTarget;
    private sealed record CancelCategoryDeleteTarget : BookmarkManagerFocusTarget;
    private sealed record RetryNavigationTarget : BookmarkManagerFocusTarget;
}
