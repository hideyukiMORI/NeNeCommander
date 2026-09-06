using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

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
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();

        CommanderSnapshot opened = await session.HandleAsync(
            UserIntent.OpenSettings,
            observer,
            CancellationToken.None);
        CommanderSnapshot frozen = await session.NavigateAsync(
            PaneSide.Left,
            ParsePath("C:\\frozen"),
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
        using FileOperationGateway gateway = CreateGateway();
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        CommanderSession session = CreateSession(left, right, gateway, store);
        RecordingCommanderObserver observer = new();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.OpenSettings, observer, CancellationToken.None);

        Task<CommanderSnapshot> changing = session.HandleAsync(
            UserIntent.SelectLaunchHiddenItemVisibility(HiddenItemVisibility.Shown),
            observer,
            CancellationToken.None);

        Assert.AreSame(HiddenItemVisibility.Shown, session.Current.Settings.Settings.HiddenItemVisibility);
        PaneContentListed leftContent = Assert.IsInstanceOfType<PaneContentListed>(
            session.Current.Panes.Left.Content);
        Assert.AreSame(HiddenItemVisibility.Hidden, leftContent.State.HiddenItemVisibility);
        Assert.AreSame(PaneContent.Absent, session.Current.Panes.Right.Content);
        write.SetResult(SettingsWriteOutcome.Succeeded());
        CommanderSnapshot changed = await changing;
        _ = Assert.IsInstanceOfType<SettingsPersistenceSucceeded>(changed.Settings.Persistence);
    }

    /// <summary>Proves an existing name modal keeps ownership when the settings shortcut arrives.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenNameModalOwnsInputRefusesToOpenSettingsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        using FileOperationGateway gateway = CreateGateway();
        CommanderSession session = CreateSession(
            left,
            right,
            gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()));
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
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

    /// <summary>Proves the selector queue accepts only settings choices while the editor is open.</summary>
    [TestMethod]
    public async Task QueueSettingsIntentWhenEditorStateVariesRoutesOnlyOpenSelectionsAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CreateGateway();
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        CommanderSession session = CreateSession(left, right, gateway, store);
        RecordingCommanderObserver observer = new();

        _ = session.QueueSettingsIntent(UserIntent.SelectColorScheme(ColorScheme.Ubuntu), observer);
        _ = await session.HandleAsync(UserIntent.OpenSettings, observer, CancellationToken.None);
        _ = session.QueueSettingsIntent(UserIntent.Escape, observer);
        CommanderSnapshot queued = session.QueueSettingsIntent(
            UserIntent.SelectColorScheme(ColorScheme.Dracula),
            observer);

        Assert.IsEmpty(observer.Settings);
        Assert.HasCount(1, store.Writes);
        Assert.AreSame(ColorScheme.Dracula, queued.Settings.Settings.ColorScheme);
        write.SetResult(SettingsWriteOutcome.Succeeded());
        await session.StopAsync();
    }

    private static CommanderSession CreateSession(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        FileOperationGateway gateway,
        ISettingsStore store)
    {
        PaneSession leftPane = new(
            left,
            Capacity(),
            DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden);
        PaneSession rightPane = new(
            right,
            Capacity(),
            DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden);
        return new CommanderSession(
            new DualPaneSession(leftPane, rightPane, gateway),
            new SettingsSession(store, SettingsReadOutcome.Absent(), static _ => { }));
    }

    private static FileOperationGateway CreateGateway()
    {
        return new FileOperationGateway(ScriptedFileOperationPort.Create(null, null));
    }

    private static VisiblePageCapacity Capacity()
    {
        return Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(4)).Capacity;
    }

    private static DirectoryListing Listing(string location, string name)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry entry = DirectoryEntry.Create(
            ParsePath(location + "\\" + name),
            name,
            DirectoryEntryKind.File,
            EntryVisibility.Normal);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(
            DirectoryListing.Create(
                parsedLocation,
                [entry],
                DirectoryListingCompleteness.Complete,
                0)).Listing;
    }

    private static FileSystemPath ParsePath(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
