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

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Creates deterministic inputs for the related projection and input contracts.</summary>
internal static class BookmarkPresenterTestContext
{
    internal static BookmarkManagerPresentation Present(
        BookmarkCatalog catalog,
        BookmarksEditorState state)
    {
        return BookmarkManagerPresenter.Present(
            Snapshot(catalog, state),
            "All bookmarks",
            "Uncategorized");
    }

    internal static BookmarksBrowsing Browsing(
        string searchText,
        BookmarkCategoryFilter filter)
    {
        return Construct<BookmarksBrowsing>(
            [typeof(BookmarkBrowseContext), typeof(BookmarkEditorProblem)],
            [new BookmarkBrowseContext(searchText, filter, null), null]);
    }

    internal static SettingsSnapshot Snapshot(
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
                UserSettings.Create(ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, catalog, PaneSortPreferences.Default),
                SettingsEditorState.Bookmarks,
                state,
                SettingsPersistenceState.Succeeded,
            ]);
    }

    internal static BookmarkCatalog Catalog(
        IReadOnlyList<BookmarkCategoryName> categories,
        IReadOnlyList<BookmarkEntry> entries)
    {
        return Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create(categories, entries)).Catalog;
    }

    internal static BookmarkCategoryName Category(string value)
    {
        return Assert.IsInstanceOfType<BookmarkCategoryNameAccepted>(
            BookmarkCategoryName.Parse(value)).Name;
    }

    internal static BookmarkEntry Entry(
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

    internal static T Construct<T>(Type[] parameterTypes, object?[] values)
    {
        ConstructorInfo constructor = typeof(T).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            parameterTypes,
            null) ?? throw new AssertFailedException($"The {typeof(T).Name} constructor was not found.");
        return (T)constructor.Invoke(values);
    }

    internal static PaneReadFailed Failed(
        FileSystemPath target,
        FileOperationFailureKind failure)
    {
        return Construct<PaneReadFailed>(
            [typeof(FileSystemPath), typeof(FileOperationFailureKind)],
            [target, failure]);
    }

    internal static PaneReadCancelled Cancelled(FileSystemPath target)
    {
        return Construct<PaneReadCancelled>([typeof(FileSystemPath)], [target]);
    }
}
