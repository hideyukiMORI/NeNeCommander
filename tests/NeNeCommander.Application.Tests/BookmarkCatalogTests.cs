using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves bookmark values and complete catalog mutations preserve their closed invariants.</summary>
[TestClass]
public sealed class BookmarkCatalogTests
{
    /// <summary>Proves category and display-name boundaries reject lossy or ambiguous text.</summary>
    [TestMethod]
    public void NamesWhenAtBoundariesAcceptLosslessTextAndRejectInvalidText()
    {
        string validNonBmp = $"Work{char.ConvertFromUtf32(0x1F4C1)}";
        string unpairedSurrogate = new((char)0xD800, 1);

        Assert.AreEqual(
            new string('c', BookmarkCategoryName.MaximumLength),
            Category(new string('c', BookmarkCategoryName.MaximumLength)).Value);
        Assert.AreSame(
            BookmarkTextFailureKind.TooLong,
            Assert.IsInstanceOfType<BookmarkCategoryNameRejected>(
                BookmarkCategoryName.Parse(
                    new string('c', BookmarkCategoryName.MaximumLength + 1))).Kind);
        Assert.AreEqual(validNonBmp, Category(validNonBmp).Value);
        Assert.AreSame(
            BookmarkTextFailureKind.InvalidUnicode,
            Assert.IsInstanceOfType<BookmarkCategoryNameRejected>(
                BookmarkCategoryName.Parse(unpairedSurrogate)).Kind);
        Assert.AreSame(
            BookmarkTextFailureKind.Empty,
            Assert.IsInstanceOfType<BookmarkDisplayNameRejected>(
                BookmarkDisplayName.Parse(" ")).Kind);
        Assert.AreSame(
            BookmarkTextFailureKind.SurroundingWhitespace,
            Assert.IsInstanceOfType<BookmarkDisplayNameRejected>(
                BookmarkDisplayName.Parse(" Name")).Kind);
        Assert.AreSame(
            BookmarkTextFailureKind.ControlCharacter,
            Assert.IsInstanceOfType<BookmarkDisplayNameRejected>(
                BookmarkDisplayName.Parse("Na\nme")).Kind);
    }

    /// <summary>Proves bookmark paths add Unicode losslessness without changing the global parser.</summary>
    [TestMethod]
    public void PathWhenUnicodeIsLosslessUsesTheCanonicalPathBoundary()
    {
        string validNonBmp = $"C:\\{char.ConvertFromUtf32(0x1F4C1)}";
        string unpairedSurrogate = $"C:\\{new string((char)0xD800, 1)}";

        Assert.AreEqual(validNonBmp, Path(validNonBmp).Value.CanonicalText);
        Assert.AreSame(
            BookmarkPathFailureKind.InvalidUnicode,
            Assert.IsInstanceOfType<BookmarkPathRejected>(
                BookmarkPath.Parse(unpairedSurrogate)).Kind);
        Assert.AreSame(
            BookmarkPathFailureKind.InvalidPath,
            Assert.IsInstanceOfType<BookmarkPathRejected>(BookmarkPath.Parse("relative")).Kind);
    }

    /// <summary>Proves null is not confused with an integer outside the nine closed slots.</summary>
    [TestMethod]
    public void ShortcutSlotWhenParsedAcceptsOnlyOneThroughNine()
    {
        Assert.AreSame(
            BookmarkShortcutSlot.One,
            Assert.IsInstanceOfType<BookmarkShortcutSlotAccepted>(
                BookmarkShortcutSlot.Parse(1)).Slot);
        Assert.AreSame(
            BookmarkShortcutSlot.Nine,
            Assert.IsInstanceOfType<BookmarkShortcutSlotAccepted>(
                BookmarkShortcutSlot.Parse(9)).Slot);
        _ = Assert.IsInstanceOfType<BookmarkShortcutSlotRejected>(BookmarkShortcutSlot.Parse(0));
        _ = Assert.IsInstanceOfType<BookmarkShortcutSlotRejected>(BookmarkShortcutSlot.Parse(10));
    }

