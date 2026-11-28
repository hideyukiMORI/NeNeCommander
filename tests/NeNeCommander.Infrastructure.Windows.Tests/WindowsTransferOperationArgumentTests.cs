using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves missing operation evidence fails at its owning boundary without producing native effects.</summary>
[TestClass]
public sealed class WindowsTransferOperationArgumentTests
{
    /// <summary>Borrowed content comparison and copying require explicit handles, length and I/O.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CopyAsyncWhenBorrowedContentEvidenceIsAbsentRefusesBeforeEffectsAsync()
    {
        using WindowsTransferContentFixture fixture = WindowsTransferContentFixture.Create("source", "target", 6);
        WindowsTransferContentPair pair = fixture.Pair;
        WindowsTransferContentIo io = WindowsTransferContentIo.Native;

        TransferArgumentAssertions.Rejects(() => WindowsTransferContentPair.Create(null!, pair.Target, pair.Size), "source");
        TransferArgumentAssertions.Rejects(() => WindowsTransferContentPair.Create(pair.Source, null!, pair.Size), "target");
        TransferArgumentAssertions.Rejects(() => WindowsTransferContentPair.Create(pair.Source, pair.Target, null!), "size");
        await TransferArgumentAssertions.RejectsAsync(() => WindowsTransferContentCopy.CopyAsync(null!, io, CancellationToken.None), "pair");
        await TransferArgumentAssertions.RejectsAsync(() => WindowsTransferContentCopy.CopyAsync(pair, null!, CancellationToken.None), "io");
        await TransferArgumentAssertions.RejectsAsync(() => VerifiedFileContent.CompareHeldAsync(null!, io, CancellationToken.None), "pair");
        await TransferArgumentAssertions.RejectsAsync(() => VerifiedFileContent.CompareHeldAsync(pair, null!, CancellationToken.None), "io");
        await TransferArgumentAssertions.RejectsAsync(() => VerifiedFileContent.ReadHeldAsync(null!, pair.Size, io, CancellationToken.None), "handle");
        await TransferArgumentAssertions.RejectsAsync(() => VerifiedFileContent.ReadHeldAsync(pair.Source, null!, io, CancellationToken.None), "size");
        await TransferArgumentAssertions.RejectsAsync(() => VerifiedFileContent.ReadHeldAsync(pair.Source, pair.Size, null!, CancellationToken.None), "io");
        Assert.AreEqual("source", fixture.Read("source"));
        Assert.AreEqual("target", fixture.Read("target"));
        Assert.IsFalse(pair.Source.IsClosed);
        Assert.IsFalse(pair.Target.IsClosed);
    }

    /// <summary>An incomplete attempt, original consent or provider boundary cannot start overwrite preparation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task CopyAsyncWhenOriginalOwnerIsAbsentRejectsBeforePreparationAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteDependencies dependencies = WindowsOverwriteDependencies.Native(new DeterministicIdentifierSource());
        FileEntrySnapshot source = FileEntrySnapshot.Create(fixture.Pair.Source.Path, fixture.Pair.Source.Baseline.Identity, DeletionCapability.NotRequested);
        WindowsOverwriteCopy copy = new(dependencies, new WindowsDriveBoundary());

        TransferArgumentAssertions.Rejects(() => WindowsOverwriteAttempt.Create(null!, fixture.Pair.Evidence, dependencies), "pair");
        TransferArgumentAssertions.Rejects(() => WindowsOverwriteAttempt.Create(fixture.Pair, null!, dependencies), "original");
        TransferArgumentAssertions.Rejects(() => WindowsOverwriteAttempt.Create(fixture.Pair, fixture.Pair.Evidence, null!), "dependencies");
        TransferArgumentAssertions.Rejects(() => WindowsOverwritePairLease.Open(source, fixture.Pair.Target.Path, null!), "driveBoundary");
        await TransferArgumentAssertions.RejectsAsync(() => copy.CopyAsync(null!, fixture.Pair.Evidence, CancellationToken.None), "source");
        await TransferArgumentAssertions.RejectsAsync(() => copy.CopyAsync(source, null!, CancellationToken.None), "original");
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
    }

    /// <summary>Initial and completion writers report the missing record, mapping or I/O without creating or writing a sibling.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task WriteAsyncWhenRecoveryOwnerIsAbsentRejectsBeforeWritingAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping(fixture.Pair.Source.Path.CanonicalText, fixture.Pair.Target.Path.CanonicalText);
        WindowsTransferContentIo io = WindowsTransferContentIo.Native;
        using WindowsTransferArtifactOwner owner = WindowsTransferArtifactOwner.Prepare(TransferArtifactRole.RecoveryRecord,
            mapping.Names.Recovery, fixture.Pair.Target, WindowsTransferArtifactQueries.Native);

        await TransferArgumentAssertions.RejectsAsync(() => WindowsTransferRecoveryWriter.WriteInitialAsync(null!, mapping, io, CancellationToken.None), "owner");
        await TransferArgumentAssertions.RejectsAsync(() => WindowsTransferRecoveryWriter.WriteInitialAsync(owner, null!, io, CancellationToken.None), "mapping");
        await TransferArgumentAssertions.RejectsAsync(() => WindowsTransferRecoveryWriter.WriteInitialAsync(owner, mapping, null!, CancellationToken.None), "io");
        await TransferArgumentAssertions.RejectsAsync(() => WindowsTransferRecoveryWriter.AppendCompletionAsync(null!, mapping, io, CancellationToken.None), "owner");
        await TransferArgumentAssertions.RejectsAsync(() => WindowsTransferRecoveryWriter.AppendCompletionAsync(owner, null!, io, CancellationToken.None), "mapping");
        await TransferArgumentAssertions.RejectsAsync(() => WindowsTransferRecoveryWriter.AppendCompletionAsync(owner, mapping, null!, CancellationToken.None), "io");
        Assert.IsNull(owner.Receipt);
        Assert.IsFalse(System.IO.File.Exists(mapping.Names.Recovery.CanonicalText));
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
    }

    /// <summary>Cleanup cannot be constructed from a missing inventory, mapping, recovery snapshot or native owner.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public void ConstructWhenCleanupEvidenceIsAbsentRejectsWithoutRemovingAnything()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryTestFixture.Mapping(fixture.Pair.Source.Path.CanonicalText, fixture.Pair.Target.Path.CanonicalText);
        WindowsOverwriteDependencies dependencies = WindowsOverwriteDependencies.Native(new DeterministicIdentifierSource());
        using WindowsTransferPreparationArtifacts artifacts = WindowsTransferPreparationArtifacts.Create(mapping.Names, fixture.Pair.Target, dependencies.Artifacts);
        VerifiedEntrySnapshot recovery = WindowsTransferRecoveryTestFixture.Entry(mapping.Names.Recovery, "recovery");

        TransferArgumentAssertions.Rejects(() => _ = new WindowsOverwriteCleanup(null!, mapping, recovery, dependencies), "artifacts");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsOverwriteCleanup(artifacts, null!, recovery, dependencies), "mapping");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsOverwriteCleanup(artifacts, mapping, null!, dependencies), "recovery");
        TransferArgumentAssertions.Rejects(() => _ = new WindowsOverwriteCleanup(artifacts, mapping, recovery, null!), "dependencies");
        Assert.IsNull(artifacts.Recovery.Receipt);
        Assert.IsNull(artifacts.Backup.Receipt);
        Assert.IsNull(artifacts.Prepared.Receipt);
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual("target", fixture.Read(fixture.Pair.Target.Path));
    }
}
