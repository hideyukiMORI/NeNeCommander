using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Bookmarks;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves BookmarkPresentationContract behavior and contracts.</summary>
[TestClass]
public sealed class BookmarkPresentationContractTests
{
    /// <summary>Proves render-ready bookmark values reject missing required boundary inputs.</summary>
    [TestMethod]
    public void BookmarkPresentationWhenRequiredInputIsMissingRejectsTheCall()
    {
        BookmarkManagerPresentation valid = BookmarkPresenterTestContext.Present(
            BookmarkPresenterTestContext.Catalog([], [BookmarkPresenterTestContext.Entry("Repo", "C:\\repo", null, null)]),
            BookmarkPresenterTestContext.Browsing(string.Empty, BookmarkCategoryFilter.All));
        BookmarkCategoryOption category = valid.Browse.Categories[0];
        BookmarkRow row = valid.Browse.Rows[0];
        BookmarkShortcutOption shortcut = valid.Details.Shortcuts[0];

        AssertBrowseAndOptionBoundaries(valid, row);
        AssertHostAndDetailsBoundaries(valid);
        Assert.AreSame(category, valid.Browse.Categories[0]);
        Assert.AreSame(shortcut, valid.Details.Shortcuts[0]);
    }

    /// <summary>Proves the selected row belongs to the frozen result even after caller lists change.</summary>
    [TestMethod]
    public void BrowseWhenSourceListsChangeRetainsSnapshotAndSelectedRowIdentity()
    {
        BookmarkEntry entry = BookmarkPresenterTestContext.Entry("Repo", "C:\\repo", null, null);
        BookmarkRow row = new(new BookmarkSelection(entry), "Uncategorized", "KeyLabelUnmapped", BookmarkOptionSelection.Selected);
        BookmarkCategoryOption category = new(BookmarkCategoryFilter.All, "All", null, BookmarkOptionSelection.Selected);
        List<BookmarkRow> rows = [row];
        List<BookmarkCategoryOption> categories = [category];
        BookmarkBrowsePresentation browse = new("repo", categories, rows, BookmarkEmptyContent.Hidden);

        rows.Clear();
        categories.Clear();

        Assert.AreEqual("repo", browse.SearchText);
        Assert.HasCount(1, browse.Rows);
        Assert.HasCount(1, browse.Categories);
        Assert.AreSame(row, browse.Rows[0]);
        Assert.AreSame(row, browse.SelectedRow);
        Assert.AreSame(category, browse.Categories[0]);
        _ = Assert.ThrowsExactly<NotSupportedException>(() => ((IList<BookmarkRow>)browse.Rows).Clear());
        _ = Assert.ThrowsExactly<NotSupportedException>(() => ((IList<BookmarkCategoryOption>)browse.Categories).Clear());
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new BookmarkBrowsePresentation(null!, [], [], BookmarkEmptyContent.NoBookmarks));
    }

    private static void AssertBrowseAndOptionBoundaries(BookmarkManagerPresentation valid, BookmarkRow row)
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkBrowsePresentation(string.Empty, null!, valid.Browse.Rows, valid.Browse.EmptyContent));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkBrowsePresentation(string.Empty, valid.Browse.Categories, null!, valid.Browse.EmptyContent));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkBrowsePresentation(string.Empty, valid.Browse.Categories, valid.Browse.Rows, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkCategoryOption(null!, "All", null, BookmarkOptionSelection.Selected));
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            new BookmarkCategoryOption(BookmarkCategoryFilter.All, " ", null, BookmarkOptionSelection.Selected));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkCategoryOption(BookmarkCategoryFilter.All, "All", null, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkRow(null!, "Uncategorized", "KeyLabelUnmapped", BookmarkOptionSelection.NotSelected));
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            new BookmarkRow(row.Selection, " ", "KeyLabelUnmapped", BookmarkOptionSelection.NotSelected));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkRow(row.Selection, "Uncategorized", null!, BookmarkOptionSelection.NotSelected));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkRow(row.Selection, "Uncategorized", "KeyLabelUnmapped", null!));
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            new BookmarkShortcutOption(null, " ", BookmarkOptionSelection.Selected));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkShortcutOption(null, "BookmarkShortcutUnassigned", null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkPersistencePresentation(null!, SettingsWarningPresentation.Hidden));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkPersistencePresentation(SettingsSaveStatus.Succeeded, null!));
    }

    private static void AssertHostAndDetailsBoundaries(BookmarkManagerPresentation valid)
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkManagerPresentation(null!, valid.Browse, valid.Details, valid.Persistence));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkManagerPresentation(SettingsEditorState.Bookmarks, null!, valid.Details, valid.Persistence));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkManagerPresentation(SettingsEditorState.Bookmarks, valid.Browse, null!, valid.Persistence));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkManagerPresentation(SettingsEditorState.Bookmarks, valid.Browse, valid.Details, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkEditorDetails(null!, valid.Details.DraftCategories, valid.Details.Shortcuts, "Ready"));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkEditorDetails(
                BookmarksEditorState.Closed,
                null!,
                valid.Details.Shortcuts,
                "Ready"));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new BookmarkEditorDetails(
                BookmarksEditorState.Closed,
                valid.Details.DraftCategories,
                null!,
                "Ready"));
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            new BookmarkEditorDetails(
                BookmarksEditorState.Closed,
                valid.Details.DraftCategories,
                valid.Details.Shortcuts,
                " "));
    }
}
