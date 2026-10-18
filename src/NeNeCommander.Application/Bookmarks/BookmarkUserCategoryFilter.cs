using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Includes bookmarks in one typed user category.</summary>
public sealed record BookmarkUserCategoryFilter : BookmarkCategoryFilter
{
    private protected override void RestrictDerivation()
    {
    }

    internal BookmarkUserCategoryFilter(BookmarkCategoryName category)
    {
        ArgumentNullException.ThrowIfNull(category);
        Category = category;
    }

    /// <summary>Gets the preserved category selected by the user.</summary>
    public BookmarkCategoryName Category { get; }
}
