using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves bookmark drafts, replacement and deletion preserve exact catalog constraints.</summary>
[TestClass]
public sealed class BookmarkEditorDraftTests
{
    /// <summary>Proves registration validates only on Save and emits one complete catalog change.</summary>
    [TestMethod]
    public void ApplyWhenAddingBookmarkPreservesDraftUntilACompleteValueIsAccepted()
    {
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
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

    /// <summary>Proves bookmark Save rejects each invalid complete draft before catalog mutation.</summary>
    [TestMethod]
    public void ApplyWhenBookmarkDraftIsInvalidReportsNamePathOrCategoryPrecisely()
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([work], []);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        _ = editor.Apply(BookmarkEditorAction.BeginAddBookmark, catalog, BookmarkEditorTestValues.EmptyDefaults());

        AssertBookmarkSaveProblem(
            editor,
            catalog,
            new BookmarkDraft("Repo", "relative", BookmarkCategoryFilter.Uncategorized, null),
            BookmarkEditorProblem.InvalidPath);
        AssertBookmarkSaveProblem(
            editor,
            catalog,
            new BookmarkDraft("Repo", "C:\\repo", BookmarkCategoryFilter.All, null),
            BookmarkEditorProblem.InvalidCategory);
        AssertBookmarkSaveProblem(
            editor,
            catalog,
            new BookmarkDraft(
                "Repo",
                "C:\\repo",
                BookmarkCategoryFilter.For(BookmarkEditorTestValues.Category("Missing")),
                null),
            BookmarkEditorProblem.InvalidCategory);
    }

    /// <summary>Proves bookmark uniqueness, shortcut uniqueness, and capacity failures remain typed.</summary>
    [TestMethod]
    public void ApplyWhenBookmarkMutationConflictsReportsItsExactCatalogConstraint()
    {
        BookmarkEntry existing = BookmarkEditorTestValues.Entry(
            "Repo",
            "C:\\repo",
            null,
            BookmarkShortcutSlot.One);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([], [existing]);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        _ = editor.Apply(BookmarkEditorAction.BeginAddBookmark, catalog, BookmarkEditorTestValues.EmptyDefaults());

        AssertBookmarkSaveProblem(
            editor,
            catalog,
            new BookmarkDraft(
                "repo",
                "C:\\other",
                BookmarkCategoryFilter.Uncategorized,
                BookmarkShortcutSlot.Two),
            BookmarkEditorProblem.DuplicateBookmark);
        AssertBookmarkSaveProblem(
            editor,
            catalog,
            new BookmarkDraft(
                "Other",
                "C:\\other",
                BookmarkCategoryFilter.Uncategorized,
                BookmarkShortcutSlot.One),
            BookmarkEditorProblem.DuplicateShortcut);

        BookmarkCatalog full = BookmarkEditorTestValues.Catalog([], BookmarkEditorTestValues.MaximumBookmarks());
        BookmarkEditorSession capacityEditor = BookmarkEditorTestValues.OpenEditor();
        _ = capacityEditor.Apply(BookmarkEditorAction.BeginAddBookmark, full, BookmarkEditorTestValues.EmptyDefaults());
        AssertBookmarkSaveProblem(
            capacityEditor,
            full,
            new BookmarkDraft(
                "Overflow",
                "C:\\overflow",
                BookmarkCategoryFilter.Uncategorized,
                null),
            BookmarkEditorProblem.BookmarkLimit);
    }

    /// <summary>Proves editing starts from the complete entry and atomically replaces it.</summary>
    [TestMethod]
    public void ApplyWhenEditingCurrentBookmarkPreservesThenReplacesAllMetadata()
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkEntry original = BookmarkEditorTestValues.Entry(
            "Repo",
            "C:\\old",
            null,
            BookmarkShortcutSlot.One);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([work], [original]);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        BookmarkSelection selection = new(original);

        _ = editor.Apply(
            BookmarkEditorAction.BeginEditBookmark(selection),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());
        BookmarkDrafting initial = Assert.IsInstanceOfType<BookmarkDrafting>(editor.Current);
        Assert.AreEqual("Repo", initial.Draft.Name);
        Assert.AreEqual("C:\\old", initial.Draft.Path);
        Assert.AreSame(BookmarkShortcutSlot.One, initial.Draft.Shortcut);
        _ = editor.Apply(
            BookmarkEditorAction.UpdateBookmark(new BookmarkDraft(
                "Project",
                "C:\\new",
                BookmarkCategoryFilter.For(work),
                BookmarkShortcutSlot.Two)),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());

