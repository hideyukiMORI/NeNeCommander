using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Identifies a bookmark by category and display name under catalog comparison rules.</summary>
public sealed record BookmarkKey
{
    /// <summary>Initializes a key from one accepted entry.</summary>
    public BookmarkKey(BookmarkCategoryName? category, BookmarkDisplayName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Category = category;
        Name = name;
    }

    /// <summary>Gets the category identity, or null for Uncategorized.</summary>
    public BookmarkCategoryName? Category { get; }

    /// <summary>Gets the display-name identity.</summary>
    public BookmarkDisplayName Name { get; }

    /// <inheritdoc />
    public bool Equals(BookmarkKey? other)
    {
        return other is not null &&
            CategoryEquals(Category, other.Category) &&
            StringComparer.OrdinalIgnoreCase.Equals(Name.Value, other.Name.Value);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Category?.Value, StringComparer.OrdinalIgnoreCase);
        hash.Add(Name.Value, StringComparer.OrdinalIgnoreCase);
        return hash.ToHashCode();
    }

    internal static bool CategoryEquals(
        BookmarkCategoryName? left,
        BookmarkCategoryName? right)
    {
        return left is null
            ? right is null
            : right is not null &&
                StringComparer.OrdinalIgnoreCase.Equals(left.Value, right.Value);
    }
}
