using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves footer preference admission, immediate state and existing persistence ownership.</summary>
[TestClass]
public sealed class KeyHintSettingsTests
{
    /// <summary>Proves only the open editor admits a preference and returns before its disk write.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenKeyHintChoiceArrivesRequiresEditorAndPreservesPanesAsync()
    {
        using DualPaneSessionTestContext context = DualPaneSessionTestContext.Create();
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession settings = new(store, SettingsReadOutcome.Absent(), static _ => { });
        CommanderSession commander = new(context.Panes, settings, Scopes(), new CommanderBookmarkCommands(context.Panes, settings));
        RecordingCommanderObserver observer = new();
        UserIntent choice = UserIntent.SelectKeyHintVisibility(KeyHintVisibility.Hidden);

        _ = await commander.HandleAsync(choice, observer, CancellationToken.None);
        Assert.IsEmpty(store.Writes);
        Assert.AreSame(KeyHintVisibility.Shown, settings.Current.Settings.Display.KeyHintVisibility);
        _ = await commander.HandleAsync(UserIntent.OpenSettings, observer, CancellationToken.None);
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        CommanderSnapshot changed = await commander.HandleAsync(choice, observer, CancellationToken.None);

        Assert.AreSame(KeyHintVisibility.Hidden, changed.Settings.Settings.Display.KeyHintVisibility);
        _ = Assert.IsInstanceOfType<SettingsPersistencePending>(changed.Settings.Persistence);
        Assert.HasCount(1, store.Writes);
        Assert.IsEmpty(context.Left.Requests);
        Assert.IsEmpty(context.Right.Requests);
        Assert.AreSame(PaneSide.Left, changed.Panes.ActiveSide);
        write.SetResult(SettingsWriteOutcome.Succeeded());
        await commander.StopAsync(null, new RecordingCommanderObserver());
    }

    /// <summary>Proves a failed save keeps the immediate choice and exposes the existing failure channel.</summary>
    [TestMethod]
    public async Task SelectWhenKeyHintSaveFailsRetainsChoiceAndWarningAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        Task pending = session.SelectKeyHintVisibilityAsync(
            KeyHintVisibility.Hidden, new RecordingCommanderObserver(), CancellationToken.None);

        Assert.AreSame(KeyHintVisibility.Hidden, session.Current.Settings.Display.KeyHintVisibility);
        write.SetResult(SettingsWriteOutcome.Rejected(
            SettingsWriteFailureKind.IoFailure, SettingsDirectoryEffect.NotAttempted, SettingsWriteEffect.None));
        await pending;

        Assert.AreSame(KeyHintVisibility.Hidden, session.Current.Settings.Display.KeyHintVisibility);
        SettingsPersistenceFailed failure = Assert.IsInstanceOfType<SettingsPersistenceFailed>(session.Current.Persistence);
        Assert.AreSame(SettingsWriteFailureKind.IoFailure, failure.Rejection.Failure);
        Assert.AreSame(ColorScheme.NeNeDark, store.Writes[0].Display.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Hidden, store.Writes[0].Display.HiddenItemVisibility);
    }

    /// <summary>Proves the new selection cannot restart a stopped writer or change its snapshot.</summary>
    [TestMethod]
    public async Task SelectWhenOwnerHasStoppedRejectsBeforeChangingOrWritingAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        await session.StopAsync();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => session.SelectKeyHintVisibilityAsync(
            KeyHintVisibility.Hidden, new RecordingCommanderObserver(), CancellationToken.None));

        Assert.AreSame(UserSettings.Default, session.Current.Settings);
        Assert.IsEmpty(store.Writes);
    }

    private static TransientScopeOwners Scopes()
    {
        return new TransientScopeOwners(new PaneScopeOwners(new AddressEditorSession(), new LocationsSession(new ScriptedDriveCatalog(), new ScriptedWslDistributionCatalog()), new PaneFilterSession()), new CommandPaletteSession(), new WindowAdjustmentSession(), new KeyboardHelpSession());
    }
}
