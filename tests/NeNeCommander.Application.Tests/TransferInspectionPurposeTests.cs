using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves an identity-only transfer retains frozen facts through conflict and verified deletion.</summary>
[TestClass]
public sealed class TransferInspectionPurposeTests
{
    /// <summary>Transfer annotations and record copies retain absence of ordinary-deletion evidence.</summary>
    [TestMethod]
    public void SnapshotWhenCapabilityWasNotRequestedKeepsItThroughAnnotations()
    {
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source.txt");
        FileEntrySnapshot source = Assert.IsInstanceOfType<FileInspectionSucceeded>(GatewayTestContext.Inspection(path, DeletionCapability.NotRequested)).Snapshot;
        FileSystemPath target = GatewayTestContext.ParsePath("D:\\destination\\source.txt");
        FileSystemPath candidate = GatewayTestContext.ParsePath("D:\\destination\\source (2).txt");
        TransferConflict conflict = TransferConflict.Create(source, target, candidate, TransferOverwriteCapability.Unavailable(TransferOverwriteUnavailableReason.Unconfirmed));
        TransferConflictChoice choice = TransferConflictChoice.Create(conflict, TransferConflictDecision.KeepBoth);

        FileEntrySnapshot annotated = source.WithTransferTarget(candidate).WithConflictChoice(choice);

        Assert.AreSame(DeletionCapability.NotRequested, annotated.DeletionCapability);
        Assert.AreSame(source.Identity, annotated.Identity);
        Assert.AreEqual(annotated, annotated with { });
        Assert.AreEqual(annotated, source.WithConflictChoice(choice).WithTransferTarget(candidate));
        Assert.IsNull(source.TransferTarget);
        Assert.IsNull(source.ConflictChoice);
    }

    /// <summary>Resume keeps the frozen uninspected capability and rejects a provider's attempted upgrade.</summary>
    [TestMethod]
    [DataRow("Retained")]
    [DataRow("Recycle")]
    [DataRow("Permanent")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-018")]
    public async Task ResumeWhenProviderChangesInspectionCapabilityRejectsBeforeEffect(string supplied)
    {
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source.txt");
        FileEntrySnapshot source = Assert.IsInstanceOfType<FileInspectionSucceeded>(GatewayTestContext.Inspection(path, DeletionCapability.NotRequested)).Snapshot;
        FileSystemPath target = GatewayTestContext.ParsePath("D:\\destination\\source.txt");
        TransferConflict conflict = TransferConflict.Create(source, target, GatewayTestContext.ParsePath("D:\\destination\\source (2).txt"), TransferOverwriteCapability.Unavailable(TransferOverwriteUnavailableReason.Unconfirmed));
        DeletionCapability capability = supplied == "Recycle" ? DeletionCapability.Recycle
            : supplied == "Permanent" ? DeletionCapability.PermanentOnly : DeletionCapability.NotRequested;
        FileEntrySnapshot resolved = FileEntrySnapshot.Create(path, source.Identity, capability)
            .WithConflictChoice(TransferConflictChoice.Create(conflict, TransferConflictDecision.Skip));
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([TransferPlanEntry.Skip(resolved, target)]));
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(GatewayTestContext.CreateCopy([path]), RecordingFileOperationProgress.Create(), CancellationToken.None);
        TransferContinuation continuation = Assert.IsInstanceOfType<TransferContinuation>(awaiting.Continuation);

        FileOperationOutcome outcome = await TransferConflictTestContext.ResumeSkipAsync(gateway, continuation);

        Assert.AreSame(supplied == "Retained" ? FileOperationCompletionKind.Succeeded : FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(supplied == "Retained" ? null : FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(supplied == "Retained" ? 1 : 0, outcome.NotTransferred);
        Assert.HasCount(1, port.InspectionPurposes);
        Assert.AreSame(FileInspectionPurpose.IdentityOnly, port.InspectionPurposes[0]);
        Assert.HasCount(3, port.Calls);
        Assert.AreSame(DeletionCapability.NotRequested, source.DeletionCapability);
    }

    /// <summary>A composite move uses verified-copy authority instead of ordinary deletion capability.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task MoveWhenCapabilityWasNotRequestedDeletesOnlyThroughVerifiedReceipt()
    {
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\owned.txt");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(path, DeletionCapability.NotRequested));
        port.EnqueuePreflight(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueCopy(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueVerification(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueDeletion(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateMove([path]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(3, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.AreSame(FileOperationEffectKind.Verified, outcome.Effects[1].Kind);
        Assert.AreSame(FileOperationEffectKind.SourceDeleted, outcome.Effects[2].Kind);
        Assert.HasCount(1, port.DeletionModes);
        _ = Assert.IsInstanceOfType<DeletionExecutionMode.VerifiedCopyMode>(port.DeletionModes[0]);
        Assert.AreSame(FileInspectionPurpose.IdentityOnly, port.InspectionPurposes[0]);
    }
}
