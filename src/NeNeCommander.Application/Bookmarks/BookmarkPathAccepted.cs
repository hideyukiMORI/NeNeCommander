namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents path text accepted as one canonical bookmark path.</summary>
public sealed record BookmarkPathAccepted : BookmarkPathParseOutcome
{
    internal BookmarkPathAccepted(BookmarkPath path)
    {
        Path = path;
    }

    /// <summary>Gets the accepted bookmark path.</summary>
    public BookmarkPath Path { get; }
}
