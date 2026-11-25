using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves DualPaneConflictSession behavior and boundaries.</summary>
[TestClass]
public sealed class DualPaneConflictSessionTests
{
    /// <summary>Proves the session owns the frozen conflict continuation, starts on Cancel, and freezes both panes.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task HandleAsyncWhenCopyConflictsAwaitsDecisionWithCancelFocusedAndFrozenPanes()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        DirectoryListing rightListing = DualPaneSessionTestContext.Listing("C:\\right", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, rightListing);
        FileInspectionSucceeded inspection = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        TransferConflict conflict = TransferConflict.Create(
            inspection.Snapshot,
            rightListing.Entries[0].Path,
            DualPaneSessionTestContext.ParsePath("C:\\right\\a (2).txt"), null);
        fixture.Port.EnqueueInspection(inspection);
        fixture.Port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));

        DualPaneSnapshot awaitingSnapshot = await fixture.Panes.HandleAsync(
            UserIntent.Copy,
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);
        await AssertConflictFreezesPanesAsync(fixture, awaitingSnapshot);
        TransferConflict repeatedConflict = await RepeatConflictAsync(fixture, conflict);

        fixture.Port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([
            TransferPlanEntry.Skip(
                repeatedConflict.Source.WithConflictChoice(TransferConflictChoice.Create(
                    repeatedConflict,
                    TransferConflictDecision.Skip)),
                conflict.ExistingTarget)]));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        DualPaneSnapshot completedSnapshot = await fixture.Panes.HandleAsync(
            UserIntent.ResolveConflict(Assert.IsInstanceOfType<OperationAwaitingConflict>(fixture.Panes.Current.Operation), TransferConflictDecision.Skip, TransferConflictScope.All),
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);

        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(completedSnapshot.Operation);
        Assert.HasCount(1, completed.Outcome.NotTransferred);
        Assert.IsEmpty(completed.Outcome.Effects);
        Assert.AreEqual(1, fixture.Port.Calls.Count(call => call.StartsWith("Inspect:", StringComparison.Ordinal)));
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    /// <summary>
    /// Proves Escape while a resumed transfer runs cancels that resumed attempt through the session's
    /// own cancellation source, with no filesystem effect, and both panes refresh.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task HandleAsyncWhenEscapeArrivesDuringResumeCancelsTheResumedTransfer()
    {
        DualPaneSessionTestContext? owner = null;
        bool armed = false;
        Task<DualPaneSnapshot>? duringResume = null;
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create(
            ScriptedCallbackPoint.AfterPreflight,
            () =>
            {
                if (armed)
                {
                    armed = false;
                    duringResume = owner!.Panes.HandleAsync(
                        UserIntent.Escape,
                        RecordingDualPaneObserver.Create(),
                        CancellationToken.None);
                }
            });
        owner = fixture;
        await PrepareResumedSkipAsync(fixture);
        armed = true;

        DualPaneSnapshot completedSnapshot = await fixture.Panes.HandleAsync(
            UserIntent.ResolveConflict(Assert.IsInstanceOfType<OperationAwaitingConflict>(fixture.Panes.Current.Operation), TransferConflictDecision.Skip, TransferConflictScope.Current),
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);

        Assert.IsNotNull(duringResume);
        _ = Assert.IsInstanceOfType<OperationRunning>((await duringResume).Operation);
        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(completedSnapshot.Operation);
        Assert.AreSame(FileOperationCompletionKind.Cancelled, completed.Outcome.Completion);
        Assert.IsEmpty(completed.Outcome.Effects);
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
        DualPaneSnapshot afterEscape = await fixture.Panes.HandleAsync(
            UserIntent.Escape,
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);
        _ = Assert.IsInstanceOfType<OperationCompleted>(afterEscape.Operation);
    }

    /// <summary>
    /// Proves a resumed transfer that throws leaves no running activity behind, as a first attempt does.
    /// </summary>
    [TestMethod]
    public async Task HandleAsyncWhenResumedTransferThrowsLeavesNoRunningOperation()
    {
        bool armed = false;
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create(
            ScriptedCallbackPoint.AfterPreflight,
            () =>
            {
                if (armed)
                {
                    armed = false;
                    throw new InvalidOperationException("provider defect");
                }
            });
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        DirectoryListing rightListing = DualPaneSessionTestContext.Listing("C:\\right", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, rightListing);
        FileInspectionSucceeded inspection = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        TransferConflict conflict = TransferConflict.Create(
            inspection.Snapshot,
            rightListing.Entries[0].Path,
            DualPaneSessionTestContext.ParsePath("C:\\right\\a (2).txt"), null);
        fixture.Port.EnqueueInspection(inspection);
        fixture.Port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        _ = await fixture.Panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([
            TransferPlanEntry.Skip(
                conflict.Source.WithConflictChoice(TransferConflictChoice.Create(conflict, TransferConflictDecision.Skip)),
                conflict.ExistingTarget)]));
        armed = true;

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => fixture.Panes.HandleAsync(
            UserIntent.ResolveConflict(Assert.IsInstanceOfType<OperationAwaitingConflict>(fixture.Panes.Current.Operation), TransferConflictDecision.Skip, TransferConflictScope.Current),
            RecordingDualPaneObserver.Create(),
            CancellationToken.None));

        Assert.AreSame(OperationActivity.Idle, fixture.Panes.Current.Operation);
    }

    /// <summary>Proves Escape consumes and cancels a conflict continuation without a filesystem effect.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenConflictIsAwaitingEscapeCancelsAndRefreshesBothPanes()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        DirectoryListing rightListing = DualPaneSessionTestContext.Listing("C:\\right", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, rightListing);
        FileInspectionSucceeded inspection = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        TransferConflict conflict = TransferConflict.Create(
            inspection.Snapshot,
            rightListing.Entries[0].Path,
            DualPaneSessionTestContext.ParsePath("C:\\right\\a (2).txt"), null);
        fixture.Port.EnqueueInspection(inspection);
        fixture.Port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        _ = await fixture.Panes.HandleAsync(
            UserIntent.Copy,
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));

        DualPaneSnapshot cancelledSnapshot = await fixture.Panes.HandleAsync(
            UserIntent.Escape,
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);

        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(cancelledSnapshot.Operation);
        Assert.AreSame(FileOperationCompletionKind.Cancelled, completed.Outcome.Completion);
        Assert.IsEmpty(completed.Outcome.Effects);
        Assert.HasCount(2, fixture.Port.Calls);
    }

    private static async Task PrepareResumedSkipAsync(DualPaneSessionTestContext fixture)
    {
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        DirectoryListing rightListing = DualPaneSessionTestContext.Listing("C:\\right", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, rightListing);
        FileInspectionSucceeded inspection = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        TransferConflict conflict = TransferConflict.Create(
            inspection.Snapshot,
            rightListing.Entries[0].Path,
            DualPaneSessionTestContext.ParsePath("C:\\right\\a (2).txt"), null);
        fixture.Port.EnqueueInspection(inspection);
        fixture.Port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        _ = await fixture.Panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([
            TransferPlanEntry.Skip(
                conflict.Source.WithConflictChoice(TransferConflictChoice.Create(conflict, TransferConflictDecision.Skip)),
                conflict.ExistingTarget)]));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
    }

    private static async Task AssertConflictFreezesPanesAsync(DualPaneSessionTestContext fixture, DualPaneSnapshot awaitingSnapshot)
    {
        OperationAwaitingConflict awaiting = Assert.IsInstanceOfType<OperationAwaitingConflict>(
            awaitingSnapshot.Operation);
        DualPaneSnapshot frozen = await fixture.Panes.HandleAsync(
            UserIntent.ActivateOtherPane,
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);
        DualPaneSnapshot navigation = await fixture.Panes.NavigateAsync(
            PaneSide.Right,
            DualPaneSessionTestContext.ParsePath("C:\\elsewhere"),
            CancellationToken.None);

        Assert.AreSame(TransferConflictDecision.Cancel, awaiting.InitialFocus);
        Assert.AreSame(PaneSide.Left, frozen.ActiveSide);
        Assert.AreEqual(awaitingSnapshot, navigation);
    }

    private static async Task<TransferConflict> RepeatConflictAsync(DualPaneSessionTestContext fixture, TransferConflict conflict)
    {
        FileEntrySnapshot resolvedCurrent = conflict.Source.WithConflictChoice(
            TransferConflictChoice.Create(conflict, TransferConflictDecision.Skip));
        TransferConflict repeatedConflict = TransferConflict.Create(
            resolvedCurrent,
            conflict.ExistingTarget,
            conflict.KeepBothCandidate, null);
        fixture.Port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([repeatedConflict]));
        DualPaneSnapshot repeatedSnapshot = await fixture.Panes.HandleAsync(
            UserIntent.ResolveConflict(Assert.IsInstanceOfType<OperationAwaitingConflict>(fixture.Panes.Current.Operation), TransferConflictDecision.Skip, TransferConflictScope.Current),
            RecordingDualPaneObserver.Create(),
            CancellationToken.None);
        _ = Assert.IsInstanceOfType<OperationAwaitingConflict>(repeatedSnapshot.Operation);
        return repeatedConflict;
    }
}
