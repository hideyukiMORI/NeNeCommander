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

/// <summary>Proves validated palette selection uses the same opposite navigation and refuses stale captured panes.</summary>
[TestClass]
public sealed class OtherPaneNavigationPaletteTests
{
    /// <summary>Proves a valid submission closes the palette and preserves the source in either direction.</summary>
    [TestMethod]
    public Task HandleAsyncWhenPaletteSubmitsOppositeCommandUsesCapturedDirectionLeftAsync()
    {
        return AssertWhenPaletteSubmitsOppositeCommandUsesCapturedDirectionAsync(PaneSide.Left);
    }

    /// <summary>Proves the same boundary for the other explicitly named case.</summary>
    [TestMethod]
    public Task HandleAsyncWhenPaletteSubmitsOppositeCommandUsesCapturedDirectionRightAsync()
    {
        return AssertWhenPaletteSubmitsOppositeCommandUsesCapturedDirectionAsync(PaneSide.Right);
    }

    private static async Task AssertWhenPaletteSubmitsOppositeCommandUsesCapturedDirectionAsync(PaneSide activeSide)
    {
        bool leftActive = activeSide == PaneSide.Left;
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        PaneSide sourceSide = leftActive ? PaneSide.Left : PaneSide.Right;
        ScriptedDirectoryReadPort source = leftActive ? left : right;
        ScriptedDirectoryReadPort destination = leftActive ? right : left;
        DirectoryListing listing = DualPaneSessionTestContext.Listing(@"C:\source", ("source.txt", DirectoryEntryKind.File));
        source.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        _ = await session.NavigateAsync(sourceSide, listing.Location, CancellationToken.None);
        RecordingCommanderObserver observer = new();
        if (!leftActive)
        {
            _ = await session.HandleAsync(UserIntent.ActivateOtherPane, observer, CancellationToken.None);
        }
        _ = await session.HandleAsync(UserIntent.ToggleSelection, observer, CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(UserIntent.OpenCommandPalette, observer, CancellationToken.None)).Scopes.CommandPalette);
        Assert.AreSame(CommandAvailability.Available, CommanderSessionTestValues.Candidate(open, UserIntent.OpenCurrentLocationInOtherPane).Availability);
        destination.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSnapshot before = session.Current.Panes.Of(sourceSide);

        CommanderSnapshot after = await session.HandleAsync(UserIntent.SubmitCommand(open, UserIntent.OpenCurrentLocationInOtherPane), observer, CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, after.Scopes.CommandPalette);
        Assert.AreSame(sourceSide, after.Panes.ActiveSide);
        Assert.AreSame(before, after.Panes.Of(sourceSide));
        Assert.AreSame(listing, Assert.IsInstanceOfType<PaneContentListed>(after.Panes.Of(sourceSide.Other).Content).Listing);
        Assert.HasCount(1, destination.Requests);
        Assert.AreSame(listing.Location, destination.Requests[0].Location);
    }

    /// <summary>Proves matching locations remain available in the palette but produce no read or state change.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteLocationsMatchKeepsNoOpAvailableAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = DualPaneSessionTestContext.Listing(@"C:\same", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(listing, listing);
        CommandPaletteSession palette = new();
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>(palette.Open(fixture.Panes.Current, CommandSurface.Palette, InteractionOwnership.ScopeOwnsInput));
        Assert.AreSame(CommandAvailability.Available, CommanderSessionTestValues.Candidate(open, UserIntent.OpenCurrentLocationInOtherPane).Availability);
        DualPaneSnapshot before = fixture.Panes.Current;
        CommandPaletteIntentAccepted accepted = Assert.IsInstanceOfType<CommandPaletteIntentAccepted>(palette.Validate(UserIntent.SubmitCommand(open, UserIntent.OpenCurrentLocationInOtherPane), before, InteractionOwnership.ScopeOwnsInput));

        DualPaneSnapshot after = await fixture.Panes.HandleAsync(accepted.Intent, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(before.Right, after.Right);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
    }

    /// <summary>Proves a passive read started after palette capture makes the submission stale rather than replacing it.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPassivePaneChangedAfterCaptureRejectsSubmissionAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession(out DualPaneSession panes);
        DirectoryListing source = DualPaneSessionTestContext.Listing(@"C:\source");
        left.Enqueue(DirectoryReadOutcome.Succeeded(source, null));
        _ = await session.NavigateAsync(PaneSide.Left, source.Location, CancellationToken.None);
        RecordingCommanderObserver observer = new();
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(UserIntent.OpenCommandPalette, observer, CancellationToken.None)).Scopes.CommandPalette);
        TaskCompletionSource<DirectoryReadOutcome> provider = right.EnqueuePending();
        Task<DualPaneSnapshot> navigation = panes.NavigateAsync(PaneSide.Right, DualPaneSessionTestContext.ParsePath(@"D:\newer"), CancellationToken.None);
        PaneSnapshot pending = panes.Current.Right;

        CommanderSnapshot after = await session.HandleAsync(UserIntent.SubmitCommand(open, UserIntent.OpenCurrentLocationInOtherPane), observer, CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, after.Scopes.CommandPalette);
        Assert.AreSame(pending, after.Panes.Right);
        Assert.AreSame(open.Left, after.Panes.Left);
        Assert.HasCount(1, right.Requests);
        Assert.IsFalse(right.Tokens[0].IsCancellationRequested);
        provider.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await navigation;
    }
}
