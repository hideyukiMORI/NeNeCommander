using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Bookmarks;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>
/// Proves the bookmark manager's keyboard decisions (ADR-0079): each action resolves to the existing
/// action of the button it replaces in the focused region, is unavailable exactly where that button
/// was disabled, and movement stops at both ends.
/// </summary>
[TestClass]
public sealed class BookmarkManagerKeyboardTests
{
    /// <summary>Proves the list's commands submit the existing bookmark actions for the selected row.</summary>
    [TestMethod]
    public void ResolveWhenListHoldsFocusSubmitsTheExistingRowActions()
    {
        (BookmarkRow row, BookmarkCategoryOption category) = Selections();
        BookmarkManagerRegion list = BookmarkManagerRegion.Bookmarks;

        Assert.AreEqual(UserIntent.NavigateBookmark(row.Selection), Resolve(list, BookmarkManagerKeyAction.Activate, row, category).Intent);
        Assert.AreEqual(Manage(BookmarkEditorAction.BeginAddBookmark), Resolve(list, BookmarkManagerKeyAction.Add, row, category).Intent);
        Assert.AreEqual(Manage(BookmarkEditorAction.BeginEditBookmark(row.Selection)), Resolve(list, BookmarkManagerKeyAction.Edit, row, category).Intent);
        Assert.AreEqual(Manage(BookmarkEditorAction.BeginDeleteBookmark(row.Selection)), Resolve(list, BookmarkManagerKeyAction.Delete, row, category).Intent);
        Assert.AreEqual(Manage(BookmarkEditorAction.BeginAddBookmark), Resolve(list, BookmarkManagerKeyAction.Add, null, null).Intent);
        Assert.AreSame(BookmarkManagerCommand.None, Resolve(list, BookmarkManagerKeyAction.Activate, null, category));
        Assert.AreSame(BookmarkManagerCommand.None, Resolve(list, BookmarkManagerKeyAction.Edit, null, category));
        Assert.AreSame(BookmarkManagerCommand.None, Resolve(list, BookmarkManagerKeyAction.Delete, null, category));
    }

    /// <summary>Proves the column's commands submit the existing category actions and Enter moves to the list.</summary>
    [TestMethod]
    public void ResolveWhenCategoriesHoldFocusSubmitsTheExistingCategoryActions()
    {
        (BookmarkRow row, BookmarkCategoryOption category) = Selections();
        BookmarkCategoryOption all = Present().Browse.Categories[0];
        BookmarkCategorySelection selection = category.Selection ?? throw new AssertFailedException("A user category is required.");
        BookmarkManagerRegion column = BookmarkManagerRegion.Categories;

        Assert.AreSame(BookmarkManagerRegion.Bookmarks, Resolve(column, BookmarkManagerKeyAction.Activate, row, category).FocusRegion);
        Assert.IsNull(Resolve(column, BookmarkManagerKeyAction.Activate, row, category).Intent);
        Assert.AreEqual(Manage(BookmarkEditorAction.BeginAddCategory), Resolve(column, BookmarkManagerKeyAction.Add, row, all).Intent);
        Assert.AreEqual(Manage(BookmarkEditorAction.BeginRenameCategory(selection)), Resolve(column, BookmarkManagerKeyAction.Edit, row, category).Intent);
        Assert.AreEqual(Manage(BookmarkEditorAction.BeginDeleteCategory(selection)), Resolve(column, BookmarkManagerKeyAction.Delete, row, category).Intent);
        Assert.AreSame(BookmarkManagerCommand.None, Resolve(column, BookmarkManagerKeyAction.Edit, row, all));
        Assert.AreSame(BookmarkManagerCommand.None, Resolve(column, BookmarkManagerKeyAction.Delete, row, all));
        Assert.AreSame(BookmarkManagerCommand.None, Resolve(column, BookmarkManagerKeyAction.Delete, row, null));
    }

    /// <summary>Proves movement, switching, search and closing behave the same in both regions.</summary>
    [TestMethod]
    public void ResolveWhenActionIsRegionNeutralMovesFocusesOrCloses()
    {
        foreach (BookmarkManagerRegion region in new[] { BookmarkManagerRegion.Bookmarks, BookmarkManagerRegion.Categories })
        {
            Assert.AreEqual(1, Resolve(region, BookmarkManagerKeyAction.MoveNext, null, null).Step);
            Assert.AreEqual(-1, Resolve(region, BookmarkManagerKeyAction.MovePrevious, null, null).Step);
            Assert.AreSame(region.Other, Resolve(region, BookmarkManagerKeyAction.SwitchRegion, null, null).FocusRegion);
            Assert.AreSame(BookmarkManagerCommand.Search, Resolve(region, BookmarkManagerKeyAction.FocusSearch, null, null));
            Assert.IsTrue(BookmarkManagerCommand.Search.FocusesSearch);
            Assert.AreSame(BookmarkManagerRegion.Bookmarks, Resolve(region, BookmarkManagerKeyAction.LeaveSearch, null, null).FocusRegion);
            Assert.AreSame(UserIntent.Escape, Resolve(region, BookmarkManagerKeyAction.Close, null, null).Intent);
        }
        Assert.AreSame(BookmarkManagerRegion.Categories, BookmarkManagerRegion.Bookmarks.Other);
        Assert.AreSame(BookmarkManagerRegion.Bookmarks, BookmarkManagerRegion.Categories.Other);
    }

