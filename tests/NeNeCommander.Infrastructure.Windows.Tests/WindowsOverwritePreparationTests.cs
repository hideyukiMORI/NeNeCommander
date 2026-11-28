using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the native preparation stage owns artifacts and mapping without mutating original files or claiming copy completion.</summary>
[TestClass]
public sealed class WindowsOverwritePreparationTests
{
    /// <summary>Proves one token and the existing scheduler produce verified native artifacts, with mapping flush acknowledged and original target untouched.</summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(0, 7)]
    [DataRow(65537, 3)]
    [DataRow(3, 131073)]
    public async Task PrepareAsyncWhenPairIsAdmittedRetainsVerifiedPreparationsAsync(int sourceLength, int targetLength)
    {
        string source = new('s', sourceLength);
        string target = new('t', targetLength);
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create(source, target);
        DeterministicIdentifierSource identifiers = new();
        using WindowsOverwriteAttempt attempt = fixture.Attempt(identifiers, WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native);
        WindowsLocalIoExecutionBoundary boundary = new();

        Task<WindowsTransferRecoveryMapping> operation = await boundary.ExecuteAsync(() => attempt.PrepareAsync(CancellationToken.None));
        WindowsTransferRecoveryMapping mapping = await operation;
        Assert.AreEqual(1, identifiers.IssuedCount);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        Assert.AreEqual(target, fixture.Read(mapping.Names.OriginalBackup));
        Assert.AreEqual(source, fixture.Read(mapping.Names.PreparedContent));
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(target))), mapping.Original.Hash.Value);
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))), mapping.Desired.Hash.Value);
        RecoveryMappingContent recovery = Assert.IsInstanceOfType<RecoveryMappingContent>(WindowsOverwritePreparationFixture.Artifact(attempt, TransferArtifactRole.RecoveryRecord).Content);
        Assert.AreEqual(RecoveryFlushObservation.Confirmed, recovery.InitialFrame.Flush);
        Assert.AreEqual(RecoveryAppendObservation.NotAttempted, recovery.Append);
        Assert.AreEqual(mapping, WindowsTransferRecoveryCodec.Inspect(Encoding.UTF8.GetBytes(fixture.Read(mapping.Names.Recovery))).Mapping);
        Assert.IsTrue(attempt.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>().All(artifact => artifact.Artifact.PathObservation == TransferPathObservation.CurrentPathVerified));
        fixture.RequireUntouched(attempt, source, target);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.PrepareAsync(CancellationToken.None));
        attempt.Dispose();
        fixture.Pair.Revalidate(fixture.Pair.Evidence);
        Assert.IsTrue(attempt.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>().All(artifact => artifact.Artifact.PathObservation == TransferPathObservation.LastKnownPath));
        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => attempt.PrepareAsync(CancellationToken.None));
    }

    /// <summary>Proves repeated candidate tokens never retry collisions and every earlier creation is retained.</summary>
    [TestMethod]
    [DataRow("recovery", 1)]
    [DataRow("backup", 2)]
    [DataRow("prepared", 3)]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task PrepareAsyncWhenCandidateIsOccupiedRetainsEarlierArtifactsAndForeignContentAsync(string collision, int expectedReceipts)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        DeterministicIdentifierSource identifiers = new();
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create(fixture.Pair.Target.Path, Guid.ParseExact(new string('0', 31) + "1", "N"));
        string candidate = collision == "recovery" ? names.Recovery.CanonicalText : collision == "backup" ? names.OriginalBackup.CanonicalText : names.PreparedContent.CanonicalText;
        _ = fixture.Root.WriteFile(Path.GetFileName(candidate), "foreign");
        using WindowsOverwriteAttempt attempt = fixture.Attempt(identifiers, WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.PrepareAsync(CancellationToken.None));
        Assert.AreEqual(1, identifiers.IssuedCount);
        Assert.HasCount(expectedReceipts, attempt.Receipt.Artifacts);
        _ = Assert.IsInstanceOfType<UnidentifiedTransferArtifact>(attempt.Receipt.Artifacts[^1]);
        Assert.AreEqual("foreign", File.ReadAllText(candidate));
        fixture.RequireUntouched(attempt, "source", "target");
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.PrepareAsync(CancellationToken.None));
    }

    /// <summary>Proves an original target named like the dummy candidate can still prepare using its one actual distinct token.</summary>
    [TestMethod]
    public async Task PrepareAsyncWhenTargetMatchesDummyTokenUsesActualTokenAsync()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target", "NeNeCommander-00000000000000000000000000000000-original.bin");
        DeterministicIdentifierSource identifiers = new();
        using WindowsOverwriteAttempt attempt = fixture.Attempt(identifiers, WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native);

        WindowsTransferRecoveryMapping mapping = await attempt.PrepareAsync(CancellationToken.None);
        Assert.AreEqual(1, identifiers.IssuedCount);
        Assert.AreNotEqual(fixture.Pair.Target.Path, mapping.Names.OriginalBackup);
        fixture.RequireUntouched(attempt, "source", "target");
    }
}
