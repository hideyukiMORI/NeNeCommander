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
        Assert.AreEqual(
            new string('n', BookmarkDisplayName.MaximumLength),
            Name(new string('n', BookmarkDisplayName.MaximumLength)).Value);
        Assert.AreSame(
            BookmarkTextFailureKind.TooLong,
            Assert.IsInstanceOfType<BookmarkDisplayNameRejected>(
                BookmarkDisplayName.Parse(
                    new string('n', BookmarkDisplayName.MaximumLength + 1))).Kind);
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

    /// <summary>Proves null category and bookmark elements are typed whole-catalog rejections.</summary>
    [TestMethod]
    public void CreateWhenCollectionContainsNullRejectsWithoutEnumeratingPartialState()
    {
        BookmarkCategoryName[] categoriesWithNull = new BookmarkCategoryName[1];
        BookmarkEntry[] bookmarksWithNull = new BookmarkEntry[1];

        Assert.AreSame(
            BookmarkCatalogFailureKind.InvalidElement,
            Rejected(categoriesWithNull, []).Kind);
        Assert.AreSame(
            BookmarkCatalogFailureKind.InvalidElement,
            Rejected([], bookmarksWithNull).Kind);
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

    /// <summary>Proves a case-only spelling change also invalidates an empty category snapshot.</summary>
    [TestMethod]
    public void DeleteCategoryWhenCapturedEmptyCategorySpellingChangedRejectsAsStale()
    {
        BookmarkCategoryName displayed = Category("Work");
        BookmarkCatalog initial = Catalog([displayed], []);
        BookmarkCategorySelection selection = initial.Select(displayed) ??
            throw new InvalidOperationException("The category fixture must be selectable.");
        BookmarkCatalog current = Catalog([Category("work")], []);

        BookmarkCatalogChangeRejected rejected =
            Assert.IsInstanceOfType<BookmarkCatalogChangeRejected>(
                current.DeleteCategory(selection));

        Assert.AreSame(BookmarkCatalogFailureKind.StaleSelection, rejected.Kind);
        Assert.HasCount(1, current.Categories);
        Assert.AreEqual("work", current.Categories[0].Value);
    }

    /// <summary>Proves Windows path casing does not make a complete category selection stale.</summary>
    [TestMethod]
    public void RenameCategoryWhenOnlyWindowsPathCasingChangedAcceptsSameIdentity()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCatalog displayed = Catalog(
            [work],
            [Entry("Repo", "C:\\Folder", work, BookmarkShortcutSlot.One)]);
        BookmarkCategorySelection selection = displayed.Select(work) ??
            throw new InvalidOperationException("The category fixture must be selectable.");
        BookmarkCatalog current = Catalog(
            [work],
            [Entry("Repo", "c:\\folder", work, BookmarkShortcutSlot.One)]);

        BookmarkCatalogChanged changed = Assert.IsInstanceOfType<BookmarkCatalogChanged>(
            current.RenameCategory(selection, Category("Projects")));

        Assert.AreEqual("Projects", changed.Catalog.Categories[0].Value);
        Assert.AreEqual("Projects", changed.Catalog.Bookmarks[0].Category?.Value);
    }

    /// <summary>Proves UNC server, share, and component casing use the existing same-identity rule.</summary>
    [TestMethod]
    public void DeleteBookmarkWhenOnlyUncPathCasingChangedAcceptsSameIdentity()
    {
        BookmarkEntry displayedEntry = Entry(
            "Share",
            "\\\\Server\\Share\\Folder",
            null,
            BookmarkShortcutSlot.One);
        BookmarkSelection selection = new(displayedEntry);
        BookmarkCatalog current = Catalog(
            [],
            [Entry(
                "Share",
                "\\\\server\\share\\folder",
                null,
                BookmarkShortcutSlot.One)]);

        BookmarkCatalogChanged changed = Assert.IsInstanceOfType<BookmarkCatalogChanged>(
            current.DeleteBookmark(selection));

        Assert.IsEmpty(changed.Catalog.Bookmarks);
    }

    /// <summary>Proves Linux component casing changes make a WSL bookmark selection stale.</summary>
    [TestMethod]
    public void DeleteBookmarkWhenWslPathComponentCasingChangedRejectsAsStale()
    {
        BookmarkEntry displayedEntry = Entry(
            "Repo",
            "\\\\wsl.localhost\\Ubuntu\\home\\Folder",
            null,
            BookmarkShortcutSlot.One);
        BookmarkSelection selection = new(displayedEntry);
        BookmarkCatalog current = Catalog(
            [],
            [Entry(
                "Repo",
                "\\\\wsl.localhost\\Ubuntu\\home\\folder",
                null,
                BookmarkShortcutSlot.One)]);

        BookmarkCatalogChangeRejected rejected =
            Assert.IsInstanceOfType<BookmarkCatalogChangeRejected>(
                current.DeleteBookmark(selection));

        Assert.AreSame(BookmarkCatalogFailureKind.StaleSelection, rejected.Kind);
        Assert.HasCount(1, current.Bookmarks);
    }

    /// <summary>Proves successful category rename and deletion preserve bookmark order and references.</summary>
    [TestMethod]
    public void CategoryMutationsWhenSelectionsAreCurrentRebindWithoutReorderingEntries()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCatalog catalog = Catalog(
            [work],
            [Entry("First", "C:\\one", work, null), Entry("Second", "C:\\two", null, null)]);
        BookmarkCategorySelection renameSelection = catalog.Select(work) ??
            throw new InvalidOperationException("The category fixture must be selectable.");
        BookmarkCatalog renamed = Assert.IsInstanceOfType<BookmarkCatalogChanged>(
            catalog.RenameCategory(renameSelection, Category("Projects"))).Catalog;
        BookmarkCategoryName projects = renamed.Categories[0];

        Assert.AreSame(projects, renamed.Bookmarks[0].Category);
        Assert.AreEqual("First", renamed.Bookmarks[0].Name.Value);
        Assert.AreEqual("Second", renamed.Bookmarks[1].Name.Value);
        BookmarkCategorySelection deleteSelection = renamed.Select(projects) ??
            throw new InvalidOperationException("The renamed category must be selectable.");
        BookmarkCatalog deleted = Assert.IsInstanceOfType<BookmarkCatalogChanged>(
            renamed.DeleteCategory(deleteSelection)).Catalog;

        Assert.IsEmpty(deleted.Categories);
        Assert.IsNull(deleted.Bookmarks[0].Category);
        Assert.AreEqual("First", deleted.Bookmarks[0].Name.Value);
        Assert.AreEqual("Second", deleted.Bookmarks[1].Name.Value);
    }

    /// <summary>Proves replacement collisions reject and an unchanged selected entry can be deleted.</summary>
    [TestMethod]
    public void BookmarkMutationsWhenReplacementCollidesRejectAndCurrentDeleteSucceeds()
    {
        BookmarkEntry first = Entry("First", "C:\\one", null, BookmarkShortcutSlot.One);
        BookmarkEntry second = Entry("Second", "C:\\two", null, BookmarkShortcutSlot.Two);
        BookmarkCatalog catalog = Catalog([], [first, second]);
        BookmarkSelection firstSelection = new(first);

        Assert.AreSame(
            BookmarkCatalogFailureKind.DuplicateBookmark,
            Assert.IsInstanceOfType<BookmarkCatalogChangeRejected>(
                catalog.ReplaceBookmark(
                    firstSelection,
                    Entry("second", "C:\\three", null, BookmarkShortcutSlot.One))).Kind);
        Assert.AreSame(
            BookmarkCatalogFailureKind.DuplicateShortcutSlot,
            Assert.IsInstanceOfType<BookmarkCatalogChangeRejected>(
                catalog.ReplaceBookmark(
                    firstSelection,
                    Entry("Third", "C:\\three", null, BookmarkShortcutSlot.Two))).Kind);

        BookmarkCatalog deleted = Assert.IsInstanceOfType<BookmarkCatalogChanged>(
            catalog.DeleteBookmark(firstSelection)).Catalog;
        Assert.HasCount(1, deleted.Bookmarks);
        Assert.AreEqual("Second", deleted.Bookmarks[0].Name.Value);
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
