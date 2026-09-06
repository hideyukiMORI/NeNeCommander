using System;
using System.Linq;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents one bounded user category name. Null elsewhere identifies Uncategorized.</summary>
public sealed record BookmarkCategoryName
{
    /// <summary>Maximum accepted category-name length in UTF-16 code units.</summary>
    public const int MaximumLength = 64;

    private BookmarkCategoryName(string value)
    {
        Value = value;
    }

    /// <summary>Gets the display spelling retained from accepted input.</summary>
    public string Value { get; }

    /// <summary>Validates untrusted category text without trimming or rewriting it.</summary>
    public static BookmarkCategoryNameParseOutcome Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new BookmarkCategoryNameRejected(BookmarkTextFailureKind.Empty);
        }
        BookmarkTextFailureKind? failure = Validate(value, MaximumLength);
        return failure is not null
            ? new BookmarkCategoryNameRejected(failure)
            : new BookmarkCategoryNameAccepted(new BookmarkCategoryName(value));
    }

    internal static BookmarkTextFailureKind? Validate(string value, int maximumLength)
    {
        return value.Length > maximumLength
            ? BookmarkTextFailureKind.TooLong
            : !string.Equals(value, value.Trim(), StringComparison.Ordinal)
                ? BookmarkTextFailureKind.SurroundingWhitespace
                : value.Any(char.IsControl)
                    ? BookmarkTextFailureKind.ControlCharacter
                    : HasInvalidSurrogate(value)
                        ? BookmarkTextFailureKind.InvalidUnicode
                        : null;
    }

    internal static bool HasInvalidSurrogate(string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    return true;
                }
                index++;
            }
            else if (char.IsLowSurrogate(character))
            {
                return true;
            }
        }
        return false;
    }
}
