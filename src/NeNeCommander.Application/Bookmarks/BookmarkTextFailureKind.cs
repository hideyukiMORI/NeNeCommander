namespace NeNeCommander.Application.Bookmarks;

/// <summary>Identifies one closed reason why a bookmark or category label was rejected.</summary>
public abstract record BookmarkTextFailureKind
{
    /// <summary>Gets the failure for absent or whitespace-only text.</summary>
    public static BookmarkTextFailureKind Empty { get; } = new EmptyFailure();

    /// <summary>Gets the failure for text beyond the value-specific maximum length.</summary>
    public static BookmarkTextFailureKind TooLong { get; } = new TooLongFailure();

    /// <summary>Gets the failure for text with leading or trailing whitespace.</summary>
    public static BookmarkTextFailureKind SurroundingWhitespace { get; } =
        new SurroundingWhitespaceFailure();

    /// <summary>Gets the failure for text containing a control character.</summary>
    public static BookmarkTextFailureKind ControlCharacter { get; } = new ControlCharacterFailure();

    /// <summary>Gets the failure for an unpaired UTF-16 surrogate.</summary>
    public static BookmarkTextFailureKind InvalidUnicode { get; } = new InvalidUnicodeFailure();

    private BookmarkTextFailureKind()
    {
    }

    private sealed record EmptyFailure : BookmarkTextFailureKind;
    private sealed record TooLongFailure : BookmarkTextFailureKind;
    private sealed record SurroundingWhitespaceFailure : BookmarkTextFailureKind;
    private sealed record ControlCharacterFailure : BookmarkTextFailureKind;
    private sealed record InvalidUnicodeFailure : BookmarkTextFailureKind;
}
