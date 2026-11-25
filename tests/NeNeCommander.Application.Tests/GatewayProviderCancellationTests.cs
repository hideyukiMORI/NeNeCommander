using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves an explicitly interrupted provider step cannot advance the gateway as success.</summary>
[TestClass]
public sealed class GatewayProviderCancellationTests
{
    /// <summary>Proves each mutation boundary retains cancellation without synthesizing a completed effect.</summary>
    [TestMethod]
    [DataRow("copy")]
    [DataRow("move")]
    [DataRow("atomic")]
    [DataRow("delete")]
    [DataRow("directory")]
    [DataRow("file")]
    [DataRow("rename")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenProviderCancelsDoesNotCompleteTheSource(string operation)
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source.txt");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        FileOperationRequest request = ProviderCancellationTestContext.Prepare(operation, source, port);
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, progress, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.IsNull(outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(1, outcome.ProviderSteps);
        Assert.AreSame(ProviderStepReceipt.Empty, outcome.ProviderSteps[0].Receipt);
        Assert.IsEmpty(progress.Reports);
        Assert.IsNull(outcome.Continuation);
        Assert.IsNull(outcome.Conflicts);
    }

    /// <summary>Proves a partially overwritten target and its recovery artifacts survive cancellation before verification or source deletion.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenCopyCancelsWithArtifactsRetainsThemAndStartsNoLaterStep()
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source.txt");
        FileSystemPath later = GatewayTestContext.ParsePath("C:\\later.txt");
        ObservedTransferEntry target = ReceiptObservationTestContext.Entry("D:\\destination\\source.txt", TransferPathObservation.LastKnownPath);
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([], ProviderTargetObservation.MayHaveChanged(target),
            [ReceiptObservationTestContext.Artifact(TransferArtifactRole.OriginalBackup, ArtifactLinkObservation.Retained)]);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.PermanentOnly));
        port.EnqueueInspection(GatewayTestContext.Inspection(later, DeletionCapability.PermanentOnly));
        port.EnqueuePreflight(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueCopy(ProviderStepOutcome.Create(ProviderStepCompletion.Cancelled, receipt));
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateMove([source, later]), progress, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(1, outcome.ProviderSteps);
        Assert.AreSame(receipt, outcome.ProviderSteps[0].Receipt);
        Assert.AreSame(source, outcome.ProviderSteps[0].Source);
        Assert.HasCount(4, port.Calls);
        Assert.AreEqual("Copy:C:\\source.txt", port.Calls[3]);
        Assert.IsEmpty(progress.Reports);
    }

    /// <summary>Proves cancelled source deletion keeps completed copy proof and partial deletion without claiming complete removal.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenSourceDeletionCancelsRetainsPriorCopyAndPartialDeletion()
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source.txt");
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([ProviderStepEffectKind.SourcePartiallyDeleted], null, []);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.PermanentOnly));
        port.EnqueuePreflight(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueCopy(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueVerification(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueDeletion(ProviderStepOutcome.Create(ProviderStepCompletion.Cancelled, receipt));
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateMove([source]), progress, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(3, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.AreSame(FileOperationEffectKind.Verified, outcome.Effects[1].Kind);
        Assert.AreSame(FileOperationEffectKind.SourcePartiallyDeleted, outcome.Effects[2].Kind);
        Assert.HasCount(2, outcome.ProviderSteps);
        Assert.AreSame(receipt, outcome.ProviderSteps[1].Receipt);
        Assert.IsEmpty(progress.Reports);
    }
    /// <summary>Proves cancellation preserves an observed recycle without claiming that an unfinished source completed.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenRecycleCancelsWithReceiptRetainsTheObservedRecycle()
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source.txt");
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([ProviderStepEffectKind.Recycled], null, []);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.Recycle));
        port.EnqueueDeletion(ProviderStepOutcome.Create(ProviderStepCompletion.Cancelled, receipt));
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateDelete([source], null), progress, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Recycled, outcome.Effects[0].Kind);
        Assert.AreSame(receipt, outcome.ProviderSteps[0].Receipt);
        Assert.IsEmpty(progress.Reports);
    }

    /// <summary>Proves a later partial cancellation preserves the earlier source's completed progress and effects.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenSecondCopyCancelsKeepsOnlyTheFirstSourceCompleted()
    {
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first.txt");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second.txt");
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([ProviderStepEffectKind.CopyTargetCreated], null, []);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.PermanentOnly));
        port.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.PermanentOnly));
        port.EnqueuePreflight(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueCopy(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueVerification(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueCopy(ProviderStepOutcome.Create(ProviderStepCompletion.Cancelled, receipt));
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateCopy([first, second]), progress, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(3, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.AreSame(FileOperationEffectKind.Verified, outcome.Effects[1].Kind);
        Assert.AreSame(FileOperationEffectKind.CopyTargetCreated, outcome.Effects[2].Kind);
        Assert.AreSame(second, outcome.Effects[2].Source);
        Assert.HasCount(2, outcome.ProviderSteps);
        Assert.AreSame(receipt, outcome.ProviderSteps[1].Receipt);
        Assert.HasCount(1, progress.Reports);
        Assert.HasCount(6, port.Calls);
    }

}
