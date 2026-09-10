namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>Closed placeholder content for the bookmark result region.</summary>
public sealed record BookmarkEmptyContent
{
    /// <summary>Gets the hidden placeholder used when rows are available.</summary>
    public static BookmarkEmptyContent Hidden { get; } = new("KeyLabelUnmapped");

    /// <summary>Gets guidance for an empty bookmark catalog.</summary>
    public static BookmarkEmptyContent NoBookmarks { get; } = new("BookmarkEmptyNoBookmarks");

    /// <summary>Gets guidance when the active search or category has no matches.</summary>
    public static BookmarkEmptyContent NoMatches { get; } = new("BookmarkEmptyNoMatches");

    private BookmarkEmptyContent(string resourceKey)
    {
        ResourceKey = resourceKey;
    }

    /// <summary>Gets the localized placeholder resource key.</summary>
    public string ResourceKey { get; }
}
