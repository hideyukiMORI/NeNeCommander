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

namespace NeNeCommander.Application.Tests;

/// <summary>Proves selection commands dispatch through existing scope and operation owners.</summary>
[TestClass]
public sealed class SelectionCommandTests
{
    /// <summary>Proves palette submission uses the captured active pane and idle clear does not become Escape.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteSubmitsSelectionUsesCanonicalPaneRouteAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway,
            new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        DirectoryListing listing = DualPaneSessionTestContext.Listing("C:\\selection", ("a", DirectoryEntryKind.File), ("b", DirectoryEntryKind.File));
        left.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        _ = await session.NavigateAsync(PaneSide.Left, listing.Location, CancellationToken.None);
        UserIntent[] commands = [UserIntent.SelectAll, UserIntent.InvertSelection, UserIntent.SelectAll, UserIntent.ClearSelection];
        int[] counts = [2, 0, 2, 0];

        for (int index = 0; index < commands.Length; index++)
        {
            CommanderSnapshot opened = await session.HandleAsync(UserIntent.OpenCommandPalette, new RecordingCommanderObserver(), CancellationToken.None);
            CommandPaletteOpen palette = Assert.IsInstanceOfType<CommandPaletteOpen>(opened.Scopes.CommandPalette);
            Assert.AreSame(CommandAvailability.Available, palette.Candidates.Single(candidate => candidate.Intent == commands[index]).Availability);
            CommanderSnapshot after = await session.HandleAsync(UserIntent.SubmitCommand(palette, commands[index]),
                new RecordingCommanderObserver(), CancellationToken.None);
            PaneState state = Assert.IsInstanceOfType<PaneContentListed>(after.Panes.Left.Content).State;
            Assert.HasCount(counts[index], state.Selection);
            Assert.IsNull(state.SelectionAnchor);
            Assert.AreSame(listing.Entries[0].Path, state.FocusItem);
            _ = Assert.IsInstanceOfType<CommandPaletteClosed>(after.Scopes.CommandPalette);
        }
        Assert.HasCount(1, left.Requests);
        Assert.IsEmpty(right.Requests);
    }

    /// <summary>Proves the three selection commands appear only on the palette, including an empty listing.</summary>
    [TestMethod]
    public async Task CaptureWhenSurfaceVariesKeepsBulkSelectionOutOfContextMenusAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing empty = DualPaneSessionTestContext.Listing("C:\\selection");
        await fixture.ListBothAsync(empty, empty);
        UserIntent[] intents = [UserIntent.SelectAll, UserIntent.InvertSelection, UserIntent.ClearSelection];

        foreach (CommandSurface surface in new[] { CommandSurface.Palette, CommandSurface.ItemContext, CommandSurface.FolderContext })
        {
            foreach (UserIntent intent in intents)
            {
                CommandCandidate? candidate = CommandCatalog.Capture(fixture.Panes.Current, surface).SingleOrDefault(item => item.Intent == intent);
                if (surface == CommandSurface.Palette)
                {
                    Assert.IsNotNull(candidate);
                    Assert.AreSame(CommandAvailability.Available, candidate.Availability);
                }
                else
                {
                    Assert.IsNull(candidate);
                }
            }
        }
    }

    /// <summary>Proves a range freezes the exact selected batch for deletion even when focus moves elsewhere.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRangeIsDeletedUsesSelectedOwnedSourcesAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = DualPaneSessionTestContext.Listing("C:\\selection",
            ("a", DirectoryEntryKind.File), ("b", DirectoryEntryKind.File), ("c", DirectoryEntryKind.File));
        await fixture.ListBothAsync(listing, DualPaneSessionTestContext.Listing("C:\\other"));
        _ = await fixture.Panes.HandleAsync(UserIntent.SelectRangeNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(listing.Entries[0].Path, DeletionCapability.PermanentOnly));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(listing.Entries[1].Path, DeletionCapability.PermanentOnly));

        DualPaneSnapshot pending = await fixture.Panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None);
        OperationAwaitingConfirmation awaiting = Assert.IsInstanceOfType<OperationAwaitingConfirmation>(pending.Operation);

        CollectionAssert.AreEqual(new[] { listing.Entries[0].Path, listing.Entries[1].Path }, awaiting.Request.Sources.ToArray());
        Assert.AreSame(listing.Entries[2].Path, Assert.IsInstanceOfType<PaneContentListed>(pending.Left.Content).State.FocusItem);
        Assert.HasCount(2, fixture.Port.Calls);
        DualPaneSnapshot refused = await fixture.Panes.HandleAsync(UserIntent.ClearSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        Assert.AreSame(awaiting, refused.Operation);
        Assert.AreSame(pending.Left, refused.Left);
    }
}
