using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Bookmarks;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves every session-owned bookmark-manager state has one deterministic projection.</summary>
[TestClass]
public sealed class BookmarkManagerPresenterTests
{
    /// <summary>Proves search preserves catalog order and slot labels come from the canonical map.</summary>
    [TestMethod]
    public void PresentWhenBrowsingFiltersInCatalogOrderAndUsesCanonicalSlotLabels()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkEntry repository = Entry("Repository", "C:\\work", work, BookmarkShortcutSlot.Two);
        BookmarkEntry notes = Entry("Notes", "C:\\notes", null, null);
        BookmarkCatalog catalog = Catalog([work], [notes, repository]);
        BookmarkBrowseContext context = new("repo", BookmarkCategoryFilter.All, new BookmarkSelection(repository));
        BookmarksEditorState state = Construct<BookmarksBrowsing>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkEditorProblem)],
            [context, null]);

        BookmarkManagerPresentation presentation = Present(catalog, state);

        Assert.IsTrue(presentation.IsOpen);
        Assert.HasCount(3, presentation.Browse.Categories);
        Assert.AreEqual("All bookmarks", presentation.Browse.Categories[0].DisplayText);
        Assert.AreEqual("Uncategorized", presentation.Browse.Categories[1].DisplayText);
        Assert.AreEqual("Work", presentation.Browse.Categories[2].DisplayText);
        Assert.HasCount(1, presentation.Browse.Rows);
        Assert.AreEqual("Repository", presentation.Browse.Rows[0].NameText);
        Assert.AreEqual("KeyLabelCtrl2", presentation.Browse.Rows[0].ShortcutLabelResourceKey);
        Assert.AreSame(presentation.Browse.Rows[0], presentation.Browse.SelectedRow);
        Assert.AreSame(BookmarkManagerFocusTarget.Search, presentation.Details.InitialFocus);
        Assert.AreSame(SettingsSaveStatus.Succeeded, presentation.Persistence.SaveStatus);
    }

    /// <summary>Proves a rejected bookmark save retains every draft choice and its correction.</summary>
    [TestMethod]
    public void PresentWhenBookmarkDraftIsRejectedKeepsDraftAndClosedChoices()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCatalog catalog = Catalog([work], []);
        BookmarkBrowseContext context = new("", BookmarkCategoryFilter.All, null);
        BookmarkDraft draft = new("Repo", "C:\\repo", BookmarkCategoryFilter.For(work), BookmarkShortcutSlot.Nine);
        BookmarksEditorState state = Construct<BookmarkDrafting>(
            [
                typeof(BookmarkBrowseContext),
                typeof(BookmarkSelection),
                typeof(BookmarkDraft),
                typeof(BookmarkEditorProblem),
            ],
            [context, null, draft, BookmarkEditorProblem.DuplicateShortcut]);

        BookmarkManagerPresentation presentation = Present(catalog, state);

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
        BookmarkEntry entry = Entry("Repo", "C:\\repo", null, null);
        BookmarkCatalog catalog = Catalog([], [entry]);
        BookmarkBrowseContext context = new(
            string.Empty,
            BookmarkCategoryFilter.All,
            new BookmarkSelection(entry));
        BookmarkDraft draft = new("Repo", "C:\\repo", BookmarkCategoryFilter.Uncategorized, null);
        BookmarksEditorState state = Construct<BookmarkDrafting>(
            [
                typeof(BookmarkBrowseContext),
                typeof(BookmarkSelection),
                typeof(BookmarkDraft),
                typeof(BookmarkEditorProblem),
            ],
            [context, new BookmarkSelection(entry), draft, null]);

        BookmarkManagerPresentation presentation = Present(catalog, state);

        Assert.IsTrue(presentation.Details.IsBookmarkDrafting);
        Assert.IsFalse(presentation.Details.IsAddingBookmark);
        Assert.AreEqual("BookmarkStatusEditing", presentation.Details.StatusResourceKey);
    }

    /// <summary>Proves category deletion exposes its affected count and safe initial focus.</summary>
    [TestMethod]
    public void PresentWhenCategoryDeleteAwaitsConfirmationReportsCountAndCancelFocus()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkEntry first = Entry("One", "C:\\one", work, null);
        BookmarkEntry second = Entry("Two", "C:\\two", work, null);
        BookmarkCatalog catalog = Catalog([work], [first, second]);
        BookmarkCategorySelection selection = catalog.Select(work)!;
        BookmarksEditorState state = Construct<BookmarkCategoryDeleteConfirmation>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkCategorySelection)],
            [new BookmarkBrowseContext("", BookmarkCategoryFilter.For(work), null), selection]);

        BookmarkManagerPresentation presentation = Present(catalog, state);

        Assert.IsTrue(presentation.Details.IsCategoryDeleteConfirmation);
        Assert.AreSame(selection, presentation.Details.CategorySelection);
        Assert.AreEqual(2, presentation.Details.CategoryDeleteCount);
        Assert.AreSame(
            BookmarkManagerFocusTarget.CancelCategoryDelete,
            presentation.Details.InitialFocus);
    }

    /// <summary>Proves pending navigation freezes input and failed navigation targets Retry.</summary>
    [TestMethod]
    public void PresentWhenNavigationVariesFreezesPendingAndFocusesFailedRetry()
    {
        BookmarkEntry entry = Entry("Offline", "\\\\server\\share", null, null);
        BookmarkCatalog catalog = Catalog([], [entry]);
        BookmarkSelection selection = new(entry);
        BookmarkBrowseContext context = new("", BookmarkCategoryFilter.All, selection);
        BookmarksEditorState pending = Construct<BookmarkNavigationPending>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkSelection)],
            [context, selection]);
        BookmarksEditorState failed = Construct<BookmarkNavigationFailed>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkSelection)],
            [context, selection]);

        BookmarkManagerPresentation pendingPresentation = Present(catalog, pending);
        BookmarkManagerPresentation failedPresentation = Present(catalog, failed);

        Assert.IsTrue(pendingPresentation.Details.IsNavigationPending);
        Assert.IsTrue(pendingPresentation.Details.IsInputFrozen);
        Assert.AreEqual("BookmarkStatusNavigationPending", pendingPresentation.Details.StatusResourceKey);
        Assert.IsTrue(failedPresentation.Details.IsNavigationFailed);
        Assert.IsFalse(failedPresentation.Details.IsInputFrozen);
        Assert.AreSame(selection, failedPresentation.Details.NavigationSelection);
        Assert.AreSame(
            BookmarkManagerFocusTarget.RetryNavigation,
            failedPresentation.Details.InitialFocus);
    }

    /// <summary>Proves the public presenter rejects missing snapshots and reserved labels.</summary>
    [TestMethod]
    public void PresentWhenSnapshotOrReservedLabelIsInvalidRejectsTheCall()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            BookmarkManagerPresenter.Present(null!, "All", "Uncategorized"));
        SettingsSnapshot snapshot = Snapshot(BookmarkCatalog.Empty, BookmarksEditorState.Closed);
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            BookmarkManagerPresenter.Present(snapshot, " ", "Uncategorized"));
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            BookmarkManagerPresenter.Present(snapshot, "All", ""));
    }

    private static BookmarkManagerPresentation Present(
        BookmarkCatalog catalog,
        BookmarksEditorState state)
    {
        return BookmarkManagerPresenter.Present(
            Snapshot(catalog, state),
            "All bookmarks",
            "Uncategorized");
    }

    private static SettingsSnapshot Snapshot(
        BookmarkCatalog catalog,
        BookmarksEditorState state)
    {
        return Construct<SettingsSnapshot>(
            [
                typeof(UserSettings),
                typeof(SettingsEditorState),
                typeof(BookmarksEditorState),
                typeof(SettingsPersistenceState),
            ],
            [
                UserSettings.Create(ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, catalog),
                SettingsEditorState.Bookmarks,
                state,
                SettingsPersistenceState.Succeeded,
            ]);
    }

    private static BookmarkCatalog Catalog(
        IReadOnlyList<BookmarkCategoryName> categories,
        IReadOnlyList<BookmarkEntry> entries)
    {
        return Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create(categories, entries)).Catalog;
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
        BookmarkShortcutSlot? shortcut)
    {
        BookmarkDisplayName displayName = Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(
            BookmarkDisplayName.Parse(name)).Name;
        BookmarkPath bookmarkPath = Assert.IsInstanceOfType<BookmarkPathAccepted>(
            BookmarkPath.Parse(path)).Path;
        return BookmarkEntry.Create(displayName, bookmarkPath, category, shortcut);
    }

    private static T Construct<T>(Type[] parameterTypes, object?[] values)
    {
        ConstructorInfo constructor = typeof(T).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            parameterTypes,
            null) ?? throw new AssertFailedException($"The {typeof(T).Name} constructor was not found.");
        return (T)constructor.Invoke(values);
    }
}
