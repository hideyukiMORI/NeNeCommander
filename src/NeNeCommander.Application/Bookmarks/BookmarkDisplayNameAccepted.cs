namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents bookmark-name text accepted as one bounded display name.</summary>
public sealed record BookmarkDisplayNameAccepted : BookmarkDisplayNameParseOutcome
{
    internal BookmarkDisplayNameAccepted(BookmarkDisplayName name)
    {
        Name = name;
    }

    /// <summary>Gets the accepted bookmark display name.</summary>
    public BookmarkDisplayName Name { get; }
}
