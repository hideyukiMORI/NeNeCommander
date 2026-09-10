namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents category text accepted as one bounded name.</summary>
public sealed record BookmarkCategoryNameAccepted : BookmarkCategoryNameParseOutcome
{
    internal BookmarkCategoryNameAccepted(BookmarkCategoryName name)
    {
        Name = name;
    }

    /// <summary>Gets the accepted category name.</summary>
    public BookmarkCategoryName Name { get; }
}
