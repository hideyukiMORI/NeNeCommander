using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves exact consent observations retain known facts and reject contradictory file bindings.</summary>
[TestClass]
public sealed class TransferOverwriteEvidenceTests
{
    /// <summary>Proves roots, remote paths, directories and last-known names cannot describe an admitted file.</summary>
    [TestMethod]
    [DataRow("sourceRoot")]
    [DataRow("targetRoot")]
    [DataRow("unc")]
    [DataRow("wsl")]
    [DataRow("lastKnown")]
    [DataRow("directory")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public void CreateWhenFileObservationIsContradictoryRefusesIt(string scenario)
    {
        string path = scenario switch
        {
            "sourceRoot" => @"C:\",
            "targetRoot" => @"D:\",
            "unc" => @"\\server\share\target.txt",
            "wsl" => @"\\wsl.localhost\Ubuntu\target.txt",
            _ => @"D:\destination\target.txt",
        };
        ObservedTransferEntry entry = ReceiptObservationTestContext.Entry(path,
            scenario == "lastKnown" ? TransferPathObservation.LastKnownPath : TransferPathObservation.CurrentPathVerified);
        DirectoryEntryKind kind = scenario == "directory" ? DirectoryEntryKind.Directory : DirectoryEntryKind.File;

        _ = Assert.ThrowsExactly<ArgumentException>(() => TransferOverwriteFileObservation.Create(entry, kind, Size(), Modified()));
    }

    /// <summary>Proves both lexical aliases and a distinct name for the same native object are refused.</summary>
    [TestMethod]
    [DataRow("path")]
    [DataRow("case")]
    [DataRow("object")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public void CreateWhenSourceAndTargetAliasRefusesIt(string scenario)
    {
        TransferOverwriteFileObservation source = FileObservation(@"C:\source.txt");
        string path = scenario switch
        {
            "path" => @"C:\source.txt",
            "case" => @"C:\SOURCE.txt",
            _ => @"D:\destination\target.txt",
        };
        ObservedTransferEntry targetEntry = ReceiptObservationTestContext.Entry(path, TransferPathObservation.CurrentPathVerified);
        FileObjectIdentity objectIdentity = scenario == "object"
            ? source.Observation.Entry.ObjectIdentity : targetEntry.Entry.ObjectIdentity;
        TransferOverwriteFileObservation target = TransferOverwriteFileObservation.Create(
            ObservedTransferEntry.Create(VerifiedEntrySnapshot.Create(targetEntry.Entry.Path, targetEntry.Entry.Identity, objectIdentity),
                TransferPathObservation.CurrentPathVerified), DirectoryEntryKind.File, Size(), Modified());

        _ = Assert.ThrowsExactly<ArgumentException>(() => TransferOverwriteEvidence.Create(source, target));
    }

    /// <summary>Proves source identity, source pathname and original target must all match the retained evidence.</summary>
    [TestMethod]
    [DataRow("sourceIdentity")]
    [DataRow("sourcePath")]
    [DataRow("targetPath")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public void CreateWhenConflictDoesNotMatchEvidenceRefusesIt(string scenario)
    {
        TransferOverwriteEvidence evidence = TransferOverwriteEvidence.Create(FileObservation(@"C:\source.txt"), FileObservation(@"D:\destination\target.txt"));
        FileSystemPath sourcePath = scenario == "sourcePath"
            ? GatewayTestContext.ParsePath(@"C:\different.txt") : evidence.Source.Observation.Entry.Path;
        FileIdentity identity = scenario == "sourceIdentity"
            ? Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("changed")).Identity : evidence.Source.Observation.Entry.Identity;
        FileEntrySnapshot source = FileEntrySnapshot.Create(sourcePath, identity, DeletionCapability.NotRequested);
        FileSystemPath target = scenario == "targetPath"
            ? GatewayTestContext.ParsePath(@"D:\destination\different.txt") : evidence.Target.Observation.Entry.Path;

        _ = Assert.ThrowsExactly<ArgumentException>(() => TransferConflict.Create(source, target,
            GatewayTestContext.ParsePath(@"D:\destination\target (2).txt"), evidence));
    }

    /// <summary>Proves complete evidence survives choice and plan without adding a destructive choice.</summary>
    [TestMethod]
    public void CreateWhenEvidenceMatchesRetainsExactObjectsThroughThePlan()
    {
        TransferOverwriteEvidence evidence = TransferOverwriteEvidence.Create(FileObservation(@"C:\source.txt"), FileObservation(@"D:\destination\target.txt"));
        FileEntrySnapshot source = FileEntrySnapshot.Create(evidence.Source.Observation.Entry.Path,
            evidence.Source.Observation.Entry.Identity, DeletionCapability.NotRequested);
        TransferConflict conflict = TransferConflict.Create(source, evidence.Target.Observation.Entry.Path,
            GatewayTestContext.ParsePath(@"D:\destination\target (2).txt"), evidence);
        TransferConflictChoice choice = TransferConflictChoice.Create(conflict, TransferConflictDecision.KeepBoth);
        TransferPlanEntry plan = TransferPlanEntry.Transfer(source.WithConflictChoice(choice), conflict.KeepBothCandidate);

        Assert.AreSame(evidence, plan.ConflictTarget?.OverwriteEvidence);
        Assert.AreSame(conflict.Target, choice.Target);
        Assert.AreSame(choice.Target, plan.ConflictTarget);
        Assert.AreSame(evidence.Target.Observation.Entry.Path, conflict.ExistingTarget);
        Assert.AreSame(DirectoryEntryKind.File, evidence.Source.Kind);
        Assert.AreEqual(17L, evidence.Source.Size.Bytes);
        Assert.AreEqual(new DateTimeOffset(2026, 11, 25, 0, 0, 0, TimeSpan.Zero), evidence.Target.Modified.Utc);
        CollectionAssert.AreEqual(new[] { TransferConflictDecision.Skip, TransferConflictDecision.KeepBoth, TransferConflictDecision.Cancel },
            System.Linq.Enumerable.ToArray(conflict.AllowedDecisions));
    }

    /// <summary>Proves a reservation has no qualified overwrite evidence and an ordinary plan has no conflict binding.</summary>
    [TestMethod]
    public void CreateWhenEvidenceIsAbsentDoesNotInferAnExistingObject()
    {
        TransferConflict conflict = TransferConflictTestContext.Conflict(GatewayTestContext.ParsePath(@"C:\source.txt"),
            GatewayTestContext.ParsePath(@"D:\destination"), "source (2).txt");
        TransferPlanEntry ordinary = TransferPlanEntry.Transfer(conflict.Source, conflict.KeepBothCandidate);
        TransferConflictChoice choice = TransferConflictChoice.Create(conflict, TransferConflictDecision.Skip);
        TransferPlanEntry skipped = TransferPlanEntry.Skip(conflict.Source.WithConflictChoice(choice), conflict.ExistingTarget);

        Assert.IsNull(conflict.Target.OverwriteEvidence);
        Assert.IsNull(ordinary.ConflictTarget);
        Assert.AreSame(conflict.Target, skipped.ConflictTarget);
    }

    /// <summary>Proves absent required observations are not accepted as unknown evidence.</summary>
    [TestMethod]
    public void CreateWhenRequiredEvidenceIsAbsentRejectsIt()
    {
        TransferOverwriteFileObservation source = FileObservation(@"C:\source.txt");
        ObservedTransferEntry entry = source.Observation;

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferOverwriteFileObservation.Create(null!, DirectoryEntryKind.File, Size(), Modified()));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferOverwriteFileObservation.Create(entry, null!, Size(), Modified()));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferOverwriteFileObservation.Create(entry, DirectoryEntryKind.File, null!, Modified()));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferOverwriteFileObservation.Create(entry, DirectoryEntryKind.File, Size(), null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferOverwriteEvidence.Create(null!, source));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferOverwriteEvidence.Create(source, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => TransferConflictTarget.Create(null!, null));
    }

    private static TransferOverwriteFileObservation FileObservation(string path)
    {
        return TransferOverwriteFileObservation.Create(ReceiptObservationTestContext.Entry(path, TransferPathObservation.CurrentPathVerified),
            DirectoryEntryKind.File, Size(), Modified());
    }

    private static KnownEntrySize Size()
    {
        return Assert.IsInstanceOfType<KnownEntrySize>(EntrySize.Create(17));
    }

    private static KnownEntryTimestamp Modified()
    {
        return Assert.IsInstanceOfType<KnownEntryTimestamp>(EntryTimestamp.Create(new DateTimeOffset(2026, 11, 25, 0, 0, 0, TimeSpan.Zero)));
    }
}
