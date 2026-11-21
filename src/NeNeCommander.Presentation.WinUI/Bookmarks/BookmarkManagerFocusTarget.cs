namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>Closed native-control focus target for each newly rendered manager substate.</summary>
public abstract record BookmarkManagerFocusTarget
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the browse search target.</summary>
    public static BookmarkManagerFocusTarget Search { get; } = new SearchTarget();
    /// <summary>Gets the bookmark-list target that receives focus while browsing (ADR-0080).</summary>
    public static BookmarkManagerFocusTarget BookmarkList { get; } = new BookmarkListTarget();
    /// <summary>Gets the bookmark-name draft target.</summary>
    public static BookmarkManagerFocusTarget BookmarkName { get; } = new BookmarkNameTarget();
    /// <summary>Gets the category-name draft target.</summary>
    public static BookmarkManagerFocusTarget CategoryName { get; } = new CategoryNameTarget();
    /// <summary>Gets the default-safe category-delete Cancel target.</summary>
    public static BookmarkManagerFocusTarget CancelCategoryDelete { get; } =
        new CancelCategoryDeleteTarget();
    /// <summary>Gets the default-safe bookmark-delete Cancel target (ADR-0080).</summary>
    public static BookmarkManagerFocusTarget CancelBookmarkDelete { get; } =
        new CancelBookmarkDeleteTarget();
    /// <summary>Gets the failed-navigation Retry target.</summary>
    public static BookmarkManagerFocusTarget RetryNavigation { get; } = new RetryNavigationTarget();

    private BookmarkManagerFocusTarget()
    {
    }

    private sealed record SearchTarget : BookmarkManagerFocusTarget
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record BookmarkListTarget : BookmarkManagerFocusTarget
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record BookmarkNameTarget : BookmarkManagerFocusTarget
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record CategoryNameTarget : BookmarkManagerFocusTarget
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record CancelCategoryDeleteTarget : BookmarkManagerFocusTarget
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record CancelBookmarkDeleteTarget : BookmarkManagerFocusTarget
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record RetryNavigationTarget : BookmarkManagerFocusTarget
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
