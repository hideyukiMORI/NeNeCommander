using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves final placement joins the complete settings queue and normal-close drain.</summary>
[TestClass]
public sealed class WindowPlacementSettingsTests
{
    /// <summary>Proves closing waits for a prior selection then writes current preferences and placement once.</summary>
    [TestMethod]
    public async Task StopAsyncWhenPriorWriteIsPendingPreservesLatestPreferencesAndDrainsAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> first = store.PlanWrite();
        TaskCompletionSource<SettingsWriteOutcome> second = store.PlanWrite();
        SettingsSession settings = new(store, SettingsReadOutcome.Absent(), static _ => { });
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        CommanderSession commander = Commander(settings, gateway);
        RecordingCommanderObserver observer = new();
        Task selecting = settings.SelectInterfaceLanguageAsync(InterfaceLanguage.SimplifiedChinese, observer, CancellationToken.None);
        SavedWindowPlacement placement = Placement();

        Task stopping = commander.StopAsync(placement, observer);

        Assert.IsFalse(stopping.IsCompleted);
        Assert.HasCount(1, store.Writes);
        first.SetResult(SettingsWriteOutcome.Succeeded());
        await selecting;
        await store.WaitForWriteCountAsync(2);
        Assert.IsFalse(stopping.IsCompleted);
        Assert.AreSame(InterfaceLanguage.SimplifiedChinese, store.Writes[1].Display.InterfaceLanguage);
        Assert.AreEqual(placement, store.Writes[1].WindowPlacement);
        second.SetResult(SettingsWriteOutcome.Succeeded());
        await stopping;
        await commander.StopAsync(placement, observer);
        Assert.HasCount(2, store.Writes);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            settings.SelectColorSchemeAsync(ColorScheme.Dracula, observer, CancellationToken.None));
    }

    /// <summary>Proves absent, unchanged and rejected-startup placement never cause an automatic write.</summary>
    [TestMethod]
    public async Task StopAsyncWhenNoChangedAdmittedPlacementExistsDoesNotWriteAsync()
    {
        SavedWindowPlacement placement = Placement();
        UserSettings initial = UserSettings.Create(DisplayPreferences.Default, BookmarkCatalog.Empty, PaneSortPreferences.Default, placement);
        foreach (SettingsReadOutcome outcome in new[]
        {
            SettingsReadOutcome.Read(initial), SettingsReadOutcome.Rejected(SettingsReadFailureKind.Malformed),
        })
        {
            ScriptedSettingsStore store = new(outcome);
            SettingsSession settings = new(store, outcome, static _ => { });
            using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
            await Commander(settings, gateway).StopAsync(placement, new RecordingCommanderObserver());
            Assert.IsEmpty(store.Writes);
        }
        ScriptedSettingsStore absentStore = new(SettingsReadOutcome.Absent());
        SettingsSession absent = new(absentStore, SettingsReadOutcome.Absent(), static _ => { });
        using FileOperationGateway absentGateway = new(ScriptedFileOperationPort.Create(null, null));
        await Commander(absent, absentGateway).StopAsync(null, new RecordingCommanderObserver());
        Assert.IsEmpty(absentStore.Writes);
    }

    /// <summary>Proves a failed placement save remains failed while the normal queue still becomes terminal.</summary>
    [TestMethod]
    public async Task StopAsyncWhenPlacementWriteFailsRetainsTypedFailureAndClosesQueueAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        SettingsSession settings = new(store, SettingsReadOutcome.Absent(), static _ => { });
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        RecordingCommanderObserver observer = new();
        Task stopping = Commander(settings, gateway).StopAsync(Placement(), observer);
        write.SetResult(SettingsWriteOutcome.Rejected(SettingsWriteFailureKind.Unauthorized,
            SettingsDirectoryEffect.NotAttempted, SettingsWriteEffect.None));
        await stopping;

        _ = Assert.IsInstanceOfType<SettingsPersistenceFailed>(settings.Current.Persistence);
        Assert.AreEqual(Placement(), settings.Current.Settings.WindowPlacement);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            settings.SelectColorSchemeAsync(ColorScheme.Dracula, observer, CancellationToken.None));
    }

    /// <summary>Proves later preference changes retain a previously saved placement.</summary>
    [TestMethod]
    public async Task SelectWhenPlacementExistsPreservesItAcrossAllDisplayChoicesAsync()
    {
        SavedWindowPlacement placement = Placement();
        UserSettings initial = UserSettings.Create(DisplayPreferences.Default, BookmarkCatalog.Empty, PaneSortPreferences.Default, placement);
        SettingsReadOutcome outcome = SettingsReadOutcome.Read(initial);
        ScriptedSettingsStore store = new(outcome);
        SettingsSession settings = new(store, outcome, static _ => { });
        RecordingCommanderObserver observer = new();
        store.PlanWrite().SetResult(SettingsWriteOutcome.Succeeded());
        await settings.SelectColorSchemeAsync(ColorScheme.Dracula, observer, CancellationToken.None);
        store.PlanWrite().SetResult(SettingsWriteOutcome.Succeeded());
        await settings.SelectLaunchHiddenItemVisibilityAsync(HiddenItemVisibility.Shown, observer, CancellationToken.None);
        store.PlanWrite().SetResult(SettingsWriteOutcome.Succeeded());
        await settings.SelectKeyHintVisibilityAsync(KeyHintVisibility.Hidden, observer, CancellationToken.None);
        store.PlanWrite().SetResult(SettingsWriteOutcome.Succeeded());
        await settings.SelectInterfaceLanguageAsync(InterfaceLanguage.English, observer, CancellationToken.None);
        BookmarkEntry entry = BookmarkEntry.Create(
            Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(BookmarkDisplayName.Parse("Saved")).Name,
            Assert.IsInstanceOfType<BookmarkPathAccepted>(BookmarkPath.Parse("C:\\saved")).Path, null, BookmarkShortcutSlot.One);
        store.PlanWrite().SetResult(SettingsWriteOutcome.Succeeded());
        await settings.SaveBookmarkCatalogAsync(CommanderSessionTestValues.Catalog(entry), observer, CancellationToken.None);
        store.PlanWrite().SetResult(SettingsWriteOutcome.Succeeded());
        await settings.SavePaneSortOrderAsync(PaneSide.Right,
            PaneSortOrder.Create(SortKey.Size, SortDirection.Descending), observer, CancellationToken.None);
        await settings.StopAsync();

        Assert.HasCount(6, store.Writes);
        foreach (UserSettings written in store.Writes)
        {
            Assert.AreEqual(placement, written.WindowPlacement);
        }
        Assert.AreSame(ColorScheme.Dracula, store.Writes[3].Display.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, store.Writes[3].Display.HiddenItemVisibility);
        Assert.AreSame(KeyHintVisibility.Hidden, store.Writes[3].Display.KeyHintVisibility);
    }

    /// <summary>Proves a defect in final persistence is observed and cannot leave settings admission open.</summary>
    [TestMethod]
    public async Task StopAsyncWhenPlacementWriteFaultsStillDrainsAndClosesQueueAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        int observations = 0;
        SettingsSession settings = new(store, SettingsReadOutcome.Absent(), _ => observations++);
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        RecordingCommanderObserver observer = new();
        Task stopping = Commander(settings, gateway).StopAsync(Placement(), observer);
        InvalidOperationException defect = new("Injected final placement write defect.");
        write.SetException(defect);

        InvalidOperationException failure = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => stopping);

        Assert.AreSame(defect, failure);
        Assert.AreEqual(1, observations);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            settings.SelectColorSchemeAsync(ColorScheme.Dracula, observer, CancellationToken.None));
    }

    private static SavedWindowPlacement Placement()
    {
        return SavedWindowPlacement.Create(WindowBounds.Create(-1200, 40, 1000, 700), WindowPresenterState.Maximized);
    }

    private static CommanderSession Commander(SettingsSession settings, FileOperationGateway gateway)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        CommanderSessionTestComposition composition = new(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        _ = composition.CreateSession(out NeNeCommander.Application.Panes.DualPaneSession panes);
        return new CommanderSession(panes, settings, new TransientScopeOwners(new PaneScopeOwners(new AddressEditorSession(), new LocationsSession(new ScriptedDriveCatalog(), new ScriptedWslDistributionCatalog()), new PaneFilterSession()), new CommandPaletteSession(), new WindowAdjustmentSession(), new KeyboardHelpSession()), new CommanderBookmarkCommands(panes, settings));
    }
}
