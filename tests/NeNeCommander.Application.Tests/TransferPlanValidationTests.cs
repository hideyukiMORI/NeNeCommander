using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves TransferPlanValidation behavior and boundaries.</summary>
[TestClass]
public sealed class TransferPlanValidationTests
{
    /// <summary>Requires a successful provider plan to contain every frozen source exactly once.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ExecuteAsyncWhenPreflightPlanOmitsBatchRejectsBeforeEffect()
    {
        FileSystemPath first = TransferConflictTestContext.ParsePath("C:\\first.txt");
        FileSystemPath second = TransferConflictTestContext.ParsePath("C:\\second.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(TransferConflictTestContext.Inspection(first));
        port.EnqueueInspection(TransferConflictTestContext.Inspection(second));
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded([]));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy([first, second], destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(3, port.Calls);
    }

    /// <summary>Requires every provider plan field to preserve the frozen batch contract.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ExecuteAsyncWhenPreflightPlanChangesFrozenEntryRejectsBeforeEffect()
    {
        FileSystemPath sourcePath = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath otherPath = TransferConflictTestContext.ParsePath("C:\\other.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        FileSystemPath target = TransferConflictTestContext.ParsePath("D:\\destination\\source.txt");
        FileEntrySnapshot source = TransferConflictTestContext.Inspection(sourcePath).Snapshot;
        FileIdentity otherIdentity = ((FileIdentityAccepted)FileIdentity.Parse("other-identity")).Identity;
        TransferConflict unexpectedConflict = TransferConflictTestContext.Conflict(sourcePath, destination, "source (2).txt");

        TransferPlanEntry[] invalidEntries =
        [
            TransferPlanEntry.Transfer(TransferConflictTestContext.Inspection(otherPath).Snapshot, target),
            TransferPlanEntry.Transfer( FileEntrySnapshot.Create(sourcePath, otherIdentity, DeletionCapability.PermanentOnly), target),
            TransferPlanEntry.Transfer( FileEntrySnapshot.Create(sourcePath, source.Identity, DeletionCapability.Recycle), target),
            TransferPlanEntry.Transfer(source.WithTransferTarget(target), target),
            TransferPlanEntry.Transfer( source.WithConflictChoice(TransferConflictChoice.Create( unexpectedConflict, TransferConflictDecision.Skip)), target),
            TransferPlanEntry.Transfer(source, TransferConflictTestContext.ParsePath("E:\\outside\\source.txt")),
            TransferPlanEntry.Transfer(source, TransferConflictTestContext.ParsePath("D:\\")),
            TransferPlanEntry.Skip(source, target),
        ];
        foreach (TransferPlanEntry entry in invalidEntries)
        {
            await AssertPlanRejected([sourcePath], destination, [entry]);
        }
    }

    /// <summary>Requires provider plans to preserve exact source order without duplicates.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ExecuteAsyncWhenPreflightPlanReordersOrDuplicatesBatchRejectsBeforeEffect()
    {
        FileSystemPath firstPath = TransferConflictTestContext.ParsePath("C:\\first.txt");
        FileSystemPath secondPath = TransferConflictTestContext.ParsePath("C:\\second.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        FileEntrySnapshot first = TransferConflictTestContext.Inspection(firstPath).Snapshot;
        FileEntrySnapshot second = TransferConflictTestContext.Inspection(secondPath).Snapshot;
        FileSystemPath firstTarget = TransferConflictTestContext.ParsePath("D:\\destination\\first.txt");
        FileSystemPath secondTarget = TransferConflictTestContext.ParsePath("D:\\destination\\second.txt");

        await AssertPlanRejected(
            [firstPath, secondPath],
            destination,
            [
                TransferPlanEntry.Transfer(second, secondTarget),
                TransferPlanEntry.Transfer(first, firstTarget),
            ]);
        await AssertPlanRejected(
            [firstPath, secondPath],
            destination,
            [
                TransferPlanEntry.Transfer(first, firstTarget),
                TransferPlanEntry.Transfer(first, firstTarget),
            ]);
    }

    /// <summary>Requires provider conflicts to refer only to frozen sources and destination children.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ExecuteAsyncWhenConflictEscapesFrozenBatchRejectsWithoutContinuation()
    {
        FileSystemPath source = TransferConflictTestContext.ParsePath("C:\\source.txt");
        FileSystemPath injected = TransferConflictTestContext.ParsePath("C:\\injected.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        await AssertConflictRejected(
            [source],
            destination,
            TransferConflictTestContext.Conflict(injected, destination, "injected (2).txt"));
        await AssertConflictRejected(
            [source],
            destination,
            TransferConflictTestContext.Conflict(source, TransferConflictTestContext.ParsePath("E:\\outside"), "source (2).txt"));
    }

    /// <summary>Requires conflict sets to retain source order, uniqueness, and distinct child targets.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ExecuteAsyncWhenConflictSetIsDuplicateOrReorderedRejectsWithoutContinuation()
    {
        FileSystemPath first = TransferConflictTestContext.ParsePath("C:\\first.txt");
        FileSystemPath second = TransferConflictTestContext.ParsePath("C:\\second.txt");
        FileSystemPath destination = TransferConflictTestContext.ParsePath("D:\\destination");
        TransferConflict firstConflict = TransferConflictTestContext.Conflict(first, destination, "first (2).txt");
        TransferConflict secondConflict = TransferConflictTestContext.Conflict(second, destination, "second (2).txt");
        TransferConflict sameTarget = TransferConflict.Create(
            firstConflict.Source,
            firstConflict.ExistingTarget,
            firstConflict.ExistingTarget);

        await AssertConflictSetRejected(
            [first, second],
            destination,
            [firstConflict, firstConflict]);
        await AssertConflictSetRejected(
            [first, second],
            destination,
            [secondConflict, firstConflict]);
        await AssertConflictSetRejected(
            [first],
            destination,
            [sameTarget]);
    }

    private static async Task AssertConflictRejected(
        FileSystemPath[] sources,
        FileSystemPath destination,
        TransferConflict conflict)
    {
        await AssertConflictSetRejected(sources, destination, [conflict]);
    }

    private static async Task AssertConflictSetRejected(
        FileSystemPath[] sources,
        FileSystemPath destination,
        TransferConflict[] conflicts)
    {
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        foreach (FileSystemPath source in sources)
        {
            port.EnqueueInspection(TransferConflictTestContext.Inspection(source));
        }
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted(conflicts));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy(sources, destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsNull(outcome.Continuation);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(sources.Length + 1, port.Calls);
    }

    private static async Task AssertPlanRejected(
        FileSystemPath[] sources,
        FileSystemPath destination,
        TransferPlanEntry[] plan)
    {
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        foreach (FileSystemPath source in sources)
        {
            port.EnqueueInspection(TransferConflictTestContext.Inspection(source));
        }
        port.EnqueuePreflight(TransferPreflightOutcome.Succeeded(plan));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            TransferConflictTestContext.CreateCopy(sources, destination),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(sources.Length + 1, port.Calls);
    }
}
