namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>
/// Identifies the closed focus region of the bookmark manager's browse state (ADR-0080): the
/// category column or the bookmark list. Movement and the row and category commands act on the
/// region that holds focus.
/// </summary>
public abstract record BookmarkManagerRegion
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the category column.</summary>
    public static BookmarkManagerRegion Categories { get; } = new CategoriesRegion();

    /// <summary>Gets the bookmark list, which holds focus when the manager opens.</summary>
    public static BookmarkManagerRegion Bookmarks { get; } = new BookmarksRegion();

    private BookmarkManagerRegion()
    {
    }

    /// <summary>Gets the region that <c>Tab</c> or <c>Shift+Tab</c> moves focus to.</summary>
    public abstract BookmarkManagerRegion Other { get; }

    private sealed record CategoriesRegion : BookmarkManagerRegion
    {
        private protected override void RestrictDerivation()
        {
        }

        public override BookmarkManagerRegion Other => Bookmarks;
    }

    private sealed record BookmarksRegion : BookmarkManagerRegion
    {
        private protected override void RestrictDerivation()
        {
        }

        public override BookmarkManagerRegion Other => Categories;
    }
}
