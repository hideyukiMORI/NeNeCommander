using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the expanded catalog retains the sole ordered complete-value settings writer.</summary>
[TestClass]
public sealed class ColorSchemeSettingsQueueTests
{
    /// <summary>Proves every new and existing scheme preserves language, bookmarks, visibility, hints and both sorts.</summary>
    [TestMethod]
    public async Task SelectWhenEveryCatalogSchemeIsChosenWritesOnlyTheSelectedFieldAsync()
    {
        BookmarkEntry entry = BookmarkEntry.Create(
            Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(BookmarkDisplayName.Parse("Saved")).Name,
            Assert.IsInstanceOfType<BookmarkPathAccepted>(BookmarkPath.Parse("C:\\saved")).Path, null, BookmarkShortcutSlot.One);
        BookmarkCatalog catalog = CommanderSessionTestValues.Catalog(entry);
        PaneSortPreferences sorts = PaneSortPreferences.Create(
            PaneSortOrder.Create(SortKey.Size, SortDirection.Descending),
            PaneSortOrder.Create(SortKey.Modified, SortDirection.Ascending));
        UserSettings initial = UserSettings.Create(
            DisplayPreferences.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, KeyHintVisibility.Hidden, InterfaceLanguage.Japanese), catalog, sorts, null);
        SettingsReadOutcome read = SettingsReadOutcome.Read(initial);
        ScriptedSettingsStore store = new(read);
        SettingsSession session = new(store, read, static _ => { });
        RecordingCommanderObserver observer = new();
        foreach (ColorScheme scheme in ColorScheme.All)
        {
            store.PlanWrite().SetResult(SettingsWriteOutcome.Succeeded());
            await session.SelectColorSchemeAsync(scheme, observer, CancellationToken.None);
            Assert.AreSame(scheme, session.Current.Settings.Display.ColorScheme);
        }
        Assert.HasCount(129, store.Writes);
        for (int index = 0; index < store.Writes.Count; index++)
        {
            UserSettings written = store.Writes[index];
            Assert.AreSame(ColorScheme.All[index], written.Display.ColorScheme);
            Assert.AreSame(InterfaceLanguage.Japanese, written.Display.InterfaceLanguage);
            Assert.AreSame(HiddenItemVisibility.Shown, written.Display.HiddenItemVisibility);
            Assert.AreSame(KeyHintVisibility.Hidden, written.Display.KeyHintVisibility);
            Assert.AreSame(catalog, written.Bookmarks);
            Assert.AreEqual(sorts, written.PaneSortOrders);
        }
        await session.StopAsync();
    }
}
