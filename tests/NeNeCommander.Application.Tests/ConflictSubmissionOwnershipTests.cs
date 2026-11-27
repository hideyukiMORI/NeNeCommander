using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves delayed modal decisions cannot resolve a later conflict owner.</summary>
[TestClass]
public sealed class ConflictSubmissionOwnershipTests
{
    /// <summary>A delayed Skip cannot skip the newly inspected target.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task HandleAsyncWhenOldSkipArrivesKeepsCurrentConflict()
    {
        await AssertOldDecisionRejectedAsync(TransferConflictDecision.Skip);
    }

    /// <summary>A delayed KeepBoth cannot accept a replacement candidate.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task HandleAsyncWhenOldKeepBothArrivesKeepsCurrentConflict()
    {
        await AssertOldDecisionRejectedAsync(TransferConflictDecision.KeepBoth);
    }

    /// <summary>A delayed Cancel button cannot cancel a replacement conflict.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task HandleAsyncWhenOldCancelArrivesKeepsCurrentConflict()
    {
        await AssertOldDecisionRejectedAsync(TransferConflictDecision.Cancel);
    }

    /// <summary>Value equality cannot impersonate the exact operation instance shown to the user.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task HandleAsyncWhenValueEqualForeignOwnerSubmitsLeavesCurrentUntouched()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        OperationAwaitingConflict current = await PrepareConflictAsync(fixture);
        OperationAwaitingConflict foreign = current with { };
        int calls = fixture.Port.Calls.Count;

        DualPaneSnapshot after = await fixture.Panes.HandleAsync(
            UserIntent.ResolveConflict(foreign, TransferConflictDecision.Cancel, TransferConflictScope.All),
            RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreEqual(current, foreign);
        Assert.AreNotSame(current, foreign);
        Assert.AreSame(current, after.Operation);
        Assert.AreEqual(calls, fixture.Port.Calls.Count);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
    }

    /// <summary>The current Cancel button works once and a duplicate does not restart or refresh anything.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task HandleAsyncWhenCurrentCancelIsRepeatedConsumesOnlyOnce()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        OperationAwaitingConflict current = await PrepareConflictAsync(fixture);
        UserIntent cancel = UserIntent.ResolveConflict(current, TransferConflictDecision.Cancel, TransferConflictScope.Current);
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(
            DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File)), null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(
            DualPaneSessionTestContext.Listing("C:\\right", ("a.txt", DirectoryEntryKind.File)), null));

        DualPaneSnapshot completed = await fixture.Panes.HandleAsync(cancel, RecordingDualPaneObserver.Create(), CancellationToken.None);
        int calls = fixture.Port.Calls.Count;
        DualPaneSnapshot repeated = await fixture.Panes.HandleAsync(cancel, RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationCompleted operation = Assert.IsInstanceOfType<OperationCompleted>(completed.Operation);
        Assert.AreSame(FileOperationCompletionKind.Cancelled, operation.Outcome.Completion);
        Assert.IsEmpty(operation.Outcome.Effects);
        Assert.AreSame(completed.Operation, repeated.Operation);
        Assert.AreEqual(calls, fixture.Port.Calls.Count);
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
    }
    private static async Task AssertOldDecisionRejectedAsync(TransferConflictDecision decision)
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        OperationAwaitingConflict first = await PrepareConflictAsync(fixture);
        UserIntent delayed = UserIntent.ResolveConflict(first, decision, TransferConflictScope.All);
        TransferConflict original = first.Conflicts.Conflicts[0];
        TransferConflict repeated = TransferConflict.Create(
            original.Source.WithConflictChoice(TransferConflictChoice.Create(original, TransferConflictDecision.KeepBoth)),
            original.ExistingTarget,
            DualPaneSessionTestContext.ParsePath("C:\\right\\a (3).txt"), TransferOverwriteCapability.Unavailable(TransferOverwriteUnavailableReason.Unconfirmed));
        fixture.Port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([repeated]));
        DualPaneSnapshot current = await fixture.Panes.HandleAsync(
            UserIntent.ResolveConflict(first, TransferConflictDecision.KeepBoth, TransferConflictScope.Current),
            RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = Assert.IsInstanceOfType<OperationAwaitingConflict>(current.Operation);
        fixture.Port.EnqueuePreflight(TransferPreflightOutcome.Rejected(FileOperationFailureKind.IdentityChanged));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(
            DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File)), null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(
            DualPaneSessionTestContext.Listing("C:\\right", ("a.txt", DirectoryEntryKind.File)), null));
        int calls = fixture.Port.Calls.Count;

        DualPaneSnapshot after = await fixture.Panes.HandleAsync(
            delayed, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(current.Operation, after.Operation);
        Assert.AreEqual(calls, fixture.Port.Calls.Count);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
    }

    private static async Task<OperationAwaitingConflict> PrepareConflictAsync(DualPaneSessionTestContext fixture)
    {
        DirectoryListing left = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File));
        DirectoryListing right = DualPaneSessionTestContext.Listing("C:\\right", ("a.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(left, right);
        FileInspectionSucceeded inspection = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            DualPaneSessionTestContext.Inspection(left.Entries[0].Path));
        TransferConflict conflict = TransferConflict.Create(
            inspection.Snapshot, right.Entries[0].Path,
            DualPaneSessionTestContext.ParsePath("C:\\right\\a (2).txt"), TransferOverwriteCapability.Unavailable(TransferOverwriteUnavailableReason.Unconfirmed));
        fixture.Port.EnqueueInspection(inspection);
        fixture.Port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(
            UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);
        return Assert.IsInstanceOfType<OperationAwaitingConflict>(snapshot.Operation);
    }
}
