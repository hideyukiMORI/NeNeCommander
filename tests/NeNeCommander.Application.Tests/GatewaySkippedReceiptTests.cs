using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves a late provider refusal cannot erase earlier explicit skips or its retained effects.</summary>
[TestClass]
public sealed class GatewaySkippedReceiptTests
{
    /// <summary>Proves a resumed batch keeps skipped sources and the failing source's exact receipt together.</summary>
    [TestMethod]
    [DataRow("failure")]
    [DataRow("cancelled")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task ResumeAsyncWhenCopyStopsAfterSkipPreservesBothFactsWithoutAnotherPause(string completion)
    {
        FileSystemPath skipped = GatewayTestContext.ParsePath("C:\\skip.txt");
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = GatewayTestContext.ParsePath("D:\\destination");
        TransferConflict conflict = TransferConflictTestContext.Conflict(skipped, destination, "skip (2).txt");
        FileInspectionSucceeded inspected = TransferConflictTestContext.Inspection(source);
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([ProviderStepEffectKind.CopyTargetCreated], null, []);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(TransferConflictTestContext.Inspection(skipped));
        port.EnqueueInspection(inspected);
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([conflict]));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([
            TransferPlanEntry.Skip(TransferConflictTestContext.ResolvedSource(conflict, TransferConflictDecision.Skip), conflict.ExistingTarget),
            TransferPlanEntry.Transfer(inspected.Snapshot, GatewayTestContext.ParsePath("D:\\destination\\source.txt"))]));
        ProviderStepCompletion providerCompletion = completion == "cancelled"
            ? ProviderStepCompletion.Cancelled : ProviderStepCompletion.Failed(FileOperationFailureKind.Conflict);
        port.EnqueueCopy(ProviderStepOutcome.Create(providerCompletion, receipt));
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy([skipped, source], destination), RecordingFileOperationProgress.Create(), CancellationToken.None);

        FileOperationOutcome outcome = await TransferConflictTestContext.ResumeSkipAsync(gateway, awaiting.Continuation!);

        Assert.AreSame(completion == "cancelled" ? FileOperationCompletionKind.Cancelled : FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.HasCount(1, outcome.NotTransferred);
        Assert.AreSame(skipped, outcome.NotTransferred[0]);
        Assert.HasCount(1, outcome.ProviderSteps);
        Assert.AreSame(source, outcome.ProviderSteps[0].Source);
        Assert.AreSame(receipt, outcome.ProviderSteps[0].Receipt);
        Assert.AreSame(providerCompletion.Failure, outcome.Failure);
        Assert.IsNull(outcome.Conflicts);
        Assert.IsNull(outcome.Continuation);
        Assert.HasCount(5, port.Calls);
    }
}
