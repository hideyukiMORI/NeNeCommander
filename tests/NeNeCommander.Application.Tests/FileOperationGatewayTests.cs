using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves FileOperationGateway behavior and boundaries.</summary>
[TestClass]
public sealed class FileOperationGatewayTests
{
    /// <summary>Proves an unregistered request variant fails closed without touching a provider.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenRequestVariantIsUnsupportedThrowsWithoutProviderAccess()
    {
        FileOperationRequest request = new UnsupportedFileOperationRequest([GatewayTestContext.ParsePath("C:\\source")]);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        using FileOperationGateway gateway = new(port);

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await gateway.ExecuteAsync(request, RecordingFileOperationProgress.Create(), CancellationToken.None));
        Assert.IsEmpty(port.Calls);
    }

    /// <summary>Proves progress is reported once per completed source for transfers and deletions, and never for a source that failed.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenSourcesCompleteReportsProgressOncePerSource()
    {
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Failed(FileOperationFailureKind.Copy));
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.Recycle));
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress copyProgress = RecordingFileOperationProgress.Create();
        RecordingFileOperationProgress deleteProgress = RecordingFileOperationProgress.Create();

        FileOperationOutcome copy = await gateway.ExecuteAsync(GatewayTestContext.CreateCopy([first, second]), copyProgress, CancellationToken.None);
        FileOperationOutcome delete = await gateway.ExecuteAsync(GatewayTestContext.CreateDelete([first, second], null), deleteProgress, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, copy.Completion);
        Assert.HasCount(1, copyProgress.Reports);
        Assert.AreEqual(FileOperationProgress.Create(1, 2), copyProgress.Reports[0]);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, delete.Completion);
        Assert.HasCount(2, deleteProgress.Reports);
        Assert.AreEqual(FileOperationProgress.Create(1, 2), deleteProgress.Reports[0]);
        Assert.AreEqual(FileOperationProgress.Create(2, 2), deleteProgress.Reports[1]);
    }

    /// <summary>Proves concurrent dispatch is rejected deterministically.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task ExecuteAsyncWhenAnotherRequestOwnsGatewayReentrantRequestIsRejected()
    {
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source");
        BlockingInspectionPort port = BlockingInspectionPort.Create(
            GatewayTestContext.Inspection(path, DeletionCapability.Recycle));
        using FileOperationGateway gateway = new(port);

        Task<FileOperationOutcome> firstExecution = gateway.ExecuteAsync(GatewayTestContext.CreateMove([path]), RecordingFileOperationProgress.Create(), CancellationToken.None);
        FileOperationOutcome second = await gateway.ExecuteAsync(GatewayTestContext.CreateMove([path]), RecordingFileOperationProgress.Create(), CancellationToken.None);
        port.Release();
        FileOperationOutcome first = await firstExecution;

        Assert.AreSame(FileOperationCompletionKind.Rejected, second.Completion);
        Assert.AreSame(FileOperationFailureKind.Reentrant, second.Failure);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, first.Completion);
    }

    /// <summary>Proves pre-cancelled work performs no provider call.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenAlreadyCancelledReturnsCancelledWithoutCallingPort()
    {
        using CancellationTokenSource source = new();
        source.Cancel();
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort transferPort = ScriptedFileOperationPort.Create(null, null);
        ScriptedFileOperationPort createPort = ScriptedFileOperationPort.Create(null, null);
        ScriptedFileOperationPort renamePort = ScriptedFileOperationPort.Create(null, null);
        using FileOperationGateway transferGateway = new(transferPort);
        using FileOperationGateway createGateway = new(createPort);
        using FileOperationGateway renameGateway = new(renamePort);

        FileOperationOutcome transfer = await transferGateway.ExecuteAsync(
            GatewayTestContext.CreateMove([path]),
            RecordingFileOperationProgress.Create(),
            source.Token);
        FileOperationOutcome create = await createGateway.ExecuteAsync(
            GatewayTestContext.CreateCreateDirectory(path, "child"),
            RecordingFileOperationProgress.Create(),
            source.Token);
        FileOperationOutcome rename = await renameGateway.ExecuteAsync(
            GatewayTestContext.CreateRename(path, "renamed"),
            RecordingFileOperationProgress.Create(),
            source.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, transfer.Completion);
        Assert.AreSame(FileOperationCompletionKind.Cancelled, create.Completion);
        Assert.AreSame(FileOperationCompletionKind.Cancelled, rename.Completion);
        Assert.IsEmpty(transferPort.Calls);
        Assert.IsEmpty(createPort.Calls);
        Assert.IsEmpty(renamePort.Calls);
    }
}
