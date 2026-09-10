using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
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

/// <summary>Proves every public Application boundary rejects absent required collaborators and values.</summary>
[TestClass]
public sealed class NullGuardTests
{
    /// <summary>Proves synchronous public factories and reducers reject each absent required argument.</summary>
    [TestMethod]
    public void InvokeWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        FileSystemPath path = ParsePath("C:\\source");
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity")).Identity;
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        FileEntrySnapshot snapshot = FileEntrySnapshot.Create(path, identity, DeletionCapability.Recycle);
        DirectoryEntry entry = DirectoryEntry.Create(
            path,
            "source",
            DirectoryEntryKind.File,
            EntryVisibility.Normal);
        PaneState state = Assert.IsInstanceOfType<PaneStateAccepted>(
            PaneState.Create(path, [entry], capacity, HiddenItemVisibility.Hidden)).State;
        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryListingAccepted>(
            DirectoryListing.Create(path, [entry], DirectoryListingCompleteness.Complete, 0)).Listing;

        AssertStaticNullGuard(typeof(FileEntrySnapshot), nameof(FileEntrySnapshot.Create),
            [null, identity, DeletionCapability.Recycle]);
        AssertStaticNullGuard(typeof(FileEntrySnapshot), nameof(FileEntrySnapshot.Create),
            [path, null, DeletionCapability.Recycle]);
        AssertStaticNullGuard(typeof(FileEntrySnapshot), nameof(FileEntrySnapshot.Create),
            [path, identity, null]);
        AssertStaticNullGuard(typeof(FileInspectionOutcome), nameof(FileInspectionOutcome.Succeeded), [null]);
        AssertStaticNullGuard(typeof(FileInspectionOutcome), nameof(FileInspectionOutcome.Failed), [null]);
        AssertStaticNullGuard(typeof(MoveRequest), nameof(MoveRequest.Create), [null, path]);
        AssertStaticNullGuard(typeof(MoveRequest), nameof(MoveRequest.Create), [new[] { path }, null]);
        AssertStaticNullGuard(typeof(CopyRequest), nameof(CopyRequest.Create), [null, path]);
        AssertStaticNullGuard(typeof(CopyRequest), nameof(CopyRequest.Create), [new[] { path }, null]);
        AssertStaticNullGuard(typeof(CreateDirectoryRequest), nameof(CreateDirectoryRequest.Create), [null, "name"]);
        AssertStaticNullGuard(typeof(CreateDirectoryRequest), nameof(CreateDirectoryRequest.Create), [path, null]);
        AssertStaticNullGuard(typeof(RenameRequest), nameof(RenameRequest.Create), [null, "name"]);
        AssertStaticNullGuard(typeof(RenameRequest), nameof(RenameRequest.Create), [path, null]);
        AssertStaticNullGuard(typeof(UserIntent), nameof(UserIntent.SubmitName), [null]);
        AssertStaticNullGuard(typeof(UserIntent), nameof(UserIntent.BeginAddressEdit), [null]);
        AssertStaticNullGuard(
            typeof(UserIntent),
            nameof(UserIntent.SubmitAddress),
            [null, "C:\\target"]);
        AssertStaticNullGuard(
            typeof(UserIntent),
            nameof(UserIntent.SubmitAddress),
            [AddressEditorState.Closed, null]);
        AssertStaticNullGuard(typeof(UserIntent), nameof(UserIntent.LeaveAddress), [null]);
        AssertStaticNullGuard(typeof(DeleteRequest), nameof(DeleteRequest.Create), [null, null]);
        AssertStaticNullGuard(
            typeof(PermanentDeletionConfirmation),
            nameof(PermanentDeletionConfirmation.CreateFor),
            [null]);
        AssertStaticNullGuard(typeof(ProviderStepOutcome), nameof(ProviderStepOutcome.Failed), [null]);
        AssertStaticNullGuard(
            typeof(AtomicMoveCapabilityOutcome),
            nameof(AtomicMoveCapabilityOutcome.Failed),
            [null]);
        AssertInternalConstructorNullGuard(typeof(AtomicMoveCapabilityFailed), [null]);
        AssertStaticNullGuard(
            typeof(ProviderStepOutcome),
            nameof(ProviderStepOutcome.FailedAfterEffect),
            [null, ProviderStepEffectKind.CopyTargetCreated]);
        AssertStaticNullGuard(
            typeof(ProviderStepOutcome),
            nameof(ProviderStepOutcome.FailedAfterEffect),
            [FileOperationFailureKind.Copy, null]);
        AssertStaticNullGuard(typeof(PaneState), nameof(PaneState.Create),
            [null, new[] { entry }, capacity, HiddenItemVisibility.Hidden]);
        AssertStaticNullGuard(typeof(PaneState), nameof(PaneState.Create),
            [path, null, capacity, HiddenItemVisibility.Hidden]);
        AssertStaticNullGuard(typeof(PaneState), nameof(PaneState.Create),
            [path, new[] { entry }, null, HiddenItemVisibility.Hidden]);
        AssertStaticNullGuard(typeof(PaneState), nameof(PaneState.Create),
            [path, new[] { entry }, capacity, null]);
        AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.Apply), [null, UserIntent.MoveNext]);
        AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.Apply), [state, null]);
        AssertStaticNullGuard(typeof(DirectoryEntry), nameof(DirectoryEntry.Create),
            [null, "name", DirectoryEntryKind.File, EntryVisibility.Normal]);
        AssertStaticNullGuard(typeof(DirectoryEntry), nameof(DirectoryEntry.Create),
            [path, null, DirectoryEntryKind.File, EntryVisibility.Normal]);
        AssertStaticNullGuard(typeof(DirectoryEntry), nameof(DirectoryEntry.Create),
            [path, "name", null, EntryVisibility.Normal]);
        AssertStaticNullGuard(typeof(DirectoryEntry), nameof(DirectoryEntry.Create),
            [path, "name", DirectoryEntryKind.File, null]);
        AssertStaticNullGuard(typeof(DirectoryListing), nameof(DirectoryListing.Create),
            [null, new[] { entry }, DirectoryListingCompleteness.Complete, 0]);
        AssertStaticNullGuard(typeof(DirectoryListing), nameof(DirectoryListing.Create),
            [path, null, DirectoryListingCompleteness.Complete, 0]);
        AssertStaticNullGuard(typeof(DirectoryListing), nameof(DirectoryListing.Create),
            [path, new[] { entry }, null, 0]);
        AssertStaticNullGuard(typeof(DirectoryReadRequest), nameof(DirectoryReadRequest.Create), [null, 1]);
        AssertStaticNullGuard(typeof(DirectoryReadOutcome), nameof(DirectoryReadOutcome.Succeeded), [null]);
        AssertStaticNullGuard(typeof(DirectoryReadOutcome), nameof(DirectoryReadOutcome.Failed), [null]);
        AssertStaticNullGuard(typeof(FileLaunchOutcome), nameof(FileLaunchOutcome.Failed), [null]);
        AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.Navigate),
            [null, capacity, null, HiddenItemVisibility.Hidden]);
        AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.Navigate),
            [listing, null, null, HiddenItemVisibility.Hidden]);
        AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.Navigate),
            [listing, capacity, null, null]);
        AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.ApplyHiddenItemVisibility),
            [null, HiddenItemVisibility.Hidden]);
        AssertStaticNullGuard(typeof(PaneReducer), nameof(PaneReducer.ApplyHiddenItemVisibility),
            [state, null]);
        AssertStaticNullGuard(typeof(UserSettings), nameof(UserSettings.Create),
            [null, HiddenItemVisibility.Hidden, BookmarkCatalog.Empty]);
        AssertStaticNullGuard(typeof(UserSettings), nameof(UserSettings.Create),
            [ColorScheme.NeNeDark, null, BookmarkCatalog.Empty]);
        AssertStaticNullGuard(typeof(UserSettings), nameof(UserSettings.Create),
            [ColorScheme.NeNeDark, HiddenItemVisibility.Hidden, null]);
        AssertStaticNullGuard(typeof(SettingsReadOutcome), nameof(SettingsReadOutcome.Read), [null]);
        AssertStaticNullGuard(typeof(SettingsReadOutcome), nameof(SettingsReadOutcome.Rejected), [null]);
        AssertStaticNullGuard(typeof(SettingsWriteOutcome), nameof(SettingsWriteOutcome.Rejected),
            [null, SettingsDirectoryEffect.NotAttempted, SettingsWriteEffect.None]);
        AssertStaticNullGuard(typeof(SettingsWriteOutcome), nameof(SettingsWriteOutcome.Rejected),
            [SettingsWriteFailureKind.IoFailure, null, SettingsWriteEffect.None]);
        AssertStaticNullGuard(typeof(SettingsWriteOutcome), nameof(SettingsWriteOutcome.Rejected),
            [SettingsWriteFailureKind.IoFailure, SettingsDirectoryEffect.NotAttempted, null]);
        AssertStaticNullGuard(typeof(SettingsPersistenceState), nameof(SettingsPersistenceState.StartupRejected),
            [null]);
        AssertStaticNullGuard(typeof(SettingsPersistenceState), nameof(SettingsPersistenceState.Failed), [null]);
        AssertStaticNullGuard(typeof(UserIntent), nameof(UserIntent.SelectColorScheme), [null]);
        AssertStaticNullGuard(typeof(UserIntent), nameof(UserIntent.SelectLaunchHiddenItemVisibility), [null]);

        ConstructorInfo constructor = typeof(FileOperationGateway).GetConstructor([typeof(IFileOperationPort)]) ??
            throw new AssertFailedException("The public gateway constructor was not found.");
        TargetInvocationException constructorFailure = Assert.ThrowsExactly<TargetInvocationException>(
            () => constructor.Invoke([null]));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(constructorFailure.InnerException);

        Assert.AreSame(snapshot.Path, path);
        Assert.AreSame(listing.Entries[0], entry);
    }

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
                typeof(VisiblePageCapacity),
                typeof(int),
                typeof(HiddenItemVisibility),
            ]) ??
            throw new AssertFailedException("The public session constructor was not found.");
        PaneSession session = new(
            port,
            new ScriptedFileLauncher(),
            capacity,
            DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden);

        AssertConstructorNullGuard(
            constructor,
            [null, new ScriptedFileLauncher(), capacity, 1, HiddenItemVisibility.Hidden]);
        AssertConstructorNullGuard(constructor, [port, null, capacity, 1, HiddenItemVisibility.Hidden]);
        AssertConstructorNullGuard(
            constructor,
            [port, new ScriptedFileLauncher(), null, 1, HiddenItemVisibility.Hidden]);
        AssertConstructorNullGuard(
            constructor,
            [port, new ScriptedFileLauncher(), capacity, 1, null]);
        AssertInstanceNullGuard(session, nameof(PaneSession.NavigateAsync), [null, CancellationToken.None]);
        AssertInstanceNullGuard(session, nameof(PaneSession.HandleAsync), [null, CancellationToken.None]);
        AssertInstanceNullGuard(session, nameof(PaneSession.RefreshFocusingAsync), [null, CancellationToken.None]);
        Assert.IsEmpty(port.Requests);
    }

    /// <summary>Proves the dual-pane coordinator and its snapshot reject absent collaborators and arguments.</summary>
    [TestMethod]
    public async Task DualPaneSessionWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        FileOperationRequest cancelledRequest = Assert.IsInstanceOfType<FileOperationRequestAccepted>(
            DeleteRequest.Create([ParsePath("C:\\source")], null)).Request;
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        PaneSession left = new(
            ScriptedDirectoryReadPort.Create(),
            new ScriptedFileLauncher(),
            capacity,
            DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden);
        PaneSession right = new(
            ScriptedDirectoryReadPort.Create(),
            new ScriptedFileLauncher(),
            capacity,
            DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden);
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        FileOperationOutcome outcome = await gateway.ExecuteAsync(cancelledRequest, RecordingFileOperationProgress.Create(), cancellation.Token);
        ConstructorInfo constructor = typeof(DualPaneSession).GetConstructor(
            [typeof(PaneSession), typeof(PaneSession), typeof(FileOperationGateway)]) ??
            throw new AssertFailedException("The public dual-pane constructor was not found.");
        DualPaneSession panes = new(left, right, gateway);
        FileSystemPath path = ParsePath("C:\\source");

        AssertConstructorNullGuard(constructor, [null, right, gateway]);
        AssertConstructorNullGuard(constructor, [left, null, gateway]);
        AssertConstructorNullGuard(constructor, [left, right, null]);
        AssertInstanceNullGuard(panes, nameof(DualPaneSession.NavigateAsync), [null, path, CancellationToken.None]);
        AssertInstanceNullGuard(panes, nameof(DualPaneSession.HandleAsync), [null, RecordingDualPaneObserver.Create(), CancellationToken.None]);
        AssertInstanceNullGuard(panes, nameof(DualPaneSession.HandleAsync), [UserIntent.Refresh, null, CancellationToken.None]);
        AssertInstanceNullGuard(panes.Current, nameof(DualPaneSnapshot.Of), [null]);
        AssertInternalConstructorNullGuard(typeof(DualPaneSnapshot), [null, PaneSnapshot.Initial, PaneSide.Left, OperationActivity.Idle]);
        AssertInternalConstructorNullGuard(typeof(DualPaneSnapshot), [PaneSnapshot.Initial, null, PaneSide.Left, OperationActivity.Idle]);
        AssertInternalConstructorNullGuard(typeof(DualPaneSnapshot), [PaneSnapshot.Initial, PaneSnapshot.Initial, null, OperationActivity.Idle]);
        AssertInternalConstructorNullGuard(typeof(DualPaneSnapshot), [PaneSnapshot.Initial, PaneSnapshot.Initial, PaneSide.Left, null]);
        AssertInternalConstructorNullGuard(typeof(OperationRunning), [null, FileOperationProgress.Create(0, 1)]);
        AssertInternalConstructorNullGuard(typeof(OperationRunning), [OperationKind.Move, null]);
        AssertInternalConstructorNullGuard(typeof(OperationAwaitingConfirmation), [null]);
        AssertInternalConstructorNullGuard(typeof(OperationAwaitingName), [null, path, "name"]);
        AssertInternalConstructorNullGuard(typeof(OperationAwaitingName), [OperationKind.Rename, null, "name"]);
        AssertInternalConstructorNullGuard(typeof(OperationAwaitingName), [OperationKind.Rename, path, null]);
        AssertInternalConstructorNullGuard(typeof(OperationCompleted), [null, outcome]);
        AssertInternalConstructorNullGuard(typeof(OperationCompleted), [OperationKind.Move, null]);
        AssertInternalConstructorNullGuard(typeof(OperationRequestRejected), [null, FileOperationRequestFailureKind.EmptySources]);
        AssertInternalConstructorNullGuard(typeof(OperationRequestRejected), [OperationKind.Move, null]);

        SettingsSession settings = new(
            new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
            SettingsReadOutcome.Absent(),
            static _ => { });
        ConstructorInfo commanderConstructor = typeof(CommanderSession).GetConstructor(
            [typeof(DualPaneSession), typeof(SettingsSession)]) ??
            throw new AssertFailedException("The public application-session constructor was not found.");
        AssertConstructorNullGuard(commanderConstructor, [null, settings]);
        AssertConstructorNullGuard(commanderConstructor, [panes, null]);
        CommandCandidate candidate = new(UserIntent.OpenFocused, CommandAvailability.Available);
        IReadOnlyList<CommandCandidate> candidates = [candidate];
        CommandPaletteOpen open = new(
            panes.Current.Left,
            panes.Current.Right,
            panes.Current.ActiveSide,
            candidates);
        AssertInternalConstructorNullGuard(
            typeof(CommanderSnapshot),
            [null, settings.Current, AddressEditorState.Closed, CommandPaletteState.Closed]);
        AssertInternalConstructorNullGuard(
            typeof(CommanderSnapshot),
            [panes.Current, null, AddressEditorState.Closed, CommandPaletteState.Closed]);
        AssertInternalConstructorNullGuard(
            typeof(CommanderSnapshot),
            [panes.Current, settings.Current, null, CommandPaletteState.Closed]);
        AssertInternalConstructorNullGuard(
            typeof(CommanderSnapshot),
            [panes.Current, settings.Current, AddressEditorState.Closed, null]);
        AssertInternalConstructorNullGuard(
            typeof(CommandCandidate),
            [null, CommandAvailability.Available]);
        AssertInternalConstructorNullGuard(
            typeof(CommandCandidate),
            [UserIntent.OpenFocused, null]);
        AssertInternalConstructorNullGuard(typeof(CommandUnavailable), [null]);
        AssertInternalConstructorNullGuard(
            typeof(CommandPaletteOpen),
            [null, panes.Current.Right, panes.Current.ActiveSide, candidates]);
        AssertInternalConstructorNullGuard(
            typeof(CommandPaletteOpen),
            [panes.Current.Left, null, panes.Current.ActiveSide, candidates]);
        AssertInternalConstructorNullGuard(
            typeof(CommandPaletteOpen),
            [panes.Current.Left, panes.Current.Right, null, candidates]);
        AssertInternalConstructorNullGuard(
            typeof(CommandPaletteOpen),
            [panes.Current.Left, panes.Current.Right, panes.Current.ActiveSide, null]);
        AssertInternalConstructorNullGuard(
            typeof(CommandPaletteSubmission),
            [null, UserIntent.OpenFocused]);
        AssertInternalConstructorNullGuard(
            typeof(CommandPaletteSubmission),
            [open, null]);
        AssertInternalConstructorNullGuard(typeof(CommandPaletteCancellation), [null]);
        AssertInternalConstructorNullGuard(typeof(AddressEditing), [null, path]);
        AssertInternalConstructorNullGuard(typeof(AddressEditing), [PaneSide.Left, null]);
        AssertInternalConstructorNullGuard(
            typeof(AddressInputRejected),
            [null, path, "raw", PathParseFailureKind.Relative]);
        AssertInternalConstructorNullGuard(
            typeof(AddressInputRejected),
            [PaneSide.Left, null, "raw", PathParseFailureKind.Relative]);
        AssertInternalConstructorNullGuard(
            typeof(AddressInputRejected),
            [PaneSide.Left, path, null, PathParseFailureKind.Relative]);
        AssertInternalConstructorNullGuard(
            typeof(AddressInputRejected),
            [PaneSide.Left, path, "raw", null]);
    }

    /// <summary>Proves asynchronous application-session entries reject absent required values.</summary>
    [TestMethod]
    public async Task CommanderSessionAsyncWhenRequiredArgumentIsNullThrowsArgumentNullExceptionAsync()
    {
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        PaneSession left = new(
            ScriptedDirectoryReadPort.Create(),
            new ScriptedFileLauncher(),
            capacity,
            DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden);
        PaneSession right = new(
            ScriptedDirectoryReadPort.Create(),
            new ScriptedFileLauncher(),
            capacity,
            DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden);
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        CommanderSession commander = new(
            new DualPaneSession(left, right, gateway),
            new SettingsSession(
                new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
                SettingsReadOutcome.Absent(),
                static _ => { }));

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => commander.NavigateAsync(null!, ParsePath("C:\\source"), CancellationToken.None));
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => commander.NavigateAsync(PaneSide.Left, null!, CancellationToken.None));
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => commander.HandleAsync(null!, new RecordingCommanderObserver(), CancellationToken.None));
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => commander.HandleAsync(UserIntent.Escape, null!, CancellationToken.None));
    }

    /// <summary>
    /// Proves the application session validates its arguments before it routes on the current
    /// modal owner. While the settings editor owns input every entry returns the unchanged
    /// snapshot, so an unvalidated absent argument would be swallowed instead of rejected.
    /// </summary>
    [TestMethod]
    public async Task CommanderSessionWhenSettingsOwnInputStillRejectsAbsentArgumentsAsync()
    {
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        CommanderSession commander = new(
            new DualPaneSession(
                new PaneSession(
                    left,
                    new ScriptedFileLauncher(),
                    capacity,
                    DirectoryListing.EntryBoundaryLimit,
                    HiddenItemVisibility.Hidden),
                new PaneSession(
                    ScriptedDirectoryReadPort.Create(),
                    new ScriptedFileLauncher(),
                    capacity,
                    DirectoryListing.EntryBoundaryLimit,
                    HiddenItemVisibility.Hidden),
                gateway),
            new SettingsSession(
                new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
                SettingsReadOutcome.Absent(),
                static _ => { }));
        CommanderSnapshot opened = await commander.HandleAsync(
            UserIntent.OpenSettings,
            new RecordingCommanderObserver(),
            CancellationToken.None);

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => commander.NavigateAsync(null!, ParsePath("C:\\source"), CancellationToken.None));
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => commander.NavigateAsync(PaneSide.Left, null!, CancellationToken.None));
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => commander.HandleAsync(null!, new RecordingCommanderObserver(), CancellationToken.None));
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => commander.HandleAsync(UserIntent.Escape, null!, CancellationToken.None));

        Assert.AreSame(SettingsEditorState.Open, opened.Settings.Editor);
        Assert.AreSame(SettingsEditorState.Open, commander.Current.Settings.Editor);
        Assert.IsEmpty(left.Requests);
    }

    /// <summary>Proves settings and application-session boundaries reject absent required values.</summary>
    [TestMethod]
    public void SettingsSessionWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsReadOutcome outcome = SettingsReadOutcome.Absent();
        ConstructorInfo settingsConstructor = typeof(SettingsSession).GetConstructor(
            [typeof(ISettingsStore), typeof(SettingsReadOutcome), typeof(Action<Exception>)]) ??
            throw new AssertFailedException("The public settings-session constructor was not found.");
        SettingsSession settings = new(store, outcome, static _ => { });

        AssertConstructorNullGuard(settingsConstructor, [null, outcome, new Action<Exception>(_ => { })]);
        AssertConstructorNullGuard(settingsConstructor, [store, null, new Action<Exception>(_ => { })]);
        AssertConstructorNullGuard(settingsConstructor, [store, outcome, null]);
        AssertInstanceNullGuard(settings, nameof(SettingsSession.SelectColorSchemeAsync),
            [null, new RecordingCommanderObserver(), CancellationToken.None]);
        AssertInstanceNullGuard(settings, nameof(SettingsSession.SelectColorSchemeAsync),
            [ColorScheme.NeNeDark, null, CancellationToken.None]);
        AssertInstanceNullGuard(settings, nameof(SettingsSession.SelectLaunchHiddenItemVisibilityAsync),
            [null, new RecordingCommanderObserver(), CancellationToken.None]);
        AssertInstanceNullGuard(settings, nameof(SettingsSession.SelectLaunchHiddenItemVisibilityAsync),
            [HiddenItemVisibility.Hidden, null, CancellationToken.None]);
        AssertInternalConstructorNullGuard(typeof(SettingsSnapshot),
            [null, SettingsEditorState.Closed, BookmarksEditorState.Closed, SettingsPersistenceState.Succeeded]);
        AssertInternalConstructorNullGuard(typeof(SettingsSnapshot),
            [UserSettings.Default, null, BookmarksEditorState.Closed, SettingsPersistenceState.Succeeded]);
        AssertInternalConstructorNullGuard(typeof(SettingsSnapshot),
            [UserSettings.Default, SettingsEditorState.Closed, null, SettingsPersistenceState.Succeeded]);
        AssertInternalConstructorNullGuard(typeof(SettingsSnapshot),
            [UserSettings.Default, SettingsEditorState.Closed, BookmarksEditorState.Closed, null]);
        AssertInternalConstructorNullGuard(typeof(SettingsPersistenceStartupRejected), [null]);
        AssertInternalConstructorNullGuard(typeof(SettingsPersistenceFailed), [null]);
        AssertInternalConstructorNullGuard(typeof(SettingsWriteRejected),
            [null, SettingsDirectoryEffect.NotAttempted, SettingsWriteEffect.None]);
        AssertInternalConstructorNullGuard(typeof(SettingsWriteRejected),
            [SettingsWriteFailureKind.IoFailure, null, SettingsWriteEffect.None]);
        AssertInternalConstructorNullGuard(typeof(SettingsWriteRejected),
            [SettingsWriteFailureKind.IoFailure, SettingsDirectoryEffect.NotAttempted, null]);
        AssertInternalConstructorNullGuard(typeof(ColorSchemeSelection), [null]);
        AssertInternalConstructorNullGuard(typeof(LaunchHiddenItemVisibilitySelection), [null]);
    }

    /// <summary>
    /// Proves each settings-session entry rejects its own absent argument and names that
    /// parameter, so the caller sees the argument it supplied rather than an internal
    /// collaborator's parameter name.
    /// </summary>
    [TestMethod]
    public void SettingsSessionWhenSelectionArgumentIsNullNamesTheRejectedParameter()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession settings = new(store, SettingsReadOutcome.Absent(), static _ => { });

        ArgumentNullException scheme = Assert.ThrowsExactly<ArgumentNullException>(
            () => settings.SelectColorSchemeAsync(
                null!,
                new RecordingCommanderObserver(),
                CancellationToken.None));
        ArgumentNullException visibility = Assert.ThrowsExactly<ArgumentNullException>(
            () => settings.SelectLaunchHiddenItemVisibilityAsync(
                null!,
                new RecordingCommanderObserver(),
                CancellationToken.None));

        Assert.AreEqual("scheme", scheme.ParamName);
        Assert.AreEqual("visibility", visibility.ParamName);
        Assert.IsEmpty(store.Writes);
    }
    /// <summary>Proves internal pane state records preserve their null invariants for every collaborator.</summary>
    [TestMethod]
    public void ConstructPaneStateRecordsWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        FileSystemPath path = ParsePath("C:\\source");
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        DirectoryListing listing = Assert.IsInstanceOfType<DirectoryListingAccepted>(
            DirectoryListing.Create(path, [], DirectoryListingCompleteness.Complete, 0)).Listing;
        PaneState state = PaneReducer.Navigate(listing, capacity, null, HiddenItemVisibility.Hidden);

        AssertInternalConstructorNullGuard(typeof(PaneContentListed), [null, listing]);
        AssertInternalConstructorNullGuard(typeof(PaneContentListed), [state, null]);
        AssertInternalConstructorNullGuard(typeof(PaneLoading), [null]);
        AssertInternalConstructorNullGuard(typeof(PaneReadCancelled), [null]);
        AssertInternalConstructorNullGuard(typeof(PaneReadFailed), [null, FileOperationFailureKind.NotFound]);
        AssertInternalConstructorNullGuard(typeof(PaneReadFailed), [path, null]);
        AssertInternalConstructorNullGuard(typeof(PaneLaunching), [null]);
        AssertInternalConstructorNullGuard(typeof(PaneLaunchCancelled), [null]);
        AssertInternalConstructorNullGuard(typeof(PaneLaunchFailed), [null, FileLaunchFailureKind.NotFound]);
        AssertInternalConstructorNullGuard(typeof(PaneLaunchFailed), [path, null]);
        AssertInternalMethodNullGuard(typeof(PaneSnapshot), nameof(PaneSnapshot.IdleWith), null, [null]);
        AssertInternalMethodNullGuard(typeof(PaneSnapshot), nameof(PaneSnapshot.WithActivity), PaneSnapshot.Initial, [null]);
    }

    /// <summary>Proves every bookmark value boundary rejects each absent required value.</summary>
    [TestMethod]
    public void BookmarkValuesWhenRequiredArgumentIsNullThrowArgumentNullException()
    {
        BookmarkCategoryName category = Category("Work");
        BookmarkDisplayName name = DisplayName("Target");
        BookmarkPath path = BookmarkPath("C:\\target");
        BookmarkEntry entry = BookmarkEntry.Create(name, path, category, BookmarkShortcutSlot.One);
        BookmarkSelection selection = new(entry);

        AssertNullGuard(() => _ = new BookmarkBrowseContext(null!, BookmarkCategoryFilter.All, null));
        AssertNullGuard(() => _ = new BookmarkBrowseContext(string.Empty, null!, null));
        AssertNullGuard(() => _ = new BookmarkDraft(null!, "C:\\target", BookmarkCategoryFilter.All, null));
        AssertNullGuard(() => _ = new BookmarkDraft("Target", null!, BookmarkCategoryFilter.All, null));
        AssertNullGuard(() => _ = new BookmarkDraft("Target", "C:\\target", null!, null));
        AssertNullGuard(() => _ = BookmarkEntry.Create(null!, path, category, null));
        AssertNullGuard(() => _ = BookmarkEntry.Create(name, null!, category, null));
        AssertNullGuard(() => _ = new BookmarkKey(category, null!));
        AssertNullGuard(() => _ = new BookmarkSelection(null!));
        AssertNullGuard(() => _ = new BookmarkCategorySelection(null!, [entry]));
        AssertNullGuard(() => _ = new BookmarkCategorySelection(category, null!));
        AssertNullGuard(() => _ = new BookmarkUserCategoryFilter(null!));
        AssertNullGuard(() => _ = new BookmarkRegistrationDefaults(null!, "C:\\target"));
        AssertNullGuard(() => _ = new BookmarkRegistrationDefaults("Target", null!));
        AssertNullGuard(() => _ = new BookmarkNavigationStart.Accepted(null!));
        AssertNullGuard(() => _ = new BookmarkEditorTransition.CatalogChanged(null!));
        AssertNullGuard(() => _ = new BookmarkEditorMutationResult(null!, new BookmarkEditorTransition.StateChanged()));
        AssertNullGuard(() => _ = new BookmarkEditorMutationResult(BookmarksEditorState.Closed, null!));
        AssertNullGuard(() => _ = UserIntent.ManageBookmarks(null!));
        AssertNullGuard(() => _ = UserIntent.NavigateBookmark(null!));
        AssertNullGuard(() => _ = new BookmarkShortcutSelection(null!));
        AssertNullGuard(() => _ = new ResolvedBookmarkNavigation(null!));

        Assert.AreSame(entry, selection.Entry);
    }

    /// <summary>Proves every bookmark editor state and action rejects each absent required value.</summary>
    [TestMethod]
    public void BookmarkEditorInputsWhenRequiredArgumentIsNullThrowArgumentNullException()
    {
        BookmarkCategoryName category = Category("Work");
        BookmarkEntry entry = BookmarkEntry.Create(
            DisplayName("Target"),
            BookmarkPath("C:\\target"),
            category,
            null);
        BookmarkSelection selection = new(entry);
        BookmarkCategorySelection categorySelection = new(category, [entry]);
        BookmarkBrowseContext context = new(string.Empty, BookmarkCategoryFilter.All, null);
        BookmarkDraft draft = new("Target", "C:\\target", BookmarkCategoryFilter.All, null);

        AssertNullGuard(() => _ = new BookmarksBrowsing(null!, null));
        AssertNullGuard(() => _ = new BookmarkDrafting(null!, null, draft, null));
        AssertNullGuard(() => _ = new BookmarkDrafting(context, null, null!, null));
        AssertNullGuard(() => _ = new BookmarkCategoryDrafting(null!, null, string.Empty, null));
        AssertNullGuard(() => _ = new BookmarkCategoryDrafting(context, null, null!, null));
        AssertNullGuard(() => _ = new BookmarkCategoryDeleteConfirmation(null!, categorySelection));
        AssertNullGuard(() => _ = new BookmarkCategoryDeleteConfirmation(context, null!));
        AssertNullGuard(() => _ = new BookmarkNavigationPending(null!, selection));
        AssertNullGuard(() => _ = new BookmarkNavigationPending(context, null!));
        AssertNullGuard(() => _ = new BookmarkNavigationFailed(
            null!,
            selection,
            new PaneReadCancelled(ParsePath("C:\\target"))));
        AssertNullGuard(() => _ = new BookmarkNavigationFailed(
            context,
            null!,
            new PaneReadCancelled(ParsePath("C:\\target"))));
        AssertNullGuard(() => _ = new BookmarkNavigationFailed(context, selection, null!));
        AssertNullGuard(() => _ = BookmarkEditorAction.Search(null!));
        AssertNullGuard(() => _ = BookmarkEditorAction.Filter(null!));
        AssertNullGuard(() => _ = BookmarkEditorAction.BeginEditBookmark(null!));
        AssertNullGuard(() => _ = BookmarkEditorAction.UpdateBookmark(null!));
        AssertNullGuard(() => _ = BookmarkEditorAction.BeginRenameCategory(null!));
        AssertNullGuard(() => _ = BookmarkEditorAction.UpdateCategory(null!));
        AssertNullGuard(() => _ = BookmarkEditorAction.DeleteBookmark(null!));
        AssertNullGuard(() => _ = BookmarkEditorAction.BeginDeleteCategory(null!));
    }

    /// <summary>Proves every catalog entry point rejects each absent required value before mutating.</summary>
    [TestMethod]
    public void BookmarkCatalogWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        BookmarkCategoryName category = Category("Work");
        BookmarkEntry entry = BookmarkEntry.Create(
            DisplayName("Target"),
            BookmarkPath("C:\\target"),
            category,
            BookmarkShortcutSlot.One);
        BookmarkCatalog catalog = Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create([category], [entry])).Catalog;
        BookmarkSelection selection = new(catalog.Bookmarks[0]);
        BookmarkCategorySelection categorySelection = catalog.Select(category) ??
            throw new AssertFailedException("The category fixture must be selectable.");

        AssertNullGuard(() => _ = BookmarkCatalog.Create(null!, [entry]));
        AssertNullGuard(() => _ = BookmarkCatalog.Create([category], null!));
        AssertNullGuard(() => _ = catalog.Find((BookmarkShortcutSlot)null!));
        AssertNullGuard(() => _ = catalog.Find((BookmarkKey)null!));
        AssertNullGuard(() => _ = catalog.Select((BookmarkCategoryName)null!));
        AssertNullGuard(() => _ = catalog.Matches((BookmarkSelection)null!));
        AssertNullGuard(() => _ = catalog.Matches((BookmarkCategorySelection)null!));
        AssertNullGuard(() => _ = BookmarkCatalog.SelectionsMatch(null!, selection));
        AssertNullGuard(() => _ = BookmarkCatalog.SelectionsMatch(selection, null!));
        AssertNullGuard(() => _ = catalog.AddCategory(null!));
        AssertNullGuard(() => _ = catalog.RenameCategory(null!, category));
        AssertNullGuard(() => _ = catalog.RenameCategory(categorySelection, null!));
        AssertNullGuard(() => _ = catalog.DeleteCategory(null!));
        AssertNullGuard(() => _ = catalog.AddBookmark(null!));
        AssertNullGuard(() => _ = catalog.ReplaceBookmark(null!, entry));
        AssertNullGuard(() => _ = catalog.ReplaceBookmark(selection, null!));
        AssertNullGuard(() => _ = catalog.DeleteBookmark(null!));
    }

    /// <summary>Proves the bookmark editor and settings owners reject absent bookmark arguments.</summary>
    [TestMethod]
    public void BookmarkSessionsWhenRequiredArgumentIsNullThrowArgumentNullException()
    {
        BookmarkCategoryName category = Category("Work");
        BookmarkEntry entry = BookmarkEntry.Create(
            DisplayName("Target"),
            BookmarkPath("C:\\target"),
            category,
            null);
        BookmarkCatalog catalog = Assert.IsInstanceOfType<BookmarkCatalogAccepted>(
            BookmarkCatalog.Create([category], [entry])).Catalog;
        BookmarkSelection selection = new(catalog.Bookmarks[0]);
        BookmarkRegistrationDefaults defaults = new(string.Empty, string.Empty);
        BookmarkEditorSession editor = new();
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession settings = new(store, SettingsReadOutcome.Absent(), static _ => { });
        RecordingCommanderObserver observer = new();

        AssertNullGuard(() => _ = editor.Apply(null!, catalog, defaults));
        AssertNullGuard(() => _ = editor.Apply(BookmarkEditorAction.Cancel, null!, defaults));
        AssertNullGuard(() => _ = editor.Apply(BookmarkEditorAction.Cancel, catalog, null!));
        AssertNullGuard(() => _ = editor.BeginNavigation(null!, catalog));
        AssertNullGuard(() => _ = editor.BeginNavigation(selection, null!));
        AssertNullGuard(() => editor.FinishNavigationFailed(null!));
        AssertNullGuard(() => _ = settings.SaveBookmarkCatalogAsync(null!, observer, CancellationToken.None));
        AssertNullGuard(() => _ = settings.SaveBookmarkCatalogAsync(catalog, null!, CancellationToken.None));
        AssertNullGuard(() => _ = settings.ApplyBookmarkEditorAction(
            null!,
            defaults,
            observer,
            CancellationToken.None));
        AssertNullGuard(() => _ = settings.ApplyBookmarkEditorAction(
            BookmarkEditorAction.Cancel,
            null!,
            observer,
            CancellationToken.None));
        AssertNullGuard(() => _ = settings.ApplyBookmarkEditorAction(
            BookmarkEditorAction.Cancel,
            defaults,
            null!,
            CancellationToken.None));
        AssertNullGuard(() => _ = settings.BeginBookmarkNavigation(null!));
        AssertNullGuard(() => settings.FinishBookmarkNavigationFailed(null!));

        Assert.IsEmpty(store.Writes);
    }

    private static void AssertNullGuard(Action action)
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(action);
    }

    private static BookmarkCategoryName Category(string value)
    {
        return Assert.IsInstanceOfType<BookmarkCategoryNameAccepted>(
            BookmarkCategoryName.Parse(value)).Name;
    }

    private static BookmarkDisplayName DisplayName(string value)
    {
        return Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(
            BookmarkDisplayName.Parse(value)).Name;
    }

    private static BookmarkPath BookmarkPath(string value)
    {
        return Assert.IsInstanceOfType<BookmarkPathAccepted>(
            NeNeCommander.Application.Bookmarks.BookmarkPath.Parse(value)).Path;
    }

    private static void AssertInternalConstructorNullGuard(Type type, object?[] arguments)
    {
        ConstructorInfo[] constructors = [.. type
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(constructor => constructor.GetParameters().Length == arguments.Length &&
                constructor.GetParameters().All(parameter => parameter.ParameterType != type))];
        Assert.HasCount(1, constructors);
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => constructors[0].Invoke(arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static void AssertInternalMethodNullGuard(Type type, string methodName, object? instance, object?[] arguments)
    {
        MethodInfo method = type.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) ??
            throw new AssertFailedException("The internal method was not found.");
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(instance, arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }
    private static void AssertConstructorNullGuard(ConstructorInfo constructor, object?[] arguments)
    {
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => constructor.Invoke(arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static void AssertInstanceNullGuard(object instance, string methodName, object?[] arguments)
    {
        MethodInfo method = GetSinglePublicStaticOrInstanceMethod(instance.GetType(), methodName);
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(instance, arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }
    /// <summary>Proves the asynchronous gateway rejects an absent request before provider access.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenRequestIsNullThrowsArgumentNullException()
    {
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        using FileOperationGateway gateway = new(port);
        MethodInfo method = GetSinglePublicStaticOrInstanceMethod(
            typeof(FileOperationGateway),
            nameof(FileOperationGateway.ExecuteAsync));

        object? invocation = method.Invoke(gateway, [null, RecordingFileOperationProgress.Create(), CancellationToken.None]);
        Task task = Assert.IsInstanceOfType<Task>(invocation);
        FileOperationRequestCreation creation = DeleteRequest.Create([ParsePath("C:\\source")], null);
        FileOperationRequest request = Assert.IsInstanceOfType<FileOperationRequestAccepted>(creation).Request;
        object? withoutObserver = method.Invoke(gateway, [request, null, CancellationToken.None]);
        Task withoutObserverTask = Assert.IsInstanceOfType<Task>(withoutObserver);

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await task);
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await withoutObserverTask);
        Assert.IsEmpty(port.Calls);
    }

    private static void AssertStaticNullGuard(Type type, string methodName, object?[] arguments)
    {
        MethodInfo method = GetSinglePublicStaticOrInstanceMethod(type, methodName);
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static MethodInfo GetSinglePublicStaticOrInstanceMethod(Type type, string methodName)
    {
        MethodInfo[] methods = [.. type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.Name.Equals(methodName, StringComparison.Ordinal))];
        Assert.HasCount(1, methods);
        return methods[0];
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
