using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves DualPaneMoveSession behavior and boundaries.</summary>
[TestClass]
public sealed class DualPaneMoveSessionTests
{
    /// <summary>Proves the focus item moves to the passive location and both panes are re-read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMoveHasNoSelectionMovesFocusItemAndRefreshesBothPanes()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        DirectoryListing leftAfter = DualPaneSessionTestContext.Listing("C:\\left", ("b.txt", DirectoryEntryKind.File));
        DirectoryListing rightAfter = DualPaneSessionTestContext.Listing("C:\\right", ("a.txt", DirectoryEntryKind.File));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftAfter, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(rightAfter, null));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreEqual("Preflight:C:\\right", fixture.Port.Calls[1]);
        Assert.AreEqual("Copy:C:\\left\\a.txt", fixture.Port.Calls[2]);
        Assert.AreSame(
            FileOperationCompletionKind.Succeeded,
            Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation).Outcome.Completion);
        Assert.AreSame(leftAfter, Assert.IsInstanceOfType<PaneContentListed>(snapshot.Left.Content).Listing);
        Assert.AreSame(rightAfter, Assert.IsInstanceOfType<PaneContentListed>(snapshot.Right.Content).Listing);
        Assert.AreSame(leftAfter.Entries[0].Path, DualPaneSessionTestContext.Focus(snapshot.Left));
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    /// <summary>Proves an explicit selection is moved instead of the focus item and focus is kept on refresh.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMoveHasSelectionMovesSelectionAndKeepsFocus()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing(
            "C:\\left",
            ("a.txt", DirectoryEntryKind.File),
            ("b.txt", DirectoryEntryKind.File),
            ("c.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[1].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        for (int index = 0; index < 2; index++)
        {
            fixture.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
            fixture.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
            fixture.Port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        }
        DirectoryListing leftAfter = DualPaneSessionTestContext.Listing("C:\\left", ("c.txt", DirectoryEntryKind.File));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftAfter, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File)), null));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreEqual("Inspect:C:\\left\\a.txt", fixture.Port.Calls[0]);
        Assert.AreEqual("Inspect:C:\\left\\b.txt", fixture.Port.Calls[1]);
        Assert.HasCount(9, fixture.Port.Calls);
        PaneContentListed left = Assert.IsInstanceOfType<PaneContentListed>(snapshot.Left.Content);
        Assert.AreSame(leftAfter.Entries[0].Path, left.State.FocusItem);
        Assert.IsEmpty(left.State.Selection);
    }

    /// <summary>Proves a move without a listed passive pane or without a focus item starts nothing.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMoveHasNoDestinationOrNoSourceDoesNothing()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File)), null));
        DualPaneSnapshot leftOnly = await fixture.Panes.NavigateAsync(PaneSide.Left, DualPaneSessionTestContext.ParsePath("C:\\left"), CancellationToken.None);

        DualPaneSnapshot noDestination = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right"), null));
        _ = await fixture.Panes.NavigateAsync(PaneSide.Right, DualPaneSessionTestContext.ParsePath("C:\\right"), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPaneSnapshot noSource = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreEqual(leftOnly, noDestination);
        Assert.AreSame(OperationActivity.Idle, noSource.Operation);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves a request the gateway would reject is recorded without touching the port.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMoveTargetsItsOwnSourceRecordsRequestRejection()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await fixture.ListBothAsync(DualPaneSessionTestContext.Listing("C:\\", ("Users", DirectoryEntryKind.Directory)), DualPaneSessionTestContext.Listing("C:\\Users"));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(
            FileOperationRequestFailureKind.DestinationIsSource,
            Assert.IsInstanceOfType<OperationRequestRejected>(snapshot.Operation).Failure);
        Assert.AreSame(OperationKind.Move, Assert.IsInstanceOfType<OperationRequestRejected>(snapshot.Operation).Kind);
        Assert.IsEmpty(fixture.Port.Calls);
        Assert.HasCount(1, fixture.Left.Requests);
    }

    /// <summary>Proves a failed gateway outcome is recorded and both panes are still re-read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMoveFailsRecordsOutcomeAndRefreshesPanes()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        fixture.Port.EnqueueInspection(FileInspectionOutcome.Failed(FileOperationFailureKind.AccessDenied));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right"), null));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation);
        Assert.AreSame(OperationKind.Move, completed.Kind);
        Assert.AreSame(FileOperationCompletionKind.Rejected, completed.Outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, completed.Outcome.Failure);
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
    }
}
