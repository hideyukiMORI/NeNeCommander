using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the application session coordinates settings modality without moving pane ownership.</summary>
[TestClass]
public sealed class CommanderSessionTests
{
    /// <summary>Proves opening settings freezes both pane navigation paths until Escape closes it.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSettingsAreOpenFreezesPanesUntilEscapeAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();

        CommanderSnapshot opened = await session.HandleAsync(
            UserIntent.OpenSettings,
            observer,
            CancellationToken.None);
        CommanderSnapshot frozen = await session.NavigateAsync(
            PaneSide.Left,
            CommanderSessionTestValues.ParsePath("C:\\frozen"),
            CancellationToken.None);

        Assert.AreSame(SettingsEditorState.Open, opened.Settings.Editor);
        Assert.AreSame(SettingsEditorState.Open, frozen.Settings.Editor);
        Assert.IsEmpty(left.Requests);

        CommanderSnapshot closed = await session.HandleAsync(
            UserIntent.Escape,
            observer,
            CancellationToken.None);
        Assert.AreSame(SettingsEditorState.Closed, closed.Settings.Editor);
    }

    /// <summary>Proves a typed settings selection saves through the settings owner and not a pane transition.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenLaunchDefaultChangesKeepsCurrentPaneStateAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, store).CreateSession();
        RecordingCommanderObserver observer = new();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenSettings, observer, CancellationToken.None);

        Task<CommanderSnapshot> changing = session.HandleAsync(
            UserIntent.SelectLaunchHiddenItemVisibility(HiddenItemVisibility.Shown),
            observer,
            CancellationToken.None);

        Assert.AreSame(HiddenItemVisibility.Shown, session.Current.Settings.Settings.Display.HiddenItemVisibility);
        PaneContentListed leftContent = Assert.IsInstanceOfType<PaneContentListed>(
            session.Current.Panes.Left.Content);
        Assert.AreSame(HiddenItemVisibility.Hidden, leftContent.State.HiddenItemVisibility);
        Assert.AreSame(PaneContent.Absent, session.Current.Panes.Right.Content);
        CommanderSnapshot changed = await changing;
        _ = Assert.IsInstanceOfType<SettingsPersistencePending>(changed.Settings.Persistence);
        write.SetResult(SettingsWriteOutcome.Succeeded());
        await session.StopAsync(null, new RecordingCommanderObserver());
        _ = Assert.IsInstanceOfType<SettingsPersistenceSucceeded>(session.Current.Settings.Persistence);
    }

    /// <summary>Proves an existing name modal keeps ownership when the settings shortcut arrives.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenNameModalOwnsInputRefusesToOpenSettingsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        CommanderSnapshot awaitingName = await session.HandleAsync(
            UserIntent.Rename,
            observer,
            CancellationToken.None);

        CommanderSnapshot refused = await session.HandleAsync(
            UserIntent.OpenSettings,
            observer,
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<OperationAwaitingName>(awaitingName.Panes.Operation);
        _ = Assert.IsInstanceOfType<OperationAwaitingName>(refused.Panes.Operation);
        Assert.AreSame(SettingsEditorState.Closed, refused.Settings.Editor);
    }

    /// <summary>Proves the transfer-conflict modal keeps ownership when the settings shortcut arrives.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenConflictModalOwnsInputRefusesToOpenSettingsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        using FileOperationGateway gateway = new(port);
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        DirectoryListing leftListing = CommanderSessionTestValues.Listing("C:\\left", "item.txt");
        DirectoryListing rightListing = CommanderSessionTestValues.Listing("C:\\right", "item.txt");
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        _ = await session.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        FileInspectionSucceeded inspection = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            CommanderSessionTestValues.Inspection(leftListing.Entries[0].Path));
        TransferConflict conflict = TransferConflict.Create(
            inspection.Snapshot,
            rightListing.Entries[0].Path,
            CommanderSessionTestValues.ParsePath("C:\\right\\item (2).txt"));
        port.EnqueueInspection(inspection);
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        _ = await session.HandleAsync(UserIntent.Copy, observer, CancellationToken.None);

        CommanderSnapshot refused = await session.HandleAsync(
            UserIntent.OpenSettings,
            observer,
            CancellationToken.None);

        _ = Assert.IsInstanceOfType<OperationAwaitingConflict>(refused.Panes.Operation);
        Assert.AreSame(SettingsEditorState.Closed, refused.Settings.Editor);
    }

    /// <summary>Proves rapid selector intents return after enqueue and persist in their canonical order.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenSettingsSelectionsOverlapEnqueuesBothWithoutAwaitingIoAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> firstWrite = store.PlanWrite();
        TaskCompletionSource<SettingsWriteOutcome> secondWrite = store.PlanWrite();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, store).CreateSession();
        RecordingCommanderObserver observer = new();

        _ = await session.HandleAsync(UserIntent.OpenSettings, observer, CancellationToken.None);
        CommanderSnapshot ignored = await session.HandleAsync(
            UserIntent.Refresh,
            observer,
            CancellationToken.None);
        Task<CommanderSnapshot> first = session.HandleAsync(
            UserIntent.SelectColorScheme(ColorScheme.Ubuntu),
            observer,
            CancellationToken.None);
        Task<CommanderSnapshot> second = session.HandleAsync(
            UserIntent.SelectColorScheme(ColorScheme.Dracula),
            observer,
            CancellationToken.None);

        Assert.IsTrue(first.IsCompletedSuccessfully);
        Assert.IsTrue(second.IsCompletedSuccessfully);
        Assert.AreSame(SettingsEditorState.Open, ignored.Settings.Editor);
        Assert.IsEmpty(observer.Settings);
        Assert.HasCount(1, store.Writes);
        Assert.AreSame(ColorScheme.Dracula, session.Current.Settings.Settings.Display.ColorScheme);
        firstWrite.SetResult(SettingsWriteOutcome.Succeeded());
        await store.WaitForWriteCountAsync(2);
        Assert.AreSame(ColorScheme.Dracula, session.Current.Settings.Settings.Display.ColorScheme);
        secondWrite.SetResult(SettingsWriteOutcome.Succeeded());
        await session.StopAsync(null, new RecordingCommanderObserver());
        Assert.AreSame(ColorScheme.Ubuntu, store.Writes[0].Display.ColorScheme);
        Assert.AreSame(ColorScheme.Dracula, store.Writes[1].Display.ColorScheme);
    }
}
