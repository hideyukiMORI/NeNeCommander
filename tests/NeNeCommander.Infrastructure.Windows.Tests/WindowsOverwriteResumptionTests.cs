using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Supplies qualified initial consent only at the declared test port, then exercises real resumed preflight, copy, verification and deletion.</summary>
[TestClass]
public sealed class WindowsOverwriteResumptionTests
{
    /// <summary>The complete resumed move uses the one provider copy and strict source-deletion path.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenExactOriginalStillMatchesCompletesVerifiedMove()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        CreateFiles(root);
        TransferOverwriteEvidence original = Original(root);
        WindowsLocalFileOperationAdapter adapter = new(new(), new DeterministicIdentifierSource());
        StepHookPort port = new(adapter) { TransformPreflight = outcome => Qualify(outcome, original) };
        using FileOperationGateway gateway = new(port);
        MoveRequest request = Assert.IsInstanceOfType<MoveRequest>(Assert.IsInstanceOfType<FileOperationRequestAccepted>(
            MoveRequest.Create([original.Source.Observation.Entry.Path], Destination(root))).Request);
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(request, IgnoredFileOperationProgress.Create(), CancellationToken.None);

        FileOperationOutcome outcome = await gateway.ResumeAsync(awaiting.Continuation!,
            TransferConflictResponse.Create(TransferConflictDecision.OverwriteContents, TransferConflictScope.Current),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(3, port.Calls);
        Assert.AreEqual("copy", port.Calls[0]);
        Assert.AreEqual("verify", port.Calls[1]);
        Assert.AreEqual("delete", port.Calls[2]);
        DeletionExecutionMode.VerifiedCopyMode deletion = Assert.IsInstanceOfType<DeletionExecutionMode.VerifiedCopyMode>(port.DeletionModes[0]);
        Assert.AreSame(FileVerificationRequirements.OverwriteContents, Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedFile>(deletion.Receipt.Root).Requirements);
        Assert.IsFalse(File.Exists(root.Resolve("source")));
        Assert.AreEqual("desired", File.ReadAllText(root.Resolve("destination/source")));
        Assert.HasCount(1, Directory.GetFiles(root.Resolve("destination")));
    }

    /// <summary>Changed original files cannot silently receive new consent during resumed preflight.</summary>
    [TestMethod]
    [DataRow("targetBytes")]
    [DataRow("sourceStream")]
    [DataRow("targetStream")]
    [DataRow("missingTarget")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task ResumeAsyncWhenOriginalAdmissionChangedRefusesBeforeArtifacts(string defect)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        CreateFiles(root);
        TransferOverwriteEvidence original = Original(root);
        DeterministicIdentifierSource identifiers = new();
        StepHookPort port = new(new WindowsLocalFileOperationAdapter(new(), identifiers)) { TransformPreflight = outcome => Qualify(outcome, original) };
        using FileOperationGateway gateway = new(port);
        CopyRequest request = Assert.IsInstanceOfType<CopyRequest>(Assert.IsInstanceOfType<FileOperationRequestAccepted>(
            CopyRequest.Create([original.Source.Observation.Entry.Path], Destination(root))).Request);
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(request, IgnoredFileOperationProgress.Create(), CancellationToken.None);
        Change(root, defect);

        FileOperationOutcome outcome = await gateway.ResumeAsync(awaiting.Continuation!,
            TransferConflictResponse.Create(TransferConflictDecision.OverwriteContents, TransferConflictScope.Current),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.IsNotNull(outcome.Failure);
        Assert.IsEmpty(port.Calls);
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.AreEqual("desired", File.ReadAllText(root.Resolve("source")));
        if (defect != "missingTarget")
        {
            Assert.AreEqual(defect == "targetBytes" ? "later" : "original", File.ReadAllText(root.Resolve("destination/source")));
        }
    }

    private static TransferPreflightOutcome Qualify(TransferPreflightOutcome outcome, TransferOverwriteEvidence original)
    {
        if (outcome is not ConflictSet set)
        {
            return outcome;
        }
        Assert.HasCount(1, set.Conflicts);
        TransferConflict conflict = set.Conflicts[0];
        return TransferPreflightOutcome.Conflicted([TransferConflict.Create(conflict.Source, conflict.ExistingTarget, conflict.KeepBothCandidate, TransferOverwriteCapability.Available(original))]);
    }

    private static void CreateFiles(TestOwnedTemporaryRoot root)
    {
        _ = root.WriteFile("source", "desired");
        _ = root.CreateDirectory("destination");
        _ = root.WriteFile("destination/source", "original");
    }

    private static TransferOverwriteEvidence Original(TestOwnedTemporaryRoot root)
    {
        FileEntrySnapshot source = WindowsLocalOperationTestContext.LocalSnapshot(root.Resolve("source"));
        WindowsLocalPath target = Assert.IsInstanceOfType<WindowsLocalPath>(WindowsLocalOperationTestContext.ParsePath(root.Resolve("destination/source")));
        using WindowsOverwritePairLease pair = WindowsOverwritePairLease.Open(source, target, new WindowsDriveBoundary());
        return pair.Evidence;
    }

    private static FileSystemPath Destination(TestOwnedTemporaryRoot root)
    {
        return WindowsLocalOperationTestContext.ParsePath(root.Resolve("destination"));
    }

    private static void Change(TestOwnedTemporaryRoot root, string defect)
    {
        switch (defect)
        {
            case "targetBytes":
                _ = root.WriteFile("destination/source", "later");
                break;
            case "sourceStream":
                _ = root.WriteFile("source:extra", string.Empty);
                break;
            case "targetStream":
                _ = root.WriteFile("destination/source:extra", string.Empty);
                break;
            case "missingTarget":
                File.Delete(root.Resolve("destination/source"));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(defect));
        }
    }
}
