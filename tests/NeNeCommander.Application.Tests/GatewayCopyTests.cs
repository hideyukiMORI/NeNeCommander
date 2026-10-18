using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves GatewayCopy behavior and boundaries.</summary>
[TestClass]
public sealed class GatewayCopyTests
{
    /// <summary>Proves a copy runs copy and verify per source after complete preflight and never deletes a source.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCopySucceedsReportsCopyAndVerifyWithoutDeletingSources()
    {
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        CopyRequest request = GatewayTestContext.CreateCopy([first, second]);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.PermanentOnly));
        port.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.PermanentOnly));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        for (int index = 0; index < 2; index++)
        {
            port.EnqueueCopy(ProviderStepOutcome.Succeeded());
            port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        }
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(4, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.AreSame(FileOperationEffectKind.Verified, outcome.Effects[1].Kind);
        Assert.AreSame(second, outcome.Effects[3].Source);
        Assert.HasCount(7, port.Calls);
        Assert.AreEqual("Inspect:C:\\first", port.Calls[0]);
        Assert.AreEqual("Inspect:C:\\second", port.Calls[1]);
        Assert.AreEqual("Preflight:D:\\destination", port.Calls[2]);
        Assert.AreEqual("Copy:C:\\first", port.Calls[3]);
        Assert.AreEqual("Verify:C:\\first", port.Calls[4]);
        Assert.AreEqual("Copy:C:\\second", port.Calls[5]);
        Assert.AreEqual("Verify:C:\\second", port.Calls[6]);
    }

    /// <summary>Proves a copy whose verification fails stops the batch with the copied effect and starts no further source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenCopyVerificationFailsStopsBeforeNextSource()
    {
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.PermanentOnly));
        port.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.PermanentOnly));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Failed(FileOperationFailureKind.IdentityChanged));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateCopy([first, second]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.HasCount(5, port.Calls);
        Assert.AreEqual("Verify:C:\\first", port.Calls[4]);
    }

    /// <summary>Proves a failed copy carries its created target into the operation effects.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenCopyFailsAfterTargetCreationReportsPartialEffect()
    {
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.PermanentOnly));
        port.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.PermanentOnly));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.FailedAfterEffect(
            FileOperationFailureKind.Copy,
            ProviderStepEffectKind.CopyTargetCreated));
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateCopy([first, second]),
            progress,
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Copy, outcome.Failure);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(first, outcome.Effects[0].Source);
        Assert.AreSame(FileOperationEffectKind.CopyTargetCreated, outcome.Effects[0].Kind);
        Assert.IsEmpty(progress.Reports);
        Assert.HasCount(4, port.Calls);
        Assert.AreEqual("Copy:C:\\first", port.Calls[3]);
    }
}
