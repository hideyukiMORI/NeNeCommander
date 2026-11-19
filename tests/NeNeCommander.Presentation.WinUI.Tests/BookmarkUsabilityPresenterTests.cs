using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Presentation.WinUI.Bookmarks;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the draft choices and save-return notice expose the keyboard workflow's state.</summary>
[TestClass]
public sealed class BookmarkUsabilityPresenterTests
{
    /// <summary>Proves a rejected shortcut choice still identifies its owner and keeps the draft selection.</summary>
    [TestMethod]
    public void OccupiedShortcutShowsItsOwnerWithoutReassigningOrDiscardingTheDraft()
    {
        BookmarkEntry owner = BookmarkPresenterTestContext.Entry("Existing", "C:\\existing", null, BookmarkShortcutSlot.One);
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([], [owner]);
        BookmarkDraft draft = new("New", "C:\\new", BookmarkCategoryFilter.Uncategorized, BookmarkShortcutSlot.One);
        BookmarkDrafting state = BookmarkPresenterTestContext.Construct<BookmarkDrafting>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkSelection), typeof(BookmarkDraft), typeof(BookmarkEditorProblem)],
            [new BookmarkBrowseContext("", BookmarkCategoryFilter.All, null), null, draft, BookmarkEditorProblem.DuplicateShortcut]);

        BookmarkEditorDetails details = BookmarkPresenterTestContext.Present(catalog, state).Details;

        Assert.AreEqual("Existing", details.Shortcuts.Single(option => option.Slot == BookmarkShortcutSlot.One).AssignedBookmarkName);
        Assert.IsNull(details.Shortcuts.Single(option => option.Slot == BookmarkShortcutSlot.Two).AssignedBookmarkName);
        Assert.AreSame(draft, details.BookmarkDraft);
        Assert.AreSame(BookmarkShortcutSlot.One, details.SelectedShortcut?.Slot);
        Assert.AreEqual("BookmarkProblemDuplicateShortcut", details.StatusResourceKey);
    }

    /// <summary>Proves a saved entry and the explicit search-reset notice are projected together.</summary>
    [TestMethod]
    public void SavedBookmarkIsSelectedAndItsSearchResetIsExplained()
    {
        BookmarkEntry entry = BookmarkPresenterTestContext.Entry("Updated", "C:\\updated", null, null);
        BookmarkSelection selection = new(entry);
        BookmarksBrowsing state = BookmarkPresenterTestContext.Construct<BookmarksBrowsing>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkEditorProblem), typeof(BookmarkSelection)],
            [new BookmarkBrowseContext("", BookmarkCategoryFilter.All, selection), null, selection]);

        BookmarkManagerPresentation presentation = BookmarkPresenterTestContext.Present(
            BookmarkPresenterTestContext.Catalog([], [entry]), state);

        Assert.AreEqual(selection, presentation.Browse.SelectedRow?.Selection);
        Assert.AreEqual("BookmarkStatusSavedSelection", presentation.Details.StatusResourceKey);
    }

    /// <summary>Proves category drafts gain Save while deletion confirmations retain the shared modal owner.</summary>
    [TestMethod]
    public void CategoryDraftOwnsSaveButDeletionConfirmationDoesNot()
    {
        BookmarkBrowseContext context = new("", BookmarkCategoryFilter.All, null);
        BookmarkCategoryDrafting draft = BookmarkPresenterTestContext.Construct<BookmarkCategoryDrafting>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkCategorySelection), typeof(string), typeof(BookmarkEditorProblem)],
            [context, null, "New category", null]);
        BookmarkEntry entry = BookmarkPresenterTestContext.Entry("Entry", "C:\\entry", null, null);
        BookmarkDeleteConfirmation confirmation = BookmarkPresenterTestContext.Construct<BookmarkDeleteConfirmation>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkSelection)], [context, new BookmarkSelection(entry)]);
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([], [entry]);

        Assert.AreSame(KeyboardContext.BookmarkDraft, BookmarkManagerKeyboard.ContextFor(
            BookmarkPresenterTestContext.Present(catalog, draft).Details, BookmarkManagerFocusTarget.CategoryName));
        Assert.IsNull(BookmarkManagerKeyboard.ContextFor(
            BookmarkPresenterTestContext.Present(catalog, confirmation).Details, BookmarkManagerFocusTarget.CancelBookmarkDelete));
    }
}
