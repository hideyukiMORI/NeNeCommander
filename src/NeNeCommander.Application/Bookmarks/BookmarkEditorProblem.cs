namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents closed, user-correctable bookmark-manager rejection reasons.</summary>
public abstract record BookmarkEditorProblem
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the invalid bookmark or category name problem.</summary>
    public static BookmarkEditorProblem InvalidName { get; } = new InvalidNameProblem();
    /// <summary>Gets the invalid bookmark path problem.</summary>
    public static BookmarkEditorProblem InvalidPath { get; } = new InvalidPathProblem();
    /// <summary>Gets the unavailable or invalid category problem.</summary>
    public static BookmarkEditorProblem InvalidCategory { get; } = new InvalidCategoryProblem();
    /// <summary>Gets the duplicate category problem.</summary>
    public static BookmarkEditorProblem DuplicateCategory { get; } = new DuplicateCategoryProblem();
    /// <summary>Gets the duplicate bookmark key problem.</summary>
    public static BookmarkEditorProblem DuplicateBookmark { get; } = new DuplicateBookmarkProblem();
    /// <summary>Gets the duplicate fixed shortcut assignment problem.</summary>
    public static BookmarkEditorProblem DuplicateShortcut { get; } = new DuplicateShortcutProblem();
    /// <summary>Gets the user-category capacity problem.</summary>
    public static BookmarkEditorProblem CategoryLimit { get; } = new CategoryLimitProblem();
    /// <summary>Gets the bookmark capacity problem.</summary>
    public static BookmarkEditorProblem BookmarkLimit { get; } = new BookmarkLimitProblem();
    /// <summary>Gets the stale displayed selection problem.</summary>
    public static BookmarkEditorProblem StaleSelection { get; } = new StaleSelectionProblem();
    /// <summary>Gets the collision that prevents atomic category deletion.</summary>
    public static BookmarkEditorProblem CategoryDeleteCollision { get; } =
        new CategoryDeleteCollisionProblem();

    private protected BookmarkEditorProblem()
    {
    }

    private sealed record InvalidNameProblem : BookmarkEditorProblem
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record InvalidPathProblem : BookmarkEditorProblem
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record InvalidCategoryProblem : BookmarkEditorProblem
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record DuplicateCategoryProblem : BookmarkEditorProblem
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record DuplicateBookmarkProblem : BookmarkEditorProblem
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record DuplicateShortcutProblem : BookmarkEditorProblem
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record CategoryLimitProblem : BookmarkEditorProblem
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record BookmarkLimitProblem : BookmarkEditorProblem
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record StaleSelectionProblem : BookmarkEditorProblem
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record CategoryDeleteCollisionProblem : BookmarkEditorProblem
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
