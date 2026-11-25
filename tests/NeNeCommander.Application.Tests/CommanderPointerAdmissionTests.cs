using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Application.Wsl;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves pointer input respects the complete session's existing input owners.</summary>
[TestClass]
public sealed class CommanderPointerAdmissionTests
{
    /// <summary>Supplies every ordinary scope entry and the name-entry operation.</summary>
    public static IEnumerable<object[]> Owners()
    {
        return [
            [UserIntent.OpenSettings], [UserIntent.OpenColorSchemes],
            [UserIntent.OpenBookmarks], [UserIntent.OpenCommandPalette],
            [UserIntent.OpenHelp], [UserIntent.OpenWindowAdjustment],
            [UserIntent.OpenLocations], [UserIntent.FocusAddress],
            [UserIntent.Rename], [UserIntent.CreateDirectory], [UserIntent.CreateTextFile],
        ];
    }

    /// <summary>Proves row/open/blank/header input cannot replace a scope, draft or operation.</summary>
    [TestMethod]
    [DynamicData(nameof(Owners))]
    public async Task HandleAsyncWhenInputOwnerIsOpenRefusesEveryPointerActionAsync(UserIntent open)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        ScriptedFileLauncher launcher = new();
        CommanderSession session = CreateSession(left, right, gateway, launcher);
        DirectoryListing listing = CommanderSessionTestValues.Listing("C:\\pointer", "item.txt");
        left.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        _ = await session.NavigateAsync(PaneSide.Left, listing.Location, CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Right, listing.Location, CancellationToken.None);
        CommanderSnapshot before = await session.HandleAsync(open, new RecordingCommanderObserver(), CancellationToken.None);
        Assert.IsTrue(before.Settings.Editor != SettingsEditorState.Closed ||
            before.Scopes.AddressEditor is not AddressEditorClosed ||
            before.Scopes.CommandPalette is CommandPaletteOpen ||
            before.Scopes.KeyboardHelp is KeyboardHelpOpen ||
            before.Scopes.WindowAdjustment is WindowAdjustmentOpen ||
            before.Scopes.Locations is not LocationsClosed ||
            before.Panes.Operation is OperationAwaitingName);

        await AssertPointerFrozenAsync(session, before, listing.Entries[0]);

        Assert.HasCount(1, left.Requests);
        Assert.HasCount(1, right.Requests);
        Assert.IsEmpty(launcher.Targets);
    }

    /// <summary>Proves a left-pane launch freezes pointer input on both panes.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenLeftPaneLaunchesRefusesEveryPointerActionAsync()
    {
        await AssertLaunchingFrozenAsync(PaneSide.Left);
    }

    /// <summary>Proves a right-pane launch freezes pointer input on both panes.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRightPaneLaunchesRefusesEveryPointerActionAsync()
    {
        await AssertLaunchingFrozenAsync(PaneSide.Right);
    }

    private static async Task AssertLaunchingFrozenAsync(PaneSide launchingSide)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        ScriptedFileLauncher launcher = new();
        CommanderSession session = CreateSession(left, right, gateway, launcher);
        DirectoryListing listing = CommanderSessionTestValues.Listing("C:\\pointer", "item.txt");
        left.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        _ = await session.NavigateAsync(PaneSide.Left, listing.Location, CancellationToken.None);
        _ = await session.NavigateAsync(PaneSide.Right, listing.Location, CancellationToken.None);
        if (launchingSide == PaneSide.Right)
        {
            _ = await session.HandleAsync(UserIntent.ActivateOtherPane, new RecordingCommanderObserver(), CancellationToken.None);
        }
        TaskCompletionSource<FileLaunchOutcome> release = launcher.EnqueuePending();
        Task<CommanderSnapshot> launching = session.HandleAsync(UserIntent.OpenFocused, new RecordingCommanderObserver(), CancellationToken.None);
        CommanderSnapshot before = session.Current;

        await AssertPointerFrozenAsync(session, before, listing.Entries[0]);

        Assert.HasCount(1, launcher.Targets);
        release.SetResult(FileLaunchOutcome.Accepted());
        _ = await launching;
    }

    private static CommanderSession CreateSession(
        ScriptedDirectoryReadPort left,
        ScriptedDirectoryReadPort right,
        FileOperationGateway gateway,
        ScriptedFileLauncher launcher)
    {
        PaneSessionConfiguration configuration = PaneSessionConfiguration.Create(
            CommanderSessionTestValues.Capacity(), DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden, PaneSortOrder.Default);
        return new CommanderSession(new DualPaneSession(new PaneSession(left, launcher, configuration),
            new PaneSession(right, launcher, configuration), gateway),
            new SettingsSession(new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
                SettingsReadOutcome.Absent(), static _ => { }),
            new TransientScopeOwners(new AddressEditorSession(), new CommandPaletteSession(),
                new WindowAdjustmentSession(), new LocationsSession(
                    ScriptedDriveCatalog.Completed(DriveCatalogOutcome.Succeeded([], 0)),
                    ScriptedWslDistributionCatalog.Completed(WslDistributionCatalogOutcome.Succeeded([]))),
                new KeyboardHelpSession()));
    }

    private static async Task AssertPointerFrozenAsync(CommanderSession session, CommanderSnapshot before, DirectoryEntry entry)
    {
        UserIntent[] attempts = [
            UserIntent.SelectAll, UserIntent.InvertSelection, UserIntent.ClearSelection,
            UserIntent.SelectRangeNext, UserIntent.SelectRangePrevious,
            UserIntent.SelectRangeHalfPageDown, UserIntent.SelectRangeHalfPageUp,
            UserIntent.SelectRangeFirst, UserIntent.SelectRangeLast,
            UserIntent.OpenContextMenu,
            UserIntent.InteractWithPaneEntry(PaneSide.Right, before.Panes.Right, entry, PaneEntryAction.OpenContextMenu),
            UserIntent.InteractWithPaneSurface(PaneSide.Left, before.Panes.Left, PaneSurfaceAction.OpenContextMenu),
            UserIntent.InteractWithPaneEntry(PaneSide.Right, before.Panes.Right, entry, PaneEntryAction.Select),
            UserIntent.InteractWithPaneEntry(PaneSide.Left, before.Panes.Left, entry, PaneEntryAction.Open),
            UserIntent.InteractWithPaneSurface(PaneSide.Right, before.Panes.Right, PaneSurfaceAction.ClearSelection),
            UserIntent.InteractWithPaneSurface(PaneSide.Right, before.Panes.Right, PaneSurfaceAction.NavigateParent),
            UserIntent.InteractWithPaneSurface(PaneSide.Left, before.Panes.Left, PaneSurfaceAction.Activate),
        ];
        foreach (UserIntent intent in attempts)
        {
            CommanderSnapshot after = await session.HandleAsync(intent, new RecordingCommanderObserver(), CancellationToken.None);
            Assert.AreSame(before.Panes.Left, after.Panes.Left);
            Assert.AreSame(before.Panes.Right, after.Panes.Right);
            Assert.AreSame(before.Panes.ActiveSide, after.Panes.ActiveSide);
            Assert.AreSame(before.Panes.Operation, after.Panes.Operation);
            Assert.AreSame(before.Settings.Editor, after.Settings.Editor);
            Assert.AreEqual(before.Scopes, after.Scopes);
        }
    }
}
