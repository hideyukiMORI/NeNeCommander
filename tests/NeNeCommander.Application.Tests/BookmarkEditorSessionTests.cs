using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves bookmark browsing and closed actions retain their current catalog context.</summary>
[TestClass]
public sealed class BookmarkEditorSessionTests
{
    /// <summary>Proves an unregistered action is a defect and cannot cancel an active draft.</summary>
    [TestMethod]
    public void ApplyWhenActionVariantIsUnsupportedThrowsWithoutChangingDraft()
    {
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        _ = editor.Apply(BookmarkEditorAction.BeginAddBookmark, BookmarkCatalog.Empty, BookmarkEditorTestValues.EmptyDefaults());
        BookmarkDrafting before = Assert.IsInstanceOfType<BookmarkDrafting>(editor.Current);

        _ = Assert.ThrowsExactly<InvalidOperationException>(
            () => editor.Apply(new UnsupportedBookmarkEditorAction(), BookmarkCatalog.Empty, BookmarkEditorTestValues.EmptyDefaults()));

        Assert.AreSame(before, editor.Current);
    }

    /// <summary>Proves an unregistered filter is a defect and preserves the complete browse state.</summary>
    [TestMethod]
    public void ApplyWhenCategoryFilterVariantIsUnsupportedThrowsWithoutChangingBrowse()
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([work], []);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        _ = editor.Apply(BookmarkEditorAction.Search("retained"), catalog, BookmarkEditorTestValues.EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.Filter(BookmarkCategoryFilter.For(work)), catalog, BookmarkEditorTestValues.EmptyDefaults());
        BookmarksBrowsing before = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);

        _ = Assert.ThrowsExactly<InvalidOperationException>(
            () => editor.Apply(BookmarkEditorAction.Filter(new UnsupportedBookmarkCategoryFilter()), catalog, BookmarkEditorTestValues.EmptyDefaults()));

        Assert.AreSame(before, editor.Current);
    }

    /// <summary>Proves stale edit actions report a closed problem without replacing the current entry.</summary>
    [TestMethod]
    public void ApplyWhenDisplayedEntryWasReplacedKeepsBrowsingAndReportsStaleSelection()
    {
        BookmarkEntry displayed = BookmarkEditorTestValues.Entry("Repo", "C:\\old", null);
        BookmarkSelection selection = new(displayed);
        BookmarkCatalog current = BookmarkEditorTestValues.Catalog([], [BookmarkEditorTestValues.Entry("Repo", "C:\\new", null)]);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();

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
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([work], []);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
        BookmarkRegistrationDefaults defaults = BookmarkEditorTestValues.EmptyDefaults();

        _ = editor.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.For(BookmarkEditorTestValues.Category("work"))),
            catalog,
            defaults);

        BookmarksBrowsing filtered = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        BookmarkUserCategoryFilter current =
            Assert.IsInstanceOfType<BookmarkUserCategoryFilter>(filtered.Context.Filter);
        Assert.AreSame(work, current.Category);

        _ = editor.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.For(BookmarkEditorTestValues.Category("Missing"))),
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
        BookmarkEntry current = BookmarkEditorTestValues.Entry("Repo", "C:\\repo", null);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([], [current]);
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();

        _ = editor.Apply(
            BookmarkEditorAction.Select(new BookmarkSelection(current)),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());
        Assert.AreEqual(
            current,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Context.Selection?.Entry);

        _ = editor.Apply(
            BookmarkEditorAction.Select(null),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());
        Assert.IsNull(Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Context.Selection);

        _ = editor.Apply(
            BookmarkEditorAction.Select(
                new BookmarkSelection(BookmarkEditorTestValues.Entry("Repo", "C:\\old", null))),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());
        Assert.AreSame(
            BookmarkEditorProblem.StaleSelection,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Problem);
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
        BookmarkEntry entry = BookmarkEditorTestValues.Entry("Repo", "C:\\repo", null);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([], [entry]);
        BookmarkEditorSession editor = new();

        _ = editor.Apply(BookmarkEditorAction.Search("ignored"), catalog, BookmarkEditorTestValues.EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.BeginAddBookmark, catalog, BookmarkEditorTestValues.EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.BeginAddCategory, catalog, BookmarkEditorTestValues.EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.Save, catalog, BookmarkEditorTestValues.EmptyDefaults());
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
            BookmarkEditorTestValues.EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.BeginAddCategory, catalog, BookmarkEditorTestValues.EmptyDefaults());
        _ = editor.Apply(BookmarkEditorAction.Save, catalog, BookmarkEditorTestValues.EmptyDefaults());
        _ = Assert.IsInstanceOfType<BookmarkNavigationPending>(editor.Current);
    }

    /// <summary>Proves each nested editor Cancel returns to its retained browse context.</summary>
    [TestMethod]
    public void ApplyWhenCancellingNestedEditorsRestoresEachRetainedBrowseContext()
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkEntry entry = BookmarkEditorTestValues.Entry("Repo", "C:\\repo", work);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([work], [entry]);
        BookmarkRegistrationDefaults defaults = BookmarkEditorTestValues.EmptyDefaults();
        BookmarkEditorAction[] actions = [
            BookmarkEditorAction.BeginAddBookmark,
            BookmarkEditorAction.BeginAddCategory,
            BookmarkEditorAction.BeginDeleteCategory(BookmarkEditorTestValues.RequireCategory(catalog, work)),
        ];
        foreach (BookmarkEditorAction action in actions)
        {
            BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();
            _ = editor.Apply(BookmarkEditorAction.Search("retained"), catalog, defaults);
            _ = editor.Apply(BookmarkEditorAction.Filter(BookmarkCategoryFilter.For(work)), catalog, defaults);
            _ = editor.Apply(BookmarkEditorAction.Select(new BookmarkSelection(entry)), catalog, defaults);
            BookmarkBrowseContext before = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Context;

            _ = editor.Apply(action, catalog, defaults);
            _ = Assert.IsInstanceOfType<BookmarkEditorTransition.StateChanged>(
                editor.Apply(BookmarkEditorAction.Cancel, catalog, defaults));

            Assert.AreSame(before, Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Context);
        }
    }

    /// <summary>Proves registration defaults retain a selected category but never retain All.</summary>
    [TestMethod]
    public void ApplyWhenBeginningBookmarkUsesOnlyAConcreteCurrentCategoryFilter()
    {
        BookmarkCategoryName work = BookmarkEditorTestValues.Category("Work");
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([work], []);
        BookmarkEditorSession categorized = BookmarkEditorTestValues.OpenEditor();
        _ = categorized.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.For(work)),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());

        _ = categorized.Apply(BookmarkEditorAction.BeginAddBookmark, catalog, BookmarkEditorTestValues.EmptyDefaults());

        BookmarkUserCategoryFilter selected = Assert.IsInstanceOfType<BookmarkUserCategoryFilter>(
            Assert.IsInstanceOfType<BookmarkDrafting>(categorized.Current).Draft.Category);
        Assert.AreSame(work, selected.Category);

        BookmarkEditorSession uncategorized = BookmarkEditorTestValues.OpenEditor();
        _ = uncategorized.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.Uncategorized),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());
        _ = uncategorized.Apply(BookmarkEditorAction.BeginAddBookmark, catalog, BookmarkEditorTestValues.EmptyDefaults());
        _ = Assert.IsInstanceOfType<BookmarkUncategorizedCategoryFilter>(
            Assert.IsInstanceOfType<BookmarkDrafting>(uncategorized.Current).Draft.Category);
    }

    /// <summary>Proves built-in All and Uncategorized filters remain the exact selected values.</summary>
    [TestMethod]
    public void ApplyWhenFilteringWithBuiltInChoicesPreservesTheirClosedIdentity()
    {
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();

        _ = editor.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.Uncategorized),
            BookmarkCatalog.Empty,
            BookmarkEditorTestValues.EmptyDefaults());
        Assert.AreSame(
            BookmarkCategoryFilter.Uncategorized,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Context.Filter);

        _ = editor.Apply(
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.All),
            BookmarkCatalog.Empty,
            BookmarkEditorTestValues.EmptyDefaults());
        Assert.AreSame(
            BookmarkCategoryFilter.All,
            Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current).Context.Filter);
    }

    /// <summary>Proves delete and confirmation actions outside their owning state are ignored.</summary>
    [TestMethod]
    public void ApplyWhenDeleteActionsHaveNoOwningStateLeaveClosedStateUnchanged()
    {
        BookmarkEntry entry = BookmarkEditorTestValues.Entry("Repo", "C:\\repo", null);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([], [entry]);
        BookmarkEditorSession editor = new();

        _ = editor.Apply(
            BookmarkEditorAction.BeginDeleteBookmark(new BookmarkSelection(entry)),
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());
        _ = editor.Apply(
            BookmarkEditorAction.ConfirmDeleteBookmark,
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());
        _ = editor.Apply(
            BookmarkEditorAction.ConfirmDeleteCategory,
            catalog,
            BookmarkEditorTestValues.EmptyDefaults());

        Assert.AreSame(BookmarksEditorState.Closed, editor.Current);
        Assert.HasCount(1, catalog.Bookmarks);
    }

    /// <summary>Proves opening the manager and a category draft start from empty declared text.</summary>
    [TestMethod]
    public void OpenAndBeginAddCategoryStartFromEmptyDeclaredText()
    {
        BookmarkEditorSession editor = BookmarkEditorTestValues.OpenEditor();

        BookmarksBrowsing opened = Assert.IsInstanceOfType<BookmarksBrowsing>(editor.Current);
        _ = editor.Apply(
            BookmarkEditorAction.BeginAddCategory,
            BookmarkCatalog.Empty,
            BookmarkEditorTestValues.EmptyDefaults());
        BookmarkCategoryDrafting drafting =
            Assert.IsInstanceOfType<BookmarkCategoryDrafting>(editor.Current);

        Assert.AreEqual(string.Empty, opened.Context.SearchText);
        Assert.AreSame(BookmarkCategoryFilter.All, opened.Context.Filter);
        Assert.IsNull(opened.Context.Selection);
        Assert.IsNull(opened.Problem);
        Assert.AreEqual(string.Empty, drafting.Name);
        Assert.IsNull(drafting.Original);
        Assert.IsNull(drafting.Problem);
    }
}
