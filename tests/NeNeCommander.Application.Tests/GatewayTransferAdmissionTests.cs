using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves GatewayTransferAdmission behavior and boundaries.</summary>
[TestClass]
public sealed class GatewayTransferAdmissionTests
{
    /// <summary>Proves complete preflight precedes ordered composite effects.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenMoveSucceedsReportsOrderedEffectsAfterCompletePreflight()
    {
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        MoveRequest request = GatewayTestContext.CreateMove([first, second]);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        for (int index = 0; index < 2; index++)
        {
            port.EnqueueCopy(ProviderStepOutcome.Succeeded());
            port.EnqueueVerification(ProviderStepOutcome.Succeeded());
            port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        }
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(6, outcome.Effects);
        Assert.HasCount(9, port.Calls);
        Assert.AreEqual("Inspect:C:\\first", port.Calls[0]);
        Assert.AreEqual("Inspect:C:\\second", port.Calls[1]);
        Assert.AreEqual("Preflight:D:\\destination", port.Calls[2]);
        Assert.AreEqual("Copy:C:\\first", port.Calls[3]);
        Assert.AreEqual("Verify:C:\\first", port.Calls[4]);
        Assert.AreEqual("Delete:C:\\first", port.Calls[5]);
        Assert.AreEqual("Copy:C:\\second", port.Calls[6]);
        Assert.AreEqual("Verify:C:\\second", port.Calls[7]);
        Assert.AreEqual("Delete:C:\\second", port.Calls[8]);
    }

    /// <summary>Proves a copy rejected by preflight starts no mutation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    [TestProperty("ThreatId", "ADV-018")]
    public async Task ExecuteAsyncWhenCopyPreflightFindsConflictNoMutationStarts()
    {
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(path, DeletionCapability.PermanentOnly));
        port.EnqueuePreflight(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateCopy([path]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(2, port.Calls);
    }

    /// <summary>Proves a later inspection failure prevents every mutation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-018")]
    public async Task ExecuteAsyncWhenLaterSourceFailsInspectionNoMutationStarts()
    {
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(FileInspectionOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateMove([first, second]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(2, port.Calls);
    }

    /// <summary>Proves a preflight collision prevents every mutation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task ExecuteAsyncWhenPreflightFindsConflictNoMutationStarts()
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateMove([source]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(2, port.Calls);
    }

    /// <summary>Proves move inspection cancellation stops before the next source.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenMoveInspectionIsCancelledStopsBeforeNextSource()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterInspection,
            cancellation.Cancel);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.Recycle));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateMove([first, second]),
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(1, port.Calls);
    }

    /// <summary>Proves cancellation after move preflight starts no copy.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCancellationArrivesAfterPreflightStartsNoCopy()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath path = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterPreflight,
            cancellation.Cancel);
        port.EnqueueInspection(GatewayTestContext.Inspection(path, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(GatewayTestContext.CreateMove([path]), RecordingFileOperationProgress.Create(), cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(2, port.Calls);
    }
}
