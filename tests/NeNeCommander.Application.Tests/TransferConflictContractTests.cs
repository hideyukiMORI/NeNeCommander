using System;
using System.Reflection;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the transfer preflight contract can represent a conflict set.</summary>
[TestClass]
public sealed class TransferConflictContractTests
{
    /// <summary>Requires a conflict-aware preflight return type.</summary>
    [TestMethod]
    public void PreflightHasAConflictAwareClosedOutcome()
    {
        MethodInfo method = typeof(IFileOperationPort).GetMethod(
            nameof(IFileOperationPort.PreflightTransferAsync))!;

        Assert.AreNotEqual(
            typeof(Task<ProviderStepOutcome>),
            method.ReturnType,
            "Transfer preflight must carry a typed ConflictSet without changing mutation-step outcomes.");
    }

    /// <summary>Requires an awaiting outcome to contain at least one concrete conflict.</summary>
    [TestMethod]
    public void ConflictedWhenSetIsEmptyRejectsConstruction()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() => TransferPreflightOutcome.Conflicted([]));
    }

    /// <summary>Proves conflict values preserve exact inputs and copy caller-owned collections.</summary>
    [TestMethod]
    public void TransferConflictValuesAreClosedAndImmutable()
    {
        FileSystemPath firstSource = ParsePath("C:\\first.txt");
        FileSystemPath secondSource = ParsePath("C:\\second.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        TransferConflict first = Conflict(firstSource, destination, "first (2).txt");
        TransferConflict second = Conflict(secondSource, destination, "second (2).txt");
        TransferConflict[] callerConflicts = [first];
        ConflictSet set = (ConflictSet)TransferPreflightOutcome.Conflicted(callerConflicts);
        callerConflicts[0] = second;
        TransferConflictChoice choice = TransferConflictChoice.Create(
            first,
            TransferConflictDecision.KeepBoth);
        TransferPlanEntry transfer = TransferPlanEntry.Transfer(first.Source, first.KeepBothCandidate);
        TransferPlanEntry skip = TransferPlanEntry.Skip(first.Source, first.ExistingTarget);
        TransferPlanEntry[] callerPlan = [transfer];
        TransferPreflightSucceeded succeeded = (TransferPreflightSucceeded)TransferPreflightOutcome.Succeeded(callerPlan);
        callerPlan[0] = skip;
        TransferPreflightRejected rejected = (TransferPreflightRejected)TransferPreflightOutcome.Rejected(
            FileOperationFailureKind.IdentityChanged);

        Assert.AreSame(first.Source, set.Conflicts[0].Source);
        Assert.AreSame(first.ExistingTarget, set.Conflicts[0].ExistingTarget);
        Assert.AreSame(first.KeepBothCandidate, set.Conflicts[0].KeepBothCandidate);
        Assert.AreSame(first.KeepBothCandidate, choice.KeepBothCandidate);
        Assert.AreSame(first.Source.Path, choice.Source);
        Assert.AreSame(TransferConflictDecision.KeepBoth, choice.Decision);
        Assert.HasCount(3, first.AllowedDecisions);
        Assert.AreSame(TransferConflictDecision.Skip, first.AllowedDecisions[0]);
        Assert.AreSame(TransferConflictDecision.KeepBoth, first.AllowedDecisions[1]);
        Assert.AreSame(TransferConflictDecision.Cancel, first.AllowedDecisions[2]);
        Assert.AreSame(first.Source, succeeded.Plan[0].Source);
        Assert.AreSame(first.KeepBothCandidate, succeeded.Plan[0].Target);
        Assert.AreSame(TransferDisposition.Transfer, succeeded.Plan[0].Disposition);
        Assert.AreSame(TransferDisposition.Skip, skip.Disposition);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, rejected.Failure);
        Assert.IsNull(succeeded.Failure);
    }

    /// <summary>Proves Current and All choices remain operation-scoped and retain earlier answers.</summary>
    [TestMethod]
    public void TransferResolutionAddsOnlyTheSelectedConflictScope()
    {
        FileSystemPath destination = ParsePath("D:\\destination");
        TransferConflict first = Conflict(ParsePath("C:\\first.txt"), destination, "first (2).txt");
        TransferConflict second = Conflict(ParsePath("C:\\second.txt"), destination, "second (2).txt");
        ConflictSet conflicts = (ConflictSet)TransferPreflightOutcome.Conflicted([first, second]);

        TransferResolution current = TransferResolution.None.Add(
            conflicts,
            TransferConflictDecision.Skip,
            TransferConflictScope.Current);
        TransferResolution all = current.Add(
            conflicts,
            TransferConflictDecision.KeepBoth,
            TransferConflictScope.All);

        Assert.HasCount(1, current.Choices);
        Assert.AreSame(TransferConflictDecision.Skip, current.Find(first.Source.Path)!.Decision);
        Assert.IsNull(current.Find(second.Source.Path));
        Assert.HasCount(3, all.Choices);
        Assert.AreSame(TransferConflictDecision.KeepBoth, all.Find(first.Source.Path)!.Decision);
        Assert.AreSame(TransferConflictDecision.KeepBoth, all.Find(second.Source.Path)!.Decision);
        Assert.IsEmpty(TransferResolution.None.Choices);
    }

    /// <summary>Proves every new transfer factory rejects absent required values.</summary>
    [TestMethod]
    public void TransferConflictFactoriesWhenRequiredValueIsNullThrow()
    {
        FileSystemPath sourcePath = ParsePath("C:\\source.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        FileEntrySnapshot source = Inspection(sourcePath).Snapshot;
        TransferConflict conflict = Conflict(sourcePath, destination, "source (2).txt");
        CopyRequest request = CreateCopy([sourcePath], destination);
        ConflictSet conflicts = (ConflictSet)TransferPreflightOutcome.Conflicted([conflict]);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferConflict.Create(null!, conflict.ExistingTarget, conflict.KeepBothCandidate));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferConflict.Create(source, null!, conflict.KeepBothCandidate));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferConflict.Create(source, conflict.ExistingTarget, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferConflictChoice.Create(null!, TransferConflictDecision.Skip));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferConflictChoice.Create(conflict, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferPlanEntry.Transfer(null!, conflict.ExistingTarget));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferPlanEntry.Transfer(source, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferPlanEntry.Skip(null!, conflict.ExistingTarget));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferPlanEntry.Skip(source, null!));
        AssertNullParameter(() => source.WithTransferTarget(null!), "target");
        AssertNullParameter(() => TransferPreflightOutcome.Succeeded(null!), "plan");
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferPreflightOutcome.Rejected(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferPreflightOutcome.Conflicted(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferContinuation.Create(null!, [source], destination, TransferResolution.None));
        AssertNullParameter(
            () => TransferContinuation.Create(request, null!, destination, TransferResolution.None),
            "sources");
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferContinuation.Create(request, [source], null!, TransferResolution.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferContinuation.Create(request, [source], destination, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferResolution.None.Add(null!, TransferConflictDecision.Skip, TransferConflictScope.Current));
        AssertNullParameter(
            () => TransferResolution.None.Add(conflicts, null!, TransferConflictScope.Current),
            "decision");
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferResolution.None.Add(conflicts, TransferConflictDecision.Skip, null!));
        AssertNullParameter(() => UserIntent.ResolveConflict(null!, TransferConflictScope.Current), "decision");
        AssertNullParameter(() => UserIntent.ResolveConflict(TransferConflictDecision.Skip, null!), "scope");
    }

    /// <summary>Proves resume rejects every absent required value before consuming its continuation.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenRequiredValueIsNullThrows()
    {
        FileSystemPath sourcePath = ParsePath("C:\\source.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        FileEntrySnapshot source = Inspection(sourcePath).Snapshot;
        TransferConflict conflict = Conflict(sourcePath, destination, "source (2).txt");
        ConflictSet conflicts = (ConflictSet)TransferPreflightOutcome.Conflicted([conflict]);
        TransferContinuation continuation = TransferContinuation.Create(
            CreateCopy([sourcePath], destination),
            [source],
            destination,
            TransferResolution.None);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => gateway.ResumeAsync(
            null!, conflicts, TransferConflictDecision.Skip, TransferConflictScope.Current, progress, CancellationToken.None));
        ArgumentNullException conflictsFailure = await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => gateway.ResumeAsync(
            continuation, null!, TransferConflictDecision.Skip, TransferConflictScope.Current, progress, CancellationToken.None));
        ArgumentNullException decisionFailure = await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => gateway.ResumeAsync(
            continuation, conflicts, null!, TransferConflictScope.Current, progress, CancellationToken.None));
        ArgumentNullException scopeFailure = await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => gateway.ResumeAsync(
            continuation, conflicts, TransferConflictDecision.Skip, null!, progress, CancellationToken.None));
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => gateway.ResumeAsync(
            continuation, conflicts, TransferConflictDecision.Skip, TransferConflictScope.Current, null!, CancellationToken.None));
        Assert.AreEqual("conflicts", conflictsFailure.ParamName);
        Assert.AreEqual("decision", decisionFailure.ParamName);
        Assert.AreEqual("scope", scopeFailure.ParamName);
        Assert.IsEmpty(port.Calls);
    }

    /// <summary>Proves transfer outcome factories own their collections and preserve every state field.</summary>
    [TestMethod]
    public void FileOperationOutcomeFactoriesOwnCollectionsAndPreserveState()
    {
        FileSystemPath path = ParsePath("C:\\source.txt");
        FileOperationEffect originalEffect = FileOperationEffect.Create(path, FileOperationEffectKind.Copied);
        FileOperationEffect[] callerEffects = [originalEffect];
        FileSystemPath[] callerSkipped = [path];
        FileOperationOutcome succeeded = FileOperationOutcome.Succeeded(callerEffects, callerSkipped);
        callerEffects[0] = FileOperationEffect.Create(path, FileOperationEffectKind.Verified);
        callerSkipped[0] = ParsePath("C:\\other.txt");
        FileOperationOutcome rejected = FileOperationOutcome.Failed([], FileOperationFailureKind.Copy);
        FileOperationOutcome simpleSucceeded = FileOperationOutcome.Succeeded([originalEffect]);
        FileOperationOutcome partial = FileOperationOutcome.Failed(
            [originalEffect],
            [path],
            FileOperationFailureKind.Verification);
        FileOperationOutcome cancelled = FileOperationOutcome.Cancelled([]);

        Assert.AreSame(originalEffect, succeeded.Effects[0]);
        Assert.AreSame(path, succeeded.NotTransferred[0]);
        Assert.AreSame(FileOperationCompletionKind.Rejected, rejected.Completion);
        Assert.IsEmpty(rejected.NotTransferred);
        Assert.IsEmpty(simpleSucceeded.NotTransferred);
        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, partial.Completion);
        Assert.AreSame(FileOperationFailureKind.Verification, partial.Failure);
        Assert.IsEmpty(cancelled.Effects);
        Assert.IsEmpty(cancelled.NotTransferred);
    }

    /// <summary>Proves the session conflict state requires and exposes its complete ownership tuple.</summary>
    [TestMethod]
    public void OperationAwaitingConflictPreservesRequiredValues()
    {
        FileSystemPath sourcePath = ParsePath("C:\\source.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        TransferConflict conflict = Conflict(sourcePath, destination, "source (2).txt");
        ConflictSet conflicts = (ConflictSet)TransferPreflightOutcome.Conflicted([conflict]);
        TransferContinuation continuation = TransferContinuation.Create(
            CreateCopy([sourcePath], destination),
            [conflict.Source],
            destination,
            TransferResolution.None);
        OperationAwaitingConflict awaiting = new(OperationKind.Copy, conflicts, continuation);

        Assert.AreSame(OperationKind.Copy, awaiting.Kind);
        Assert.AreSame(conflicts, awaiting.Conflicts);
        Assert.AreSame(continuation, awaiting.Continuation);
        Assert.AreSame(TransferConflictDecision.Cancel, awaiting.InitialFocus);
        AssertNullParameter(() => _ = new OperationAwaitingConflict(null!, conflicts, continuation), "kind");
        AssertNullParameter(() => _ = new OperationAwaitingConflict(OperationKind.Copy, null!, continuation), "conflicts");
        AssertNullParameter(() => _ = new OperationAwaitingConflict(OperationKind.Copy, conflicts, null!), "continuation");
    }

    /// <summary>Proves disposing the gateway releases its operation lease resource.</summary>
    [TestMethod]
    public async Task ExecuteAsyncAfterGatewayDisposalThrowsObjectDisposedException()
    {
        FileSystemPath destination = ParsePath("D:\\destination");
        FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        gateway.Dispose();

        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => gateway.ExecuteAsync(
            CreateCopy([ParsePath("C:\\source.txt")], destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None));
    }

    /// <summary>Requires a later batch conflict to stop every effect before the first copy.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ExecuteAsyncWhenLaterSourceConflictsStartsNoEffect()
    {
        FileSystemPath first = ParsePath("C:\\first.txt");
        FileSystemPath second = ParsePath("C:\\second.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(first));
        port.EnqueueInspection(Inspection(second));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([
            Conflict(second, destination, "second (2).txt")]));
        using FileOperationGateway gateway = new(port);
        CopyRequest request = CreateCopy([first, second], destination);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            request,
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.IsNotNull(outcome.Conflicts);
        Assert.IsNotNull(outcome.Continuation);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(3, port.Calls);
    }

    /// <summary>Requires Skip to complete as a non-effect without copy or deletion.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenConflictIsSkippedRecordsNotTransferredWithoutEffect()
    {
        FileSystemPath source = ParsePath("C:\\source.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        TransferConflict conflict = Conflict(source, destination, "source (2).txt");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([
            TransferPlanEntry.Skip(conflict.Source, conflict.ExistingTarget)]));
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            CreateCopy([source], destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();
        FileOperationOutcome completed = await gateway.ResumeAsync(
            awaiting.Continuation!,
            awaiting.Conflicts!,
            TransferConflictDecision.Skip,
            TransferConflictScope.Current,
            progress,
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Completion);
        Assert.IsEmpty(completed.Effects);
        Assert.HasCount(1, completed.NotTransferred);
        Assert.HasCount(3, port.Calls);
        Assert.HasCount(1, progress.Reports);
        Assert.AreEqual(1, progress.Reports[0].Completed);
        Assert.AreEqual(1, progress.Reports[0].Total);

        FileOperationOutcome replayed = await gateway.ResumeAsync(
            awaiting.Continuation!,
            awaiting.Conflicts!,
            TransferConflictDecision.Skip,
            TransferConflictScope.Current,
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);
        Assert.AreSame(FileOperationFailureKind.Reentrant, replayed.Failure);
        Assert.HasCount(3, port.Calls);
    }

    /// <summary>Requires cancellation raised by complete preflight to stop before copying.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenPreflightCancelsStopsBeforeFirstEffect()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath source = ParsePath("C:\\source.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterPreflight,
            cancellation.Cancel);
        FileEntrySnapshot snapshot = Inspection(source).Snapshot;
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(snapshot));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([
            TransferPlanEntry.Transfer(snapshot, ((PathParseSuccess)destination.Child("source.txt")).Path)]));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            CreateCopy([source], destination),
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(2, port.Calls);
    }

    /// <summary>Requires cancellation during move capability discovery to stop before mutation.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCapabilityDiscoveryCancelsStopsBeforeFirstEffect()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath first = ParsePath("C:\\first.txt");
        FileSystemPath second = ParsePath("C:\\second.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterAtomicCapability,
            cancellation.Cancel);
        port.EnqueueInspection(Inspection(first));
        port.EnqueueInspection(Inspection(second));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueAtomicMoveCapability(AtomicMoveCapabilityOutcome.Supported);
        using FileOperationGateway gateway = new(port);
        MoveRequest request = (MoveRequest)((FileOperationRequestAccepted)MoveRequest.Create(
            [first, second],
            destination)).Request;

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            request,
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(4, port.Calls);
        Assert.AreEqual("AtomicCapability:C:\\first.txt", port.Calls[3]);
    }

    /// <summary>Requires a skipped move to avoid capability discovery and every filesystem effect.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenMoveConflictIsSkippedDoesNotQueryMoveCapability()
    {
        FileSystemPath source = ParsePath("C:\\source.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        TransferConflict conflict = Conflict(source, destination, "source (2).txt");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([
            TransferPlanEntry.Skip(conflict.Source, conflict.ExistingTarget)]));
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
            awaiting.Conflicts!,
            TransferConflictDecision.Skip,
            TransferConflictScope.Current,
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Completion);
        Assert.HasCount(1, completed.NotTransferred);
        Assert.IsFalse(port.Calls.Any(call => call.StartsWith("AtomicMoveCapability:", StringComparison.Ordinal)));
    }

    /// <summary>Requires a KeepBoth move to use composite copy and verification before source deletion.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenMoveKeepsBothDoesNotQueryAtomicMoveCapability()
    {
        FileSystemPath source = ParsePath("C:\\source.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        TransferConflict conflict = Conflict(source, destination, "source (2).txt");
        TransferConflictChoice choice = TransferConflictChoice.Create(
            conflict,
            TransferConflictDecision.KeepBoth);
        FileEntrySnapshot resolvedSource = conflict.Source.WithConflictChoice(choice);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(source));
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
            awaiting.Conflicts!,
            TransferConflictDecision.KeepBoth,
            TransferConflictScope.Current,
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Completion);
        Assert.HasCount(6, port.Calls);
        Assert.AreEqual("Copy:C:\\source.txt", port.Calls[3]);
        Assert.AreEqual("Verify:C:\\source.txt", port.Calls[4]);
        Assert.AreEqual("Delete:C:\\source.txt", port.Calls[5]);
    }

    /// <summary>Requires resume preflight to reject a changed original identity without reinspection.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ResumeAsyncWhenFrozenIdentityChangedRejectsWithoutReplacingSnapshot()
    {
        FileSystemPath source = ParsePath("C:\\source.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        TransferConflict conflict = Conflict(source, destination, "source (2).txt");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        port.EnqueuePreflight(TransferPreflightOutcome.Rejected(FileOperationFailureKind.IdentityChanged));
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            CreateCopy([source], destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        FileOperationOutcome rejected = await gateway.ResumeAsync(
            awaiting.Continuation!,
            awaiting.Conflicts!,
            TransferConflictDecision.KeepBoth,
            TransferConflictScope.Current,
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
        FileSystemPath source = ParsePath("C:\\source.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        TransferConflict conflict = Conflict(source, destination, "source (2).txt");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            CreateCopy([source], destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        FileOperationOutcome cancelled = await gateway.ResumeAsync(
            awaiting.Continuation!,
            awaiting.Conflicts!,
            TransferConflictDecision.Cancel,
            TransferConflictScope.Current,
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);
        FileOperationOutcome replayed = await gateway.ResumeAsync(
            awaiting.Continuation!,
            awaiting.Conflicts!,
            TransferConflictDecision.Cancel,
            TransferConflictScope.Current,
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
        FileSystemPath source = ParsePath("C:\\source.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        FileEntrySnapshot snapshot = Inspection(source).Snapshot;
        TransferConflict conflict = Conflict(source, destination, "source (2).txt");
        CopyRequest request = CreateCopy([source], destination);
        TransferContinuation continuation = TransferContinuation.Create(
            request,
            [snapshot],
            destination,
            TransferResolution.None);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        using FileOperationGateway gateway = new(port);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        FileOperationOutcome outcome = await gateway.ResumeAsync(
            continuation,
            (ConflictSet)TransferPreflightOutcome.Conflicted([conflict]),
            TransferConflictDecision.Skip,
            TransferConflictScope.Current,
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.IsEmpty(port.Calls);
    }

    /// <summary>Requires resume to fail closed while another operation owns the single gateway lease.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenGatewayIsOwnedIsRejectedAsReentrant()
    {
        FileSystemPath source = ParsePath("C:\\source.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        FileEntrySnapshot snapshot = Inspection(source).Snapshot;
        TransferConflict conflict = Conflict(source, destination, "source (2).txt");
        CopyRequest request = CreateCopy([source], destination);
        TransferContinuation continuation = TransferContinuation.Create(
            request,
            [snapshot],
            destination,
            TransferResolution.None);
        BlockingInspectionPort port = BlockingInspectionPort.Create(Inspection(source));
        using FileOperationGateway gateway = new(port);
        Task<FileOperationOutcome> running = gateway.ExecuteAsync(
            request,
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        FileOperationOutcome reentrant = await gateway.ResumeAsync(
            continuation,
            (ConflictSet)TransferPreflightOutcome.Conflicted([conflict]),
            TransferConflictDecision.Skip,
            TransferConflictScope.Current,
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);
        port.Release();
        FileOperationOutcome first = await running;
        FileOperationOutcome resumed = await gateway.ResumeAsync(
            continuation,
            (ConflictSet)TransferPreflightOutcome.Conflicted([conflict]),
            TransferConflictDecision.Skip,
            TransferConflictScope.Current,
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);
        FileOperationOutcome replayed = await gateway.ResumeAsync(
            continuation,
            (ConflictSet)TransferPreflightOutcome.Conflicted([conflict]),
            TransferConflictDecision.Skip,
            TransferConflictScope.Current,
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

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
        FileSystemPath source = ParsePath("C:\\source.txt");
        FileSystemPath replacement = ParsePath("C:\\replacement.txt");
        FileSystemPath destination = ParsePath("D:\\destination");
        FileEntrySnapshot[] callerSources = [Inspection(source).Snapshot];

        TransferContinuation continuation = TransferContinuation.Create(
            CreateCopy([source], destination),
            callerSources,
            destination,
            TransferResolution.None);
        callerSources[0] = Inspection(replacement).Snapshot;

        Assert.AreEqual(source, continuation.Sources[0].Path);
    }

    private static TransferConflict Conflict(
        FileSystemPath source,
        FileSystemPath destination,
        string candidateName)
    {
        string sourceName = source.CanonicalText[(source.CanonicalText.LastIndexOf('\\') + 1)..];
        return TransferConflict.Create(
            Inspection(source).Snapshot,
            ((PathParseSuccess)destination.Child(sourceName)).Path,
            ((PathParseSuccess)destination.Child(candidateName)).Path);
    }

    private static FileInspectionSucceeded Inspection(FileSystemPath source)
    {
        FileIdentity identity = ((FileIdentityAccepted)FileIdentity.Parse("identity:" + source.CanonicalText)).Identity;
        return (FileInspectionSucceeded)FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(source, identity, DeletionCapability.PermanentOnly));
    }

    private static CopyRequest CreateCopy(FileSystemPath[] sources, FileSystemPath destination)
    {
        return (CopyRequest)((FileOperationRequestAccepted)CopyRequest.Create(sources, destination)).Request;
    }

    private static void AssertNullParameter(Action action, string expectedParameter)
    {
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(action);
        Assert.AreEqual(expectedParameter, exception.ParamName);
    }

    private static FileSystemPath ParsePath(string text)
    {
        return ((PathParseSuccess)FileSystemPath.Parse(text)).Path;
    }
}