    /// <summary>Proves movement steps one item, stops at both ends and starts at the first item.</summary>
    [TestMethod]
    public void MoveWhenItemsExistStepsAndStopsAtBothEnds()
    {
        Assert.AreEqual(1, BookmarkManagerKeyboard.Move(0, 3, 1));
        Assert.AreEqual(2, BookmarkManagerKeyboard.Move(2, 3, 1));
        Assert.AreEqual(1, BookmarkManagerKeyboard.Move(2, 3, -1));
        Assert.AreEqual(0, BookmarkManagerKeyboard.Move(0, 3, -1));
        Assert.AreEqual(0, BookmarkManagerKeyboard.Move(-1, 3, -1));
        Assert.AreEqual(0, BookmarkManagerKeyboard.Move(3, 3, 1));
        Assert.AreEqual(-1, BookmarkManagerKeyboard.Move(-1, 0, 1));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BookmarkManagerKeyboard.Move(0, -1, 1));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BookmarkManagerCommand.Move(0));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BookmarkManagerCommand.Move(2));
    }

    /// <summary>Proves only browsing owns the bookmark contexts; drafts keep the shared modal context.</summary>
    [TestMethod]
    public void ContextForWhenStateVariesOwnsInputOnlyWhileBrowsing()
    {
        BookmarkManagerPresentation browsing = Present();
        BookmarkManagerPresentation drafting = BookmarkPresenterTestContext.Present(
            Catalog(),
            BookmarkPresenterTestContext.Construct<BookmarkDrafting>(
                [typeof(BookmarkBrowseContext), typeof(BookmarkSelection), typeof(BookmarkDraft), typeof(BookmarkEditorProblem)],
                [new BookmarkBrowseContext(string.Empty, BookmarkCategoryFilter.All, null), null, new BookmarkDraft("Name", "C:\\name", BookmarkCategoryFilter.Uncategorized, null), null]));

        Assert.AreSame(KeyboardContext.Bookmarks, BookmarkManagerKeyboard.ContextFor(browsing.Details, BookmarkManagerFocusTarget.BookmarkList));
        Assert.AreSame(KeyboardContext.BookmarkSearch, BookmarkManagerKeyboard.ContextFor(browsing.Details, BookmarkManagerFocusTarget.Search));
        Assert.IsNull(BookmarkManagerKeyboard.ContextFor(drafting.Details, BookmarkManagerFocusTarget.BookmarkList));
        Assert.IsNull(BookmarkManagerKeyboard.ContextFor(drafting.Details, BookmarkManagerFocusTarget.Search));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => BookmarkManagerKeyboard.ContextFor(null!, BookmarkManagerFocusTarget.Search));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => BookmarkManagerKeyboard.ContextFor(browsing.Details, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => Resolve(null!, BookmarkManagerKeyAction.Add, null, null));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => Resolve(BookmarkManagerRegion.Bookmarks, null!, null, null));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => BookmarkManagerCommand.Forward(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => BookmarkManagerCommand.Focus(null!));
    }

    private static BookmarkManagerCommand Resolve(
        BookmarkManagerRegion region,
        BookmarkManagerKeyAction action,
        BookmarkRow? row,
        BookmarkCategoryOption? category)
    {
        return BookmarkManagerKeyboard.Resolve(region, action, row, category);
    }

    private static UserIntent Manage(BookmarkEditorAction action)
    {
        return UserIntent.ManageBookmarks(action);
    }

    private static (BookmarkRow Row, BookmarkCategoryOption Category) Selections()
    {
        BookmarkManagerPresentation presentation = Present();
        return (presentation.Browse.Rows.Single(), presentation.Browse.Categories.Single(option => option.Selection is not null));
    }

    private static BookmarkManagerPresentation Present()
    {
        return BookmarkPresenterTestContext.Present(
            Catalog(),
            BookmarkPresenterTestContext.Browsing(string.Empty, BookmarkCategoryFilter.All));
    }

    private static BookmarkCatalog Catalog()
    {
        BookmarkCategoryName work = BookmarkPresenterTestContext.Category("Work");
        BookmarkEntry repository = BookmarkPresenterTestContext.Entry("Repository", "C:\\work", work, BookmarkShortcutSlot.One);
        return BookmarkPresenterTestContext.Catalog([work], [repository]);
    }
}
