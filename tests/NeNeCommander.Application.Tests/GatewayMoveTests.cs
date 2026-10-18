using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves GatewayMove behavior and boundaries.</summary>
[TestClass]
public sealed class GatewayMoveTests
{
    /// <summary>Proves a move never verifies or deletes a source whose copy left a partial target.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task ExecuteAsyncWhenMoveCopyFailsAfterTargetCreationReportsEffectWithoutDeletingSource()
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.FailedAfterEffect(
            FileOperationFailureKind.Copy,
            ProviderStepEffectKind.CopyTargetCreated));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateMove([source]),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.CopyTargetCreated, outcome.Effects[0].Kind);
        Assert.HasCount(3, port.Calls);
        Assert.AreEqual("Copy:C:\\source", port.Calls[2]);
    }

    /// <summary>Proves verification failure never deletes the source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task ExecuteAsyncWhenCopiedIdentityCannotBeVerifiedSourceIsNotDeleted()
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Failed(FileOperationFailureKind.IdentityChanged));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateMove([source]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.HasCount(4, port.Calls);
        Assert.AreEqual("Verify:C:\\source", port.Calls[3]);
    }

    /// <summary>Proves cancellation reports completed effects and starts no new step.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenCancellationArrivesAfterCopyReportsCopyAndStartsNoNewStep()
    {
        using CancellationTokenSource source = new();
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterCopy,
            source.Cancel);
        port.EnqueueInspection(GatewayTestContext.Inspection(path, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateMove([path]), RecordingFileOperationProgress.Create(), source.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.HasCount(3, port.Calls);
    }

    /// <summary>Proves copy failure reports no completed effect.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCopyFailsReportsNoCompletedEffect()
    {
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(path, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Failed(FileOperationFailureKind.Copy));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateMove([path]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Copy, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(3, port.Calls);
    }

    /// <summary>Proves cancellation after verification preserves two exact effects.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCancellationArrivesAfterVerificationStartsNoDelete()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterVerification,
            cancellation.Cancel);
        port.EnqueueInspection(GatewayTestContext.Inspection(path, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateMove([path]), RecordingFileOperationProgress.Create(), cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(2, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Verified, outcome.Effects[1].Kind);
        Assert.HasCount(4, port.Calls);
    }

    /// <summary>Proves move source-deletion failure preserves copy and verification effects.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenMoveSourceDeletionFailsReportsPartialCompletion()
    {
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(path, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        port.EnqueueDeletion(ProviderStepOutcome.Failed(FileOperationFailureKind.Delete));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateMove([path]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Delete, outcome.Failure);
        Assert.HasCount(2, outcome.Effects);
        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
    }

    /// <summary>Proves move cancellation between sources starts no second copy.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenMoveCancellationArrivesBetweenSourcesStopsBatch()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterDeletion,
            cancellation.Cancel);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateMove([first, second]),
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(3, outcome.Effects);
        Assert.HasCount(6, port.Calls);
    }
}
