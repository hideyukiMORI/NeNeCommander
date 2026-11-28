using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves byte equality cannot replace the first complete preparation identity before target mutation.</summary>
[TestClass]
public sealed class WindowsOverwritePreparedIdentityTests
{
    /// <summary>Metadata-only changes before or during the last original-content check retain all preparations and leave target untouched.</summary>
    [TestMethod]
    [DataRow("backup", "before")]
    [DataRow("prepared", "before")]
    [DataRow("recovery", "before")]
    [DataRow("backup", "afterRead")]
    [DataRow("prepared", "afterRead")]
    [DataRow("recovery", "afterRead")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task OverwritePreparedAsyncWhenArtifactMetadataChangesRefusesBeforeTargetEffectAsync(string name, string timing)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteCleanupFixture calls = new();
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), calls.Io, WindowsTransferArtifactQueries.Native);
        calls.Attempt = attempt;
        _ = await attempt.PrepareAsync(CancellationToken.None);
        TransferArtifactRole role = name == "backup" ? TransferArtifactRole.OriginalBackup
            : name == "prepared" ? TransferArtifactRole.PreparedContent : TransferArtifactRole.RecoveryRecord;
        IdentifiedTransferArtifact original = WindowsOverwritePreparationFixture.Artifact(attempt, role);
        string artifactName = Path.GetFileName(original.Artifact.Path.CanonicalText);
        bool changedAtRead = false;
        int forbiddenReads = 0;

        if (timing == "before")
        {
            calls.Read = (handle, bytes, offset, token) => { forbiddenReads++; return RandomAccess.ReadAsync(handle, bytes, offset, token); };
            Change(files, artifactName);
        }
        else
        {
            SafeFileHandle recovery = calls.Handle(TransferArtifactRole.RecoveryRecord);
            calls.Read = async (handle, bytes, offset, token) =>
            {
                int count = await RandomAccess.ReadAsync(handle, bytes, offset, token);
                if (!changedAtRead && count == 0 && ReferenceEquals(handle, recovery))
                {
                    changedAtRead = true;
                    Change(files, artifactName);
                }
                return count;
            };
        }

        _ = await Assert.ThrowsExactlyAsync<IOException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));

        Assert.AreEqual(0, forbiddenReads);
        Assert.AreEqual(timing == "afterRead", changedAtRead);
        files.RequireUntouched(attempt, "source", "target");
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        RequireRetained(original, WindowsOverwritePreparationFixture.Artifact(attempt, role));
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
    }

    private static void RequireRetained(IdentifiedTransferArtifact original, IdentifiedTransferArtifact retained)
    {
        Assert.AreEqual(original.Artifact.ObjectIdentity, retained.Artifact.ObjectIdentity);
        Assert.AreEqual(original.Content, retained.Content);
        Assert.AreEqual(ArtifactLinkObservation.Retained, retained.Link);
    }

    private static void Change(WindowsOverwritePreparationFixture files, string artifactName)
    {
        files.Root.SetLastWriteTimeUtc(artifactName, new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc));
    }
}
