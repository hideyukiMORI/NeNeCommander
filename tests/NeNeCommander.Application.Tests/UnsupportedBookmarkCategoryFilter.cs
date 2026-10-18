using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Tests;

/// <summary>Test-only future category filter used to prove explicit defect rejection.</summary>
internal sealed record UnsupportedBookmarkCategoryFilter : BookmarkCategoryFilter
{
    private protected override void RestrictDerivation()
    {
    }
}
