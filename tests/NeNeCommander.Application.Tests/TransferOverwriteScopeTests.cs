using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves overwrite consent is limited to the exact eligible conflicts already disclosed.</summary>
[TestClass]
public sealed class TransferOverwriteScopeTests
{
    /// <summary>All selects only eligible original objects, while Current selects just the first file.</summary>
    [TestMethod]
    [DataRow("all", 2)]
    [DataRow("current", 1)]
    public void AddWhenOverwriteScopeIsExplicitRetainsOnlyTheDisclosedEligibleObjects(string scope, int count)
    {
        TransferConflict first = Qualified("first");
        TransferConflict unsupported = TransferConflictTestContext.Conflict(GatewayTestContext.ParsePath(@"C:\unsupported"),
            GatewayTestContext.ParsePath(@"D:\destination"), "unsupported (2)");
        TransferConflict last = Qualified("last");
        ConflictSet conflicts = Assert.IsInstanceOfType<ConflictSet>(TransferPreflightOutcome.Conflicted([first, unsupported, last]));
        TransferConflict later = Qualified("later");

        TransferResolution result = TransferResolution.None.Add(conflicts, TransferConflictDecision.OverwriteContents,
            scope == "all" ? TransferConflictScope.All : TransferConflictScope.Current);

        Assert.HasCount(count, result.Choices);
        Assert.AreSame(first.Target, result.Choices[0].Target);
        Assert.IsNull(result.Find(unsupported.Source.Path));
        Assert.IsNull(result.Find(later.Source.Path));
        if (scope == "all")
        {
            Assert.AreSame(last.Target, result.Choices[1].Target);
        }
        else
        {
            Assert.IsNull(result.Find(last.Source.Path));
        }
    }

    /// <summary>An unavailable first conflict cannot authorize other files through a forged All submission.</summary>
    [TestMethod]
    [DataRow("all")]
    [DataRow("current")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task ResumeAsyncWhenCurrentOverwriteIsUnavailableRejectsAndConsumesWithoutProviderWork(string scope)
    {
        TransferConflict unavailable = TransferConflictTestContext.Conflict(GatewayTestContext.ParsePath(@"C:\first"),
            GatewayTestContext.ParsePath(@"D:\destination"), "first (2)");
        TransferConflict eligible = Qualified("last");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(unavailable.Source));
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(eligible.Source));
        port.EnqueuePreflight(TransferPreflightOutcome.Conflicted([unavailable, eligible]));
        using FileOperationGateway gateway = new(port);
        CopyRequest request = Assert.IsInstanceOfType<CopyRequest>(Assert.IsInstanceOfType<FileOperationRequestAccepted>(
            CopyRequest.Create([unavailable.Source.Path, eligible.Source.Path], GatewayTestContext.ParsePath(@"D:\destination"))).Request);
        FileOperationOutcome paused = await gateway.ExecuteAsync(request, RecordingFileOperationProgress.Create(), CancellationToken.None);
        string[] callsBeforeResume = [.. port.Calls];
        TransferConflictResponse response = TransferConflictResponse.Create(TransferConflictDecision.OverwriteContents,
            scope == "all" ? TransferConflictScope.All : TransferConflictScope.Current);

        FileOperationOutcome rejected = await gateway.ResumeAsync(paused.Continuation!, response, RecordingFileOperationProgress.Create(), CancellationToken.None);
        FileOperationOutcome replayed = await gateway.ResumeAsync(paused.Continuation!, response, RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, rejected.Failure);
        Assert.IsEmpty(rejected.Effects);
        Assert.IsEmpty(rejected.ProviderSteps);
        Assert.AreSame(FileOperationFailureKind.Reentrant, replayed.Failure);
        CollectionAssert.AreEqual(callsBeforeResume, System.Linq.Enumerable.ToArray(port.Calls));
    }

    /// <summary>The choice factory cannot turn unavailable evidence into a destructive choice.</summary>
    [TestMethod]
    public void CreateWhenOverwriteIsUnavailableRejectsTheChoice()
    {
        TransferConflict unavailable = TransferConflictTestContext.Conflict(GatewayTestContext.ParsePath(@"C:\first"),
            GatewayTestContext.ParsePath(@"D:\destination"), "first (2)");

        _ = Assert.ThrowsExactly<ArgumentException>(() => TransferConflictChoice.Create(unavailable, TransferConflictDecision.OverwriteContents));
    }

    private static TransferConflict Qualified(string name)
    {
        TransferOverwriteEvidence evidence = TransferOverwriteEvidence.Create(Observation(@"C:\" + name), Observation(@"D:\destination\" + name));
        FileEntrySnapshot source = FileEntrySnapshot.Create(evidence.Source.Observation.Entry.Path, evidence.Source.Observation.Entry.Identity, DeletionCapability.NotRequested);
        return TransferConflict.Create(source, evidence.Target.Observation.Entry.Path, GatewayTestContext.ParsePath(@"D:\destination\" + name + " (2)"),
            TransferOverwriteCapability.Available(evidence));
    }

    private static TransferOverwriteFileObservation Observation(string path)
    {
        return TransferOverwriteFileObservation.Create(ReceiptObservationTestContext.Entry(path, TransferPathObservation.CurrentPathVerified),
            DirectoryEntryKind.File, Assert.IsInstanceOfType<KnownEntrySize>(EntrySize.Create(17)),
            Assert.IsInstanceOfType<KnownEntryTimestamp>(EntryTimestamp.Create(new DateTimeOffset(2026, 11, 25, 0, 0, 0, TimeSpan.Zero))));
    }
}
