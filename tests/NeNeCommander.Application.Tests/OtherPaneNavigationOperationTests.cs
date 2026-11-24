using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves operation ownership remains above opposite-pane navigation.</summary>
[TestClass]
public sealed class OtherPaneNavigationOperationTests
{
    /// <summary>Proves both name-entry operations refuse navigation without changing their captured subject.</summary>
    [TestMethod]
    public Task HandleAsyncWhenNameEntryOwnsInputKeepsCapturedSubjectRenameAsync()
    {
        return AssertWhenNameEntryOwnsInputKeepsCapturedSubjectAsync(UserIntent.Rename);
    }

    /// <summary>Proves the same boundary for the other explicitly named case.</summary>
    [TestMethod]
    public Task HandleAsyncWhenNameEntryOwnsInputKeepsCapturedSubjectCreateDirectoryAsync()
    {
        return AssertWhenNameEntryOwnsInputKeepsCapturedSubjectAsync(UserIntent.CreateDirectory);
    }

    private static async Task AssertWhenNameEntryOwnsInputKeepsCapturedSubjectAsync(UserIntent nameIntent)
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await ListAsync(fixture);
        DualPaneSnapshot before = await HandleAsync(fixture, nameIntent);
        _ = Assert.IsInstanceOfType<OperationAwaitingName>(before.Operation);

        await AssertFrozenAsync(fixture, before);

        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves a pending deletion confirmation cannot be displaced by opposite navigation.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenConfirmationOwnsInputKeepsConfirmedSourcesAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await ListAsync(fixture);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(DualPaneSessionTestContext.ParsePath(@"C:\source\a.txt"), DeletionCapability.PermanentOnly));
        DualPaneSnapshot before = await HandleAsync(fixture, UserIntent.Delete);
        _ = Assert.IsInstanceOfType<OperationAwaitingConfirmation>(before.Operation);

        await AssertFrozenAsync(fixture, before);

        Assert.HasCount(1, fixture.Port.Calls);
    }

    /// <summary>Proves a transfer conflict keeps its continuation and performs no new read or effect.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenConflictOwnsInputKeepsContinuationAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await ListAsync(fixture);
        FileInspectionSucceeded inspection = Assert.IsInstanceOfType<FileInspectionSucceeded>(DualPaneSessionTestContext.Inspection(DualPaneSessionTestContext.ParsePath(@"C:\source\a.txt")));
        TransferConflict conflict = TransferConflict.Create(inspection.Snapshot,
            DualPaneSessionTestContext.ParsePath(@"D:\target\a.txt"), DualPaneSessionTestContext.ParsePath(@"D:\target\a (2).txt"));
        fixture.Port.EnqueueInspection(inspection);
        fixture.Port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        DualPaneSnapshot before = await HandleAsync(fixture, UserIntent.Copy);
        _ = Assert.IsInstanceOfType<OperationAwaitingConflict>(before.Operation);
        int calls = fixture.Port.Calls.Count;

        await AssertFrozenAsync(fixture, before);

        Assert.HasCount(calls, fixture.Port.Calls);
    }

    /// <summary>Proves a running operation refuses the command, then its completed state allows it normally.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOperationRunsRefusesUntilCompletedAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await ListAsync(fixture);
        BlockingInspectionPort blocking = BlockingInspectionPort.Create(DualPaneSessionTestContext.Inspection(DualPaneSessionTestContext.ParsePath(@"C:\source\a.txt")));
        using FileOperationGateway gateway = new(blocking);
        DualPaneSession panes = new(fixture.LeftSession, fixture.RightSession, gateway);
        RecordingDualPaneObserver observer = RecordingDualPaneObserver.Create();
        Task<DualPaneSnapshot> move = panes.HandleAsync(UserIntent.Move, observer, CancellationToken.None);
        DualPaneSnapshot before = panes.Current;

        DualPaneSnapshot frozen = await panes.HandleAsync(UserIntent.OpenCurrentLocationInOtherPane, observer, CancellationToken.None);

        _ = Assert.IsInstanceOfType<OperationRunning>(before.Operation);
        Assert.IsFalse(before.CanOpenCurrentLocationInOtherPane);
        Assert.AreSame(before.Operation, frozen.Operation);
        Assert.AreSame(before.Left, frozen.Left);
        Assert.AreSame(before.Right, frozen.Right);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
        DirectoryListing remaining = DualPaneSessionTestContext.Listing(@"C:\source");
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(remaining, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing(@"D:\target", ("a.txt", DirectoryEntryKind.File)), null));
        blocking.Release();
        DualPaneSnapshot completed = await move;
        _ = Assert.IsInstanceOfType<OperationCompleted>(completed.Operation);
        Assert.IsTrue(completed.CanOpenCurrentLocationInOtherPane);
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(remaining, null));
        DualPaneSnapshot navigated = await panes.HandleAsync(UserIntent.OpenCurrentLocationInOtherPane, observer, CancellationToken.None);
        Assert.AreSame(completed.Left, navigated.Left);
        Assert.AreSame(remaining, Assert.IsInstanceOfType<PaneContentListed>(navigated.Right.Content).Listing);
        Assert.HasCount(3, fixture.Right.Requests);
    }

    /// <summary>Proves a rejected operation is historical status and does not freeze a new valid navigation.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPreviousOperationWasRejectedAllowsNavigationAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await ListAsync(fixture);
        _ = await HandleAsync(fixture, UserIntent.Rename);
        DualPaneSnapshot before = await HandleAsync(fixture, UserIntent.SubmitName("bad:name"));
        _ = Assert.IsInstanceOfType<OperationRequestRejected>(before.Operation);
        DirectoryListing fresh = DualPaneSessionTestContext.Listing(@"C:\source");
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(fresh, null));

        DualPaneSnapshot after = await HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);

        Assert.IsTrue(before.CanOpenCurrentLocationInOtherPane);
        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(fresh, Assert.IsInstanceOfType<PaneContentListed>(after.Right.Content).Listing);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    private static async Task AssertFrozenAsync(DualPaneSessionTestContext fixture, DualPaneSnapshot before)
    {
        DualPaneSnapshot after = await HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);
        Assert.IsFalse(before.CanOpenCurrentLocationInOtherPane);
        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(before.Right, after.Right);
        Assert.AreSame(before.Operation, after.Operation);
        Assert.AreSame(before.ActiveSide, after.ActiveSide);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
    }

    private static Task ListAsync(DualPaneSessionTestContext fixture)
    {
        return fixture.ListBothAsync(
            DualPaneSessionTestContext.Listing(@"C:\source", ("a.txt", DirectoryEntryKind.File)),
            DualPaneSessionTestContext.Listing(@"D:\target", ("a.txt", DirectoryEntryKind.File)));
    }

    private static Task<DualPaneSnapshot> HandleAsync(DualPaneSessionTestContext fixture, UserIntent intent)
    {
        return fixture.Panes.HandleAsync(intent, RecordingDualPaneObserver.Create(), CancellationToken.None);
    }
}
