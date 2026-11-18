using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves captured bookmark and category selections retain their complete identity before a mutation.</summary>
[TestClass]
public sealed class BookmarkCatalogSelectionTests
{
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

    /// <summary>Proves complete bookmark snapshots reject case-only spelling and slot changes.</summary>
    [TestMethod]
    public void BookmarkMutationWhenCompleteDisplayValueChangesRejectsEachStaleField()
    {
        BookmarkCategoryName displayedCategory = Category("Work");
        BookmarkEntry displayed = Entry(
            "Repo",
            "C:\\repo",
            displayedCategory,
            BookmarkShortcutSlot.One);
        BookmarkSelection selection = new(displayed);
        BookmarkCatalog nameChanged = Catalog(
            [displayedCategory],
            [Entry("repo", "C:\\repo", displayedCategory, BookmarkShortcutSlot.One)]);
        BookmarkCategoryName currentCategory = Category("work");
        BookmarkCatalog categoryChanged = Catalog(
            [currentCategory],
            [Entry("Repo", "C:\\repo", currentCategory, BookmarkShortcutSlot.One)]);
        BookmarkCatalog slotChanged = Catalog(
            [displayedCategory],
            [Entry("Repo", "C:\\repo", displayedCategory, BookmarkShortcutSlot.Two)]);

        AssertStale(nameChanged.DeleteBookmark(selection));
        AssertStale(categoryChanged.DeleteBookmark(selection));
        AssertStale(slotChanged.DeleteBookmark(selection));
    }

    /// <summary>Proves missing bookmark and category snapshots reject both mutation shapes.</summary>
    [TestMethod]
    public void MutationWhenSelectedValuesAreNoLongerPresentRejectsAsStale()
    {
        BookmarkCategoryName category = Category("Work");
        BookmarkEntry entry = Entry("Repo", "C:\\repo", category, null);
        BookmarkCatalog initial = Catalog([category], [entry]);
        BookmarkSelection bookmark = new(entry);
        BookmarkCategorySelection categorySelection = initial.Select(category) ??
            throw new InvalidOperationException("The category fixture must be selectable.");

        AssertStale(BookmarkCatalog.Empty.ReplaceBookmark(
            bookmark,
            Entry("Replacement", "C:\\replacement", null, null)));
        Assert.IsFalse(BookmarkCatalog.Empty.Matches(categorySelection));
    }

    /// <summary>Proves category confirmation rejects an added affected entry without partial mutation.</summary>
    [TestMethod]
    public void CategoryMutationWhenAffectedEntrySetChangesRejectsTheWholeCatalog()
    {
        BookmarkCategoryName other = Category("Other");
        BookmarkCategoryName work = Category("Work");
        BookmarkEntry first = Entry("First", "C:\\one", work, null);
        BookmarkCatalog initial = Catalog([other, work], [first]);
        BookmarkCategorySelection selection = initial.Select(work) ??
            throw new InvalidOperationException("The category fixture must be selectable.");
        BookmarkCatalog current = Catalog(
            [other, work],
            [first, Entry("Second", "C:\\two", work, null)]);

        AssertStale(current.RenameCategory(selection, Category("Projects")));
        Assert.HasCount(2, current.Categories);
        Assert.HasCount(2, current.Bookmarks);
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

    private static void AssertStale(BookmarkCatalogMutationOutcome outcome)
    {
        Assert.AreSame(
            BookmarkCatalogFailureKind.StaleSelection,
            Assert.IsInstanceOfType<BookmarkCatalogChangeRejected>(outcome).Kind);
    }
}
