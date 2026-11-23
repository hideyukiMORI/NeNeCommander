using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the picker commits only an owned explicit choice through the existing settings queue.</summary>
[TestClass]
public sealed class ColorSchemePickerSessionTests
{
    /// <summary>Proves each entry returns to its own surface and cancellation never changes accepted settings.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task CancelWhenPickerIsOwnedReturnsWithoutWritingAsync(int origin)
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        if (origin == 1) { _ = session.Open(); }
        SettingsSnapshot before = session.Current;
        session.OpenColorSchemes();
        ColorSchemePickerOpen open = Assert.IsInstanceOfType<ColorSchemePickerOpen>(session.Current.Editor);
        session.OpenColorSchemes();
        Assert.AreSame(open, session.Current.Editor);
        await session.HandleEditorIntentAsync(UserIntent.CancelColorSchemePicker(open), new RecordingCommanderObserver(), CancellationToken.None);
        Assert.AreSame(before.Editor, session.Current.Editor);
        Assert.AreSame(before.Settings, session.Current.Settings);
        Assert.AreSame(before.Persistence, session.Current.Persistence);
        Assert.IsEmpty(store.Writes);
        await session.StopAsync();
    }

    /// <summary>Proves a previous open identity and an unqualified legacy selection cannot commit a preview.</summary>
    [TestMethod]
    public async Task HandleWhenOldPickerOrUnqualifiedSelectionArrivesKeepsCurrentPickerAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        RecordingCommanderObserver observer = new();
        session.OpenColorSchemes();
        ColorSchemePickerOpen old = Assert.IsInstanceOfType<ColorSchemePickerOpen>(session.Current.Editor);
        _ = session.Close();
        session.OpenColorSchemes();
        ColorSchemePickerOpen current = Assert.IsInstanceOfType<ColorSchemePickerOpen>(session.Current.Editor);
        await session.HandleEditorIntentAsync(UserIntent.SubmitColorScheme(old, ColorScheme.Dracula), observer, CancellationToken.None);
        await session.HandleEditorIntentAsync(UserIntent.CancelColorSchemePicker(old), observer, CancellationToken.None);
        await session.HandleEditorIntentAsync(UserIntent.SelectColorScheme(ColorScheme.Dracula), observer, CancellationToken.None);
        await session.HandleEditorIntentAsync(UserIntent.Escape, observer, CancellationToken.None);
        Assert.AreSame(current, session.Current.Editor);
        Assert.AreSame(UserSettings.Default, session.Current.Settings);
        Assert.IsEmpty(store.Writes);
        await session.StopAsync();
    }

    /// <summary>Proves accepting the existing theme closes without creating a redundant settings revision.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task SubmitWhenThemeAlreadyAcceptedClosesWithoutWritingAsync(int origin)
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        if (origin == 1) { _ = session.Open(); }
        SettingsSnapshot before = session.Current;
        session.OpenColorSchemes();
        ColorSchemePickerOpen open = Assert.IsInstanceOfType<ColorSchemePickerOpen>(session.Current.Editor);
        await session.HandleEditorIntentAsync(UserIntent.SubmitColorScheme(open, before.Settings.Display.ColorScheme), new RecordingCommanderObserver(), CancellationToken.None);
        Assert.AreSame(before.Editor, session.Current.Editor);
        Assert.AreSame(before.Settings, session.Current.Settings);
        Assert.AreSame(before.Persistence, session.Current.Persistence);
        Assert.IsEmpty(store.Writes);
        await session.StopAsync();
    }

    /// <summary>Proves acceptance closes before I/O and preserves newer preferences across ordered and failed writes.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task SubmitWhenEarlierSaveIsPendingKeepsLatestSettingsAndFailureAsync(int outcome)
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        RecordingCommanderObserver observer = new();
        TaskCompletionSource<SettingsWriteOutcome> earlier = store.PlanWrite();
        TaskCompletionSource<SettingsWriteOutcome> selected = store.PlanWrite();
        Task changingHints = session.SelectKeyHintVisibilityAsync(KeyHintVisibility.Hidden, observer, CancellationToken.None);
        session.OpenColorSchemes();
        ColorSchemePickerOpen open = Assert.IsInstanceOfType<ColorSchemePickerOpen>(session.Current.Editor);
        Task accepting = session.HandleEditorIntentAsync(UserIntent.SubmitColorScheme(open, ColorScheme.Dracula), observer, CancellationToken.None);
        Assert.AreSame(SettingsEditorState.Closed, session.Current.Editor);
        Assert.AreSame(ColorScheme.Dracula, session.Current.Settings.Display.ColorScheme);
        Assert.AreSame(KeyHintVisibility.Hidden, session.Current.Settings.Display.KeyHintVisibility);
        Assert.IsFalse(accepting.IsCompleted);
        earlier.SetResult(SettingsWriteOutcome.Succeeded());
        await changingHints;
        await store.WaitForWriteCountAsync(2);
        _ = Assert.IsInstanceOfType<SettingsPersistencePending>(session.Current.Persistence);
        selected.SetResult(outcome == 0 ? SettingsWriteOutcome.Succeeded() : SettingsWriteOutcome.Rejected(
            SettingsWriteFailureKind.IoFailure, SettingsDirectoryEffect.NotAttempted, SettingsWriteEffect.None));
        await accepting;
        Assert.AreEqual(session.Current.Settings, store.Writes[1]);
        Assert.AreSame(KeyHintVisibility.Hidden, store.Writes[1].Display.KeyHintVisibility);
        Assert.AreSame(ColorScheme.Dracula, session.Current.Settings.Display.ColorScheme);
        Assert.AreEqual(outcome == 0, session.Current.Persistence is SettingsPersistenceSucceeded);
        Assert.AreEqual(outcome == 1, session.Current.Persistence is SettingsPersistenceFailed);
        await session.StopAsync();
    }

    /// <summary>Proves the bookmark editor cannot be replaced by a theme picker through the settings owner.</summary>
    [TestMethod]
    public async Task OpenWhenBookmarksOwnEditorPreservesTheirStateAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        SettingsSnapshot before = session.OpenBookmarks();
        session.OpenColorSchemes();
        Assert.AreSame(before.Editor, session.Current.Editor);
        Assert.AreSame(before.BookmarksEditor, session.Current.BookmarksEditor);
        Assert.IsEmpty(store.Writes);
        await session.StopAsync();
    }
}
