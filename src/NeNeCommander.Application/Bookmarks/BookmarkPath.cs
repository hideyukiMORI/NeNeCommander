using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Wraps one canonical filesystem path whose text is lossless UTF-16 bookmark data.</summary>
public sealed record BookmarkPath
{
    private BookmarkPath(FileSystemPath value)
    {
        Value = value;
    }

    /// <summary>Gets the canonical path passed to the existing navigation route.</summary>
    public FileSystemPath Value { get; }

    /// <summary>Validates text through the bookmark Unicode and canonical path boundaries.</summary>
    public static BookmarkPathParseOutcome Parse(string? input)
    {
        return input is not null && BookmarkCategoryName.HasInvalidSurrogate(input)
            ? new BookmarkPathRejected(BookmarkPathFailureKind.InvalidUnicode)
            : FileSystemPath.Parse(input) is PathParseSuccess accepted
                ? new BookmarkPathAccepted(new BookmarkPath(accepted.Path))
                : new BookmarkPathRejected(BookmarkPathFailureKind.InvalidPath);
    }
}
