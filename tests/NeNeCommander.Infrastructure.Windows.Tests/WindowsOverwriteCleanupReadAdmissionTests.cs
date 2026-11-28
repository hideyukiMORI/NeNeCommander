using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves cleanup admits remaining artifact shape before reading content, retaining all links on refusal.</summary>
[TestClass]
public sealed class WindowsOverwriteCleanupReadAdmissionTests
{
    /// <summary>A changed artifact rejects at the first metadata admission, before any artifact-content comparison.</summary>
    [TestMethod]
    [DataRow("prepared")]
    [DataRow("backup")]
    [DataRow("recovery")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task CleanupAsyncWhenArtifactShapeChangesRefusesBeforeArtifactReadsAsync(string artifact)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        bool armed = false;
        int artifactReads = 0;
        WindowsTransferContentIo io = new((handle, bytes, offset, token) =>
        {
            if (armed && !ReferenceEquals(handle, files.Pair.Source.Handle) && !ReferenceEquals(handle, files.Pair.Target.Handle))
            {
                artifactReads++;
            }
            return RandomAccess.ReadAsync(handle, bytes, offset, token);
        }, RandomAccess.WriteAsync, RandomAccess.SetLength, RandomAccess.FlushToDisk);
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        WindowsTransferRecoveryMapping mapping = await PrepareCompletedAsync(attempt);
        TransferArtifactRole role = Role(artifact);
        IdentifiedTransferArtifact previous = WindowsOverwritePreparationFixture.Artifact(attempt, role);
        string path = previous.Artifact.Path.CanonicalText;
        FileAttributes attributes = File.GetAttributes(path);
        File.SetAttributes(path, attributes | FileAttributes.Hidden);
        armed = true;
        try
        {
            _ = await Assert.ThrowsAsync<IOException>(() => attempt.CleanupAsync(CancellationToken.None));

            Assert.AreEqual(0, artifactReads);
            IdentifiedTransferArtifact saved = WindowsOverwritePreparationFixture.Artifact(attempt, role);
            Assert.AreEqual(previous.Content, saved.Content);
            Assert.AreEqual(ArtifactLinkObservation.Retained, saved.Link);
            Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
            Assert.AreEqual("source", files.Read(files.Pair.Target.Path));
            Assert.AreEqual("target", files.Read(mapping.Names.OriginalBackup));
            Assert.HasCount(3, attempt.Receipt.Artifacts);
        }
        finally
        {
            File.SetAttributes(path, attributes);
        }
    }

    private static TransferArtifactRole Role(string artifact)
    {
        return artifact switch
        {
            "prepared" => TransferArtifactRole.PreparedContent,
            "backup" => TransferArtifactRole.OriginalBackup,
            "recovery" => TransferArtifactRole.RecoveryRecord,
            _ => throw new ArgumentOutOfRangeException(nameof(artifact)),
        };
    }

    private static async Task<WindowsTransferRecoveryMapping> PrepareCompletedAsync(WindowsOverwriteAttempt attempt)
    {
        WindowsTransferRecoveryMapping mapping = await attempt.PrepareAsync(CancellationToken.None);
        await attempt.OverwritePreparedAsync(CancellationToken.None);
        await attempt.AppendCompletionAsync(CancellationToken.None);
        return mapping;
    }
}
