using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves bookmark editor actions form one closed, stale-safe interaction state machine.</summary>
[TestClass]
public sealed class BookmarkEditorSessionTests
{
    /// <summary>Proves registration validates only on Save and emits one complete catalog change.</summary>
    [TestMethod]
    public void ApplyWhenAddingBookmarkPreservesDraftUntilACompleteValueIsAccepted()
    {
        BookmarkEditorSession editor = OpenEditor();
        BookmarkRegistrationDefaults defaults = new("Folder", "C:\\Folder");

        _ = editor.Apply(BookmarkEditorAction.BeginAddBookmark, BookmarkCatalog.Empty, defaults);
        BookmarkDrafting initial = Assert.IsInstanceOfType<BookmarkDrafting>(editor.Current);
        Assert.AreEqual("Folder", initial.Draft.Name);
        Assert.AreEqual("C:\\Folder", initial.Draft.Path);
        _ = Assert.IsInstanceOfType<BookmarkUncategorizedCategoryFilter>(initial.Draft.Category);

        _ = editor.Apply(
            BookmarkEditorAction.UpdateBookmark(
                new BookmarkDraft("", "C:\\Folder", BookmarkCategoryFilter.Uncategorized, null)),
            BookmarkCatalog.Empty,
            defaults);
        _ = editor.Apply(BookmarkEditorAction.Save, BookmarkCatalog.Empty, defaults);

        BookmarkDrafting rejected = Assert.IsInstanceOfType<BookmarkDrafting>(editor.Current);
        Assert.AreSame(BookmarkEditorProblem.InvalidName, rejected.Problem);
        Assert.AreEqual("C:\\Folder", rejected.Draft.Path);

        _ = editor.Apply(
            BookmarkEditorAction.UpdateBookmark(
                new BookmarkDraft(
                    "Folder",
                    "C:\\Folder",
                    BookmarkCategoryFilter.Uncategorized,
                    BookmarkShortcutSlot.One)),
            BookmarkCatalog.Empty,
            defaults);
        BookmarkEditorTransition.CatalogChanged changed =
            Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
                editor.Apply(BookmarkEditorAction.Save, BookmarkCatalog.Empty, defaults));

        Assert.HasCount(1, changed.Catalog.Bookmarks);
        Assert.AreSame(BookmarkShortcutSlot.One, changed.Catalog.Bookmarks[0].ShortcutSlot);
        _ = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
    }

    /// <summary>Proves a category deletion collision changes neither the catalog nor confirmation context.</summary>
    [TestMethod]
    public void ApplyWhenCategoryDeletionWouldCollideKeepsEveryRegistration()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCatalog catalog = Catalog(
            [work],
            [Entry("Repo", "C:\\one", null), Entry("repo", "C:\\two", work)]);
        BookmarkCategorySelection selection = catalog.Select(work) ??
            throw new InvalidOperationException("The category fixture must be selectable.");
        BookmarkEditorSession editor = OpenEditor();
        BookmarkRegistrationDefaults defaults = new(string.Empty, string.Empty);

        _ = editor.Apply(BookmarkEditorAction.BeginDeleteCategory(selection), catalog, defaults);
        BookmarkCategoryDeleteConfirmation confirmation =
            Assert.IsInstanceOfType<BookmarkCategoryDeleteConfirmation>(editor.Current);
        Assert.HasCount(1, confirmation.Selection.Entries);
        BookmarkEditorTransition transition = editor.Apply(
            BookmarkEditorAction.ConfirmDeleteCategory,
            catalog,
            defaults);

        _ = Assert.IsInstanceOfType<BookmarkEditorTransition.StateChanged>(transition);
        BookmarksBrowsing rejected = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        Assert.AreSame(BookmarkEditorProblem.CategoryDeleteCollision, rejected.Problem);
        Assert.HasCount(1, catalog.Categories);
        Assert.HasCount(2, catalog.Bookmarks);
    }

    /// <summary>Proves pending navigation freezes Escape and failure retains the browse context for retry.</summary>
    [TestMethod]
    public void NavigationWhenPendingFreezesActionsAndFailureReturnsThroughTheClosedLayers()
    {
        BookmarkEntry entry = Entry("Repo", "C:\\repo", null);
        BookmarkCatalog catalog = Catalog([], [entry]);
        BookmarkSelection selection = new(entry);
        BookmarkEditorSession editor = OpenEditor();
        BookmarkRegistrationDefaults defaults = new(string.Empty, string.Empty);
        _ = editor.Apply(BookmarkEditorAction.Search("rep"), catalog, defaults);
        _ = editor.Apply(BookmarkEditorAction.Select(selection), catalog, defaults);

        Assert.IsTrue(editor.BeginNavigation(selection, catalog));
        _ = Assert.IsInstanceOfType<BookmarkEditorTransition.StateChanged>(
            editor.Apply(BookmarkEditorAction.Cancel, catalog, defaults));
        _ = Assert.IsInstanceOfType<BookmarkNavigationPending>(editor.Current);

        editor.FinishNavigationFailed();
        BookmarkNavigationFailed failed = Assert.IsInstanceOfType<BookmarkNavigationFailed>(editor.Current);
        Assert.AreEqual("rep", failed.ReturnContext.SearchText);
        Assert.AreEqual(selection, failed.Selection);

        _ = editor.Apply(BookmarkEditorAction.Cancel, catalog, defaults);
        BookmarksBrowsing browsing = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        Assert.AreEqual("rep", browsing.Context.SearchText);
        _ = Assert.IsInstanceOfType<BookmarkEditorTransition.CloseRequested>(
            editor.Apply(BookmarkEditorAction.Cancel, catalog, defaults));
    }

    /// <summary>Proves stale edit actions report a closed problem without replacing the current entry.</summary>
    [TestMethod]
    public void ApplyWhenDisplayedEntryWasReplacedKeepsBrowsingAndReportsStaleSelection()
    {
        BookmarkEntry displayed = Entry("Repo", "C:\\old", null);
        BookmarkSelection selection = new(displayed);
        BookmarkCatalog current = Catalog([], [Entry("Repo", "C:\\new", null)]);
        BookmarkEditorSession editor = OpenEditor();

        _ = editor.Apply(
            BookmarkEditorAction.BeginEditBookmark(selection),
            current,
            new BookmarkRegistrationDefaults(string.Empty, string.Empty));

        BookmarksBrowsing browsing = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        Assert.AreSame(BookmarkEditorProblem.StaleSelection, browsing.Problem);
        Assert.AreEqual("C:\\new", current.Bookmarks[0].Path.Value.CanonicalText);
    }

    private static BookmarkEditorSession OpenEditor()
    {
        BookmarkEditorSession editor = new();
        editor.Open();
        return editor;
    }

    private static BookmarkCategoryName Category(string value)
    {
        return Assert.IsInstanceOfType<BookmarkCategoryNameAccepted>(
            BookmarkCategoryName.Parse(value)).Name;
    }

    private static BookmarkEntry Entry(string name, string path, BookmarkCategoryName? category)
    {
        BookmarkDisplayName displayName = Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(
            BookmarkDisplayName.Parse(name)).Name;
        BookmarkPath bookmarkPath = Assert.IsInstanceOfType<BookmarkPathAccepted>(
            BookmarkPath.Parse(path)).Path;
        return BookmarkEntry.Create(displayName, bookmarkPath, category, null);
    }

    private static BookmarkCatalog Catalog(
        BookmarkCategoryName[] categories,
        BookmarkEntry[] bookmarks)
    {
        return Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create(categories, bookmarks)).Catalog;
    }
}
