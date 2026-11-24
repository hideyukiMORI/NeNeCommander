using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves pointer input cannot replace frozen gateway subjects or pending decisions.</summary>
[TestClass]
public sealed class PanePointerOperationTests
{
    /// <summary>Proves a permanent deletion question retains its exact request after pointer input.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenDeletionAwaitsConfirmationRejectsPointerChangesAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        await fixture.ListBothAsync(listing, listing);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(listing.Entries[0].Path, DeletionCapability.PermanentOnly));
        DualPaneSnapshot before = await fixture.Panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = Assert.IsInstanceOfType<OperationAwaitingConfirmation>(before.Operation);

        await AssertFrozenAsync(fixture.Panes, before, listing.Entries[1]);

        Assert.HasCount(1, fixture.Port.Calls);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
    }

    /// <summary>Proves a transfer conflict retains its exact continuation and sources after clicks.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenTransferAwaitsConflictRejectsPointerChangesAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = Listing();
        DirectoryListing destination = DualPaneSessionTestContext.Listing("C:\\other", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(listing, destination);
        FileInspectionSucceeded inspection = Assert.IsInstanceOfType<FileInspectionSucceeded>(DualPaneSessionTestContext.Inspection(listing.Entries[0].Path));
        TransferConflict conflict = TransferConflict.Create(inspection.Snapshot, destination.Entries[0].Path,
            DualPaneSessionTestContext.ParsePath("C:\\other\\a (2).txt"));
        fixture.Port.EnqueueInspection(inspection);
        fixture.Port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        DualPaneSnapshot before = await fixture.Panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = Assert.IsInstanceOfType<OperationAwaitingConflict>(before.Operation);

        await AssertFrozenAsync(fixture.Panes, before, destination.Entries[1]);

        Assert.HasCount(2, fixture.Port.Calls);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
    }

    /// <summary>Proves reentrant pointer input during gateway inspection cannot activate another pane.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenGatewayIsRunningRejectsReentrantPointerChangesAsync()
    {
        DualPaneSessionTestContext? running = null;
        Task? rejected = null;
        DirectoryListing listing = Listing();
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create(ScriptedCallbackPoint.AfterInspection,
            () => rejected = AssertRunningFrozenAsync(running));
        running = fixture;
        await fixture.ListBothAsync(listing, DualPaneSessionTestContext.Listing("C:\\other", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File)));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(listing.Entries[0].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\other"), null));

        DualPaneSnapshot after = await fixture.Panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.IsNotNull(rejected);
        await rejected;
        Assert.AreSame(PaneSide.Left, after.ActiveSide);
        Assert.HasCount(4, fixture.Port.Calls);
    }

    private static async Task AssertRunningFrozenAsync(DualPaneSessionTestContext? fixture)
    {
        Assert.IsNotNull(fixture);
        DualPaneSnapshot before = fixture.Panes.Current;
        _ = Assert.IsInstanceOfType<OperationRunning>(before.Operation);
        DirectoryEntry entry = Assert.IsInstanceOfType<PaneContentListed>(before.Right.Content).Listing.Entries[1];
        await AssertFrozenAsync(fixture.Panes, before, entry);
    }

    private static async Task AssertFrozenAsync(DualPaneSession panes, DualPaneSnapshot before, DirectoryEntry entry)
    {
        UserIntent[] intents = [
            UserIntent.InteractWithPaneEntry(PaneSide.Right, before.Right, entry, PaneEntryAction.Select),
            UserIntent.InteractWithPaneEntry(PaneSide.Right, before.Right, entry, PaneEntryAction.Open),
            UserIntent.InteractWithPaneSurface(PaneSide.Right, before.Right, PaneSurfaceAction.Activate),
            UserIntent.InteractWithPaneSurface(PaneSide.Right, before.Right, PaneSurfaceAction.ClearSelection),
        ];
        foreach (UserIntent intent in intents)
        {
            DualPaneSnapshot after = await panes.HandleAsync(intent, RecordingDualPaneObserver.Create(), CancellationToken.None);
            Assert.AreSame(before.ActiveSide, after.ActiveSide);
            Assert.AreSame(before.Left, after.Left);
            Assert.AreSame(before.Right, after.Right);
            Assert.AreSame(before.Operation, after.Operation);
        }
    }

    private static DirectoryListing Listing()
    {
        return DualPaneSessionTestContext.Listing("C:\\pointer", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
    }
}
