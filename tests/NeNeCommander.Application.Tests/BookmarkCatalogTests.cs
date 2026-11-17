using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves bookmark values, complete catalog construction, and current mutations preserve their invariants.</summary>
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
            BookmarkTextFailureKind.InvalidUnicode,
            Assert.IsInstanceOfType<BookmarkCategoryNameRejected>(
                BookmarkCategoryName.Parse("bad\uDC00")).Kind);
        AssertDisplayNameBoundaries();
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

    /// <summary>Proves lookup traverses ordered entries and absent keys remain unbound.</summary>
    [TestMethod]
    public void LookupWhenEntriesVaryFindsLaterMatchesAndReturnsNullForAbsentKeys()
    {
        BookmarkEntry first = Entry("First", "C:\\one", null, BookmarkShortcutSlot.One);
        BookmarkEntry second = Entry("Second", "C:\\two", null, BookmarkShortcutSlot.Two);
        BookmarkCatalog catalog = Catalog([], [first, second]);

        Assert.AreSame(second, catalog.Find(BookmarkShortcutSlot.Two));
        Assert.AreSame(second, catalog.Find(new BookmarkKey(null, second.Name)));
        Assert.IsNull(catalog.Find(new BookmarkKey(null, Name("Missing"))));
        Assert.IsNull(catalog.Select(new BookmarkKey(null, Name("Missing"))));
        Assert.IsNull(catalog.Select(Category("Missing")));
    }

    /// <summary>Proves case-insensitive bookmark keys keep equality and hash behavior aligned.</summary>
    [TestMethod]
    public void BookmarkKeyWhenSpellingVariesKeepsEqualityAndHashCodesAligned()
    {
        BookmarkKey uncategorizedUpper = new(null, Name("Repo"));
        BookmarkKey uncategorizedLower = new(null, Name("repo"));
        BookmarkKey categorizedUpper = new(Category("Work"), Name("Repo"));
        BookmarkKey categorizedLower = new(Category("work"), Name("repo"));

        Assert.AreEqual(uncategorizedUpper, uncategorizedLower);
        Assert.AreEqual(uncategorizedUpper.GetHashCode(), uncategorizedLower.GetHashCode());
        Assert.AreEqual(categorizedUpper, categorizedLower);
        Assert.AreEqual(categorizedUpper.GetHashCode(), categorizedLower.GetHashCode());
        Assert.AreNotEqual(uncategorizedUpper, categorizedUpper);
        Assert.AreNotEqual(uncategorizedUpper.GetHashCode(), categorizedUpper.GetHashCode());
        Assert.AreNotEqual(
            categorizedUpper.GetHashCode(),
            new BookmarkKey(Category("Work"), Name("Other")).GetHashCode());
    }

    /// <summary>Proves a later category selection rebinds only the entries that reference it.</summary>
    [TestMethod]
    public void CategoryMutationsWhenLaterCategoryIsSelectedRebindOnlyThatCategory()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCategoryName home = Category("Home");
        BookmarkCatalog catalog = Catalog(
            [work, home],
            [
                Entry("First", "C:\\one", work, null),
                Entry("Second", "C:\\two", home, null),
                Entry("Third", "C:\\three", null, null),
            ]);
        BookmarkCategorySelection homeSelection = catalog.Select(home) ??
            throw new InvalidOperationException("The later category fixture must be selectable.");

        BookmarkCatalog renamed = Assert.IsInstanceOfType<BookmarkCatalogChanged>(
            catalog.RenameCategory(homeSelection, Category("Projects"))).Catalog;

        Assert.AreEqual("Work", renamed.Categories[0].Value);
        Assert.AreEqual("Projects", renamed.Categories[1].Value);
        Assert.AreEqual("Work", renamed.Bookmarks[0].Category?.Value);
        Assert.AreEqual("Projects", renamed.Bookmarks[1].Category?.Value);
        Assert.IsNull(renamed.Bookmarks[2].Category);

        BookmarkCategorySelection projectsSelection = renamed.Select(renamed.Categories[1]) ??
            throw new InvalidOperationException("The renamed category must be selectable.");
        BookmarkCatalog deleted = Assert.IsInstanceOfType<BookmarkCatalogChanged>(
            renamed.DeleteCategory(projectsSelection)).Catalog;

        Assert.HasCount(1, deleted.Categories);
        Assert.AreEqual("Work", deleted.Categories[0].Value);
        Assert.AreEqual("Work", deleted.Bookmarks[0].Category?.Value);
        Assert.IsNull(deleted.Bookmarks[1].Category);
        Assert.IsNull(deleted.Bookmarks[2].Category);
    }

    private static void AssertDisplayNameBoundaries()
    {
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
