using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents the complete bounded bookmark and category catalog.</summary>
public sealed record BookmarkCatalog
{
    /// <summary>Maximum number of user categories. Uncategorized is represented by null.</summary>
    public const int MaximumCategoryCount = 32;

    /// <summary>Maximum number of registered bookmarks.</summary>
    public const int MaximumBookmarkCount = 128;

    private readonly ReadOnlyCollection<BookmarkEntry> _bookmarks;
    private readonly ReadOnlyCollection<BookmarkCategoryName> _categories;

    private BookmarkCatalog(
        IReadOnlyList<BookmarkCategoryName> categories,
        IReadOnlyList<BookmarkEntry> bookmarks)
    {
        _categories = new List<BookmarkCategoryName>(categories).AsReadOnly();
        _bookmarks = new List<BookmarkEntry>(bookmarks).AsReadOnly();
    }

    /// <summary>Gets the empty catalog used by defaults and version-1 settings migration.</summary>
    public static BookmarkCatalog Empty { get; } = new([], []);

    /// <summary>Gets user categories in their preserved order.</summary>
    public IReadOnlyList<BookmarkCategoryName> Categories => _categories;

    /// <summary>Gets bookmark registrations in their preserved order.</summary>
    public IReadOnlyList<BookmarkEntry> Bookmarks => _bookmarks;

