using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves gateway completion and interruption never discard provider receipts.</summary>
[TestClass]
public sealed class GatewayReceiptTests
{
    /// <summary>Proves success retains its provider evidence alongside copy and verification facts.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCopySucceedsRetainsItsReceipt()
    {
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([ProviderStepEffectKind.CopyTargetCreated], null, []);

        FileOperationOutcome outcome = await CopyAsync(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, receipt), null, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(1, outcome.ProviderSteps);
        Assert.AreSame(receipt, outcome.ProviderSteps[0].Receipt);
        Assert.AreEqual("C:\\source.txt", outcome.ProviderSteps[0].Source.CanonicalText);
        Assert.HasCount(3, outcome.Effects);
    }

    /// <summary>Proves a provider fact without a gateway count still makes a failure partial.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenFailureHasOnlyProviderEvidenceCannotReportNoEffectRejection()
    {
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([ProviderStepEffectKind.SourcePartiallyDeleted], null, []);

        FileOperationOutcome outcome = await CopyAsync(ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Conflict), receipt), null, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(1, outcome.ProviderSteps);
        Assert.AreSame(receipt, outcome.ProviderSteps[0].Receipt);
        Assert.IsNull(outcome.Conflicts);
        Assert.IsNull(outcome.Continuation);
    }

    /// <summary>Proves cancellation after a provider return retains that return's facts.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenCancelledAfterCopyRetainsTheProviderReceipt()
    {
        using CancellationTokenSource cancellation = new();
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([ProviderStepEffectKind.CopyTargetCreated], null, []);

        FileOperationOutcome outcome = await CopyAsync(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, receipt), cancellation.Cancel, cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.AreSame(receipt, outcome.ProviderSteps[0].Receipt);
        Assert.HasCount(2, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[1].Kind);
    }

    /// <summary>Proves move source removal retains both the copy receipt and its separate partial deletion receipt.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenMoveDeletionFailsRetainsReceiptsInExecutionOrder()
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source.txt");
        ProviderStepReceipt copy = ProviderStepReceipt.Create([ProviderStepEffectKind.CopyTargetCreated], null, []);
        ProviderStepReceipt deletion = ProviderStepReceipt.Create([ProviderStepEffectKind.SourcePartiallyDeleted], null, []);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.PermanentOnly));
        port.EnqueuePreflight(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueCopy(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, copy));
        port.EnqueueVerification(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueDeletion(ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Delete), deletion));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateMove([source]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.HasCount(2, outcome.ProviderSteps);
        Assert.AreSame(copy, outcome.ProviderSteps[0].Receipt);
        Assert.AreSame(deletion, outcome.ProviderSteps[1].Receipt);
        Assert.AreSame(source, outcome.ProviderSteps[1].Source);
        Assert.AreSame(FileOperationEffectKind.SourcePartiallyDeleted, outcome.Effects[3].Kind);
    }

    private static async Task<FileOperationOutcome> CopyAsync(ProviderStepOutcome copy, Action? afterCopy, CancellationToken token)
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source.txt");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(ScriptedCallbackPoint.AfterCopy, afterCopy);
        port.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.PermanentOnly));
        port.EnqueuePreflight(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueCopy(copy);
        port.EnqueueVerification(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        using FileOperationGateway gateway = new(port);
        return await gateway.ExecuteAsync(GatewayTestContext.CreateCopy([source]), RecordingFileOperationProgress.Create(), token);
    }
}
