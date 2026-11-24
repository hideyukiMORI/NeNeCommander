using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves rejected pointer owners cannot activate a pane, change marks or open entries.</summary>
[TestClass]
public sealed class PanePointerOwnershipTests
{
    /// <summary>Proves a stale request is rejected even when current listing and focus still match it.</summary>
    [TestMethod]
    public async Task StaleSnapshotWithSameFocusedEntryDoesNotCreateAnAcceptedTransition()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        await fixture.ListBothAsync(listing, listing);
        PaneSnapshot stale = fixture.Panes.Current.Left;
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot before = fixture.Panes.Current;
        Assert.AreNotSame(stale, before.Left);
        Assert.AreSame(DualPaneSessionTestContext.Focus(stale), DualPaneSessionTestContext.Focus(before.Left));

        DualPaneSnapshot after = await fixture.Panes.HandleAsync(UserIntent.InteractWithPaneEntry(PaneSide.Left, stale, listing.Entries[0], PaneEntryAction.Open), RecordingDualPaneObserver.Create(), CancellationToken.None);

        AssertUnchanged(before, after);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
    }

    /// <summary>Proves equal-valued entry records cannot substitute for an owned visible entry.</summary>
    [TestMethod]
    public async Task ForeignEntryWithSameValueIsRejectedBeforeInactivePaneActivation()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        await fixture.ListBothAsync(listing, listing);
        DirectoryEntry owned = listing.Entries[0];
        DirectoryEntry foreign = DirectoryEntry.Create(owned.Path, owned.Name, owned.Kind, owned.Metadata);
        Assert.AreEqual(owned, foreign);
        Assert.AreNotSame(owned, foreign);
        DualPaneSnapshot before = fixture.Panes.Current;

        DualPaneSnapshot after = await fixture.Panes.HandleAsync(UserIntent.InteractWithPaneEntry(PaneSide.Right, before.Right, foreign, PaneEntryAction.Open), RecordingDualPaneObserver.Create(), CancellationToken.None);

        AssertUnchanged(before, after);
    }

    /// <summary>Proves a current snapshot of the other pane is not the requested pane's owner.</summary>
    [TestMethod]
    public async Task OtherPaneSnapshotCannotAuthorizeRowOrBackgroundInput()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        await fixture.ListBothAsync(listing, listing);
        DualPaneSnapshot before = fixture.Panes.Current;

        DualPaneSnapshot row = await fixture.Panes.HandleAsync(UserIntent.InteractWithPaneEntry(PaneSide.Right, before.Left, listing.Entries[0], PaneEntryAction.Select), RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot background = await fixture.Panes.HandleAsync(UserIntent.InteractWithPaneSurface(PaneSide.Right, before.Left, PaneSurfaceAction.ClearSelection), RecordingDualPaneObserver.Create(), CancellationToken.None);

        AssertUnchanged(before, row);
        AssertUnchanged(before, background);
    }

    /// <summary>Proves membership in the listing alone cannot admit an invisible entry.</summary>
    [TestMethod]
    public async Task HiddenEntryIsRejectedEvenWithItsCurrentOwner()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = DualPaneSessionTestContext.ListingWithVisibility("C:\\pointer", ("a", DirectoryEntryKind.File, EntryVisibility.Normal), ("b", DirectoryEntryKind.File, EntryVisibility.Hidden));
        await fixture.ListBothAsync(listing, listing);
        DualPaneSnapshot before = fixture.Panes.Current;

        DualPaneSnapshot after = await fixture.Panes.HandleAsync(UserIntent.InteractWithPaneEntry(PaneSide.Right, before.Right, listing.Entries[1], PaneEntryAction.Open), RecordingDualPaneObserver.Create(), CancellationToken.None);

        AssertUnchanged(before, after);
    }

    /// <summary>Proves a left-pane read freezes current pointer owners.</summary>
    [TestMethod]
    public async Task LoadingLeftPaneFreezesCurrentPointerOwnersAsync()
    {
        await AssertReadingFrozenAsync(PaneSide.Left);
    }

    /// <summary>Proves a right-pane read freezes current pointer owners.</summary>
    [TestMethod]
    public async Task LoadingRightPaneFreezesCurrentPointerOwnersAsync()
    {
        await AssertReadingFrozenAsync(PaneSide.Right);
    }

    private static async Task AssertReadingFrozenAsync(PaneSide readingSide)
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        await fixture.ListBothAsync(listing, listing);
        ScriptedDirectoryReadPort port = readingSide == PaneSide.Left ? fixture.Left : fixture.Right;
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        Task<DualPaneSnapshot> read = fixture.Panes.NavigateAsync(readingSide, listing.Location, CancellationToken.None);
        DualPaneSnapshot before = fixture.Panes.Current;

        DualPaneSnapshot row = await fixture.Panes.HandleAsync(UserIntent.InteractWithPaneEntry(PaneSide.Right, before.Right, listing.Entries[1], PaneEntryAction.Select), RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot background = await fixture.Panes.HandleAsync(UserIntent.InteractWithPaneSurface(PaneSide.Right, before.Right, PaneSurfaceAction.ClearSelection), RecordingDualPaneObserver.Create(), CancellationToken.None);

        AssertUnchanged(before, row);
        AssertUnchanged(before, background);
        pending.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await read;
    }

    /// <summary>Proves an unlisted pane can be explicitly activated without inventing content.</summary>
    [TestMethod]
    public async Task EmptyUnlistedHeaderCanActivateWithNewReceiptAndPreservedContent()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DualPaneSnapshot before = fixture.Panes.Current;

        DualPaneSnapshot after = await fixture.Panes.HandleAsync(UserIntent.InteractWithPaneSurface(PaneSide.Right, before.Right, PaneSurfaceAction.Activate), RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(PaneSide.Right, after.ActiveSide);
        Assert.AreSame(before.Left, after.Left);
        Assert.AreNotSame(before.Right, after.Right);
        Assert.AreSame(before.Right.Content, after.Right.Content);
        Assert.AreSame(before.Right.Activity, after.Right.Activity);
        Assert.IsEmpty(fixture.Left.Requests);
        Assert.IsEmpty(fixture.Right.Requests);
    }

    /// <summary>Proves cancelled read indications survive header activation and row selection.</summary>
    [TestMethod]
    public async Task TerminalReadIndicationSurvivesAcceptedSelection()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        await fixture.ListBothAsync(listing, listing);
        fixture.Right.Enqueue(DirectoryReadOutcome.Cancelled());
        _ = await fixture.Panes.NavigateAsync(PaneSide.Right, listing.Location, CancellationToken.None);
        PaneSnapshot before = fixture.Panes.Current.Right;
        _ = Assert.IsInstanceOfType<PaneReadCancelled>(before.Activity);

        DualPaneSnapshot after = await fixture.Panes.HandleAsync(UserIntent.InteractWithPaneEntry(PaneSide.Right, before, listing.Entries[1], PaneEntryAction.Select), RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(PaneSide.Right, after.ActiveSide);
        Assert.AreSame(before.Activity, after.Right.Activity);
        Assert.AreSame(listing.Entries[1].Path, DualPaneSessionTestContext.Focus(after.Right));
    }

    private static DirectoryListing Listing()
    {
        return DualPaneSessionTestContext.Listing("C:\\pointer", ("a", DirectoryEntryKind.File), ("b", DirectoryEntryKind.File));
    }

    private static void AssertUnchanged(DualPaneSnapshot before, DualPaneSnapshot after)
    {
        Assert.AreSame(before.ActiveSide, after.ActiveSide);
        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(before.Right, after.Right);
        Assert.AreSame(before.Operation, after.Operation);
    }
}
