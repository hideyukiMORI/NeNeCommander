using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Tests;

internal static class BookmarkEditorTestValues
{
    internal static BookmarkEditorSession OpenEditor()
    {
        BookmarkEditorSession editor = new();
        editor.Open();
        return editor;
    }

    internal static BookmarkCategoryName Category(string value)
    {
        return Assert.IsInstanceOfType<BookmarkCategoryNameAccepted>(
            BookmarkCategoryName.Parse(value)).Name;
    }

    internal static BookmarkEntry Entry(
        string name,
        string path,
        BookmarkCategoryName? category,
        BookmarkShortcutSlot? shortcut = null)
    {
        BookmarkDisplayName displayName = Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(
            BookmarkDisplayName.Parse(name)).Name;
        BookmarkPath bookmarkPath = Assert.IsInstanceOfType<BookmarkPathAccepted>(
            BookmarkPath.Parse(path)).Path;
        return BookmarkEntry.Create(displayName, bookmarkPath, category, shortcut);
    }

    internal static BookmarkCatalog Catalog(
        BookmarkCategoryName[] categories,
        BookmarkEntry[] bookmarks)
    {
        return Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create(categories, bookmarks)).Catalog;
    }

    internal static BookmarkRegistrationDefaults EmptyDefaults()
    {
        return new BookmarkRegistrationDefaults(string.Empty, string.Empty);
    }

    internal static BookmarkCategorySelection RequireCategory(
        BookmarkCatalog catalog,
        BookmarkCategoryName category)
    {
        return catalog.Select(category) ??
            throw new InvalidOperationException("The category fixture must be selectable.");
    }

    internal static BookmarkCategoryName[] MaximumCategories()
    {
        BookmarkCategoryName[] categories = new BookmarkCategoryName[BookmarkCatalog.MaximumCategoryCount];
        for (int index = 0; index < categories.Length; index++)
        {
            categories[index] = Category($"Category {index}");
        }
        return categories;
    }

    internal static BookmarkEntry[] MaximumBookmarks()
    {
        BookmarkEntry[] bookmarks = new BookmarkEntry[BookmarkCatalog.MaximumBookmarkCount];
        for (int index = 0; index < bookmarks.Length; index++)
        {
            bookmarks[index] = Entry($"Bookmark {index}", $"C:\\path{index}", null);
        }
        return bookmarks;
    }
}
