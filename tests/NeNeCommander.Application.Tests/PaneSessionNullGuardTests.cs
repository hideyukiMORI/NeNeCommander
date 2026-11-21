using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves pane sessions and their captured snapshots reject absent required values.</summary>
[TestClass]
public sealed class PaneSessionNullGuardTests
{
    /// <summary>Proves the pane session rejects absent collaborators and arguments before any read.</summary>
    [TestMethod]
    public void PaneSessionWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        ConstructorInfo constructor = typeof(PaneSession).GetConstructor(
            [
                typeof(IDirectoryReadPort),
                typeof(IFileLauncher),
                typeof(PaneSessionConfiguration),
            ]) ??
            throw new AssertFailedException("The public session constructor was not found.");
        PaneSession session = new(port,
            new ScriptedFileLauncher(),
            PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));

        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneSessionConfiguration), nameof(PaneSessionConfiguration.Create),
            [null, 1, HiddenItemVisibility.Hidden, PaneSortOrder.Default]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneSessionConfiguration), nameof(PaneSessionConfiguration.Create),
            [capacity, 1, null, PaneSortOrder.Default]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(PaneSessionConfiguration), nameof(PaneSessionConfiguration.Create),
            [capacity, 1, HiddenItemVisibility.Hidden, null]);

        NullGuardAssertions.AssertConstructorNullGuard(
            constructor,
            [null, new ScriptedFileLauncher(), PaneSessionConfiguration.Create(capacity, 1, HiddenItemVisibility.Hidden, PaneSortOrder.Default)]);
        NullGuardAssertions.AssertConstructorNullGuard(constructor, [port, null, PaneSessionConfiguration.Create(capacity, 1, HiddenItemVisibility.Hidden, PaneSortOrder.Default)]);
        NullGuardAssertions.AssertConstructorNullGuard(
            constructor,
            [port, new ScriptedFileLauncher(), null]);
        NullGuardAssertions.AssertInstanceNullGuard(session, nameof(PaneSession.NavigateAsync), [null, CancellationToken.None]);
        NullGuardAssertions.AssertInstanceNullGuard(session, nameof(PaneSession.HandleAsync), [null, CancellationToken.None]);
        NullGuardAssertions.AssertInstanceNullGuard(session, nameof(PaneSession.RefreshFocusingAsync), [null, CancellationToken.None]);
        Assert.IsEmpty(port.Requests);
    }

    /// <summary>Proves the dual-pane coordinator and its snapshot reject absent collaborators and arguments.</summary>
    [TestMethod]
    public async Task DualPaneSessionWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        FileOperationRequest cancelledRequest = Assert.IsInstanceOfType<FileOperationRequestAccepted>(
            DeleteRequest.Create([NullGuardAssertions.ParsePath("C:\\source")], null)).Request;
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        PaneSession left = new(ScriptedDirectoryReadPort.Create(),
            new ScriptedFileLauncher(),
            PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
        PaneSession right = new(ScriptedDirectoryReadPort.Create(),
            new ScriptedFileLauncher(),
            PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        FileOperationOutcome outcome = await gateway.ExecuteAsync(cancelledRequest, RecordingFileOperationProgress.Create(), cancellation.Token);
        ConstructorInfo constructor = typeof(DualPaneSession).GetConstructor(
            [typeof(PaneSession), typeof(PaneSession), typeof(FileOperationGateway)]) ??
            throw new AssertFailedException("The public dual-pane constructor was not found.");
        DualPaneSession panes = new(left, right, gateway);
        FileSystemPath path = NullGuardAssertions.ParsePath("C:\\source");

        AssertDualPaneConstructorArguments(constructor, left, right, gateway);
        AssertDualPaneStateArguments(panes, path, outcome);
        AssertCommanderCompositionArguments(panes, path);

    }

    /// <summary>Proves internal pane state records preserve their null invariants for every collaborator.</summary>
    [TestMethod]
    public void ConstructPaneStateRecordsWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        FileSystemPath path = NullGuardAssertions.ParsePath("C:\\source");
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryListingAccepted>(
            DirectoryListing.Create(path, [], DirectoryListingCompleteness.Complete, 0)).Listing;
        PaneState state = PaneReducer.Navigate(listing, capacity, null, HiddenItemVisibility.Hidden);

        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(PaneContentListed), [null, listing, null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(PaneContentListed), [state, null, null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(PaneLoading), [null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(PaneReadCancelled), [null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(PaneReadAbandoned), [null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(PaneReadFailed), [null, FileOperationFailureKind.NotFound]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(PaneReadFailed), [path, null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(PaneLaunching), [null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(PaneLaunchCancelled), [null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(PaneLaunchFailed), [null, FileLaunchFailureKind.NotFound]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(PaneLaunchFailed), [path, null]);
        NullGuardAssertions.AssertInternalMethodNullGuard(typeof(PaneSnapshot), nameof(PaneSnapshot.IdleWith), null, [null]);
        NullGuardAssertions.AssertInternalMethodNullGuard(typeof(PaneSnapshot), nameof(PaneSnapshot.WithActivity), PaneSnapshot.Initial, [null]);
    }

    private static void AssertDualPaneConstructorArguments(ConstructorInfo constructor, PaneSession left, PaneSession right, FileOperationGateway gateway)
    {
        NullGuardAssertions.AssertConstructorNullGuard(constructor, [null, right, gateway]);
        NullGuardAssertions.AssertConstructorNullGuard(constructor, [left, null, gateway]);
        NullGuardAssertions.AssertConstructorNullGuard(constructor, [left, right, null]);
    }

    private static void AssertDualPaneStateArguments(DualPaneSession panes, FileSystemPath path, FileOperationOutcome outcome)
    {
        NullGuardAssertions.AssertInstanceNullGuard(panes, nameof(DualPaneSession.NavigateAsync), [null, path, CancellationToken.None]);
        NullGuardAssertions.AssertInstanceNullGuard(panes, nameof(DualPaneSession.HandleAsync), [null, RecordingDualPaneObserver.Create(), CancellationToken.None]);
        NullGuardAssertions.AssertInstanceNullGuard(panes, nameof(DualPaneSession.HandleAsync), [UserIntent.Refresh, null, CancellationToken.None]);
        NullGuardAssertions.AssertInstanceNullGuard(panes.Current, nameof(DualPaneSnapshot.Of), [null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(DualPaneSnapshot), [null, PaneSnapshot.Initial, PaneSide.Left, OperationActivity.Idle]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(DualPaneSnapshot), [PaneSnapshot.Initial, null, PaneSide.Left, OperationActivity.Idle]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(DualPaneSnapshot), [PaneSnapshot.Initial, PaneSnapshot.Initial, null, OperationActivity.Idle]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(DualPaneSnapshot), [PaneSnapshot.Initial, PaneSnapshot.Initial, PaneSide.Left, null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(OperationRunning), [null, FileOperationProgress.Create(0, 1)]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(OperationRunning), [OperationKind.Move, null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(OperationAwaitingConfirmation), [null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(OperationAwaitingName), [null, path, "name"]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(OperationAwaitingName), [OperationKind.Rename, null, "name"]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(OperationAwaitingName), [OperationKind.Rename, path, null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(OperationCompleted), [null, outcome]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(OperationCompleted), [OperationKind.Move, null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(OperationRequestRejected), [null, FileOperationRequestFailureKind.EmptySources]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(OperationRequestRejected), [OperationKind.Move, null]);

    }

    private static void AssertCommanderCompositionArguments(DualPaneSession panes, FileSystemPath path)
    {
        SettingsSession settings = new(
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
            SettingsReadOutcome.Absent(),
            static _ => { });
        TransientScopeOwners scopes = new(
            new AddressEditorSession(),
            new CommandPaletteSession(),
            new WindowAdjustmentSession(),
            new LocationsSession(new ScriptedDriveCatalog(), new ScriptedWslDistributionCatalog()),
            new KeyboardHelpSession());
        TransientScopeSnapshot scopeStates = new(
            AddressEditorState.Closed,
            CommandPaletteState.Closed,
            WindowAdjustmentState.Closed,
            LocationsState.Closed,
            KeyboardHelpState.Closed);
        ConstructorInfo commanderConstructor = typeof(CommanderSession).GetConstructor(
            [typeof(DualPaneSession), typeof(SettingsSession), typeof(TransientScopeOwners)]) ??
            throw new AssertFailedException("The public application-session constructor was not found.");
        NullGuardAssertions.AssertConstructorNullGuard(commanderConstructor, [null, settings, scopes]);
        NullGuardAssertions.AssertConstructorNullGuard(commanderConstructor, [panes, null, scopes]);
        NullGuardAssertions.AssertConstructorNullGuard(commanderConstructor, [panes, settings, null]);
        CommandCandidate candidate = new(UserIntent.OpenFocused, CommandAvailability.Available);
        IReadOnlyList<CommandCandidate> candidates = [candidate];
        CommandPaletteOpen open = new(
            panes.Current.Left,
            panes.Current.Right,
            panes.Current.ActiveSide,
            candidates);
        AssertCommanderSnapshotArguments(panes, settings, scopeStates);
        AssertPaletteRecordArguments(panes, candidates, open);
        AssertAddressRecordArguments(path);

    }

    private static void AssertCommanderSnapshotArguments(DualPaneSession panes, SettingsSession settings, TransientScopeSnapshot scopeStates)
    {
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(CommanderSnapshot),
            [null, settings.Current, scopeStates]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(CommanderSnapshot),
            [panes.Current, null, scopeStates]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(CommanderSnapshot),
            [panes.Current, settings.Current, null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(CommandCandidate),
            [null, CommandAvailability.Available]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(CommandCandidate),
            [UserIntent.OpenFocused, null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(CommandUnavailable), [null]);
    }

    private static void AssertPaletteRecordArguments(DualPaneSession panes, IReadOnlyList<CommandCandidate> candidates, CommandPaletteOpen open)
    {
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(CommandPaletteOpen),
            [null, panes.Current.Right, panes.Current.ActiveSide, candidates]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(CommandPaletteOpen),
            [panes.Current.Left, null, panes.Current.ActiveSide, candidates]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(CommandPaletteOpen),
            [panes.Current.Left, panes.Current.Right, null, candidates]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(CommandPaletteOpen),
            [panes.Current.Left, panes.Current.Right, panes.Current.ActiveSide, null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(CommandPaletteSubmission),
            [null, UserIntent.OpenFocused]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(CommandPaletteSubmission),
            [open, null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(CommandPaletteCancellation), [null]);
    }

    private static void AssertAddressRecordArguments(FileSystemPath path)
    {
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(AddressEditing), [null, path]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(AddressEditing), [PaneSide.Left, null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(AddressInputRejected),
            [null, path, "raw", PathParseFailureKind.Relative]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(AddressInputRejected),
            [PaneSide.Left, null, "raw", PathParseFailureKind.Relative]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(AddressInputRejected),
            [PaneSide.Left, path, null, PathParseFailureKind.Relative]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(AddressInputRejected),
            [PaneSide.Left, path, "raw", null]);
    }
}
