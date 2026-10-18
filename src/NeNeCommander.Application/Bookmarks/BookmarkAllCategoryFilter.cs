namespace NeNeCommander.Application.Bookmarks;

/// <summary>Includes bookmarks from every category.</summary>
public sealed record BookmarkAllCategoryFilter : BookmarkCategoryFilter
{
    private protected override void RestrictDerivation()
    {
    }
}