    /// <summary>Validates and defensively copies one complete catalog.</summary>
    public static BookmarkCatalogCreationOutcome Create(
        IReadOnlyList<BookmarkCategoryName> categories,
        IReadOnlyList<BookmarkEntry> bookmarks)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(bookmarks);
        if (categories.Count > MaximumCategoryCount)
        {
            return new BookmarkCatalogRejected(BookmarkCatalogFailureKind.TooManyCategories);
        }
        if (bookmarks.Count > MaximumBookmarkCount)
        {
            return new BookmarkCatalogRejected(BookmarkCatalogFailureKind.TooManyBookmarks);
        }
        if (ContainsNull(categories) || ContainsNull(bookmarks))
        {
            return new BookmarkCatalogRejected(BookmarkCatalogFailureKind.InvalidElement);
        }
        if (HasDuplicateCategories(categories))
        {
            return new BookmarkCatalogRejected(BookmarkCatalogFailureKind.DuplicateCategory);
        }
        List<BookmarkEntry> normalized = [];
        foreach (BookmarkEntry bookmark in bookmarks)
        {
            BookmarkCategoryName? category = ResolveCategory(categories, bookmark.Category);
            if (bookmark.Category is not null && category is null)
            {
                return new BookmarkCatalogRejected(
                    BookmarkCatalogFailureKind.InvalidCategoryReference);
            }
            normalized.Add(category is null
                ? bookmark
                : BookmarkEntry.Create(
                    bookmark.Name,
                    bookmark.Path,
                    category,
                    bookmark.ShortcutSlot));
        }
        return HasDuplicateBookmarks(normalized)
            ? new BookmarkCatalogRejected(BookmarkCatalogFailureKind.DuplicateBookmark)
            : HasDuplicateSlots(normalized)
                ? new BookmarkCatalogRejected(BookmarkCatalogFailureKind.DuplicateShortcutSlot)
                : new BookmarkCatalogAccepted(new BookmarkCatalog(categories, normalized));
    }

    /// <summary>Finds the bookmark assigned to one slot in the current immutable catalog.</summary>
    public BookmarkEntry? Find(BookmarkShortcutSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return _bookmarks.FirstOrDefault(bookmark => bookmark.ShortcutSlot == slot);
    }

    /// <summary>Finds the bookmark with one case-insensitive category/name key.</summary>
    public BookmarkEntry? Find(BookmarkKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _bookmarks.FirstOrDefault(
            bookmark => new BookmarkKey(bookmark.Category, bookmark.Name) == key);
    }

    /// <summary>Captures a current bookmark for a later stale-safe action.</summary>
    public BookmarkSelection? Select(BookmarkKey key)
    {
        BookmarkEntry? entry = Find(key);
        return entry is null ? null : new BookmarkSelection(entry);
    }

    internal bool Matches(BookmarkSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        return BookmarkIndex(selection) >= 0;
    }

    internal static bool SelectionsMatch(BookmarkSelection left, BookmarkSelection right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return EntriesMatch(left.Entry, right.Entry);
    }

    /// <summary>Captures a current user category and all entries that reference it.</summary>
    public BookmarkCategorySelection? Select(BookmarkCategoryName category)
    {
        ArgumentNullException.ThrowIfNull(category);
        BookmarkCategoryName? current = ResolveCategory(_categories, category);
        if (current is null)
        {
            return null;
        }
        List<BookmarkEntry> entries =
            [.. _bookmarks.Where(bookmark => BookmarkKey.CategoryEquals(bookmark.Category, current))];
        return new BookmarkCategorySelection(current, entries);
    }

    internal bool Matches(BookmarkCategorySelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        return CategoryIndex(selection.Category) >= 0 && CategorySelectionMatches(selection);
    }

    /// <summary>Adds one user category at the end of the preserved category order.</summary>
    public BookmarkCatalogMutationOutcome AddCategory(BookmarkCategoryName category)
    {
        ArgumentNullException.ThrowIfNull(category);
        List<BookmarkCategoryName> categories = [.. _categories, category];
        return Mutation(Create(categories, _bookmarks));
    }

    /// <summary>Renames one unchanged selected category and all of its current references.</summary>
    public BookmarkCatalogMutationOutcome RenameCategory(
        BookmarkCategorySelection selection,
        BookmarkCategoryName replacement)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(replacement);
        int categoryIndex = CategoryIndex(selection.Category);
        if (categoryIndex < 0 || !CategorySelectionMatches(selection))
        {
            return Stale();
        }
        List<BookmarkCategoryName> categories = [.. _categories];
        categories[categoryIndex] = replacement;
        List<BookmarkEntry> bookmarks = [];
        foreach (BookmarkEntry bookmark in _bookmarks)
        {
            bookmarks.Add(BookmarkKey.CategoryEquals(bookmark.Category, selection.Category)
                ? BookmarkEntry.Create(
                    bookmark.Name,
                    bookmark.Path,
                    replacement,
                    bookmark.ShortcutSlot)
                : bookmark);
        }
        return Mutation(Create(categories, bookmarks));
    }

    /// <summary>Deletes one unchanged category by moving every entry to Uncategorized.</summary>
    public BookmarkCatalogMutationOutcome DeleteCategory(BookmarkCategorySelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        int categoryIndex = CategoryIndex(selection.Category);
        if (categoryIndex < 0 || !CategorySelectionMatches(selection))
        {
            return Stale();
        }
        List<BookmarkCategoryName> categories = [.. _categories];
        categories.RemoveAt(categoryIndex);
        List<BookmarkEntry> bookmarks = [];
        foreach (BookmarkEntry bookmark in _bookmarks)
        {
            bookmarks.Add(BookmarkKey.CategoryEquals(bookmark.Category, selection.Category)
                ? BookmarkEntry.Create(
                    bookmark.Name,
                    bookmark.Path,
                    null,
                    bookmark.ShortcutSlot)
                : bookmark);
        }
        return Mutation(Create(categories, bookmarks));
    }

    /// <summary>Adds one complete bookmark at the end of the preserved bookmark order.</summary>
    public BookmarkCatalogMutationOutcome AddBookmark(BookmarkEntry bookmark)
    {
        ArgumentNullException.ThrowIfNull(bookmark);
        List<BookmarkEntry> bookmarks = [.. _bookmarks, bookmark];
        return Mutation(Create(_categories, bookmarks));
    }

    /// <summary>Replaces one entry only when its complete captured value is still current.</summary>
    public BookmarkCatalogMutationOutcome ReplaceBookmark(
        BookmarkSelection selection,
        BookmarkEntry replacement)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(replacement);
        int index = BookmarkIndex(selection);
        if (index < 0)
        {
            return Stale();
        }
        List<BookmarkEntry> bookmarks = [.. _bookmarks];
        bookmarks[index] = replacement;
        return Mutation(Create(_categories, bookmarks));
    }

    /// <summary>Deletes one entry only when its complete captured value is still current.</summary>
    public BookmarkCatalogMutationOutcome DeleteBookmark(BookmarkSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        int index = BookmarkIndex(selection);
        if (index < 0)
        {
            return Stale();
        }
        List<BookmarkEntry> bookmarks = [.. _bookmarks];
        bookmarks.RemoveAt(index);
        return Mutation(Create(_categories, bookmarks));
    }

    private int BookmarkIndex(BookmarkSelection selection)
    {
        for (int index = 0; index < _bookmarks.Count; index++)
        {
            BookmarkEntry current = _bookmarks[index];
            if (new BookmarkKey(current.Category, current.Name) == selection.Key &&
                EntriesMatch(current, selection.Entry))
            {
                return index;
            }
        }
        return -1;
    }

    private int CategoryIndex(BookmarkCategoryName category)
    {
        for (int index = 0; index < _categories.Count; index++)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(_categories[index].Value, category.Value))
            {
                return index;
            }
        }
        return -1;
    }

    private bool CategorySelectionMatches(BookmarkCategorySelection selection)
    {
        int categoryIndex = CategoryIndex(selection.Category);
        if (categoryIndex < 0 || !StringComparer.Ordinal.Equals(
            _categories[categoryIndex].Value,
            selection.Category.Value))
        {
            return false;
        }
        List<BookmarkEntry> current =
            [.. _bookmarks.Where(bookmark =>
                BookmarkKey.CategoryEquals(bookmark.Category, selection.Category))];
        if (current.Count != selection.Entries.Count)
        {
            return false;
        }
        for (int index = 0; index < current.Count; index++)
        {
            if (!EntriesMatch(current[index], selection.Entries[index]))
            {
                return false;
            }
        }
        return true;
    }

    private static bool EntriesMatch(BookmarkEntry left, BookmarkEntry right)
    {
        return left.Name == right.Name &&
            left.Category == right.Category &&
            left.ShortcutSlot == right.ShortcutSlot &&
            FileSystemPathIdentityComparer.Instance.Equals(left.Path.Value, right.Path.Value);
    }

    private static BookmarkCatalogMutationOutcome Mutation(
        BookmarkCatalogCreationOutcome outcome)
    {
        return outcome is BookmarkCatalogAccepted accepted
            ? new BookmarkCatalogChanged(accepted.Catalog)
            : new BookmarkCatalogChangeRejected(((BookmarkCatalogRejected)outcome).Kind);
    }

    private static BookmarkCatalogChangeRejected Stale()
    {
        return new BookmarkCatalogChangeRejected(BookmarkCatalogFailureKind.StaleSelection);
    }

    private static bool HasDuplicateCategories(IReadOnlyList<BookmarkCategoryName> categories)
    {
        for (int index = 0; index < categories.Count; index++)
        {
            for (int candidate = index + 1; candidate < categories.Count; candidate++)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(
                    categories[index].Value,
                    categories[candidate].Value))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static BookmarkCategoryName? ResolveCategory(
        IReadOnlyList<BookmarkCategoryName> categories,
        BookmarkCategoryName? category)
    {
        return category is null
            ? null
            : categories.FirstOrDefault(candidate =>
                StringComparer.OrdinalIgnoreCase.Equals(candidate.Value, category.Value));
    }

    private static bool HasDuplicateBookmarks(List<BookmarkEntry> bookmarks)
    {
        for (int index = 0; index < bookmarks.Count; index++)
        {
            BookmarkKey key = new(bookmarks[index].Category, bookmarks[index].Name);
            for (int candidate = index + 1; candidate < bookmarks.Count; candidate++)
            {
                if (key == new BookmarkKey(
                    bookmarks[candidate].Category,
                    bookmarks[candidate].Name))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool HasDuplicateSlots(IReadOnlyList<BookmarkEntry> bookmarks)
    {
        HashSet<int> slots = [];
        return bookmarks
            .Select(bookmark => bookmark.ShortcutSlot)
            .OfType<BookmarkShortcutSlot>()
            .Any(slot => !slots.Add(slot.Number));
    }

    private static bool ContainsNull<T>(IReadOnlyList<T> values)
        where T : class
    {
        return values.Any(value => value is null);
    }
}
