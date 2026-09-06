using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
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
            [typeof(BookmarkBrowseContext), typeof(BookmarkSelection), typeof(PaneActivity)],
            [context, selection, Cancelled(entry.Path.Value)]);

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
        Assert.AreEqual(
            "BookmarkStatusNavigationCancelled",
            failedPresentation.Details.StatusResourceKey);
    }

    /// <summary>Proves every reachable pane failure uses the shared normalized status mapping.</summary>
    [TestMethod]
    public void PresentWhenNavigationFailsReportsItsNormalizedReasonWithoutRawDetails()
    {
        BookmarkEntry entry = Entry("Offline", "C:\\offline", null, null);
        BookmarkCatalog catalog = Catalog([], [entry]);
        BookmarkSelection selection = new(entry);
        BookmarkBrowseContext context = new("", BookmarkCategoryFilter.All, selection);
        (PaneActivity Reason, string ResourceKey)[] cases =
        [
            (Failed(entry.Path.Value, FileOperationFailureKind.AccessDenied),
                "BookmarkStatusNavigationAccessDenied"),
            (Failed(entry.Path.Value, FileOperationFailureKind.NotFound),
                "BookmarkStatusNavigationNotFound"),
            (Failed(entry.Path.Value, FileOperationFailureKind.ProviderUnavailable),
                "BookmarkStatusNavigationProviderUnavailable"),
            (Cancelled(entry.Path.Value), "BookmarkStatusNavigationCancelled"),
        ];

        foreach ((PaneActivity reason, string resourceKey) in cases)
        {
            BookmarksEditorState state = Construct<BookmarkNavigationFailed>(
                [typeof(BookmarkBrowseContext), typeof(BookmarkSelection), typeof(PaneActivity)],
                [context, selection, reason]);

            BookmarkManagerPresentation presentation = Present(catalog, state);

            Assert.AreEqual(resourceKey, presentation.Details.StatusResourceKey);
            Assert.AreSame(selection, presentation.Details.NavigationSelection);
            Assert.AreSame(
                BookmarkManagerFocusTarget.RetryNavigation,
                presentation.Details.InitialFocus);
        }
    }

    /// <summary>Proves the result region distinguishes an empty catalog from an empty result.</summary>
    [TestMethod]
    public void PresentWhenRowsAreEmptyProjectsClosedEmptyContent()
    {
        BookmarkEntry entry = Entry("Repo", "C:\\repo", null, null);
        BookmarkCatalog catalog = Catalog([], [entry]);

        BookmarkManagerPresentation emptyCatalog = Present(
            BookmarkCatalog.Empty,
            Browsing("", BookmarkCategoryFilter.All));
        BookmarkManagerPresentation noMatches = Present(
            catalog,
            Browsing("missing", BookmarkCategoryFilter.All));
        BookmarkManagerPresentation rowsAvailable = Present(
            catalog,
            Browsing("", BookmarkCategoryFilter.All));

        Assert.AreSame(BookmarkEmptyContent.NoBookmarks, emptyCatalog.Browse.EmptyContent);
        Assert.IsEmpty(emptyCatalog.Browse.Rows);
        Assert.AreSame(BookmarkEmptyContent.NoMatches, noMatches.Browse.EmptyContent);
        Assert.IsEmpty(noMatches.Browse.Rows);
        Assert.AreSame(BookmarkEmptyContent.Hidden, rowsAvailable.Browse.EmptyContent);
        Assert.HasCount(1, rowsAvailable.Browse.Rows);
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
            BookmarksEditorState state = Construct<BookmarksBrowsing>(
                [typeof(BookmarkBrowseContext), typeof(BookmarkEditorProblem)],
                [new BookmarkBrowseContext("", BookmarkCategoryFilter.All, null), problem]);

            BookmarkManagerPresentation presentation = Present(BookmarkCatalog.Empty, state);

            Assert.AreEqual(resourceKey, presentation.Details.StatusResourceKey);
        }
    }

    /// <summary>Proves add and rename category states retain their draft and focus semantics.</summary>
    [TestMethod]
    public void PresentWhenCategoryDraftVariesDistinguishesAddFromRename()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCatalog catalog = Catalog([work], []);
        BookmarkBrowseContext context = new("", BookmarkCategoryFilter.For(work), null);
        BookmarkCategorySelection selection = catalog.Select(work) ??
            throw new InvalidOperationException("The category fixture must be selectable.");
        BookmarksEditorState adding = Construct<BookmarkCategoryDrafting>(
            [
                typeof(BookmarkBrowseContext),
                typeof(BookmarkCategorySelection),
                typeof(string),
                typeof(BookmarkEditorProblem),
            ],
            [context, null, "New", null]);
        BookmarksEditorState renaming = Construct<BookmarkCategoryDrafting>(
            [
                typeof(BookmarkBrowseContext),
                typeof(BookmarkCategorySelection),
                typeof(string),
                typeof(BookmarkEditorProblem),
            ],
            [context, selection, "Projects", null]);

        BookmarkEditorDetails addDetails = Present(catalog, adding).Details;
        BookmarkEditorDetails renameDetails = Present(catalog, renaming).Details;

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

    /// <summary>Proves search covers displayed category and canonical path while filters remain closed.</summary>
    [TestMethod]
    public void PresentWhenSearchAndCategoryFiltersVaryKeepsCatalogOrderAndClosedMembership()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkEntry uncategorized = Entry("Notes", "C:\\notes", null, null);
        BookmarkEntry repository = Entry("Repository", "C:\\work\\src", work, BookmarkShortcutSlot.One);
        BookmarkCatalog catalog = Catalog([work], [uncategorized, repository]);

        BookmarkManagerPresentation byPath = Present(
            catalog,
            Browsing("src", BookmarkCategoryFilter.All));
        BookmarkManagerPresentation byCategory = Present(
            catalog,
            Browsing("work", BookmarkCategoryFilter.All));
        BookmarkManagerPresentation uncategorizedOnly = Present(
            catalog,
            Browsing("", BookmarkCategoryFilter.Uncategorized));
        BookmarkManagerPresentation workOnly = Present(
            catalog,
            Browsing("", BookmarkCategoryFilter.For(work)));

        Assert.HasCount(1, byPath.Browse.Rows);
        Assert.AreEqual("Repository", byPath.Browse.Rows[0].NameText);
        Assert.HasCount(1, byCategory.Browse.Rows);
        Assert.AreEqual("Repository", byCategory.Browse.Rows[0].NameText);
        Assert.HasCount(1, uncategorizedOnly.Browse.Rows);
        Assert.AreEqual("Notes", uncategorizedOnly.Browse.Rows[0].NameText);
        Assert.HasCount(1, workOnly.Browse.Rows);
        Assert.AreEqual("Repository", workOnly.Browse.Rows[0].NameText);
    }

    /// <summary>Proves a closed editor produces a hidden, empty, ready projection.</summary>
    [TestMethod]
    public void PresentWhenManagerIsClosedKeepsTheOverlayHidden()
    {
        SettingsSnapshot snapshot = Construct<SettingsSnapshot>(
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
        Assert.AreEqual("BookmarkStatusReady", presentation.Details.StatusResourceKey);
        Assert.IsNull(presentation.Details.BookmarkDraft);
        Assert.IsNull(presentation.Details.CategorySelection);
        Assert.IsNull(presentation.Details.NavigationSelection);
        Assert.AreEqual(0, presentation.Details.CategoryDeleteCount);
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

    private static BookmarksBrowsing Browsing(
        string searchText,
        BookmarkCategoryFilter filter)
    {
        return Construct<BookmarksBrowsing>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkEditorProblem)],
            [new BookmarkBrowseContext(searchText, filter, null), null]);
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

    private static PaneReadFailed Failed(
        FileSystemPath target,
        FileOperationFailureKind failure)
    {
        return Construct<PaneReadFailed>(
            [typeof(FileSystemPath), typeof(FileOperationFailureKind)],
            [target, failure]);
    }

    private static PaneReadCancelled Cancelled(FileSystemPath target)
    {
        return Construct<PaneReadCancelled>([typeof(FileSystemPath)], [target]);
    }
}
