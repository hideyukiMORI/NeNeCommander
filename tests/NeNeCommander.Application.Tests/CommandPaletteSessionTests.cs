using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the command palette scope owner answers every intent from the state it owns.</summary>
[TestClass]
public sealed class CommandPaletteSessionTests
{
    /// <summary>
    /// Proves validation is total over the closed scope. The session validates precedence under its
    /// own read, so a closed scope can still receive a cancellation or a submission qualified by an
    /// open state it never held; each keeps the closed state and dispatches nothing.
    /// </summary>
    [TestMethod]
    public void ValidateWhenScopeIsClosedKeepsClosedStateAndDispatchesNothing()
    {
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        DualPaneSnapshot panes = CreatePanes(gateway).Current;
        IReadOnlyList<CommandCandidate> candidates =
            [new CommandCandidate(UserIntent.OpenFocused, CommandAvailability.Available)];
        CommandPaletteOpen foreign = new(panes.Left, panes.Right, panes.ActiveSide, candidates);
        CommandPaletteSession owner = new();

        CommandPaletteValidation cancelled = owner.Validate(
            UserIntent.CancelCommandPalette(foreign),
            panes,
            InteractionOwnership.ScopeOwnsInput);
        CommandPaletteValidation submitted = owner.Validate(
            UserIntent.SubmitCommand(foreign, UserIntent.OpenFocused),
            panes,
            InteractionOwnership.ScopeOwnsInput);
        CommandPaletteValidation unrelated = owner.Validate(
            UserIntent.MoveNext,
            panes,
            InteractionOwnership.AnotherScopeOwnsInput);

        Assert.AreSame(CommandPaletteValidation.NothingToDispatch, cancelled);
        Assert.AreSame(CommandPaletteValidation.NothingToDispatch, submitted);
        Assert.AreSame(CommandPaletteValidation.NothingToDispatch, unrelated);
        Assert.AreSame(CommandPaletteState.Closed, owner.Current);
    }

    /// <summary>Proves an unlisted active pane refuses admission even while this scope owns input.</summary>
    [TestMethod]
    public void OpenWhenActivePaneHasNoListingKeepsClosedState()
    {
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        DualPaneSnapshot panes = CreatePanes(gateway).Current;
        CommandPaletteSession owner = new();

        CommandPaletteState refused = owner.Open(panes, InteractionOwnership.ScopeOwnsInput);

        Assert.AreSame(CommandPaletteState.Closed, refused);
        Assert.AreSame(CommandPaletteState.Closed, owner.Current);
    }

    private static DualPaneSession CreatePanes(FileOperationGateway gateway)
    {
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;
        return new DualPaneSession(
            new PaneSession(
                ScriptedDirectoryReadPort.Create(),
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
            gateway);
    }
}
