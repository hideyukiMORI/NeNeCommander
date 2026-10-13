using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves palette scope entries reuse their existing owners without touching pane or storage state.</summary>
[TestClass]
public sealed class PaletteScopeEntryTests
{
    /// <summary>Proves both entries are available with an empty listed active pane and an unlisted passive pane.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task OpenWhenOnlyActivePaneIsListedAndEmptyAdmitsBothScopeEntriesAsync(int side)
    {
        using Fixture fixture = new();
        PaneSide active = Side(side);
        await fixture.ListEmptyAsync(active);

        CommandPaletteOpen palette = await fixture.OpenAsync();

        Assert.AreSame(CommandAvailability.Available, palette.Candidates.Single(candidate => candidate.Intent == UserIntent.OpenBookmarks).Availability);
        Assert.AreSame(CommandAvailability.Available, palette.Candidates.Single(candidate => candidate.Intent == UserIntent.OpenWindowAdjustment).Availability);
        Assert.AreEqual(active, palette.ActiveSide);
        Assert.IsEmpty(fixture.Store.Writes);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves qualified submission closes the palette and opens only the selected existing owner.</summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(0, 1)]
    [DataRow(1, 0)]
    [DataRow(1, 1)]
    public async Task HandleAsyncWhenScopeEntryIsSubmittedPreservesBothPaneSnapshotsAsync(int command, int side)
    {
        using Fixture fixture = new();
        await fixture.PrepareAsync(Side(side));
        CommandPaletteOpen palette = await fixture.OpenAsync();
        CommanderSnapshot before = fixture.Session.Current;

        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.SubmitCommand(palette, Entry(command)));

