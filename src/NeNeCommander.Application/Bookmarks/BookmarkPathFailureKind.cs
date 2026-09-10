namespace NeNeCommander.Application.Bookmarks;

/// <summary>Identifies one closed reason bookmark path text was rejected.</summary>
public abstract record BookmarkPathFailureKind
{
    /// <summary>Gets the failure for an unpaired UTF-16 surrogate.</summary>
    public static BookmarkPathFailureKind InvalidUnicode { get; } = new InvalidUnicodeFailure();

    /// <summary>Gets the failure for text rejected by the canonical filesystem-path parser.</summary>
    public static BookmarkPathFailureKind InvalidPath { get; } = new InvalidPathFailure();

    private BookmarkPathFailureKind()
    {
    }

    private sealed record InvalidUnicodeFailure : BookmarkPathFailureKind;
    private sealed record InvalidPathFailure : BookmarkPathFailureKind;
}
