using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves language preference admission, selected state and existing persistence ownership.</summary>
[TestClass]
public sealed class InterfaceLanguageSettingsTests
{
    /// <summary>Proves only the open editor admits a preference and returns before its disk write.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenLanguageChoiceArrivesRequiresEditorAndPreservesPanesAsync()
    {
        using DualPaneSessionTestContext context = DualPaneSessionTestContext.Create();
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession settings = new(store, SettingsReadOutcome.Absent(), static _ => { });
        CommanderSession commander = new(context.Panes, settings, Scopes());
        RecordingCommanderObserver observer = new();
        UserIntent choice = UserIntent.SelectInterfaceLanguage(InterfaceLanguage.SimplifiedChinese);

        _ = await commander.HandleAsync(choice, observer, CancellationToken.None);
        Assert.IsEmpty(store.Writes);
        Assert.AreSame(InterfaceLanguage.System, settings.Current.Settings.Display.InterfaceLanguage);
        _ = await commander.HandleAsync(UserIntent.OpenSettings, observer, CancellationToken.None);
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        CommanderSnapshot changed = await commander.HandleAsync(choice, observer, CancellationToken.None);

        Assert.AreSame(InterfaceLanguage.SimplifiedChinese, changed.Settings.Settings.Display.InterfaceLanguage);
        _ = Assert.IsInstanceOfType<SettingsPersistencePending>(changed.Settings.Persistence);
        Assert.HasCount(1, store.Writes);
        Assert.IsEmpty(context.Left.Requests);
        Assert.IsEmpty(context.Right.Requests);
        Assert.AreSame(PaneSide.Left, changed.Panes.ActiveSide);
        write.SetResult(SettingsWriteOutcome.Succeeded());
        await commander.StopAsync(null, new RecordingCommanderObserver());
    }

    /// <summary>Proves a failed save keeps the current choice and exposes the existing failure channel.</summary>
    [TestMethod]
    public async Task SelectWhenLanguageSaveFailsRetainsChoiceAndWarningAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        Task pending = session.SelectInterfaceLanguageAsync(
            InterfaceLanguage.SimplifiedChinese, new RecordingCommanderObserver(), CancellationToken.None);

        Assert.AreSame(InterfaceLanguage.SimplifiedChinese, session.Current.Settings.Display.InterfaceLanguage);
        write.SetResult(SettingsWriteOutcome.Rejected(
            SettingsWriteFailureKind.IoFailure, SettingsDirectoryEffect.NotAttempted, SettingsWriteEffect.None));
        await pending;

        Assert.AreSame(InterfaceLanguage.SimplifiedChinese, session.Current.Settings.Display.InterfaceLanguage);
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

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => session.SelectInterfaceLanguageAsync(
            InterfaceLanguage.SimplifiedChinese, new RecordingCommanderObserver(), CancellationToken.None));

        Assert.AreSame(UserSettings.Default, session.Current.Settings);
        Assert.IsEmpty(store.Writes);
    }

    private static TransientScopeOwners Scopes()
    {
        return new TransientScopeOwners(
            new AddressEditorSession(), new CommandPaletteSession(), new WindowAdjustmentSession(),
            new LocationsSession(new ScriptedDriveCatalog(), new ScriptedWslDistributionCatalog()), new KeyboardHelpSession());
    }
}