        BookmarkEditorTransition.CatalogChanged changed =
            Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
                editor.Apply(BookmarkEditorAction.Save, catalog, BookmarkEditorTestValues.EmptyDefaults()));

        Assert.HasCount(1, changed.Catalog.Bookmarks);
        Assert.AreEqual("Project", changed.Catalog.Bookmarks[0].Name.Value);
        Assert.AreEqual("C:\\new", changed.Catalog.Bookmarks[0].Path.Value.CanonicalText);
        Assert.AreSame(work, changed.Catalog.Bookmarks[0].Category);
        Assert.AreSame(BookmarkShortcutSlot.Two, changed.Catalog.Bookmarks[0].ShortcutSlot);
    }

    /// <summary>
    /// Proves deleting a bookmark first asks (ADR-0079): the question changes neither the catalog
    /// nor the saved settings, and only the confirmation deletes, exactly once.
    /// </summary>
    [TestMethod]
    public void ApplyWhenDeletingBookmarkAsksBeforeOneCatalogChange()
    {
        BookmarkEntry displayed = BookmarkEditorTestValues.Entry("Repo", "C:\\old", null);
        BookmarkSelection selection = new(displayed);
        BookmarkCatalog initial = BookmarkEditorTestValues.Catalog([], [displayed]);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();

        _ = Assert.IsInstanceOfType<BookmarkEditorTransition.StateChanged>(
            editor.Apply(BookmarkEditorAction.BeginDeleteBookmark(selection), initial, BookmarkEditorTestValues.EmptyDefaults()));
        BookmarkDeleteConfirmation confirmation =
            Assert.IsInstanceOfType<BookmarkDeleteConfirmation>(editor.Current);
        Assert.AreSame(displayed, confirmation.Selection.Entry);
        Assert.HasCount(1, initial.Bookmarks);

        BookmarkEditorTransition.CatalogChanged changed =
            Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
                editor.Apply(BookmarkEditorAction.ConfirmDeleteBookmark, initial, BookmarkEditorTestValues.EmptyDefaults()));
        Assert.IsEmpty(changed.Catalog.Bookmarks);
        _ = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        _ = Assert.IsInstanceOfType<BookmarkEditorTransition.StateChanged>(
            editor.Apply(BookmarkEditorAction.ConfirmDeleteBookmark, changed.Catalog, BookmarkEditorTestValues.EmptyDefaults()));
    }

    /// <summary>Proves cancelling the bookmark deletion question returns to the unchanged browse state.</summary>
    [TestMethod]
    public void ApplyWhenBookmarkDeletionIsCancelledRestoresBrowsingWithoutChange()
    {
        BookmarkEntry displayed = BookmarkEditorTestValues.Entry("Repo", "C:\\old", null);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([], [displayed]);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        _ = editor.Apply(BookmarkEditorAction.Search("rep"), catalog, BookmarkEditorTestValues.EmptyDefaults());
        BookmarksBrowsing before = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        _ = editor.Apply(BookmarkEditorAction.BeginDeleteBookmark(new BookmarkSelection(displayed)), catalog, BookmarkEditorTestValues.EmptyDefaults());

        _ = Assert.IsInstanceOfType<BookmarkEditorTransition.StateChanged>(
            editor.Apply(BookmarkEditorAction.Cancel, catalog, BookmarkEditorTestValues.EmptyDefaults()));

        BookmarksBrowsing after = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        Assert.AreEqual(before.Context, after.Context);
        Assert.IsNull(after.Problem);
        Assert.HasCount(1, catalog.Bookmarks);
    }

    /// <summary>Proves a bookmark replaced before the question or before confirmation is rejected as stale.</summary>
    [TestMethod]
    public void ApplyWhenBookmarkDeletionTargetChangesRejectsItAsStale()
    {
        BookmarkEntry displayed = BookmarkEditorTestValues.Entry("Repo", "C:\\old", null);
        BookmarkSelection selection = new(displayed);
        BookmarkCatalog replaced = BookmarkEditorTestValues.Catalog([], [BookmarkEditorTestValues.Entry("Repo", "C:\\new", null)]);
        BookmarkEditorSession early = BookmarkEditorTestValues.OpenEditor();
        _ = early.Apply(BookmarkEditorAction.BeginDeleteBookmark(selection), replaced, BookmarkEditorTestValues.EmptyDefaults());
        Assert.AreSame(
            BookmarkEditorProblem.StaleSelection,
            Assert.IsInstanceOfType<BookmarksBrowsing>(early.Current).Problem);

        BookmarkEditorSession late = BookmarkEditorTestValues.OpenEditor();
        _ = late.Apply(BookmarkEditorAction.BeginDeleteBookmark(selection), BookmarkEditorTestValues.Catalog([], [displayed]), BookmarkEditorTestValues.EmptyDefaults());
        _ = Assert.IsInstanceOfType<BookmarkDeleteConfirmation>(late.Current);
        _ = Assert.IsInstanceOfType<BookmarkEditorTransition.StateChanged>(
            late.Apply(BookmarkEditorAction.ConfirmDeleteBookmark, replaced, BookmarkEditorTestValues.EmptyDefaults()));
        Assert.AreSame(
            BookmarkEditorProblem.StaleSelection,
            Assert.IsInstanceOfType<BookmarksBrowsing>(late.Current).Problem);
        Assert.HasCount(1, replaced.Bookmarks);
    }

    /// <summary>Proves a categorized entry seeds an edit draft with that exact current category.</summary>
    [TestMethod]
    public void ApplyWhenEditingCategorizedBookmarkPreservesItsCategoryChoice()
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkEntry entry = BookmarkEditorTestValues.Entry("Repo", "C:\\repo", work);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([work], [entry]);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();

        _ = editor.Apply(
            BookmarkEditorAction.BeginEditBookmark(new BookmarkSelection(entry)),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());

        BookmarkUserCategoryFilter category = Assert.IsInstanceOfType<BookmarkUserCategoryFilter>(
            Assert.IsInstanceOfType<BookmarkDrafting>(editor.Current).Draft.Category);
        Assert.AreSame(work, category.Category);
    }

    private static void AssertBookmarkSaveProblem(
        BookmarkEditorSession editor,
        BookmarkCatalog catalog,
        BookmarkDraft draft,
        BookmarkEditorProblem expected)
    {
        _ = editor.Apply(BookmarkEditorAction.UpdateBookmark(draft), catalog, BookmarkEditorTestValues.EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.Save, catalog, BookmarkEditorTestValues.EmptyDefaults());
        BookmarkDrafting rejected = Assert.IsInstanceOfType<BookmarkDrafting>(editor.Current);
        Assert.AreSame(expected, rejected.Problem);
        Assert.AreSame(draft, rejected.Draft);
    }
}
