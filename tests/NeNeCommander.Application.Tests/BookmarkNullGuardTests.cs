using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves bookmark values, catalogs and editing owners reject absent required values.</summary>
[TestClass]
public sealed class BookmarkNullGuardTests
{
    /// <summary>Proves every bookmark value boundary rejects each absent required value.</summary>
    [TestMethod]
    public void BookmarkValuesWhenRequiredArgumentIsNullThrowArgumentNullException()
    {
        BookmarkCategoryName category = NullGuardAssertions.Category("Work");
        BookmarkDisplayName name = NullGuardAssertions.DisplayName("Target");
        BookmarkPath path = NullGuardAssertions.BookmarkPath("C:\\target");
        BookmarkEntry entry = BookmarkEntry.Create(name, path, category, BookmarkShortcutSlot.One);
        BookmarkSelection selection = new(entry);

        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkBrowseContext(null!, BookmarkCategoryFilter.All, null));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkBrowseContext(string.Empty, null!, null));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkDraft(null!, "C:\\target", BookmarkCategoryFilter.All, null));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkDraft("Target", null!, BookmarkCategoryFilter.All, null));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkDraft("Target", "C:\\target", null!, null));
        NullGuardAssertions.AssertNullGuard(() => _ = BookmarkEntry.Create(null!, path, category, null));
        NullGuardAssertions.AssertNullGuard(() => _ = BookmarkEntry.Create(name, null!, category, null));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkKey(category, null!));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkSelection(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkCategorySelection(null!, [entry]));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkCategorySelection(category, null!));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkUserCategoryFilter(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkRegistrationDefaults(null!, "C:\\target"));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkRegistrationDefaults("Target", null!));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkNavigationStart.Accepted(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkEditorTransition.CatalogChanged(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkEditorMutationResult(null!, new BookmarkEditorTransition.StateChanged()));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkEditorMutationResult(BookmarksEditorState.Closed, null!));
        NullGuardAssertions.AssertNullGuard(() => _ = UserIntent.ManageBookmarks(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = UserIntent.NavigateBookmark(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkShortcutSelection(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = new ResolvedBookmarkNavigation(null!));

        Assert.AreSame(entry, selection.Entry);
    }

    /// <summary>Proves every bookmark editor state and action rejects each absent required value.</summary>
    [TestMethod]
    public void BookmarkEditorInputsWhenRequiredArgumentIsNullThrowArgumentNullException()
    {
        BookmarkCategoryName category = NullGuardAssertions.Category("Work");
        BookmarkEntry entry = BookmarkEntry.Create(
            NullGuardAssertions.DisplayName("Target"),
            NullGuardAssertions.BookmarkPath("C:\\target"),
            category,
            null);
        BookmarkSelection selection = new(entry);
        BookmarkCategorySelection categorySelection = new(category, [entry]);
        BookmarkBrowseContext context = new(string.Empty, BookmarkCategoryFilter.All, null);
        BookmarkDraft draft = new("Target", "C:\\target", BookmarkCategoryFilter.All, null);

        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarksBrowsing(null!, null));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkDrafting(null!, null, draft, null));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkDrafting(context, null, null!, null));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkCategoryDrafting(null!, null, string.Empty, null));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkCategoryDrafting(context, null, null!, null));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkCategoryDeleteConfirmation(null!, categorySelection));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkCategoryDeleteConfirmation(context, null!));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkDeleteConfirmation(null!, selection));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkDeleteConfirmation(context, null!));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkNavigationPending(null!, selection));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkNavigationPending(context, null!));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkNavigationFailed(
            null!,
            selection,
            new PaneReadCancelled(NullGuardAssertions.ParsePath("C:\\target"))));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkNavigationFailed(
            context,
            null!,
            new PaneReadCancelled(NullGuardAssertions.ParsePath("C:\\target"))));
        NullGuardAssertions.AssertNullGuard(() => _ = new BookmarkNavigationFailed(context, selection, null!));
        NullGuardAssertions.AssertNullGuard(() => _ = BookmarkEditorAction.Search(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = BookmarkEditorAction.Filter(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = BookmarkEditorAction.BeginEditBookmark(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = BookmarkEditorAction.UpdateBookmark(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = BookmarkEditorAction.BeginRenameCategory(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = BookmarkEditorAction.UpdateCategory(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = BookmarkEditorAction.BeginDeleteBookmark(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = BookmarkEditorAction.BeginDeleteCategory(null!));
    }

    /// <summary>Proves every catalog entry point rejects each absent required value before mutating.</summary>
    [TestMethod]
    public void BookmarkCatalogWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        BookmarkCategoryName category = NullGuardAssertions.Category("Work");
        BookmarkEntry entry = BookmarkEntry.Create(
            NullGuardAssertions.DisplayName("Target"),
            NullGuardAssertions.BookmarkPath("C:\\target"),
            category,
            BookmarkShortcutSlot.One);
        BookmarkCatalog catalog = Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create([category], [entry])).Catalog;
        BookmarkSelection selection = new(catalog.Bookmarks[0]);
        BookmarkCategorySelection categorySelection = catalog.Select(category) ??
            throw new AssertFailedException("The category fixture must be selectable.");

        NullGuardAssertions.AssertNullGuard(() => _ = BookmarkCatalog.Create(null!, [entry]));
        NullGuardAssertions.AssertNullGuard(() => _ = BookmarkCatalog.Create([category], null!));
        NullGuardAssertions.AssertNullGuard(() => _ = catalog.Find((BookmarkShortcutSlot)null!));
        NullGuardAssertions.AssertNullGuard(() => _ = catalog.Find((BookmarkKey)null!));
        NullGuardAssertions.AssertNullGuard(() => _ = catalog.Select((BookmarkCategoryName)null!));
        NullGuardAssertions.AssertNullGuard(() => _ = catalog.Matches((BookmarkSelection)null!));
        NullGuardAssertions.AssertNullGuard(() => _ = catalog.Matches((BookmarkCategorySelection)null!));
        NullGuardAssertions.AssertNullGuard(() => _ = BookmarkCatalog.SelectionsMatch(null!, selection));
        NullGuardAssertions.AssertNullGuard(() => _ = BookmarkCatalog.SelectionsMatch(selection, null!));
        NullGuardAssertions.AssertNullGuard(() => _ = catalog.AddCategory(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = catalog.RenameCategory(null!, category));
        NullGuardAssertions.AssertNullGuard(() => _ = catalog.RenameCategory(categorySelection, null!));
        NullGuardAssertions.AssertNullGuard(() => _ = catalog.DeleteCategory(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = catalog.AddBookmark(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = catalog.ReplaceBookmark(null!, entry));
        NullGuardAssertions.AssertNullGuard(() => _ = catalog.ReplaceBookmark(selection, null!));
        NullGuardAssertions.AssertNullGuard(() => _ = catalog.DeleteBookmark(null!));
    }

    /// <summary>Proves the bookmark editor and settings owners reject absent bookmark arguments.</summary>
    [TestMethod]
    public void BookmarkSessionsWhenRequiredArgumentIsNullThrowArgumentNullException()
    {
        BookmarkCategoryName category = NullGuardAssertions.Category("Work");
        BookmarkEntry entry = BookmarkEntry.Create(
            NullGuardAssertions.DisplayName("Target"),
            NullGuardAssertions.BookmarkPath("C:\\target"),
            category,
            null);
        BookmarkCatalog catalog = Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create([category], [entry])).Catalog;
        BookmarkSelection selection = new(catalog.Bookmarks[0]);
        BookmarkRegistrationDefaults defaults = new(string.Empty, string.Empty);
        BookmarkEditorSession editor = new();
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession settings = new(store, SettingsReadOutcome.Absent(), static _ => { });
        RecordingCommanderObserver observer = new();

        NullGuardAssertions.AssertNullGuard(() => _ = editor.Apply(null!, catalog, defaults));
        NullGuardAssertions.AssertNullGuard(() => _ = editor.Apply(BookmarkEditorAction.Cancel, null!, defaults));
        NullGuardAssertions.AssertNullGuard(() => _ = editor.Apply(BookmarkEditorAction.Cancel, catalog, null!));
        NullGuardAssertions.AssertNullGuard(() => _ = editor.BeginNavigation(null!, catalog));
        NullGuardAssertions.AssertNullGuard(() => _ = editor.BeginNavigation(selection, null!));
        NullGuardAssertions.AssertNullGuard(() => editor.FinishNavigationFailed(null!));
        AssertBookmarkSettingsArguments(settings, catalog, defaults, observer);
        Assert.IsEmpty(store.Writes);
    }

    private static void AssertBookmarkSettingsArguments(SettingsSession settings, BookmarkCatalog catalog, BookmarkRegistrationDefaults defaults, RecordingCommanderObserver observer)
    {
        NullGuardAssertions.AssertNullGuard(() => _ = settings.SaveBookmarkCatalogAsync(null!, observer, CancellationToken.None));
        NullGuardAssertions.AssertNullGuard(() => _ = settings.SaveBookmarkCatalogAsync(catalog, null!, CancellationToken.None));
        NullGuardAssertions.AssertNullGuard(() => _ = settings.ApplyBookmarkEditorAction(
            null!,
            defaults,
            observer,
            CancellationToken.None));
        NullGuardAssertions.AssertNullGuard(() => _ = settings.ApplyBookmarkEditorAction(
            BookmarkEditorAction.Cancel,
            null!,
            observer,
            CancellationToken.None));
        NullGuardAssertions.AssertNullGuard(() => _ = settings.ApplyBookmarkEditorAction(
            BookmarkEditorAction.Cancel,
            defaults,
            null!,
            CancellationToken.None));
        NullGuardAssertions.AssertNullGuard(() => _ = settings.BeginBookmarkNavigation(null!));
        NullGuardAssertions.AssertNullGuard(() => settings.FinishBookmarkNavigationFailed(null!));

    }
}