    /// <summary>Proves catalog creation owns its lists and canonicalizes a category reference.</summary>
    [TestMethod]
    public void CreateWhenCollectionsAreValidOwnsOrderAndPreservedCategorySpelling()
    {
        BookmarkCategoryName category = Category("Work");
        List<BookmarkCategoryName> categories = [category];
        List<BookmarkEntry> bookmarks =
        [
            Entry("Repository", "C:\\repo", Category("work"), BookmarkShortcutSlot.One),
        ];

        BookmarkCatalog catalog = Catalog(categories, bookmarks);
        categories.Clear();
        bookmarks.Clear();

        Assert.HasCount(1, catalog.Categories);
        Assert.HasCount(1, catalog.Bookmarks);
        Assert.AreSame(category, catalog.Bookmarks[0].Category);
        Assert.AreEqual("Work", catalog.Bookmarks[0].Category?.Value);
    }

    /// <summary>Proves name, category, and slot uniqueness are checked across the complete value.</summary>
    [TestMethod]
    public void CreateWhenCatalogContainsCollisionsRejectsTheCompleteValue()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCategoryName other = Category("Other");

        Assert.AreSame(
            BookmarkCatalogFailureKind.DuplicateCategory,
            Rejected([work, Category("work")], []).Kind);
        Assert.AreSame(
            BookmarkCatalogFailureKind.DuplicateBookmark,
            Rejected(
                [work],
                [
                    Entry("Repo", "C:\\one", work, null),
                    Entry("repo", "C:\\two", work, null),
                ]).Kind);
        Assert.AreSame(
            BookmarkCatalogFailureKind.DuplicateShortcutSlot,
            Rejected(
                [work, other],
                [
                    Entry("One", "C:\\one", work, BookmarkShortcutSlot.Two),
                    Entry("Two", "C:\\two", other, BookmarkShortcutSlot.Two),
                ]).Kind);
        Assert.AreSame(
            BookmarkCatalogFailureKind.InvalidCategoryReference,
            Rejected([], [Entry("Orphan", "C:\\one", work, null)]).Kind);
    }

    /// <summary>Proves exact collection limits are accepted and their next values are rejected.</summary>
    [TestMethod]
    public void CreateWhenCountsReachBoundariesAcceptsExactAndRejectsNext()
    {
        List<BookmarkCategoryName> categories = [];
        for (int index = 0; index < BookmarkCatalog.MaximumCategoryCount; index++)
        {
            categories.Add(Category($"Category {index}"));
        }
        List<BookmarkEntry> bookmarks = [];
        for (int index = 0; index < BookmarkCatalog.MaximumBookmarkCount; index++)
        {
            bookmarks.Add(Entry($"Bookmark {index}", $"C:\\path{index}", null, null));
        }

        _ = Catalog(categories, bookmarks);
        categories.Add(Category("One too many"));
        Assert.AreSame(
            BookmarkCatalogFailureKind.TooManyCategories,
            Rejected(categories, bookmarks).Kind);
        categories.RemoveAt(categories.Count - 1);
        bookmarks.Add(Entry("One too many", "C:\\overflow", null, null));
        Assert.AreSame(
            BookmarkCatalogFailureKind.TooManyBookmarks,
            Rejected(categories, bookmarks).Kind);
    }

    /// <summary>Proves deleting a category cannot partially overwrite Uncategorized metadata.</summary>
    [TestMethod]
    public void DeleteCategoryWhenUncategorizedNameCollidesRejectsEverything()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCatalog catalog = Catalog(
            [work],
            [
                Entry("Repo", "C:\\uncategorized", null, null),
                Entry("repo", "C:\\work", work, BookmarkShortcutSlot.One),
                Entry("Other", "C:\\other", work, null),
            ]);
        BookmarkCategorySelection selection = catalog.Select(work) ??
            throw new InvalidOperationException("The category fixture must be selectable.");

        BookmarkCatalogChangeRejected rejected =
            Assert.IsInstanceOfType<BookmarkCatalogChangeRejected>(catalog.DeleteCategory(selection));

        Assert.AreSame(BookmarkCatalogFailureKind.DuplicateBookmark, rejected.Kind);
        Assert.HasCount(1, catalog.Categories);
        Assert.HasCount(3, catalog.Bookmarks);
        Assert.AreEqual("Work", catalog.Bookmarks[1].Category?.Value);
    }

    /// <summary>Proves a manager selection cannot bind its old key to a changed path.</summary>
    [TestMethod]
    public void ReplaceBookmarkWhenSelectedEntryChangedRejectsAsStale()
    {
        BookmarkEntry original = Entry("Repo", "C:\\old", null, BookmarkShortcutSlot.One);
        BookmarkCatalog first = Catalog([], [original]);
        BookmarkSelection selection = first.Select(new BookmarkKey(null, original.Name)) ??
            throw new InvalidOperationException("The bookmark fixture must be selectable.");
        BookmarkEntry changed = Entry("Repo", "C:\\new", null, BookmarkShortcutSlot.One);
        BookmarkCatalog current = Assert.IsInstanceOfType<BookmarkCatalogChanged>(
            first.ReplaceBookmark(selection, changed)).Catalog;

        BookmarkCatalogChangeRejected rejected =
            Assert.IsInstanceOfType<BookmarkCatalogChangeRejected>(
                current.DeleteBookmark(selection));

        Assert.AreSame(BookmarkCatalogFailureKind.StaleSelection, rejected.Kind);
        Assert.AreEqual("C:\\new", current.Bookmarks[0].Path.Value.CanonicalText);
    }

    /// <summary>Proves category confirmation becomes stale when any captured entry changes.</summary>
    [TestMethod]
    public void RenameCategoryWhenCapturedEntryChangedRejectsWithoutPartialReferences()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkEntry original = Entry("Repo", "C:\\old", work, null);
        BookmarkCatalog first = Catalog([work], [original]);
        BookmarkCategorySelection categorySelection = first.Select(work) ??
            throw new InvalidOperationException("The category fixture must be selectable.");
        BookmarkSelection bookmarkSelection = new(original);
        BookmarkCatalog current = Assert.IsInstanceOfType<BookmarkCatalogChanged>(
            first.ReplaceBookmark(
                bookmarkSelection,
                Entry("Repo", "C:\\new", work, null))).Catalog;

        BookmarkCatalogChangeRejected rejected =
            Assert.IsInstanceOfType<BookmarkCatalogChangeRejected>(
                current.RenameCategory(categorySelection, Category("Projects")));

        Assert.AreSame(BookmarkCatalogFailureKind.StaleSelection, rejected.Kind);
        Assert.AreEqual("Work", current.Categories[0].Value);
        Assert.AreEqual("Work", current.Bookmarks[0].Category?.Value);
    }

    private static BookmarkCategoryName Category(string value)
    {
        return Assert.IsInstanceOfType<BookmarkCategoryNameAccepted>(
            BookmarkCategoryName.Parse(value)).Name;
    }

    private static BookmarkDisplayName Name(string value)
    {
        return Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(
            BookmarkDisplayName.Parse(value)).Name;
    }

    private static BookmarkPath Path(string value)
    {
        return Assert.IsInstanceOfType<BookmarkPathAccepted>(BookmarkPath.Parse(value)).Path;
    }

    private static BookmarkEntry Entry(
        string name,
        string path,
        BookmarkCategoryName? category,
        BookmarkShortcutSlot? slot)
    {
        return BookmarkEntry.Create(Name(name), Path(path), category, slot);
    }

    private static BookmarkCatalog Catalog(
        IReadOnlyList<BookmarkCategoryName> categories,
        IReadOnlyList<BookmarkEntry> bookmarks)
    {
        return Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create(categories, bookmarks)).Catalog;
    }

    private static BookmarkCatalogRejected Rejected(
        IReadOnlyList<BookmarkCategoryName> categories,
        IReadOnlyList<BookmarkEntry> bookmarks)
    {
        return Assert.IsInstanceOfType<BookmarkCatalogRejected>(
            BookmarkCatalog.Create(categories, bookmarks));
    }
}
