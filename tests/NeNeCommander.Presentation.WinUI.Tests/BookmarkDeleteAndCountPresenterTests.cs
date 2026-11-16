using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Presentation.WinUI.Bookmarks;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>
/// Proves the bookmark deletion question and the category counts of the manager (ADR-0079): the
/// question replaces browsing, starts on Cancel and keeps the shared modal context, and each
/// category counts its bookmarks regardless of the search text.
/// </summary>
[TestClass]
public sealed class BookmarkDeleteAndCountPresenterTests
{
    private static readonly int[] ExpectedCounts = [3, 1, 2, 0];

    /// <summary>Proves the deletion question shows its target, starts on Cancel and leaves browsing.</summary>
    [TestMethod]
    public void PresentWhenBookmarkDeletionAwaitsConfirmationShowsTheTargetAndStartsOnCancel()
    {
        BookmarkEntry entry = BookmarkPresenterTestContext.Entry("Repo", "C:\\repo", null, null);
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([], [entry]);
        BookmarksEditorState state = BookmarkPresenterTestContext.Construct<BookmarkDeleteConfirmation>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkSelection)],
            [new BookmarkBrowseContext("re", BookmarkCategoryFilter.All, null), new BookmarkSelection(entry)]);

        BookmarkManagerPresentation presentation = BookmarkPresenterTestContext.Present(catalog, state);

        Assert.IsTrue(presentation.Details.IsBookmarkDeleteConfirmation);
        Assert.IsFalse(presentation.Details.IsBrowsing);
        Assert.IsFalse(presentation.Details.IsCategoryDeleteConfirmation);
        Assert.AreSame(entry, presentation.Details.BookmarkDeleteSelection?.Entry);
        Assert.AreSame(BookmarkManagerFocusTarget.CancelBookmarkDelete, presentation.Details.InitialFocus);
        Assert.AreEqual("BookmarkStatusConfirmBookmarkDelete", presentation.Details.StatusResourceKey);
        Assert.AreEqual("re", presentation.Browse.SearchText);
        Assert.IsNull(BookmarkManagerKeyboard.ContextFor(presentation.Details, BookmarkManagerFocusTarget.BookmarkList));
    }

    /// <summary>Proves browsing has no deletion target.</summary>
    [TestMethod]
    public void PresentWhenBrowsingHasNoDeletionTarget()
    {
        BookmarkManagerPresentation presentation = BookmarkPresenterTestContext.Present(
            BookmarkCatalog.Empty,
            BookmarkPresenterTestContext.Browsing(string.Empty, BookmarkCategoryFilter.All));

        Assert.IsFalse(presentation.Details.IsBookmarkDeleteConfirmation);
        Assert.IsNull(presentation.Details.BookmarkDeleteSelection);
    }

    /// <summary>Proves All counts every bookmark, Uncategorized its own and a category its entries, ignoring search.</summary>
    [TestMethod]
    public void PresentWhenSearchingCountsEveryCategoryRegardlessOfTheSearch()
    {
        BookmarkCategoryName work = BookmarkPresenterTestContext.Category("Work");
        BookmarkCategoryName home = BookmarkPresenterTestContext.Category("Home");
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog(
            [work, home],
            [
                BookmarkPresenterTestContext.Entry("Repo", "C:\\repo", work, null),
                BookmarkPresenterTestContext.Entry("Docs", "C:\\docs", work, null),
                BookmarkPresenterTestContext.Entry("Notes", "C:\\notes", null, null),
            ]);

        BookmarkManagerPresentation presentation = BookmarkPresenterTestContext.Present(
            catalog,
            BookmarkPresenterTestContext.Browsing("repo", BookmarkCategoryFilter.All));

        int[] counts = [.. presentation.Browse.Categories.Select(option => option.EntryCount)];
        CollectionAssert.AreEqual(ExpectedCounts, counts);
        Assert.AreEqual("3", presentation.Browse.Categories[0].EntryCountText);
        Assert.HasCount(1, presentation.Browse.Rows);
    }
}
