using System;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves TransferConflictContract behavior and boundaries.</summary>
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

        MethodInfo resume = typeof(FileOperationGateway).GetMethod(nameof(FileOperationGateway.ResumeAsync))!;
        Assert.IsFalse(
            resume.GetParameters().Any(parameter => parameter.ParameterType == typeof(ConflictSet)),
            "A caller must not be able to substitute conflicts when resuming a gateway-issued continuation.");
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
        FileSystemPath firstSource = TransferConflictTestContext.ParsePath("C:\\first.txt");
        FileSystemPath secondSource = TransferConflictTestContext.ParsePath("C:\\second.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        TransferConflict first = TransferConflictTestContext.Conflict(firstSource, destination, "first (2).txt");
        TransferConflict second = TransferConflictTestContext.Conflict(secondSource, destination, "second (2).txt");
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
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        TransferConflict first = TransferConflictTestContext.Conflict(TransferConflictTestContext.ParsePath("C:\\first.txt"), destination, "first (2).txt");
        TransferConflict second = TransferConflictTestContext.Conflict(TransferConflictTestContext.ParsePath("C:\\second.txt"), destination, "second (2).txt");
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
        FileSystemPath sourcePath = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        FileEntrySnapshot source = TransferConflictTestContext.Inspection(sourcePath).Snapshot;
        TransferConflict conflict = TransferConflictTestContext.Conflict(sourcePath, destination, "source (2).txt");
        CopyRequest request = TransferConflictTestContext.CreateCopy([sourcePath], destination);
        ConflictSet conflicts = (ConflictSet)TransferPreflightOutcome.Conflicted([conflict]);

        AssertConflictFactories(source, conflict);
        AssertContinuationFactories(source, request, destination, conflicts);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferResolution.None.Add(null!, TransferConflictDecision.Skip, TransferConflictScope.Current));
        TransferConflictTestContext.AssertNullParameter(
            () => TransferResolution.None.Add(conflicts, null!, TransferConflictScope.Current),
            "decision");
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferResolution.None.Add(conflicts, TransferConflictDecision.Skip, null!));
        TransferConflictTestContext.AssertNullParameter(() => UserIntent.ResolveConflict(null!, TransferConflictScope.Current), "decision");
        TransferConflictTestContext.AssertNullParameter(() => UserIntent.ResolveConflict(TransferConflictDecision.Skip, null!), "scope");
    }

    /// <summary>Proves transfer outcome factories own their collections and preserve every state field.</summary>
    [TestMethod]
    public void FileOperationOutcomeFactoriesOwnCollectionsAndPreserveState()
    {
        FileSystemPath path = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileOperationEffect originalEffect = FileOperationEffect.Create(path, FileOperationEffectKind.Copied);
        FileOperationEffect[] callerEffects = [originalEffect];
        FileSystemPath[] callerSkipped = [path];
        FileOperationOutcome succeeded = FileOperationOutcome.Succeeded(callerEffects, callerSkipped);
        callerEffects[0] = FileOperationEffect.Create(path, FileOperationEffectKind.Verified);
        callerSkipped[0] = TransferConflictTestContext.ParsePath("C:\\other.txt");
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
        FileSystemPath sourcePath = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        TransferConflict conflict = TransferConflictTestContext.Conflict(sourcePath, destination, "source (2).txt");
        ConflictSet conflicts = (ConflictSet)TransferPreflightOutcome.Conflicted([conflict]);
        TransferContinuation continuation = TransferContinuation.Create(
            FrozenTransfer.Create(TransferConflictTestContext.CreateCopy([sourcePath], destination), [conflict.Source], destination),
            TransferResolution.None,
            conflicts,
            new object());
        OperationAwaitingConflict awaiting = new(OperationKind.Copy, conflicts, continuation);

        Assert.AreSame(OperationKind.Copy, awaiting.Kind);
        Assert.AreSame(conflicts, awaiting.Conflicts);
        Assert.AreSame(continuation, awaiting.Continuation);
        Assert.AreSame(TransferConflictDecision.Cancel, awaiting.InitialFocus);
        TransferConflictTestContext.AssertNullParameter(() => _ = new OperationAwaitingConflict(null!, conflicts, continuation), "kind");
        TransferConflictTestContext.AssertNullParameter(() => _ = new OperationAwaitingConflict(OperationKind.Copy, null!, continuation), "conflicts");
        TransferConflictTestContext.AssertNullParameter(() => _ = new OperationAwaitingConflict(OperationKind.Copy, conflicts, null!), "continuation");
    }

    private static void AssertConflictFactories(FileEntrySnapshot source, TransferConflict conflict)
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferConflict.Create(null!, conflict.ExistingTarget, conflict.KeepBothCandidate));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferConflict.Create(source, null!, conflict.KeepBothCandidate));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferConflict.Create(source, conflict.ExistingTarget, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferConflictChoice.Create(null!, TransferConflictDecision.Skip));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferConflictChoice.Create(conflict, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferPlanEntry.Transfer(null!, conflict.ExistingTarget));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferPlanEntry.Transfer(source, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferPlanEntry.Skip(null!, conflict.ExistingTarget));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferPlanEntry.Skip(source, null!));
        TransferConflictTestContext.AssertNullParameter(() => source.WithTransferTarget(null!), "target");
        TransferConflictTestContext.AssertNullParameter(() => TransferPreflightOutcome.Succeeded(null!), "plan");
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferPreflightOutcome.Rejected(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferPreflightOutcome.Conflicted(null!));
    }

    private static void AssertContinuationFactories(
        FileEntrySnapshot source, CopyRequest request, FileSystemPath destination, ConflictSet conflicts)
    {
        object owner = new();
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferContinuation.Create(
            FrozenTransfer.Create(null!, [source], destination),
            TransferResolution.None,
            conflicts,
            owner));
        TransferConflictTestContext.AssertNullParameter(
            () => TransferContinuation.Create(
            FrozenTransfer.Create(request, null!, destination),
            TransferResolution.None,
            conflicts,
            owner),
            "sources");
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferContinuation.Create(
            FrozenTransfer.Create(request, [source], null!),
            TransferResolution.None,
            conflicts,
            owner));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferContinuation.Create(
            FrozenTransfer.Create(request, [source], destination),
            null!,
            conflicts,
            owner));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferContinuation.Create(
            FrozenTransfer.Create(request, [source], destination),
            TransferResolution.None,
            null!,
            owner));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferContinuation.Create(
            FrozenTransfer.Create(request, [source], destination),
            TransferResolution.None,
            conflicts,
            null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferContinuation.Create(
            null!, TransferResolution.None, conflicts, owner));
    }
}
