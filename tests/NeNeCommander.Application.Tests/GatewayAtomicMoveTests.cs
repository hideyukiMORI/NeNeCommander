using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves GatewayAtomicMove behavior and boundaries.</summary>
[TestClass]
public sealed class GatewayAtomicMoveTests
{
    /// <summary>Proves every capability is resolved before a mixed atomic and composite move starts.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-018")]
    public async Task ExecuteAsyncWhenAtomicMoveIsSupportedUsesItAfterCompleteCapabilityPreflight()
    {
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueAtomicMoveCapability(AtomicMoveCapabilityOutcome.Supported);
        port.EnqueueAtomicMoveCapability(AtomicMoveCapabilityOutcome.Unsupported);
        port.EnqueueAtomicMove(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateMove([first, second]),
            progress,
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(4, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.AtomicallyMoved, outcome.Effects[0].Kind);
        Assert.AreEqual("AtomicCapability:C:\\first", port.Calls[3]);
        Assert.AreEqual("AtomicCapability:C:\\second", port.Calls[4]);
        Assert.AreEqual("AtomicMove:C:\\first", port.Calls[5]);
        Assert.AreEqual("Copy:C:\\second", port.Calls[6]);
        Assert.HasCount(2, progress.Reports);
        Assert.AreEqual(FileOperationProgress.Create(1, 2), progress.Reports[0]);
        Assert.AreEqual(FileOperationProgress.Create(2, 2), progress.Reports[1]);
    }

    /// <summary>Proves one failed capability decision stops the whole batch before mutation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [TestProperty("ThreatId", "ADV-018")]
    public async Task ExecuteAsyncWhenAtomicMoveCapabilityFailsStartsNoMutation()
    {
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueAtomicMoveCapability(AtomicMoveCapabilityOutcome.Supported);
        port.EnqueueAtomicMoveCapability(AtomicMoveCapabilityOutcome.Failed(FileOperationFailureKind.Inspection));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateMove([first, second]),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Inspection, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(5, port.Calls);
    }

    /// <summary>Proves cancellation during capability preflight stops further queries and every mutation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenCancellationArrivesDuringAtomicCapabilityPreflightStopsBatch()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterAtomicCapability,
            cancellation.Cancel);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueAtomicMoveCapability(AtomicMoveCapabilityOutcome.Supported);
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateMove([first, second]),
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(4, port.Calls);
        Assert.AreEqual("AtomicCapability:C:\\first", port.Calls[3]);
    }

    /// <summary>Proves an atomic provider failure reports no effect and starts no fallback mutation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task ExecuteAsyncWhenAtomicMoveFailsReportsFailureWithoutCompositeFallback()
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueAtomicMoveCapability(AtomicMoveCapabilityOutcome.Supported);
        port.EnqueueAtomicMove(ProviderStepOutcome.Failed(FileOperationFailureKind.Move));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateMove([source]),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Move, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(4, port.Calls);
        Assert.AreEqual("AtomicMove:C:\\source", port.Calls[3]);
    }

    /// <summary>Proves cancellation after one atomic item prevents the next provider effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenCancellationArrivesAfterAtomicMoveStartsNoNextItem()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterAtomicMove,
            cancellation.Cancel);
        port.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueAtomicMoveCapability(AtomicMoveCapabilityOutcome.Supported);
        port.EnqueueAtomicMoveCapability(AtomicMoveCapabilityOutcome.Supported);
        port.EnqueueAtomicMove(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateMove([first, second]),
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.AtomicallyMoved, outcome.Effects[0].Kind);
        Assert.HasCount(6, port.Calls);
        Assert.AreEqual("AtomicMove:C:\\first", port.Calls[5]);
    }

    /// <summary>Proves copy never queries or invokes the provider's atomic-move path.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCopyRunsDoesNotQueryAtomicMoveCapability()
    {
        FileSystemPath source = GatewayTestContext.ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.PermanentOnly));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueAtomicMoveCapability(AtomicMoveCapabilityOutcome.Supported);
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            GatewayTestContext.CreateCopy([source]),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(4, port.Calls);
        Assert.AreEqual("Inspect:C:\\source", port.Calls[0]);
        Assert.AreEqual("Preflight:D:\\destination", port.Calls[1]);
        Assert.AreEqual("Copy:C:\\source", port.Calls[2]);
        Assert.AreEqual("Verify:C:\\source", port.Calls[3]);
    }
}
