using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Presentation.WinUI.Bookmarks;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves BookmarkDraftPresenter behavior and contracts.</summary>
[TestClass]
public sealed class BookmarkDraftPresenterTests
{
    /// <summary>Proves a rejected bookmark save retains every draft choice and its correction.</summary>
    [TestMethod]
    public void PresentWhenBookmarkDraftIsRejectedKeepsDraftAndClosedChoices()
    {
        BookmarkCategoryName work = BookmarkPresenterTestContext.Category("Work");
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([work], []);
        BookmarkBrowseContext context = new("", BookmarkCategoryFilter.All, null);
        BookmarkDraft draft = new("Repo", "C:\\repo", BookmarkCategoryFilter.For(work), BookmarkShortcutSlot.Nine);
        BookmarksEditorState state = BookmarkPresenterTestContext.Construct<BookmarkDrafting>(
            [
                typeof(BookmarkBrowseContext),
                typeof(BookmarkSelection),
                typeof(BookmarkDraft),
                typeof(BookmarkEditorProblem),
            ],
            [context, null, draft, BookmarkEditorProblem.DuplicateShortcut]);

        BookmarkManagerPresentation presentation = BookmarkPresenterTestContext.Present(catalog, state);

        Assert.IsTrue(presentation.Details.IsBookmarkDrafting);
        Assert.IsTrue(presentation.Details.IsAddingBookmark);
        Assert.AreSame(draft, presentation.Details.BookmarkDraft);
        Assert.HasCount(2, presentation.Details.DraftCategories);
        Assert.AreEqual("Work", presentation.Details.SelectedDraftCategory!.DisplayText);
        Assert.HasCount(10, presentation.Details.Shortcuts);
        Assert.AreEqual("KeyLabelCtrl9", presentation.Details.SelectedShortcut!.LabelResourceKey);
        Assert.AreEqual("BookmarkProblemDuplicateShortcut", presentation.Details.StatusResourceKey);
        Assert.AreSame(BookmarkManagerFocusTarget.BookmarkName, presentation.Details.InitialFocus);
    }

    /// <summary>Proves an existing-entry draft is never projected as a new registration.</summary>
    [TestMethod]
    public void PresentWhenExistingBookmarkIsDraftedReportsEditing()
    {
        BookmarkEntry entry = BookmarkPresenterTestContext.Entry("Repo", "C:\\repo", null, null);
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([], [entry]);
        BookmarkBrowseContext context = new(
            string.Empty,
            BookmarkCategoryFilter.All,
            new BookmarkSelection(entry));
        BookmarkDraft draft = new("Repo", "C:\\repo", BookmarkCategoryFilter.Uncategorized, null);
        BookmarksEditorState state = BookmarkPresenterTestContext.Construct<BookmarkDrafting>(
            [
                typeof(BookmarkBrowseContext),
                typeof(BookmarkSelection),
                typeof(BookmarkDraft),
                typeof(BookmarkEditorProblem),
            ],
            [context, new BookmarkSelection(entry), draft, null]);

        BookmarkManagerPresentation presentation = BookmarkPresenterTestContext.Present(catalog, state);

        Assert.IsTrue(presentation.Details.IsBookmarkDrafting);
        Assert.IsFalse(presentation.Details.IsAddingBookmark);
        Assert.AreEqual("BookmarkStatusEditing", presentation.Details.StatusResourceKey);
    }

