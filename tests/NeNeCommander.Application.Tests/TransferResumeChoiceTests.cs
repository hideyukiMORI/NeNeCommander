using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves TransferResumeChoice behavior and boundaries.</summary>
[TestClass]
public sealed class TransferResumeChoiceTests
{
    /// <summary>Requires Skip to complete as a non-effect without copy or deletion.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenConflictIsSkippedRecordsNotTransferredWithoutEffect()
    {
        FileSystemPath source = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        TransferConflict conflict = TransferConflictTestContext.Conflict(source, destination, "source (2).txt");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(TransferConflictTestContext.Inspection(source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([
            TransferPlanEntry.Skip(
                TransferConflictTestContext.ResolvedSource(conflict, TransferConflictDecision.Skip),
                conflict.ExistingTarget)]));
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy([source], destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();
        FileOperationOutcome completed = await gateway.ResumeAsync(
            awaiting.Continuation!,
            TransferConflictResponse.Create(TransferConflictDecision.Skip, TransferConflictScope.Current),
            progress,
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Completion);
        Assert.IsEmpty(completed.Effects);
        Assert.HasCount(1, completed.NotTransferred);
        Assert.HasCount(3, port.Calls);
        Assert.HasCount(1, progress.Reports);
        Assert.AreEqual(1, progress.Reports[0].Completed);
        Assert.AreEqual(1, progress.Reports[0].Total);

        FileOperationOutcome replayed = await TransferConflictTestContext.ResumeSkipAsync(gateway, awaiting.Continuation!);
        Assert.AreSame(FileOperationFailureKind.Reentrant, replayed.Failure);
        Assert.HasCount(3, port.Calls);
    }

    /// <summary>Requires a provider plan to preserve the explicit disposition chosen by the user.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ResumeAsyncWhenProviderIgnoresSkipChoiceRejectsBeforeEffect()
    {
        FileSystemPath source = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        TransferConflict conflict = TransferConflictTestContext.Conflict(source, destination, "source (2).txt");
        FileEntrySnapshot resolved = TransferConflictTestContext.ResolvedSource(conflict, TransferConflictDecision.Skip);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(TransferConflictTestContext.Inspection(source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([
            TransferPlanEntry.Transfer(resolved, conflict.ExistingTarget)]));
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy([source], destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        FileOperationOutcome rejected = await TransferConflictTestContext.ResumeSkipAsync(gateway, awaiting.Continuation!);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, rejected.Failure);
        Assert.IsEmpty(rejected.Effects);
        Assert.HasCount(3, port.Calls);
    }

    /// <summary>Requires provider plans to retain the exact operation-scoped choice and KeepBoth target.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ResumeAsyncWhenProviderChangesChoiceOrKeepBothTargetRejectsBeforeEffect()
    {
        FileSystemPath source = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath other = TransferConflictTestContext.ParsePath("C:\\other.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        TransferConflict conflict = TransferConflictTestContext.Conflict(source, destination, "source (2).txt");
        TransferConflict otherSourceConflict = TransferConflictTestContext.Conflict(other, destination, "other (2).txt");
        TransferConflict otherCandidateConflict = TransferConflictTestContext.Conflict(source, destination, "source (3).txt");
        FileEntrySnapshot matchingSkip = TransferConflictTestContext.ResolvedSource(conflict, TransferConflictDecision.Skip);
        FileEntrySnapshot matchingKeepBoth = TransferConflictTestContext.ResolvedSource(conflict, TransferConflictDecision.KeepBoth);

        (TransferConflictDecision Decision, TransferPlanEntry Entry)[] invalidPlans =
        [
            (TransferConflictDecision.Skip, TransferPlanEntry.Skip(conflict.Source, conflict.ExistingTarget)),
            (TransferConflictDecision.Skip, TransferPlanEntry.Skip( conflict.Source.WithConflictChoice(TransferConflictChoice.Create( otherSourceConflict, TransferConflictDecision.Skip)), conflict.ExistingTarget)),
            (TransferConflictDecision.Skip, TransferPlanEntry.Skip(matchingKeepBoth, conflict.ExistingTarget)),
            (TransferConflictDecision.Skip, TransferPlanEntry.Skip( conflict.Source.WithConflictChoice(TransferConflictChoice.Create( otherCandidateConflict, TransferConflictDecision.Skip)), conflict.ExistingTarget)),
            (TransferConflictDecision.KeepBoth, TransferPlanEntry.Skip(matchingKeepBoth, conflict.ExistingTarget)),
            (TransferConflictDecision.KeepBoth, TransferPlanEntry.Transfer(matchingKeepBoth, conflict.ExistingTarget)),
        ];
        foreach ((TransferConflictDecision decision, TransferPlanEntry entry) in invalidPlans)
        {
            await AssertResolvedPlanRejected(conflict, decision, entry);
        }

        Assert.AreSame(TransferConflictDecision.Skip, matchingSkip.ConflictChoice!.Decision);
    }

    /// <summary>Requires a skipped move to avoid capability discovery and every filesystem effect.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenMoveConflictIsSkippedDoesNotQueryMoveCapability()
    {
        FileSystemPath source = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        TransferConflict conflict = TransferConflictTestContext.Conflict(source, destination, "source (2).txt");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(TransferConflictTestContext.Inspection(source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([
            TransferPlanEntry.Skip(
                TransferConflictTestContext.ResolvedSource(conflict, TransferConflictDecision.Skip),
                conflict.ExistingTarget)]));
        using FileOperationGateway gateway = new(port);
        MoveRequest request = (MoveRequest)((FileOperationRequestAccepted)MoveRequest.Create(
            [source],
            destination)).Request;
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            request,
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        FileOperationOutcome completed = await TransferConflictTestContext.ResumeSkipAsync(gateway, awaiting.Continuation!);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Completion);
        Assert.HasCount(1, completed.NotTransferred);
        Assert.IsFalse(port.Calls.Any(call => call.StartsWith("AtomicMoveCapability:", StringComparison.Ordinal)));
    }

    /// <summary>Requires a KeepBoth move to use composite copy and verification before source deletion.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenMoveKeepsBothDoesNotQueryAtomicMoveCapability()
    {
        FileSystemPath source = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        TransferConflict conflict = TransferConflictTestContext.Conflict(source, destination, "source (2).txt");
        TransferConflictChoice choice = TransferConflictChoice.Create(
            conflict,
            TransferConflictDecision.KeepBoth);
        FileEntrySnapshot resolvedSource = conflict.Source.WithConflictChoice(choice);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(TransferConflictTestContext.Inspection(source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([
            TransferPlanEntry.Transfer(resolvedSource, conflict.KeepBothCandidate)]));
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);
        MoveRequest request = (MoveRequest)((FileOperationRequestAccepted)MoveRequest.Create(
            [source],
            destination)).Request;
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            request,
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        FileOperationOutcome completed = await gateway.ResumeAsync(
            awaiting.Continuation!,
            TransferConflictResponse.Create(TransferConflictDecision.KeepBoth, TransferConflictScope.Current),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Completion);
        Assert.HasCount(6, port.Calls);
        Assert.AreEqual("Copy:C:\\source.txt", port.Calls[3]);
        Assert.AreEqual("Verify:C:\\source.txt", port.Calls[4]);
        Assert.AreEqual("Delete:C:\\source.txt", port.Calls[5]);
    }

    private static async Task AssertResolvedPlanRejected(
        TransferConflict conflict,
        TransferConflictDecision decision,
        TransferPlanEntry plan)
    {
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(conflict.Source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([plan]));
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy([conflict.Source.Path], conflict.ExistingTarget.Parent!),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        FileOperationOutcome outcome = await gateway.ResumeAsync(
            awaiting.Continuation!,
            TransferConflictResponse.Create(decision, TransferConflictScope.Current),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(3, port.Calls);
    }
}
