using System;
using System.Reflection;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves TransferContinuation behavior and boundaries.</summary>
[TestClass]
public sealed class TransferContinuationTests
{
    /// <summary>Proves resume rejects every absent required value before consuming its continuation.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenRequiredValueIsNullThrows()
    {
        FileSystemPath sourcePath = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        FileEntrySnapshot source = TransferConflictTestContext.Inspection(sourcePath).Snapshot;
        TransferConflict conflict = TransferConflictTestContext.Conflict(sourcePath, destination, "source (2).txt");
        ConflictSet conflicts = (ConflictSet)TransferPreflightOutcome.Conflicted([conflict]);
        TransferContinuation continuation = TransferContinuation.Create(
            FrozenTransfer.Create(TransferConflictTestContext.CreateCopy([sourcePath], destination), [source], destination),
            TransferResolution.None,
            conflicts,
            new object());
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => gateway.ResumeAsync(
            null!,
            TransferConflictResponse.Create(TransferConflictDecision.Skip, TransferConflictScope.Current),
            progress,
            CancellationToken.None));
        ArgumentNullException decisionFailure = await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => gateway.ResumeAsync(
            continuation,
            TransferConflictResponse.Create(null!, TransferConflictScope.Current),
            progress,
            CancellationToken.None));
        ArgumentNullException scopeFailure = await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => gateway.ResumeAsync(
            continuation,
            TransferConflictResponse.Create(TransferConflictDecision.Skip, null!),
            progress,
            CancellationToken.None));
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => gateway.ResumeAsync(
            continuation,
            TransferConflictResponse.Create(TransferConflictDecision.Skip, TransferConflictScope.Current),
            null!,
            CancellationToken.None));
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => gateway.ResumeAsync(
            continuation, null!, progress, CancellationToken.None));
        Assert.AreEqual("decision", decisionFailure.ParamName);
        Assert.AreEqual("scope", scopeFailure.ParamName);
        Assert.IsEmpty(port.Calls);
    }

    /// <summary>Requires resume preflight to reject a changed original identity without reinspection.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ResumeAsyncWhenFrozenIdentityChangedRejectsWithoutReplacingSnapshot()
    {
        FileSystemPath source = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        TransferConflict conflict = TransferConflictTestContext.Conflict(source, destination, "source (2).txt");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(TransferConflictTestContext.Inspection(source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        port.EnqueuePreflight(TransferPreflightOutcome.Rejected(FileOperationFailureKind.IdentityChanged));
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy([source], destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        FileOperationOutcome rejected = await gateway.ResumeAsync(
            awaiting.Continuation!,
            TransferConflictResponse.Create(TransferConflictDecision.KeepBoth, TransferConflictScope.Current),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, rejected.Failure);
        Assert.IsEmpty(rejected.Effects);
        Assert.AreEqual(1, port.Calls.Count(call => call.StartsWith("Inspect:", StringComparison.Ordinal)));
    }

    /// <summary>Requires a continuation to be a one-shot operation-owned token after cancellation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ResumeAsyncAfterCancellationRejectsReplayWithoutProviderCall()
    {
        FileSystemPath source = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        TransferConflict conflict = TransferConflictTestContext.Conflict(source, destination, "source (2).txt");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(TransferConflictTestContext.Inspection(source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy([source], destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        FileOperationOutcome cancelled = await gateway.ResumeAsync(
            awaiting.Continuation!,
            TransferConflictResponse.Create(TransferConflictDecision.Cancel, TransferConflictScope.Current),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);
        FileOperationOutcome replayed = await gateway.ResumeAsync(
            awaiting.Continuation!,
            TransferConflictResponse.Create(TransferConflictDecision.Cancel, TransferConflictScope.Current),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, cancelled.Completion);
        Assert.AreSame(FileOperationFailureKind.Reentrant, replayed.Failure);
        Assert.IsEmpty(replayed.Effects);
        Assert.HasCount(2, port.Calls);
    }

    /// <summary>Requires cancellation observed at resume to consume the continuation without preflight.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenAlreadyCancelledStartsNoProviderCall()
    {
        FileSystemPath source = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        TransferConflict conflict = TransferConflictTestContext.Conflict(source, destination, "source (2).txt");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(TransferConflictTestContext.Inspection(source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy([source], destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        FileOperationOutcome outcome = await gateway.ResumeAsync(
            awaiting.Continuation!,
            TransferConflictResponse.Create(TransferConflictDecision.Skip, TransferConflictScope.Current),
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(2, port.Calls);
    }

    /// <summary>Requires a continuation to remain bound to the gateway that issued it.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ResumeAsyncFromAnotherGatewayCannotConsumeIssuedContinuation()
    {
        FileSystemPath source = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        TransferConflict conflict = TransferConflictTestContext.Conflict(source, destination, "source (2).txt");
        ScriptedFileOperationPort issuingPort = ScriptedFileOperationPort.Create(null, null);
        issuingPort.EnqueueInspection(TransferConflictTestContext.Inspection(source));
        issuingPort.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        issuingPort.EnqueuePreflight(TransferPreflightOutcome.Succeeded([
            TransferPlanEntry.Skip(
                TransferConflictTestContext.ResolvedSource(conflict, TransferConflictDecision.Skip),
                conflict.ExistingTarget)]));
        ScriptedFileOperationPort otherPort = ScriptedFileOperationPort.Create(null, null);
        using FileOperationGateway issuingGateway = new(issuingPort);
        using FileOperationGateway otherGateway = new(otherPort);
        FileOperationOutcome awaiting = await issuingGateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy([source], destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        FileOperationOutcome foreign = await TransferConflictTestContext.ResumeSkipAsync(otherGateway, awaiting.Continuation!);
        FileOperationOutcome resumed = await TransferConflictTestContext.ResumeSkipAsync(issuingGateway, awaiting.Continuation!);
        FileOperationOutcome replayed = await TransferConflictTestContext.ResumeSkipAsync(issuingGateway, awaiting.Continuation!);

        Assert.AreSame(FileOperationFailureKind.Reentrant, foreign.Failure);
        Assert.IsEmpty(foreign.Effects);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, resumed.Completion);
        Assert.AreSame(FileOperationFailureKind.Reentrant, replayed.Failure);
        Assert.IsEmpty(otherPort.Calls);
        Assert.HasCount(3, issuingPort.Calls);
    }

    /// <summary>Requires the public continuation capability to expose no record clone mechanism.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public void ContinuationIsAnOpaqueReferenceCapabilityWithoutRecordClone()
    {
        MethodInfo[] nonPublicMethods = typeof(TransferContinuation).GetMethods(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        Assert.IsFalse(nonPublicMethods.Any(method => method.Name.Contains("Clone", StringComparison.Ordinal)));
        Assert.IsNull(typeof(TransferContinuation).GetProperty(
            "EqualityContract",
            BindingFlags.Instance | BindingFlags.NonPublic));
    }

    /// <summary>Requires a busy issuing gateway to reject resume without consuming its continuation.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenIssuingGatewayIsOwnedDoesNotConsumeContinuation()
    {
        FileSystemPath source = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        TransferConflict conflict = TransferConflictTestContext.Conflict(source, destination, "source (2).txt");
        BlockingInspectionPort port = BlockingInspectionPort.CreateAfterFirst(
            TransferConflictTestContext.Inspection(source),
            TransferPreflightOutcome.Conflicted([conflict]),
            TransferConflictTestContext.Inspection(source));
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy([source], destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);
        RenameRequest rename = (RenameRequest)((FileOperationRequestAccepted)RenameRequest.Create(
            source,
            "renamed.txt")).Request;
        Task<FileOperationOutcome> running = gateway.ExecuteAsync(
            rename,
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        FileOperationOutcome reentrant = await TransferConflictTestContext.ResumeSkipAsync(gateway, awaiting.Continuation!);
        port.Release();
        FileOperationOutcome first = await running;
        FileOperationOutcome resumed = await TransferConflictTestContext.ResumeSkipAsync(gateway, awaiting.Continuation!);
        FileOperationOutcome replayed = await TransferConflictTestContext.ResumeSkipAsync(gateway, awaiting.Continuation!);

        Assert.AreSame(FileOperationFailureKind.Reentrant, reentrant.Failure);
        Assert.IsEmpty(reentrant.Effects);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, first.Completion);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, resumed.Completion);
        Assert.AreSame(FileOperationFailureKind.Reentrant, replayed.Failure);
    }

    /// <summary>Requires caller-owned source storage to be copied into the continuation.</summary>
    [TestMethod]
    public void ContinuationCopiesCallerOwnedSourceCollection()
    {
        FileSystemPath source = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath replacement = TransferConflictTestContext.ParsePath("C:\\replacement.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        FileEntrySnapshot[] callerSources = [TransferConflictTestContext.Inspection(source).Snapshot];

        TransferContinuation continuation = TransferContinuation.Create(
            FrozenTransfer.Create(TransferConflictTestContext.CreateCopy([source], destination), callerSources, destination),
            TransferResolution.None,
            (ConflictSet)TransferPreflightOutcome.Conflicted([
                TransferConflictTestContext.Conflict(source, destination, "source (2).txt")]),
            new object());
        callerSources[0] = TransferConflictTestContext.Inspection(replacement).Snapshot;

        Assert.AreEqual(source, continuation.Sources[0].Path);
    }
}
