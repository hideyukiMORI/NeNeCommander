using System;
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

/// <summary>Proves Commander and transient scope entries reject absent required values before state changes.</summary>
[TestClass]
public sealed class CommanderScopeNullGuardTests
{
    /// <summary>Proves asynchronous application-session entries reject absent required values.</summary>
    [TestMethod]
    public async Task CommanderSessionAsyncWhenRequiredArgumentIsNullThrowsArgumentNullExceptionAsync()
    {
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        PaneSession left = new(ScriptedDirectoryReadPort.Create(),
            new ScriptedFileLauncher(),
            PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
        PaneSession right = new(ScriptedDirectoryReadPort.Create(),
            new ScriptedFileLauncher(),
            PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        CommanderSession commander = new(
            new DualPaneSession(left, right, gateway),
            new SettingsSession(
                new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
                SettingsReadOutcome.Absent(),
                static _ => { }),
            new TransientScopeOwners(
                new AddressEditorSession(),
                new CommandPaletteSession(),
                new WindowAdjustmentSession(),
                new LocationsSession(new ScriptedDriveCatalog(), new ScriptedWslDistributionCatalog()),
                new KeyboardHelpSession()));

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => commander.NavigateAsync(null!, NullGuardAssertions.ParsePath("C:\\source"), CancellationToken.None));
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
                new PaneSession(left,
            new ScriptedFileLauncher(),
            PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default)),
                new PaneSession(ScriptedDirectoryReadPort.Create(),
            new ScriptedFileLauncher(),
            PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default)),
                gateway),
            new SettingsSession(
                new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
                SettingsReadOutcome.Absent(),
                static _ => { }),
            new TransientScopeOwners(
                new AddressEditorSession(),
                new CommandPaletteSession(),
                new WindowAdjustmentSession(),
                new LocationsSession(new ScriptedDriveCatalog(), new ScriptedWslDistributionCatalog()),
                new KeyboardHelpSession()));
        CommanderSnapshot opened = await commander.HandleAsync(
            UserIntent.OpenSettings,
            new RecordingCommanderObserver(),
            CancellationToken.None);

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => commander.NavigateAsync(null!, NullGuardAssertions.ParsePath("C:\\source"), CancellationToken.None));
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

    /// <summary>
    /// Proves both transient scope owners, the records that carry them, and every closed result
    /// they return reject each absent intent, pane snapshot, and interaction ownership before the
    /// owner changes its scope state.
    /// </summary>
    [TestMethod]
    public void TransientScopesWhenRequiredArgumentIsNullThrowArgumentNullException()
    {
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        DualPaneSession panes = new(
            new PaneSession(ScriptedDirectoryReadPort.Create(),
            new ScriptedFileLauncher(),
            PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default)),
            new PaneSession(ScriptedDirectoryReadPort.Create(),
            new ScriptedFileLauncher(),
            PaneSessionConfiguration.Create(capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default)),
            gateway);
        DualPaneSnapshot snapshot = panes.Current;
        AddressEditorSession addressEditor = new();
        CommandPaletteSession commandPalette = new();
        WindowAdjustmentSession windowAdjustment = new();
        LocationsSession locations = new(new ScriptedDriveCatalog(), new ScriptedWslDistributionCatalog());
        KeyboardHelpSession keyboardHelp = new();

        NullGuardAssertions.AssertNullGuard(() => _ = new TransientScopeOwners(null!, commandPalette, windowAdjustment, locations, keyboardHelp));
        NullGuardAssertions.AssertNullGuard(() => _ = new TransientScopeOwners(addressEditor, null!, windowAdjustment, locations, keyboardHelp));
        NullGuardAssertions.AssertNullGuard(() => _ = new TransientScopeOwners(addressEditor, commandPalette, null!, locations, keyboardHelp));
        NullGuardAssertions.AssertNullGuard(() => _ = new TransientScopeOwners(addressEditor, commandPalette, windowAdjustment, null!, keyboardHelp));
        NullGuardAssertions.AssertNullGuard(() => _ = new TransientScopeOwners(addressEditor, commandPalette, windowAdjustment, locations, null!));
        AssertTransientScopeSnapshotArguments();
        AssertCommandPaletteArguments(commandPalette, snapshot);
        AssertAddressEditorArguments(addressEditor, snapshot);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(CommandPaletteIntentAccepted), [null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(AddressEditAdmitted), [null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(
            typeof(AddressTargetAccepted),
            [null, NullGuardAssertions.ParsePath("C:\\target")]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(AddressTargetAccepted), [PaneSide.Left, null]);

        Assert.AreSame(AddressEditorState.Closed, addressEditor.Current);
        Assert.AreSame(CommandPaletteState.Closed, commandPalette.Current);
    }

    private static void AssertTransientScopeSnapshotArguments()
    {
        NullGuardAssertions.AssertNullGuard(() => _ = new TransientScopeSnapshot(
            null!,
            CommandPaletteState.Closed,
            WindowAdjustmentState.Closed,
            LocationsState.Closed,
            KeyboardHelpState.Closed));
        NullGuardAssertions.AssertNullGuard(() => _ = new TransientScopeSnapshot(
            AddressEditorState.Closed,
            null!,
            WindowAdjustmentState.Closed,
            LocationsState.Closed,
            KeyboardHelpState.Closed));
        NullGuardAssertions.AssertNullGuard(() => _ = new TransientScopeSnapshot(
            AddressEditorState.Closed,
            CommandPaletteState.Closed,
            null!,
            LocationsState.Closed,
            KeyboardHelpState.Closed));
        NullGuardAssertions.AssertNullGuard(() => _ = new TransientScopeSnapshot(
            AddressEditorState.Closed,
            CommandPaletteState.Closed,
            WindowAdjustmentState.Closed,
            null!,
            KeyboardHelpState.Closed));
        NullGuardAssertions.AssertNullGuard(() => _ = new TransientScopeSnapshot(
            AddressEditorState.Closed,
            CommandPaletteState.Closed,
            WindowAdjustmentState.Closed,
            LocationsState.Closed,
            null!));
    }

    private static void AssertCommandPaletteArguments(CommandPaletteSession commandPalette, DualPaneSnapshot snapshot)
    {
        NullGuardAssertions.AssertNullGuard(() => _ = commandPalette.Open(null!, InteractionOwnership.ScopeOwnsInput));
        NullGuardAssertions.AssertNullGuard(() => _ = commandPalette.Open(snapshot, null!));
        NullGuardAssertions.AssertNullGuard(() => _ = commandPalette.Validate(
            null!,
            snapshot,
            InteractionOwnership.ScopeOwnsInput));
        NullGuardAssertions.AssertNullGuard(() => _ = commandPalette.Validate(
            UserIntent.Escape,
            null!,
            InteractionOwnership.ScopeOwnsInput));
        NullGuardAssertions.AssertNullGuard(() => _ = commandPalette.Validate(UserIntent.Escape, snapshot, null!));
    }

    private static void AssertAddressEditorArguments(AddressEditorSession addressEditor, DualPaneSnapshot snapshot)
    {
        NullGuardAssertions.AssertNullGuard(() => _ = addressEditor.Admit(
            null!,
            PaneSide.Left,
            InteractionOwnership.ScopeOwnsInput));
        NullGuardAssertions.AssertNullGuard(() => _ = addressEditor.Admit(
            snapshot,
            null!,
            InteractionOwnership.ScopeOwnsInput));
        NullGuardAssertions.AssertNullGuard(() => _ = addressEditor.Admit(snapshot, PaneSide.Left, null!));
        NullGuardAssertions.AssertNullGuard(() => _ = addressEditor.Open(null!));
        NullGuardAssertions.AssertNullGuard(() => _ = addressEditor.Validate(null!, InteractionOwnership.ScopeOwnsInput));
        NullGuardAssertions.AssertNullGuard(() => _ = addressEditor.Validate(UserIntent.Escape, null!));
    }
}
