using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves TransferAdmission behavior and boundaries.</summary>
[TestClass]
public sealed class TransferAdmissionTests
{
    /// <summary>Proves disposing the gateway releases its operation lease resource.</summary>
    [TestMethod]
    public async Task ExecuteAsyncAfterGatewayDisposalThrowsObjectDisposedException()
    {
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        gateway.Dispose();

        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => gateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy([TransferConflictTestContext.ParsePath("C:\\source.txt")], destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None));
    }

    /// <summary>Requires a later batch conflict to stop every effect before the first copy.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ExecuteAsyncWhenLaterSourceConflictsStartsNoEffect()
    {
        FileSystemPath first = TransferConflictTestContext.ParsePath("C:\\first.txt");
        FileSystemPath second = TransferConflictTestContext.ParsePath("C:\\second.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(TransferConflictTestContext.Inspection(first));
        port.EnqueueInspection(TransferConflictTestContext.Inspection(second));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([
            TransferConflictTestContext.Conflict(second, destination, "second (2).txt")]));
        using FileOperationGateway gateway = new(port);
        CopyRequest request = TransferConflictTestContext.CreateCopy([first, second], destination);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            request,
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.IsNotNull(outcome.Conflicts);
        Assert.IsNotNull(outcome.Continuation);
        Assert.AreSame(outcome.Conflicts, outcome.Continuation.PendingConflicts);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(3, port.Calls);
    }

    /// <summary>Requires cancellation raised by complete preflight to stop before copying.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenPreflightCancelsStopsBeforeFirstEffect()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath source = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterPreflight,
            cancellation.Cancel);
        FileEntrySnapshot snapshot = TransferConflictTestContext.Inspection(source).Snapshot;
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(snapshot));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([
            TransferPlanEntry.Transfer(snapshot, ((PathParseSuccess)destination.Child("source.txt")).Path)]));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy([source], destination),
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(2, port.Calls);
    }

    /// <summary>Requires cancellation during move capability discovery to stop before mutation.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCapabilityDiscoveryCancelsStopsBeforeFirstEffect()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath first = TransferConflictTestContext.ParsePath("C:\\first.txt");
        FileSystemPath second = TransferConflictTestContext.ParsePath("C:\\second.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterAtomicCapability,
            cancellation.Cancel);
        port.EnqueueInspection(TransferConflictTestContext.Inspection(first));
        port.EnqueueInspection(TransferConflictTestContext.Inspection(second));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueAtomicMoveCapability(AtomicMoveCapabilityOutcome.Supported);
        using FileOperationGateway gateway = new(port);
        MoveRequest request = (MoveRequest)((FileOperationRequestAccepted)MoveRequest.Create(
            [first, second],
            destination)).Request;

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            request,
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(4, port.Calls);
        Assert.AreEqual("AtomicCapability:C:\\first.txt", port.Calls[3]);
    }
}
