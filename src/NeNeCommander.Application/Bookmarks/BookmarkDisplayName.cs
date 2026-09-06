namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents one bounded bookmark display name.</summary>
public sealed record BookmarkDisplayName
{
    /// <summary>Maximum accepted bookmark-name length in UTF-16 code units.</summary>
    public const int MaximumLength = 128;

    private BookmarkDisplayName(string value)
    {
        Value = value;
    }

    /// <summary>Gets the accepted display spelling.</summary>
    public string Value { get; }

    /// <summary>Validates untrusted bookmark-name text without trimming or rewriting it.</summary>
    public static BookmarkDisplayNameParseOutcome Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new BookmarkDisplayNameRejected(BookmarkTextFailureKind.Empty);
        }
        BookmarkTextFailureKind? failure = BookmarkCategoryName.Validate(value, MaximumLength);
        return failure is not null
            ? new BookmarkDisplayNameRejected(failure)
            : new BookmarkDisplayNameAccepted(new BookmarkDisplayName(value));
    }
}
