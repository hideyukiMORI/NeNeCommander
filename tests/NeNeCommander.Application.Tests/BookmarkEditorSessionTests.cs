using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Panes;

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

        editor.FinishNavigationFailed(new PaneReadCancelled(entry.Path.Value));
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

    /// <summary>Proves a current category filter is rebound and an unknown category is rejected.</summary>
    [TestMethod]
    public void ApplyWhenFilteringResolvesOnlyTheCurrentCatalogCategory()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCatalog catalog = Catalog([work], []);
        BookmarkEditorSession editor = OpenEditor();
        BookmarkRegistrationDefaults defaults = EmptyDefaults();

        _ = editor.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.For(Category("work"))),
            catalog,
            defaults);

        BookmarksBrowsing filtered = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        BookmarkUserCategoryFilter current =
            Assert.IsInstanceOfType<BookmarkUserCategoryFilter>(filtered.Context.Filter);
        Assert.AreSame(work, current.Category);

        _ = editor.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.For(Category("Missing"))),
            catalog,
            defaults);

        BookmarksBrowsing rejected = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        Assert.AreSame(BookmarkEditorProblem.InvalidCategory, rejected.Problem);
        Assert.AreSame(current, rejected.Context.Filter);
    }

    /// <summary>Proves selection accepts only a current complete entry and supports explicit clearing.</summary>
    [TestMethod]
    public void ApplyWhenSelectingRequiresCurrentMetadataAndCanClearSelection()
    {
        BookmarkEntry current = Entry("Repo", "C:\\repo", null);
        BookmarkCatalog catalog = Catalog([], [current]);
        BookmarkEditorSession editor = OpenEditor();

        _ = editor.Apply(
            BookmarkEditorAction.Select(new BookmarkSelection(current)),
            catalog,
            EmptyDefaults());
        Assert.AreEqual(
            current,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Context.Selection?.Entry);

        _ = editor.Apply(
            BookmarkEditorAction.Select(null),
            catalog,
            EmptyDefaults());
        Assert.IsNull(Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Context.Selection);

        _ = editor.Apply(
            BookmarkEditorAction.Select(
                new BookmarkSelection(Entry("Repo", "C:\\old", null))),
            catalog,
            EmptyDefaults());
        Assert.AreSame(
            BookmarkEditorProblem.StaleSelection,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Problem);
    }

    /// <summary>Proves a category draft rejects invalid text before accepting one complete addition.</summary>
    [TestMethod]
    public void ApplyWhenAddingCategoryRetainsInvalidTextThenAppendsTheAcceptedName()
    {
        BookmarkEditorSession editor = OpenEditor();
        _ = editor.Apply(BookmarkEditorAction.BeginAddCategory, BookmarkCatalog.Empty, EmptyDefaults());
        _ = editor.Apply(
            BookmarkEditorAction.UpdateCategory(""),
            BookmarkCatalog.Empty,
            EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.Save, BookmarkCatalog.Empty, EmptyDefaults());

        BookmarkCategoryDrafting rejected =
            Assert.IsInstanceOfType<BookmarkCategoryDrafting>(editor.Current);
        Assert.AreEqual(string.Empty, rejected.Name);
        Assert.AreSame(BookmarkEditorProblem.InvalidName, rejected.Problem);

        _ = editor.Apply(
            BookmarkEditorAction.UpdateCategory("Work"),
            BookmarkCatalog.Empty,
            EmptyDefaults());
        BookmarkEditorTransition.CatalogChanged changed =
            Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
                editor.Apply(BookmarkEditorAction.Save, BookmarkCatalog.Empty, EmptyDefaults()));

        Assert.HasCount(1, changed.Catalog.Categories);
        Assert.AreEqual("Work", changed.Catalog.Categories[0].Value);
    }

    /// <summary>Proves category rename updates references and rebinds the active category filter.</summary>
    [TestMethod]
    public void ApplyWhenRenamingCurrentCategoryRebindsFilterAndEveryReference()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCatalog catalog = Catalog([work], [Entry("Repo", "C:\\repo", work)]);
        BookmarkEditorSession editor = OpenEditor();
        _ = editor.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.For(work)),
            catalog,
            EmptyDefaults());
        BookmarkCategorySelection selection = RequireCategory(catalog, work);
        _ = editor.Apply(
            BookmarkEditorAction.BeginRenameCategory(selection),
            catalog,
            EmptyDefaults());
        _ = editor.Apply(
            BookmarkEditorAction.UpdateCategory("Projects"),
            catalog,
            EmptyDefaults());

        BookmarkEditorTransition.CatalogChanged changed =
            Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
                editor.Apply(BookmarkEditorAction.Save, catalog, EmptyDefaults()));

        Assert.AreEqual("Projects", changed.Catalog.Categories[0].Value);
        Assert.AreEqual("Projects", changed.Catalog.Bookmarks[0].Category?.Value);
        BookmarkUserCategoryFilter filter = Assert.IsInstanceOfType<BookmarkUserCategoryFilter>(
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Context.Filter);
        Assert.AreEqual("Projects", filter.Category.Value);
    }

    /// <summary>Proves duplicate and capacity category failures keep their respective drafts open.</summary>
    [TestMethod]
    public void ApplyWhenCategoryMutationConflictsReportsDuplicateOrCapacityWithoutChange()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCategoryName other = Category("Other");
        BookmarkCatalog catalog = Catalog([work, other], []);
        BookmarkEditorSession renameEditor = OpenEditor();
        _ = renameEditor.Apply(
            BookmarkEditorAction.BeginRenameCategory(RequireCategory(catalog, work)),
            catalog,
            EmptyDefaults());
        _ = renameEditor.Apply(
            BookmarkEditorAction.UpdateCategory("other"),
            catalog,
            EmptyDefaults());
        _ = renameEditor.Apply(BookmarkEditorAction.Save, catalog, EmptyDefaults());
        Assert.AreSame(
            BookmarkEditorProblem.DuplicateCategory,
            Assert.IsInstanceOfType<BookmarkCategoryDrafting>(renameEditor.Current).Problem);

        BookmarkCatalog full = Catalog(MaximumCategories(), []);
        BookmarkEditorSession addEditor = OpenEditor();
        _ = addEditor.Apply(BookmarkEditorAction.BeginAddCategory, full, EmptyDefaults());
        _ = addEditor.Apply(
            BookmarkEditorAction.UpdateCategory("Overflow"),
            full,
            EmptyDefaults());
        _ = addEditor.Apply(BookmarkEditorAction.Save, full, EmptyDefaults());
        Assert.AreSame(
            BookmarkEditorProblem.CategoryLimit,
            Assert.IsInstanceOfType<BookmarkCategoryDrafting>(addEditor.Current).Problem);
    }

    /// <summary>Proves bookmark Save rejects each invalid complete draft before catalog mutation.</summary>
    [TestMethod]
    public void ApplyWhenBookmarkDraftIsInvalidReportsNamePathOrCategoryPrecisely()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCatalog catalog = Catalog([work], []);
        BookmarkEditorSession editor = OpenEditor();
        _ = editor.Apply(BookmarkEditorAction.BeginAddBookmark, catalog, EmptyDefaults());

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
                BookmarkCategoryFilter.For(Category("Missing")),
                null),
            BookmarkEditorProblem.InvalidCategory);
    }

    /// <summary>Proves bookmark uniqueness, shortcut uniqueness, and capacity failures remain typed.</summary>
    [TestMethod]
    public void ApplyWhenBookmarkMutationConflictsReportsItsExactCatalogConstraint()
    {
        BookmarkEntry existing = Entry(
            "Repo",
            "C:\\repo",
            null,
            BookmarkShortcutSlot.One);
        BookmarkCatalog catalog = Catalog([], [existing]);
        BookmarkEditorSession editor = OpenEditor();
        _ = editor.Apply(BookmarkEditorAction.BeginAddBookmark, catalog, EmptyDefaults());

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

        BookmarkCatalog full = Catalog([], MaximumBookmarks());
        BookmarkEditorSession capacityEditor = OpenEditor();
        _ = capacityEditor.Apply(BookmarkEditorAction.BeginAddBookmark, full, EmptyDefaults());
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
        BookmarkCategoryName work = Category("Work");
        BookmarkEntry original = Entry(
            "Repo",
            "C:\\old",
            null,
            BookmarkShortcutSlot.One);
        BookmarkCatalog catalog = Catalog([work], [original]);
        BookmarkEditorSession editor = OpenEditor();
        BookmarkSelection selection = new(original);

        _ = editor.Apply(
            BookmarkEditorAction.BeginEditBookmark(selection),
            catalog,
            EmptyDefaults());
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
            EmptyDefaults());

        BookmarkEditorTransition.CatalogChanged changed =
            Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
                editor.Apply(BookmarkEditorAction.Save, catalog, EmptyDefaults()));

        Assert.HasCount(1, changed.Catalog.Bookmarks);
        Assert.AreEqual("Project", changed.Catalog.Bookmarks[0].Name.Value);
        Assert.AreEqual("C:\\new", changed.Catalog.Bookmarks[0].Path.Value.CanonicalText);
        Assert.AreSame(work, changed.Catalog.Bookmarks[0].Category);
        Assert.AreSame(BookmarkShortcutSlot.Two, changed.Catalog.Bookmarks[0].ShortcutSlot);
    }

    /// <summary>Proves deleting a current bookmark succeeds while a replaced selection is stale.</summary>
    [TestMethod]
    public void ApplyWhenDeletingBookmarkRequiresTheDisplayedCompleteEntry()
    {
        BookmarkEntry displayed = Entry("Repo", "C:\\old", null);
        BookmarkSelection selection = new(displayed);
        BookmarkCatalog initial = Catalog([], [displayed]);
        BookmarkEditorSession success = OpenEditor();

        BookmarkEditorTransition.CatalogChanged changed =
            Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
                success.Apply(
                    BookmarkEditorAction.DeleteBookmark(selection),
                    initial,
                    EmptyDefaults()));
        Assert.IsEmpty(changed.Catalog.Bookmarks);

        BookmarkCatalog replaced = Catalog([], [Entry("Repo", "C:\\new", null)]);
        BookmarkEditorSession stale = OpenEditor();
        _ = stale.Apply(
            BookmarkEditorAction.DeleteBookmark(selection),
            replaced,
            EmptyDefaults());
        Assert.AreSame(
            BookmarkEditorProblem.StaleSelection,
            Assert.IsInstanceOfType<BookmarksBrowsing>(stale.Current).Problem);
    }

    /// <summary>Proves successful category deletion moves entries and the active filter together.</summary>
    [TestMethod]
    public void ApplyWhenDeletingCurrentCategoryMovesEntriesToUncategorizedAtomically()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCatalog catalog = Catalog([work], [Entry("Repo", "C:\\repo", work)]);
        BookmarkEditorSession editor = OpenEditor();
        _ = editor.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.For(work)),
            catalog,
            EmptyDefaults());
        BookmarkCategorySelection selection = RequireCategory(catalog, work);
        _ = editor.Apply(
            BookmarkEditorAction.BeginDeleteCategory(selection),
            catalog,
            EmptyDefaults());

        BookmarkEditorTransition.CatalogChanged changed =
            Assert.IsInstanceOfType<BookmarkEditorTransition.CatalogChanged>(
                editor.Apply(
                    BookmarkEditorAction.ConfirmDeleteCategory,
                    catalog,
                    EmptyDefaults()));

        Assert.IsEmpty(changed.Catalog.Categories);
        Assert.IsNull(changed.Catalog.Bookmarks[0].Category);
        _ = Assert.IsInstanceOfType<BookmarkUncategorizedCategoryFilter>(
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Context.Filter);
    }

    /// <summary>Proves category confirmation is invalidated by catalog changes before confirmation.</summary>
    [TestMethod]
    public void ApplyWhenConfirmedCategorySelectionBecameStaleRejectsTheWholeDeletion()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkEntry displayed = Entry("Repo", "C:\\old", work);
        BookmarkCatalog initial = Catalog([work], [displayed]);
        BookmarkEditorSession editor = OpenEditor();
        _ = editor.Apply(
            BookmarkEditorAction.BeginDeleteCategory(RequireCategory(initial, work)),
            initial,
            EmptyDefaults());
        BookmarkCatalog replaced = Catalog([work], [Entry("Repo", "C:\\new", work)]);

        _ = editor.Apply(
            BookmarkEditorAction.ConfirmDeleteCategory,
            replaced,
            EmptyDefaults());

        Assert.AreSame(
            BookmarkEditorProblem.StaleSelection,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Problem);
        Assert.AreEqual("Work", replaced.Categories[0].Value);
        Assert.AreEqual("C:\\new", replaced.Bookmarks[0].Path.Value.CanonicalText);
    }

    /// <summary>Proves navigation rejects closed or stale selections and closes only after success.</summary>
    [TestMethod]
    public void NavigationWhenStartingRequiresOpenCurrentSelectionAndSuccessClosesEditor()
    {
        BookmarkEntry current = Entry("Repo", "C:\\repo", null);
        BookmarkCatalog catalog = Catalog([], [current]);
        BookmarkEditorSession editor = new();

        Assert.IsFalse(editor.BeginNavigation(new BookmarkSelection(current), catalog));
        Assert.AreSame(BookmarksEditorState.Closed, editor.Current);
        editor.Open();
        Assert.IsFalse(editor.BeginNavigation(
            new BookmarkSelection(Entry("Repo", "C:\\old", null)),
            catalog));
        Assert.AreSame(
            BookmarkEditorProblem.StaleSelection,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Problem);
        Assert.IsTrue(editor.BeginNavigation(new BookmarkSelection(current), catalog));

        editor.FinishNavigationSucceeded();

        Assert.AreSame(BookmarksEditorState.Closed, editor.Current);
        editor.FinishNavigationSucceeded();
        editor.FinishNavigationFailed(new PaneReadCancelled(current.Path.Value));
        Assert.AreSame(BookmarksEditorState.Closed, editor.Current);
    }

    /// <summary>Proves Retry reuses the immutable selection retained by a failed navigation.</summary>
    [TestMethod]
    public void NavigationWhenRetryingFailureReturnsToPendingWithTheSameSelection()
    {
        BookmarkEntry entry = Entry("Repo", "C:\\repo", null);
        BookmarkCatalog catalog = Catalog([], [entry]);
        BookmarkSelection selection = new(entry);
        BookmarkEditorSession editor = OpenEditor();
        Assert.IsTrue(editor.BeginNavigation(selection, catalog));
        editor.FinishNavigationFailed(new PaneReadCancelled(entry.Path.Value));

        Assert.IsTrue(editor.BeginNavigation(selection, catalog));

        BookmarkNavigationPending pending =
            Assert.IsInstanceOfType<BookmarkNavigationPending>(editor.Current);
        Assert.AreSame(selection, pending.Selection);
    }

    /// <summary>Proves a failed navigation accepts only its retained complete selection.</summary>
    [TestMethod]
    public void NavigationWhenFailureReceivesAnotherCurrentSelectionKeepsTheFailureUnchanged()
    {
        BookmarkEntry retained = Entry("Retained", "C:\\retained", null);
        BookmarkEntry other = Entry("Other", "C:\\other", null);
        BookmarkCatalog catalog = Catalog([], [retained, other]);
        BookmarkEditorSession editor = OpenEditor();
        Assert.IsTrue(editor.BeginNavigation(new BookmarkSelection(retained), catalog));
        editor.FinishNavigationFailed(new PaneReadCancelled(retained.Path.Value));
        BookmarkNavigationFailed failed =
            Assert.IsInstanceOfType<BookmarkNavigationFailed>(editor.Current);

        Assert.IsFalse(editor.BeginNavigation(new BookmarkSelection(other), catalog));

        Assert.AreSame(failed, editor.Current);
        Assert.IsTrue(editor.BeginNavigation(new BookmarkSelection(retained), catalog));
        BookmarkNavigationPending retry =
            Assert.IsInstanceOfType<BookmarkNavigationPending>(editor.Current);
        Assert.AreEqual("Retained", retry.Selection.Entry.Name.Value);
    }

    /// <summary>Proves a failure state cannot retain an idle or in-progress pane activity.</summary>
    [TestMethod]
    public void NavigationFailureWhenReasonIsNotACompletedRejectionRejectsTheReason()
    {
        BookmarkEntry entry = Entry("Repo", "C:\\repo", null);
        BookmarkBrowseContext context = new(string.Empty, BookmarkCategoryFilter.All, null);

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            new BookmarkNavigationFailed(context, new BookmarkSelection(entry), PaneActivity.Idle));
    }

    /// <summary>Proves every closed catalog rejection maps to its user-correctable editor problem.</summary>
    [TestMethod]
    public void ProblemForMapsEveryCatalogFailureToItsClosedEditorProblem()
    {
        (BookmarkCatalogFailureKind Failure, BookmarkEditorProblem Problem)[] mappings =
        [
            (BookmarkCatalogFailureKind.TooManyCategories, BookmarkEditorProblem.CategoryLimit),
            (BookmarkCatalogFailureKind.TooManyBookmarks, BookmarkEditorProblem.BookmarkLimit),
            (BookmarkCatalogFailureKind.DuplicateCategory, BookmarkEditorProblem.DuplicateCategory),
            (BookmarkCatalogFailureKind.InvalidCategoryReference, BookmarkEditorProblem.InvalidCategory),
            (BookmarkCatalogFailureKind.DuplicateBookmark, BookmarkEditorProblem.DuplicateBookmark),
            (BookmarkCatalogFailureKind.DuplicateShortcutSlot, BookmarkEditorProblem.DuplicateShortcut),
            (BookmarkCatalogFailureKind.InvalidElement, BookmarkEditorProblem.InvalidCategory),
            (BookmarkCatalogFailureKind.StaleSelection, BookmarkEditorProblem.StaleSelection),
        ];

        foreach ((BookmarkCatalogFailureKind failure, BookmarkEditorProblem problem) in mappings)
        {
            Assert.AreSame(problem, BookmarkEditorMutator.ProblemFor(failure));
        }
    }

    /// <summary>Proves closed and pending states ignore editor actions without changing ownership.</summary>
    [TestMethod]
    public void ApplyWhenStateDoesNotAcceptActionLeavesTheClosedStateUnchanged()
    {
        BookmarkEntry entry = Entry("Repo", "C:\\repo", null);
        BookmarkCatalog catalog = Catalog([], [entry]);
        BookmarkEditorSession editor = new();

        _ = editor.Apply(BookmarkEditorAction.Search("ignored"), catalog, EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.BeginAddBookmark, catalog, EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.BeginAddCategory, catalog, EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.Save, catalog, EmptyDefaults());
        Assert.AreSame(BookmarksEditorState.Closed, editor.Current);

        editor.Open();
        Assert.IsTrue(editor.BeginNavigation(new BookmarkSelection(entry), catalog));
        _ = editor.Apply(
            BookmarkEditorAction.UpdateBookmark(new BookmarkDraft(
                "Changed",
                "C:\\changed",
                BookmarkCategoryFilter.Uncategorized,
                null)),
            catalog,
            EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.BeginAddCategory, catalog, EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.Save, catalog, EmptyDefaults());
        _ = Assert.IsInstanceOfType<BookmarkNavigationPending>(editor.Current);
    }

    /// <summary>Proves each nested editor Cancel returns to its retained browse context.</summary>
    [TestMethod]
    public void ApplyWhenCancellingNestedEditorsRestoresEachRetainedBrowseContext()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkEntry entry = Entry("Repo", "C:\\repo", work);
        BookmarkCatalog catalog = Catalog([work], [entry]);
        BookmarkRegistrationDefaults defaults = EmptyDefaults();

        BookmarkEditorSession bookmark = OpenEditor();
        _ = bookmark.Apply(BookmarkEditorAction.Search("book"), catalog, defaults);
        _ = bookmark.Apply(BookmarkEditorAction.BeginAddBookmark, catalog, defaults);
        _ = bookmark.Apply(BookmarkEditorAction.Cancel, catalog, defaults);
        Assert.AreEqual(
            "book",
            Assert.IsInstanceOfType<BookmarksBrowsing>(bookmark.Current).Context.SearchText);

        BookmarkEditorSession category = OpenEditor();
        _ = category.Apply(BookmarkEditorAction.Search("category"), catalog, defaults);
        _ = category.Apply(BookmarkEditorAction.BeginAddCategory, catalog, defaults);
        _ = category.Apply(BookmarkEditorAction.Cancel, catalog, defaults);
        Assert.AreEqual(
            "category",
            Assert.IsInstanceOfType<BookmarksBrowsing>(category.Current).Context.SearchText);

        BookmarkEditorSession deletion = OpenEditor();
        _ = deletion.Apply(BookmarkEditorAction.Search("delete"), catalog, defaults);
        _ = deletion.Apply(
            BookmarkEditorAction.BeginDeleteCategory(RequireCategory(catalog, work)),
            catalog,
            defaults);
        _ = deletion.Apply(BookmarkEditorAction.Cancel, catalog, defaults);
        Assert.AreEqual(
            "delete",
            Assert.IsInstanceOfType<BookmarksBrowsing>(deletion.Current).Context.SearchText);
    }

    /// <summary>Proves registration defaults retain a selected category but never retain All.</summary>
    [TestMethod]
    public void ApplyWhenBeginningBookmarkUsesOnlyAConcreteCurrentCategoryFilter()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCatalog catalog = Catalog([work], []);
        BookmarkEditorSession categorized = OpenEditor();
        _ = categorized.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.For(work)),
            catalog,
            EmptyDefaults());

        _ = categorized.Apply(BookmarkEditorAction.BeginAddBookmark, catalog, EmptyDefaults());

        BookmarkUserCategoryFilter selected = Assert.IsInstanceOfType<BookmarkUserCategoryFilter>(
            Assert.IsInstanceOfType<BookmarkDrafting>(categorized.Current).Draft.Category);
        Assert.AreSame(work, selected.Category);

        BookmarkEditorSession uncategorized = OpenEditor();
        _ = uncategorized.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.Uncategorized),
            catalog,
            EmptyDefaults());
        _ = uncategorized.Apply(BookmarkEditorAction.BeginAddBookmark, catalog, EmptyDefaults());
        _ = Assert.IsInstanceOfType<BookmarkUncategorizedCategoryFilter>(
            Assert.IsInstanceOfType<BookmarkDrafting>(uncategorized.Current).Draft.Category);
    }

    /// <summary>Proves built-in All and Uncategorized filters remain the exact selected values.</summary>
    [TestMethod]
    public void ApplyWhenFilteringWithBuiltInChoicesPreservesTheirClosedIdentity()
    {
        BookmarkEditorSession editor = OpenEditor();

        _ = editor.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.Uncategorized),
            BookmarkCatalog.Empty,
            EmptyDefaults());
        Assert.AreSame(
            BookmarkCategoryFilter.Uncategorized,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Context.Filter);

        _ = editor.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.All),
            BookmarkCatalog.Empty,
            EmptyDefaults());
        Assert.AreSame(
            BookmarkCategoryFilter.All,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Context.Filter);
    }

    /// <summary>Proves a categorized entry seeds an edit draft with that exact current category.</summary>
    [TestMethod]
    public void ApplyWhenEditingCategorizedBookmarkPreservesItsCategoryChoice()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkEntry entry = Entry("Repo", "C:\\repo", work);
        BookmarkCatalog catalog = Catalog([work], [entry]);
        BookmarkEditorSession editor = OpenEditor();

        _ = editor.Apply(
            BookmarkEditorAction.BeginEditBookmark(new BookmarkSelection(entry)),
            catalog,
            EmptyDefaults());

        BookmarkUserCategoryFilter category = Assert.IsInstanceOfType<BookmarkUserCategoryFilter>(
            Assert.IsInstanceOfType<BookmarkDrafting>(editor.Current).Draft.Category);
        Assert.AreSame(work, category.Category);
    }

    /// <summary>Proves rename begins only from the unchanged complete category selection.</summary>
    [TestMethod]
    public void ApplyWhenRenameCategorySelectionIsStaleRemainsBrowsing()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCatalog displayed = Catalog([work], [Entry("Repo", "C:\\old", work)]);
        BookmarkCategorySelection selection = RequireCategory(displayed, work);
        BookmarkCatalog current = Catalog([work], [Entry("Repo", "C:\\new", work)]);
        BookmarkEditorSession editor = OpenEditor();

        _ = editor.Apply(
            BookmarkEditorAction.BeginRenameCategory(selection),
            current,
            EmptyDefaults());

        Assert.AreSame(
            BookmarkEditorProblem.StaleSelection,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Problem);
    }

    /// <summary>Proves category deletion confirmation opens only for unchanged category metadata.</summary>
    [TestMethod]
    public void ApplyWhenDeleteCategorySelectionIsStaleDoesNotOpenConfirmation()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCatalog displayed = Catalog([work], [Entry("Repo", "C:\\old", work)]);
        BookmarkCategorySelection selection = RequireCategory(displayed, work);
        BookmarkCatalog current = Catalog([work], [Entry("Repo", "C:\\new", work)]);
        BookmarkEditorSession editor = OpenEditor();

        _ = editor.Apply(
            BookmarkEditorAction.BeginDeleteCategory(selection),
            current,
            EmptyDefaults());

        Assert.AreSame(
            BookmarkEditorProblem.StaleSelection,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Problem);
    }

    /// <summary>Proves delete and confirmation actions outside their owning state are ignored.</summary>
    [TestMethod]
    public void ApplyWhenDeleteActionsHaveNoOwningStateLeaveClosedStateUnchanged()
    {
        BookmarkEntry entry = Entry("Repo", "C:\\repo", null);
        BookmarkCatalog catalog = Catalog([], [entry]);
        BookmarkEditorSession editor = new();

        _ = editor.Apply(
            BookmarkEditorAction.DeleteBookmark(new BookmarkSelection(entry)),
            catalog,
            EmptyDefaults());
        _ = editor.Apply(
            BookmarkEditorAction.ConfirmDeleteCategory,
            catalog,
            EmptyDefaults());

        Assert.AreSame(BookmarksEditorState.Closed, editor.Current);
        Assert.HasCount(1, catalog.Bookmarks);
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

    private static BookmarkEntry Entry(
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

    private static BookmarkCatalog Catalog(
        BookmarkCategoryName[] categories,
        BookmarkEntry[] bookmarks)
    {
        return Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create(categories, bookmarks)).Catalog;
    }

    private static BookmarkRegistrationDefaults EmptyDefaults()
    {
        return new BookmarkRegistrationDefaults(string.Empty, string.Empty);
    }

    private static BookmarkCategorySelection RequireCategory(
        BookmarkCatalog catalog,
        BookmarkCategoryName category)
    {
        return catalog.Select(category) ??
            throw new InvalidOperationException("The category fixture must be selectable.");
    }

    private static void AssertBookmarkSaveProblem(
        BookmarkEditorSession editor,
        BookmarkCatalog catalog,
        BookmarkDraft draft,
        BookmarkEditorProblem expected)
    {
        _ = editor.Apply(BookmarkEditorAction.UpdateBookmark(draft), catalog, EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.Save, catalog, EmptyDefaults());
        BookmarkDrafting rejected = Assert.IsInstanceOfType<BookmarkDrafting>(editor.Current);
        Assert.AreSame(expected, rejected.Problem);
        Assert.AreSame(draft, rejected.Draft);
    }

    private static BookmarkCategoryName[] MaximumCategories()
    {
        BookmarkCategoryName[] categories = new BookmarkCategoryName[BookmarkCatalog.MaximumCategoryCount];
        for (int index = 0; index < categories.Length; index++)
        {
            categories[index] = Category($"Category {index}");
        }
        return categories;
    }

    private static BookmarkEntry[] MaximumBookmarks()
    {
        BookmarkEntry[] bookmarks = new BookmarkEntry[BookmarkCatalog.MaximumBookmarkCount];
        for (int index = 0; index < bookmarks.Length; index++)
        {
            bookmarks[index] = Entry($"Bookmark {index}", $"C:\\path{index}", null);
        }
        return bookmarks;
    }
}
