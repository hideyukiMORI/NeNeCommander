using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Bookmarks;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves BookmarkManagerPresenter behavior and contracts.</summary>
[TestClass]
public sealed class BookmarkManagerPresenterTests
{
    /// <summary>Proves search preserves catalog order and slot labels come from the canonical map.</summary>
    [TestMethod]
    public void PresentWhenBrowsingFiltersInCatalogOrderAndUsesCanonicalSlotLabels()
    {
        BookmarkCategoryName work = BookmarkPresenterTestContext.Category("Work");
        BookmarkEntry repository = BookmarkPresenterTestContext.Entry("Repository", "C:\\work", work, BookmarkShortcutSlot.Two);
        BookmarkEntry notes = BookmarkPresenterTestContext.Entry("Notes", "C:\\notes", null, null);
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([work], [notes, repository]);
        BookmarkBrowseContext context = new("repo", BookmarkCategoryFilter.All, new BookmarkSelection(repository));
        BookmarksEditorState state = BookmarkPresenterTestContext.Construct<BookmarksBrowsing>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkEditorProblem)],
            [context, null]);

        BookmarkManagerPresentation presentation = BookmarkPresenterTestContext.Present(catalog, state);

        Assert.IsTrue(presentation.IsOpen);
        Assert.HasCount(3, presentation.Browse.Categories);
        Assert.AreEqual("All bookmarks", presentation.Browse.Categories[0].DisplayText);
        Assert.AreEqual("Uncategorized", presentation.Browse.Categories[1].DisplayText);
        Assert.AreEqual("Work", presentation.Browse.Categories[2].DisplayText);
        Assert.IsTrue(presentation.Browse.Categories[0].IsSelected);
        Assert.IsFalse(presentation.Browse.Categories[1].IsSelected);
        Assert.IsFalse(presentation.Browse.Categories[2].IsSelected);
        Assert.IsNull(presentation.Browse.Categories[0].Selection);
        Assert.IsNull(presentation.Browse.Categories[1].Selection);
        Assert.IsNotNull(presentation.Browse.Categories[2].Selection);
        Assert.HasCount(1, presentation.Browse.Rows);
        Assert.AreEqual("Repository", presentation.Browse.Rows[0].NameText);
        Assert.AreEqual("C:\\work", presentation.Browse.Rows[0].PathText);
        Assert.AreEqual("Work", presentation.Browse.Rows[0].CategoryText);
        Assert.AreEqual("KeyLabelCtrl2", presentation.Browse.Rows[0].ShortcutLabelResourceKey);
        Assert.IsTrue(presentation.Browse.Rows[0].IsSelected);
        Assert.AreSame(presentation.Browse.Rows[0], presentation.Browse.SelectedRow);
        Assert.AreSame(BookmarkManagerFocusTarget.Search, presentation.Details.InitialFocus);
        Assert.AreSame(SettingsSaveStatus.Succeeded, presentation.Persistence.SaveStatus);
    }

    /// <summary>Proves the result region distinguishes an empty catalog from an empty result.</summary>
    [TestMethod]
    public void PresentWhenRowsAreEmptyProjectsClosedEmptyContent()
    {
        BookmarkEntry entry = BookmarkPresenterTestContext.Entry("Repo", "C:\\repo", null, null);
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([], [entry]);

        BookmarkManagerPresentation emptyCatalog = BookmarkPresenterTestContext.Present(
            BookmarkCatalog.Empty,
            BookmarkPresenterTestContext.Browsing("", BookmarkCategoryFilter.All));
        BookmarkManagerPresentation noMatches = BookmarkPresenterTestContext.Present(
            catalog,
            BookmarkPresenterTestContext.Browsing("missing", BookmarkCategoryFilter.All));
        BookmarkManagerPresentation rowsAvailable = BookmarkPresenterTestContext.Present(
            catalog,
            BookmarkPresenterTestContext.Browsing("", BookmarkCategoryFilter.All));

        Assert.AreSame(BookmarkEmptyContent.NoBookmarks, emptyCatalog.Browse.EmptyContent);
        Assert.IsEmpty(emptyCatalog.Browse.Rows);
        Assert.AreSame(BookmarkEmptyContent.NoMatches, noMatches.Browse.EmptyContent);
        Assert.IsEmpty(noMatches.Browse.Rows);
        Assert.AreSame(BookmarkEmptyContent.Hidden, rowsAvailable.Browse.EmptyContent);
        Assert.HasCount(1, rowsAvailable.Browse.Rows);
    }

    /// <summary>Proves search covers displayed category and canonical path while filters remain closed.</summary>
    [TestMethod]
    public void PresentWhenSearchAndCategoryFiltersVaryKeepsCatalogOrderAndClosedMembership()
    {
        BookmarkCategoryName work = BookmarkPresenterTestContext.Category("Work");
        BookmarkCategoryName personal = BookmarkPresenterTestContext.Category("Personal");
        BookmarkEntry uncategorized = BookmarkPresenterTestContext.Entry("Notes", "C:\\notes", null, null);
        BookmarkEntry repository = BookmarkPresenterTestContext.Entry("Repository", "C:\\work\\src", work, BookmarkShortcutSlot.One);
        BookmarkEntry journal = BookmarkPresenterTestContext.Entry("Journal", "C:\\personal", personal, null);
        BookmarkCatalog catalog = BookmarkPresenterTestContext.Catalog([work, personal], [uncategorized, repository, journal]);

        BookmarkManagerPresentation byPath = BookmarkPresenterTestContext.Present(
            catalog,
            BookmarkPresenterTestContext.Browsing("src", BookmarkCategoryFilter.All));
        BookmarkManagerPresentation byCategory = BookmarkPresenterTestContext.Present(
            catalog,
            BookmarkPresenterTestContext.Browsing("work", BookmarkCategoryFilter.All));
        BookmarkManagerPresentation uncategorizedOnly = BookmarkPresenterTestContext.Present(
            catalog,
            BookmarkPresenterTestContext.Browsing("", BookmarkCategoryFilter.Uncategorized));
        BookmarkManagerPresentation workOnly = BookmarkPresenterTestContext.Present(
            catalog,
            BookmarkPresenterTestContext.Browsing("", BookmarkCategoryFilter.For(work)));

        Assert.HasCount(1, byPath.Browse.Rows);
        Assert.AreEqual("Repository", byPath.Browse.Rows[0].NameText);
        Assert.HasCount(1, byCategory.Browse.Rows);
        Assert.AreEqual("Repository", byCategory.Browse.Rows[0].NameText);
        Assert.HasCount(1, uncategorizedOnly.Browse.Rows);
        Assert.AreEqual("Notes", uncategorizedOnly.Browse.Rows[0].NameText);
        Assert.AreEqual("Uncategorized", uncategorizedOnly.Browse.Rows[0].CategoryText);
        Assert.AreEqual("KeyLabelUnmapped", uncategorizedOnly.Browse.Rows[0].ShortcutLabelResourceKey);
        Assert.IsTrue(uncategorizedOnly.Browse.Categories[1].IsSelected);
        Assert.HasCount(1, workOnly.Browse.Rows);
        Assert.AreEqual("Repository", workOnly.Browse.Rows[0].NameText);
        Assert.IsTrue(workOnly.Browse.Categories[2].IsSelected);
        Assert.IsFalse(workOnly.Browse.Categories[3].IsSelected);
    }

    /// <summary>Proves a closed editor produces a hidden, empty, ready projection.</summary>
    [TestMethod]
    public void PresentWhenManagerIsClosedKeepsTheOverlayHidden()
    {
        SettingsSnapshot snapshot = BookmarkPresenterTestContext.Construct<SettingsSnapshot>(
            [
                typeof(UserSettings),
                typeof(SettingsEditorState),
                typeof(BookmarksEditorState),
                typeof(SettingsPersistenceState),
            ],
            [
                UserSettings.Default,
                SettingsEditorState.Closed,
                BookmarksEditorState.Closed,
                SettingsPersistenceState.Succeeded,
            ]);

        BookmarkManagerPresentation presentation = BookmarkManagerPresenter.Present(
            snapshot,
            "All bookmarks",
            "Uncategorized");

        Assert.IsFalse(presentation.IsOpen);
        Assert.AreEqual(string.Empty, presentation.Browse.SearchText);
        Assert.IsTrue(presentation.Browse.Categories[0].IsSelected);
        Assert.IsNull(presentation.Browse.SelectedRow);
        Assert.IsNotNull(presentation.Browse);
        Assert.IsNotNull(presentation.Details);
        Assert.IsNotNull(presentation.Persistence);
        Assert.AreEqual("BookmarkStatusReady", presentation.Details.StatusResourceKey);
        Assert.IsNull(presentation.Details.BookmarkDraft);
        Assert.IsNull(presentation.Details.CategorySelection);
        Assert.IsNull(presentation.Details.NavigationSelection);
        Assert.AreEqual(string.Empty, presentation.Details.CategoryDraftName);
        Assert.AreEqual(0, presentation.Details.CategoryDeleteCount);
    }

    /// <summary>Proves the public presenter rejects missing snapshots and reserved labels.</summary>
    [TestMethod]
    public void PresentWhenSnapshotOrReservedLabelIsInvalidRejectsTheCall()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            BookmarkManagerPresenter.Present(null!, "All", "Uncategorized"));
        SettingsSnapshot snapshot = BookmarkPresenterTestContext.Snapshot(BookmarkCatalog.Empty, BookmarksEditorState.Closed);
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            BookmarkManagerPresenter.Present(snapshot, " ", "Uncategorized"));
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            BookmarkManagerPresenter.Present(snapshot, "All", ""));
    }
}