        Assert.IsNull(Assert.IsInstanceOfType<CommandPaletteClosed>(after.Scopes.CommandPalette).FileListFocusSide);
        AssertScope(after, command);
        Assert.AreSame(before.Panes.Left, after.Panes.Left);
        Assert.AreSame(before.Panes.Right, after.Panes.Right);
        Assert.AreSame(before.Panes.ActiveSide, after.Panes.ActiveSide);
        Assert.HasCount(1, State(after.Panes.Left).Selection);
        Assert.HasCount(2, State(after.Panes.Left).NavigationHistory.Locations);
        Assert.HasCount(1, State(after.Panes.Right).Selection);
        Assert.HasCount(2, State(after.Panes.Right).NavigationHistory.Locations);
        fixture.AssertNoEffects();
    }

    /// <summary>Proves direct inputs stay frozen while the palette owns input.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task HandleAsyncWhenScopeEntryArrivesUnqualifiedKeepsPaletteOpenAsync(int command)
    {
        using Fixture fixture = new();
        await fixture.PrepareAsync(PaneSide.Left);
        CommandPaletteOpen palette = await fixture.OpenAsync();

        CommanderSnapshot ignored = await fixture.HandleAsync(Entry(command));

        Assert.AreSame(palette, ignored.Scopes.CommandPalette);
        Assert.AreSame(SettingsEditorState.Closed, ignored.Settings.Editor);
        Assert.AreSame(WindowAdjustmentState.Closed, ignored.Scopes.WindowAdjustment);
        fixture.AssertNoEffects();
    }

    /// <summary>Proves a stale open-state submission cannot close a newer palette or open either scope.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task HandleAsyncWhenScopeEntryUsesStalePaletteKeepsCurrentPaletteAsync(int command)
    {
        using Fixture fixture = new();
        await fixture.PrepareAsync(PaneSide.Left);
        CommandPaletteOpen old = await fixture.OpenAsync();
        _ = await fixture.HandleAsync(UserIntent.CancelCommandPalette(old));
        CommandPaletteOpen current = await fixture.OpenAsync();

        CommanderSnapshot ignored = await fixture.HandleAsync(UserIntent.SubmitCommand(old, Entry(command)));

        Assert.AreSame(current, ignored.Scopes.CommandPalette);
        Assert.AreSame(SettingsEditorState.Closed, ignored.Settings.Editor);
        Assert.AreSame(WindowAdjustmentState.Closed, ignored.Scopes.WindowAdjustment);
        fixture.AssertNoEffects();
    }

    /// <summary>Proves replay during and after the opened scope cannot reopen it or claim old-side focus.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task HandleAsyncWhenAcceptedScopeSubmissionIsReplayedDoesNotReopenAsync(int command)
    {
        using Fixture fixture = new();
        await fixture.PrepareAsync(PaneSide.Right);
        CommandPaletteOpen palette = await fixture.OpenAsync();
        UserIntent submission = UserIntent.SubmitCommand(palette, Entry(command));
        CommanderSnapshot first = await fixture.HandleAsync(submission);
        CommanderSnapshot replayed = await fixture.HandleAsync(submission);
        Assert.AreSame(first.Settings.Editor, replayed.Settings.Editor);
        Assert.AreSame(first.Scopes.WindowAdjustment, replayed.Scopes.WindowAdjustment);
        if (command == 0)
        {
            _ = await fixture.HandleAsync(UserIntent.Escape);
        }
        else
        {
            WindowAdjustmentOpen open = Assert.IsInstanceOfType<WindowAdjustmentOpen>(first.Scopes.WindowAdjustment);
            WindowAdjustmentClosed closed = Assert.IsInstanceOfType<WindowAdjustmentClosed>(fixture.Session.LeaveWindowAdjustment(open));
            Assert.AreSame(PaneSide.Right, closed.FileListFocusSide);
        }

        CommanderSnapshot ignored = await fixture.HandleAsync(submission);

        Assert.AreSame(SettingsEditorState.Closed, ignored.Settings.Editor);
        _ = Assert.IsInstanceOfType<WindowAdjustmentClosed>(ignored.Scopes.WindowAdjustment);
        fixture.AssertNoEffects();
    }

    /// <summary>Proves left, right, or active-side changes invalidate both scope entries.</summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(0, 1)]
    [DataRow(0, 2)]
    [DataRow(1, 0)]
    [DataRow(1, 1)]
    [DataRow(1, 2)]
    public async Task HandleAsyncWhenCapturedEndpointChangesRejectsScopeEntryAsync(int command, int endpoint)
    {
        using Fixture fixture = new();
        await fixture.PrepareAsync(PaneSide.Left);
        CommandPaletteOpen palette = await fixture.OpenAsync();
        await fixture.ChangeEndpointAsync(endpoint);

        CommanderSnapshot ignored = await fixture.HandleAsync(UserIntent.SubmitCommand(palette, Entry(command)));

        Assert.IsNull(Assert.IsInstanceOfType<CommandPaletteClosed>(ignored.Scopes.CommandPalette).FileListFocusSide);
        Assert.AreSame(SettingsEditorState.Closed, ignored.Settings.Editor);
        Assert.AreSame(WindowAdjustmentState.Closed, ignored.Scopes.WindowAdjustment);
        fixture.AssertNoEffects();
    }

    private static void AssertScope(CommanderSnapshot snapshot, int command)
    {
        if (command == 0)
        {
            Assert.AreSame(SettingsEditorState.Bookmarks, snapshot.Settings.Editor);
            Assert.AreSame(WindowAdjustmentState.Closed, snapshot.Scopes.WindowAdjustment);
        }
        else
        {
            WindowAdjustmentOpen open = Assert.IsInstanceOfType<WindowAdjustmentOpen>(snapshot.Scopes.WindowAdjustment);
            Assert.AreSame(snapshot.Panes.ActiveSide, open.ActiveSide);
            Assert.AreSame(WindowAdjustmentOutcome.None, open.Outcome);
            Assert.AreSame(SettingsEditorState.Closed, snapshot.Settings.Editor);
        }
    }

    private static UserIntent Entry(int command)
    {
        return command == 0 ? UserIntent.OpenBookmarks : UserIntent.OpenWindowAdjustment;
    }

    private static PaneSide Side(int side)
    {
        return side == 0 ? PaneSide.Left : PaneSide.Right;
    }

    private static PaneState State(PaneSnapshot pane)
    {
        return Assert.IsInstanceOfType<PaneContentListed>(pane.Content).State;
    }

    private static FileSystemPath Path(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }

    private static DirectoryListing Listing(string location, params string[] names)
    {
        DirectoryEntry[] entries = [.. names.Select(name => DirectoryEntry.Create(Path(location + "\\" + name), name,
            DirectoryEntryKind.File, EntryMetadata.Unmeasured(EntryVisibility.Normal)))];
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(DirectoryListing.Create(Path(location), entries, DirectoryListingCompleteness.Complete, 0)).Listing;
    }

    private sealed class Fixture : IDisposable
    {
        private readonly FileOperationGateway _gateway;
        private readonly RecordingCommanderObserver _observer = new();
        internal ScriptedDirectoryReadPort Left { get; } = ScriptedDirectoryReadPort.Create();
        internal ScriptedDirectoryReadPort Right { get; } = ScriptedDirectoryReadPort.Create();
        internal ScriptedFileOperationPort Port { get; } = ScriptedFileOperationPort.Create(null, null);
        internal ScriptedSettingsStore Store { get; } = new(SettingsReadOutcome.Absent());
        internal DualPaneSession Panes { get; }
        internal CommanderSession Session { get; }

        internal Fixture()
        {
            _gateway = new FileOperationGateway(Port);
            VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(4)).Capacity;
            PaneSessionConfiguration configuration = PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default);
            Panes = new DualPaneSession(new PaneSession(Left, new ScriptedFileLauncher(), configuration), new PaneSession(Right, new ScriptedFileLauncher(), configuration), _gateway);
            Session = new CommanderSession(Panes,
                new SettingsSession(Store, SettingsReadOutcome.Absent(), static exception => throw new AssertFailedException("Unexpected storage fault.", exception)),
                new TransientScopeOwners(new AddressEditorSession(), new CommandPaletteSession(), new WindowAdjustmentSession(), new LocationsSession(new ScriptedDriveCatalog(), new ScriptedWslDistributionCatalog())));
        }

        internal async Task ListEmptyAsync(PaneSide side)
        {
            ScriptedDirectoryReadPort port = side == PaneSide.Left ? Left : Right;
            port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\empty")));
            _ = await Session.NavigateAsync(side, Path("C:\\empty"), CancellationToken.None);
            if (side == PaneSide.Right)
            {
                _ = await HandleAsync(UserIntent.ActivateOtherPane);
            }
        }

        internal async Task PrepareAsync(PaneSide activeSide)
        {
            Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "a.txt", "b.txt")));
            Left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left\\next", "a.txt", "b.txt")));
            Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "a.txt", "b.txt")));
            Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\right\\next", "a.txt", "b.txt")));
            _ = await Session.NavigateAsync(PaneSide.Left, Path("C:\\left"), CancellationToken.None);
            _ = await Session.NavigateAsync(PaneSide.Left, Path("C:\\left\\next"), CancellationToken.None);
            _ = await Session.NavigateAsync(PaneSide.Right, Path("C:\\right"), CancellationToken.None);
            _ = await Session.NavigateAsync(PaneSide.Right, Path("C:\\right\\next"), CancellationToken.None);
            _ = await HandleAsync(UserIntent.ToggleSelection);
            _ = await HandleAsync(UserIntent.ActivateOtherPane);
            _ = await HandleAsync(UserIntent.ToggleSelection);
            if (activeSide == PaneSide.Left)
            {
                _ = await HandleAsync(UserIntent.ActivateOtherPane);
            }
        }

        internal async Task<CommandPaletteOpen> OpenAsync()
        {
            return Assert.IsInstanceOfType<CommandPaletteOpen>((await HandleAsync(UserIntent.OpenCommandPalette)).Scopes.CommandPalette);
        }

        internal Task<CommanderSnapshot> HandleAsync(UserIntent intent)
        {
            return Session.HandleAsync(intent, _observer, CancellationToken.None);
        }

        internal async Task ChangeEndpointAsync(int endpoint)
        {
            if (endpoint != 0)
            {
                _ = await Panes.HandleAsync(UserIntent.ActivateOtherPane, _observer, CancellationToken.None);
            }
            if (endpoint != 2)
            {
                _ = await Panes.HandleAsync(UserIntent.MoveNext, _observer, CancellationToken.None);
            }
            if (endpoint == 1)
            {
                _ = await Panes.HandleAsync(UserIntent.ActivateOtherPane, _observer, CancellationToken.None);
            }
        }

        internal void AssertNoEffects()
        {
            Assert.HasCount(2, Left.Requests);
            Assert.HasCount(2, Right.Requests);
            Assert.IsEmpty(Port.Calls);
            Assert.IsEmpty(Store.Writes);
        }

        public void Dispose()
        {
            _gateway.Dispose();
        }
    }
}