    /// <summary>Proves an add draft exposes its selected category, unassigned shortcut, and ready status.</summary>
    [TestMethod]
    public void PresentWhenNewBookmarkDraftHasNoProblemProjectsEveryDefaultChoice()
    {
        BookmarkCategoryName work = BookmarkPresenterTestContext.Category("Work");
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([work], []);
        BookmarkDraft draft = new(
            "Repo",
            "C:\\repo",
            BookmarkCategoryFilter.Uncategorized,
            null);
        BookmarksEditorState state = BookmarkPresenterTestContext.Construct<BookmarkDrafting>(
            [
                typeof(BookmarkBrowseContext),
                typeof(BookmarkSelection),
                typeof(BookmarkDraft),
                typeof(BookmarkEditorProblem),
            ],
            [new BookmarkBrowseContext(string.Empty, BookmarkCategoryFilter.All, null), null, draft, null]);

        BookmarkManagerPresentation presentation = BookmarkPresenterTestContext.Present(catalog, state);

        Assert.IsTrue(presentation.Details.IsAddingBookmark);
        Assert.AreSame(draft, presentation.Details.BookmarkDraft);
        Assert.AreEqual("BookmarkStatusAdding", presentation.Details.StatusResourceKey);
        Assert.AreEqual("Uncategorized", presentation.Details.SelectedDraftCategory!.DisplayText);
        Assert.IsTrue(presentation.Details.Shortcuts[0].IsSelected);
        Assert.IsNull(presentation.Details.SelectedShortcut!.Slot);
        Assert.AreEqual("BookmarkShortcutUnassigned", presentation.Details.SelectedShortcut.LabelResourceKey);
        Assert.AreSame(BookmarkManagerFocusTarget.BookmarkName, presentation.Details.InitialFocus);
    }

