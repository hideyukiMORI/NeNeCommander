using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Presentation.WinUI.Bookmarks;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves bookmark presentation keeps visible choices aligned with session state.</summary>
[TestClass]
public sealed class BookmarkPresentationBehaviorTests
{
    /// <summary>Proves only the selected catalog entry is rendered as selected and returned as the selected row.</summary>
    [TestMethod]
    public void PresentWhenSelectionTargetsSecondVisibleEntrySelectsOnlyThatRow()
    {
        BookmarkEntry first = Entry("First", "C:\\first", null, null);
        BookmarkEntry second = Entry("Second", "C:\\second", null, null);
        BookmarkSelection selection = new(second);
        BookmarkManagerPresentation presentation = Present(
            Catalog([], [first, second]),
            Browsing(new BookmarkBrowseContext(string.Empty, BookmarkCategoryFilter.All, selection)));

        Assert.HasCount(2, presentation.Browse.Rows);
        Assert.IsFalse(presentation.Browse.Rows[0].IsSelected);
        Assert.IsTrue(presentation.Browse.Rows[1].IsSelected);
        Assert.AreSame(presentation.Browse.Rows[1], presentation.Browse.SelectedRow);
        Assert.AreEqual(second, presentation.Browse.Rows[1].Selection.Entry);
    }

    /// <summary>Proves a user category filter selects its option and excludes other categories from rows.</summary>
    [TestMethod]
    public void PresentWhenUserCategoryFilterIsActiveSelectsOnlyThatCategory()
    {
        BookmarkCategoryName work = Category("Work");
        BookmarkCategoryName personal = Category("Personal");
        BookmarkCatalog catalog = Catalog(
            [work, personal],
            [Entry("Work note", "C:\\work", work, null), Entry("Personal note", "C:\\personal", personal, null)]);

        BookmarkManagerPresentation presentation = Present(
            catalog,
            Browsing(new BookmarkBrowseContext(string.Empty, BookmarkCategoryFilter.For(personal), null)));

        Assert.IsFalse(presentation.Browse.Categories[0].IsSelected);
        Assert.IsFalse(presentation.Browse.Categories[1].IsSelected);
        Assert.IsFalse(presentation.Browse.Categories[2].IsSelected);
        Assert.IsTrue(presentation.Browse.Categories[3].IsSelected);
        Assert.IsNotNull(presentation.Browse.Categories[3].Selection);
        Assert.HasCount(1, presentation.Browse.Rows);
        Assert.AreEqual("Personal note", presentation.Browse.Rows[0].NameText);
        Assert.AreEqual("Personal", presentation.Browse.Rows[0].CategoryText);
    }

    /// <summary>Proves pending and failed navigation both retain the immutable navigation selection.</summary>
    [TestMethod]
    public void PresentWhenNavigationStateChangesRetainsNavigationSelection()
    {
        BookmarkEntry entry = Entry("Offline", "C:\\offline", null, null);
        BookmarkSelection selection = new(entry);
        BookmarkBrowseContext context = new(string.Empty, BookmarkCategoryFilter.All, selection);
        BookmarksEditorState pending = Construct<BookmarkNavigationPending>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkSelection)], [context, selection]);
        BookmarksEditorState failed = Construct<BookmarkNavigationFailed>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkSelection), typeof(PaneActivity)],
            [context, selection, Cancelled(entry.Path.Value)]);

        BookmarkManagerPresentation pendingPresentation = Present(Catalog([], [entry]), pending);
        BookmarkManagerPresentation failedPresentation = Present(Catalog([], [entry]), failed);

        Assert.AreSame(selection, pendingPresentation.Details.NavigationSelection);
        Assert.AreSame(selection, failedPresentation.Details.NavigationSelection);
    }

    /// <summary>Proves no selected option is exposed when a draft choice is stale or absent.</summary>
    [TestMethod]
    public void BookmarkEditorDetailsWhenNoChoiceIsSelectedReturnsNullSelections()
    {
        BookmarkCategoryOption category = new(
            BookmarkCategoryFilter.Uncategorized,
            "Uncategorized",
            null,
            BookmarkOptionSelection.NotSelected);
        BookmarkShortcutOption shortcut = new(
            null,
            "BookmarkShortcutUnassigned",
            BookmarkOptionSelection.NotSelected);
        BookmarkEditorDetails details = new(
            BookmarksEditorState.Closed,
            [category],
            [shortcut],
            "BookmarkStatusReady");

        Assert.IsNull(details.SelectedDraftCategory);
        Assert.IsNull(details.SelectedShortcut);
    }

    /// <summary>Proves every closed empty-content state retains its localized resource key.</summary>
    [TestMethod]
    public void BookmarkEmptyContentStatesExposeStableResourceKeys()
    {
        Assert.AreEqual("KeyLabelUnmapped", BookmarkEmptyContent.Hidden.ResourceKey);
        Assert.AreEqual("BookmarkEmptyNoBookmarks", BookmarkEmptyContent.NoBookmarks.ResourceKey);
        Assert.AreEqual("BookmarkEmptyNoMatches", BookmarkEmptyContent.NoMatches.ResourceKey);
    }

    private static BookmarkManagerPresentation Present(BookmarkCatalog catalog, BookmarksEditorState state)
    {
        SettingsSnapshot snapshot = Construct<SettingsSnapshot>(
            [typeof(UserSettings), typeof(SettingsEditorState), typeof(BookmarksEditorState), typeof(SettingsPersistenceState)],
            [UserSettings.Create(ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, catalog), SettingsEditorState.Bookmarks, state, SettingsPersistenceState.Succeeded]);
        return BookmarkManagerPresenter.Present(snapshot, "All bookmarks", "Uncategorized");
    }

    private static BookmarksBrowsing Browsing(BookmarkBrowseContext context)
    {
        return Construct<BookmarksBrowsing>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkEditorProblem)], [context, null]);
    }

    private static BookmarkCatalog Catalog(IReadOnlyList<BookmarkCategoryName> categories, IReadOnlyList<BookmarkEntry> entries)
    {
        return Assert.IsInstanceOfType<BookmarkCatalogAccepted>(BookmarkCatalog.Create(categories, entries)).Catalog;
    }

    private static BookmarkCategoryName Category(string value)
    {
        return Assert.IsInstanceOfType<BookmarkCategoryNameAccepted>(BookmarkCategoryName.Parse(value)).Name;
    }

    private static BookmarkEntry Entry(string name, string path, BookmarkCategoryName? category, BookmarkShortcutSlot? shortcut)
    {
        BookmarkDisplayName displayName = Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(BookmarkDisplayName.Parse(name)).Name;
        BookmarkPath bookmarkPath = Assert.IsInstanceOfType<BookmarkPathAccepted>(BookmarkPath.Parse(path)).Path;
        return BookmarkEntry.Create(displayName, bookmarkPath, category, shortcut);
    }

    private static T Construct<T>(Type[] parameterTypes, object?[] values)
    {
        ConstructorInfo constructor = typeof(T).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic, null, parameterTypes, null)
            ?? throw new AssertFailedException($"The {typeof(T).Name} constructor was not found.");
        return (T)constructor.Invoke(values);
    }

    private static PaneReadCancelled Cancelled(FileSystemPath target)
    {
        return Construct<PaneReadCancelled>([typeof(FileSystemPath)], [target]);
    }
}
