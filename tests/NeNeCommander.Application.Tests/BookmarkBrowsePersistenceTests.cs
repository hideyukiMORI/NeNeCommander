using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves selecting the first visible bookmark stays a browse action, not a settings write.</summary>
[TestClass]
public sealed class BookmarkBrowsePersistenceTests
{
    /// <summary>Proves focus selection, search and filtering preserve the catalog without saving.</summary>
    [TestMethod]
    public async Task SelectingAVisibleResultDoesNotQueuePersistenceAsync()
    {
        BookmarkEntry entry = BookmarkEditorTestValues.Entry("First", "C:\\first", null);
        BookmarkCatalog catalog = BookmarkEditorTestValues.Catalog([], [entry]);
        UserSettings settings = UserSettings.Create(DisplayPreferences.Create(ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, KeyHintVisibility.Shown), catalog, PaneSortPreferences.Default);
        ScriptedSettingsStore store = new(SettingsReadOutcome.Read(settings));
        SettingsSession session = new(store, SettingsReadOutcome.Read(settings), static _ => { });
        _ = session.OpenBookmarks();
        RecordingCommanderObserver observer = new();

        foreach (BookmarkEditorAction action in new[]
        {
            BookmarkEditorAction.Select(new BookmarkSelection(entry)),
            BookmarkEditorAction.Search("First"),
            BookmarkEditorAction.Filter(BookmarkCategoryFilter.Uncategorized),
        })
        {
            await session.ApplyBookmarkEditorAction(action, BookmarkEditorTestValues.EmptyDefaults(), observer, CancellationToken.None);
        }

        Assert.AreSame(settings, session.Current.Settings);
        Assert.IsEmpty(store.Writes);
    }
}