    /// <summary>Proves category deletion exposes its affected count and safe initial focus.</summary>
    [TestMethod]
    public void PresentWhenCategoryDeleteAwaitsConfirmationReportsCountAndCancelFocus()
    {
        BookmarkCategoryName work = BookmarkPresenterTestContext.Category("Work");
        BookmarkEntry first = BookmarkPresenterTestContext.Entry("One", "C:\\one", work, null);
        BookmarkEntry second = BookmarkPresenterTestContext.Entry("Two", "C:\\two", work, null);
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([work], [first, second]);
        BookmarkCategorySelection selection = catalog.Select(work)!;
        BookmarksEditorState state = BookmarkPresenterTestContext.Construct<BookmarkCategoryDeleteConfirmation>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkCategorySelection)],
            [new BookmarkBrowseContext("", BookmarkCategoryFilter.For(work), null), selection]);

        BookmarkManagerPresentation presentation = BookmarkPresenterTestContext.Present(catalog, state);

        Assert.IsTrue(presentation.Details.IsCategoryDeleteConfirmation);
        Assert.AreSame(selection, presentation.Details.CategorySelection);
        Assert.AreEqual(2, presentation.Details.CategoryDeleteCount);
        Assert.AreSame(
            BookmarkManagerFocusTarget.CancelCategoryDelete,
            presentation.Details.InitialFocus);
    }

    /// <summary>Proves every catalog validation problem has one stable product resource.</summary>
    [TestMethod]
    public void PresentWhenEditorReportsCatalogProblemsMapsEveryClosedProblemResource()
    {
        (BookmarkEditorProblem Problem, string ResourceKey)[] cases =
        [
            (BookmarkEditorProblem.InvalidName, "BookmarkProblemInvalidName"),
            (BookmarkEditorProblem.InvalidPath, "BookmarkProblemInvalidPath"),
            (BookmarkEditorProblem.InvalidCategory, "BookmarkProblemInvalidCategory"),
            (BookmarkEditorProblem.DuplicateCategory, "BookmarkProblemDuplicateCategory"),
            (BookmarkEditorProblem.DuplicateBookmark, "BookmarkProblemDuplicateBookmark"),
            (BookmarkEditorProblem.DuplicateShortcut, "BookmarkProblemDuplicateShortcut"),
            (BookmarkEditorProblem.CategoryLimit, "BookmarkProblemCategoryLimit"),
            (BookmarkEditorProblem.BookmarkLimit, "BookmarkProblemBookmarkLimit"),
            (BookmarkEditorProblem.CategoryDeleteCollision, "BookmarkProblemCategoryDeleteCollision"),
            (BookmarkEditorProblem.StaleSelection, "BookmarkProblemStaleSelection"),
        ];

        foreach ((BookmarkEditorProblem problem, string resourceKey) in cases)
        {
            BookmarksEditorState state = BookmarkPresenterTestContext.Construct<BookmarksBrowsing>(
                [typeof(BookmarkBrowseContext), typeof(BookmarkEditorProblem)],
                [new BookmarkBrowseContext("", BookmarkCategoryFilter.All, null), problem]);

            BookmarkManagerPresentation presentation = BookmarkPresenterTestContext.Present(BookmarkCatalog.Empty, state);

            Assert.AreEqual(resourceKey, presentation.Details.StatusResourceKey);
        }
    }

    /// <summary>Proves add and rename category states retain their draft and focus semantics.</summary>
    [TestMethod]
    public void PresentWhenCategoryDraftVariesDistinguishesAddFromRename()
    {
        BookmarkCategoryName work = BookmarkPresenterTestContext.Category("Work");
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([work], []);
        BookmarkBrowseContext context = new("", BookmarkCategoryFilter.For(work), null);
        BookmarkCategorySelection selection = catalog.Select(work) ??
            throw new InvalidOperationException("The category fixture must be selectable.");
        BookmarksEditorState adding = BookmarkPresenterTestContext.Construct<BookmarkCategoryDrafting>(
            [
                typeof(BookmarkBrowseContext),
                typeof(BookmarkCategorySelection),
                typeof(string),
                typeof(BookmarkEditorProblem),
            ],
            [context, null, "New", null]);
        BookmarksEditorState renaming = BookmarkPresenterTestContext.Construct<BookmarkCategoryDrafting>(
            [
                typeof(BookmarkBrowseContext),
                typeof(BookmarkCategorySelection),
                typeof(string),
                typeof(BookmarkEditorProblem),
            ],
            [context, selection, "Projects", null]);

        BookmarkEditorDetails addDetails = BookmarkPresenterTestContext.Present(catalog, adding).Details;
        BookmarkEditorDetails renameDetails = BookmarkPresenterTestContext.Present(catalog, renaming).Details;

        Assert.IsTrue(addDetails.IsCategoryDrafting);
        Assert.IsTrue(addDetails.IsAddingCategory);
        Assert.AreEqual("New", addDetails.CategoryDraftName);
        Assert.AreEqual("BookmarkStatusAddingCategory", addDetails.StatusResourceKey);
        Assert.AreSame(BookmarkManagerFocusTarget.CategoryName, addDetails.InitialFocus);
        Assert.IsFalse(renameDetails.IsAddingCategory);
        Assert.AreSame(selection, renameDetails.CategorySelection);
        Assert.AreEqual("Projects", renameDetails.CategoryDraftName);
        Assert.AreEqual("BookmarkStatusRenamingCategory", renameDetails.StatusResourceKey);
    }

    /// <summary>Proves stale draft categories do not fabricate a selected presentation option.</summary>
    [TestMethod]
    public void PresentWhenDraftCategoryIsAbsentFromCatalogLeavesDraftCategoryUnselected()
    {
        BookmarkCategoryName work = BookmarkPresenterTestContext.Category("Work");
        BookmarkCategoryName stale = BookmarkPresenterTestContext.Category("Stale");
        BookmarkDraft draft = new(
            "Repo",
            "C:\\repo",
            BookmarkCategoryFilter.For(stale),
            BookmarkShortcutSlot.One);
        BookmarksEditorState state = BookmarkPresenterTestContext.Construct<BookmarkDrafting>(
            [
                typeof(BookmarkBrowseContext),
                typeof(BookmarkSelection),
                typeof(BookmarkDraft),
                typeof(BookmarkEditorProblem),
            ],
            [new BookmarkBrowseContext(string.Empty, BookmarkCategoryFilter.All, null), null, draft, null]);

        BookmarkEditorDetails details = BookmarkPresenterTestContext.Present(BookmarkPresenterTestContext.Catalog([work], []), state).Details;

        Assert.IsNull(details.SelectedDraftCategory);
    }
}
