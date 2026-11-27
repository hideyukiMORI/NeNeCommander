using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves resumed plans cannot substitute a different target confirmation while keeping source and alternate name equal.</summary>
[TestClass]
public sealed class TransferConflictTargetOwnershipTests
{
    /// <summary>Proves a foreign target observation is rejected even when the visible pathname has not changed.</summary>
    [TestMethod]
    [DataRow("differentPath")]
    [DataRow("samePath")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task ResumeAsyncWhenProviderSubstitutesTargetConfirmationRefusesBeforeCopy(string replacement)
    {
        FileSystemPath sourcePath = TransferConflictTestContext.ParsePath(@"C:\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath(@"D:\target");
        TransferConflict shown = TransferConflictTestContext.Conflict(sourcePath, destination, "source (2).txt");
        FileSystemPath foreignTarget = replacement == "differentPath"
            ? TransferConflictTestContext.ParsePath(@"D:\target\other.txt") : shown.ExistingTarget;
        TransferConflict foreign = TransferConflict.Create(shown.Source, foreignTarget, shown.KeepBothCandidate, TransferOverwriteCapability.Unavailable(TransferOverwriteUnavailableReason.Unconfirmed));
        FileEntrySnapshot substituted = shown.Source.WithConflictChoice(
            TransferConflictChoice.Create(foreign, TransferConflictDecision.KeepBoth));
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(TransferConflictTestContext.Inspection(sourcePath));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([shown]));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([TransferPlanEntry.Transfer(substituted, shown.KeepBothCandidate)]));
        port.EnqueueCopy(ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Copy), ProviderStepReceipt.Empty));
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome paused = await gateway.ExecuteAsync(TransferConflictTestContext.CreateCopy([sourcePath], destination),
            RecordingFileOperationProgress.Create(), CancellationToken.None);
        TransferContinuation continuation = Assert.IsInstanceOfType<TransferContinuation>(paused.Continuation);

        FileOperationOutcome refused = await gateway.ResumeAsync(continuation,
            TransferConflictResponse.Create(TransferConflictDecision.KeepBoth, TransferConflictScope.Current),
            RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, refused.Failure);
        Assert.IsEmpty(refused.Effects);
        Assert.HasCount(3, port.Calls);
    }
}
