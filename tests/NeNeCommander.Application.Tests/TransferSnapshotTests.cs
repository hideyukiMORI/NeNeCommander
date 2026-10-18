using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves grouped transfer facts preserve the immutable public contracts.</summary>
[TestClass]
public sealed class TransferSnapshotTests
{
    /// <summary>Proves annotations preserve original provider facts without altering the source value.</summary>
    [TestMethod]
    public void SnapshotWhenTransferAnnotationsAreAddedPreservesIdentityAndValueEquality()
    {
        FileSystemPath path = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        FileEntrySnapshot original = TransferConflictTestContext.Inspection(path).Snapshot;
        TransferConflict conflict = TransferConflictTestContext.Conflict(path, destination, "source (2).txt");
        TransferConflictChoice choice = TransferConflictChoice.Create(conflict, TransferConflictDecision.KeepBoth);

        FileEntrySnapshot annotated = original.WithConflictChoice(choice).WithTransferTarget(conflict.KeepBothCandidate);
        FileEntrySnapshot reversed = original.WithTransferTarget(conflict.KeepBothCandidate).WithConflictChoice(choice);
        FileEntrySnapshot clone = annotated with { };

        Assert.AreSame(path, annotated.Path);
        Assert.AreSame(original.Identity, annotated.Identity);
        Assert.AreSame(original.DeletionCapability, annotated.DeletionCapability);
        Assert.AreSame(choice, annotated.ConflictChoice);
        Assert.AreSame(conflict.KeepBothCandidate, annotated.TransferTarget);
        Assert.IsNull(original.ConflictChoice);
        Assert.IsNull(original.TransferTarget);
        Assert.AreEqual(annotated, reversed);
        Assert.AreEqual(annotated, clone);
        Assert.AreEqual(annotated.GetHashCode(), clone.GetHashCode());
    }

    /// <summary>Proves the response retains the exact explicit decision and selected operation scope.</summary>
    [TestMethod]
    public void ResponseWhenCreatedPreservesDecisionScopeAndRecordCopy()
    {
        TransferConflictResponse response = TransferConflictResponse.Create(TransferConflictDecision.KeepBoth, TransferConflictScope.All);

        TransferConflictResponse clone = response with { };

        Assert.AreSame(TransferConflictDecision.KeepBoth, response.Decision);
        Assert.AreSame(TransferConflictScope.All, response.Scope);
        Assert.AreEqual(response, clone);
        Assert.AreEqual(response.GetHashCode(), clone.GetHashCode());
    }

    /// <summary>Proves outcome record copies retain frozen effects and skipped-source collections.</summary>
    [TestMethod]
    public void OutcomeWhenCopiedKeepsReadOnlyEffectsAndSkippedSources()
    {
        FileSystemPath path = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileOperationEffect effect = FileOperationEffect.Create(path, FileOperationEffectKind.Copied);
        FileOperationOutcome original = FileOperationOutcome.Succeeded([effect], [path]);

        FileOperationOutcome clone = original with { };

        Assert.AreEqual(original, clone);
        Assert.AreEqual(original.GetHashCode(), clone.GetHashCode());
        Assert.AreSame(original.Effects, clone.Effects);
        Assert.AreSame(original.NotTransferred, clone.NotTransferred);
        _ = Assert.ThrowsExactly<NotSupportedException>(() => ((IList<FileOperationEffect>)clone.Effects).Clear());
        _ = Assert.ThrowsExactly<NotSupportedException>(() => ((IList<FileSystemPath>)clone.NotTransferred).Clear());
    }
}
