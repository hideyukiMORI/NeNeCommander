using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves cleanup repeats original identity and content admission before any irreversible removal.</summary>
[TestClass]
public sealed class WindowsOverwriteCleanupAdmissionTests
{
    /// <summary>Proves changed original metadata, desired bytes, saved content or artifact aliases retain every preparation.</summary>
    [TestMethod]
    [DataRow("sourceMetadata")]
    [DataRow("targetBytes")]
    [DataRow("backupBytes")]
    [DataRow("preparedBytes")]
    [DataRow("recoveryBytes")]
    [DataRow("backupAlias")]
    [DataRow("preparedAlias")]
    [DataRow("recoveryAlias")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task CleanupAsyncWhenSavedInputsChangeRefusesBeforeFirstRemovalAsync(string change)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteCleanupFixture calls = new();
        int removals = 0;
        WindowsTransferArtifactRemoval removal = new(_ => { removals++; return 0; }, _ => throw new AssertFailedException("Removal was not admitted."));
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), calls.Io, WindowsTransferArtifactQueries.Native, removal);
        calls.Attempt = attempt;
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);
        ArgumentNullException.ThrowIfNull(change);
        Change(files, calls, change);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.CleanupAsync(CancellationToken.None));
        Assert.AreEqual(0, removals);
        WindowsOverwriteCleanupTests.RequirePartial(attempt, 0, ArtifactLinkObservation.Retained);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        _ = Assert.IsInstanceOfType<DesiredTargetContentVerified>(attempt.Receipt.Target);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.CleanupAsync(CancellationToken.None));
    }

    private static void Change(WindowsOverwritePreparationFixture files, WindowsOverwriteCleanupFixture calls, string change)
    {
        if (change == "sourceMetadata")
        {
            files.Root.SetLastWriteTimeUtc("source", new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            return;
        }
        if (change == "targetBytes")
        {
            RandomAccess.Write(files.Pair.Target.Handle, [42], 0);
            return;
        }
        TransferArtifactRole role = change.StartsWith("backup", StringComparison.Ordinal) ? TransferArtifactRole.OriginalBackup
            : change.StartsWith("prepared", StringComparison.Ordinal) ? TransferArtifactRole.PreparedContent : TransferArtifactRole.RecoveryRecord;
        if (change.EndsWith("Alias", StringComparison.Ordinal))
        {
            string path = WindowsOverwritePreparationFixture.Artifact(calls.Attempt ?? throw new AssertFailedException("Attempt missing."), role).Artifact.Path.CanonicalText;
            _ = files.Root.CreateHardLink("late-alias", Path.GetFileName(path));
        }
        else
        {
            RandomAccess.Write(calls.Handle(role), [42], 0);
        }
    }

    /// <summary>Proves failed reads, wrong bytes, short reads and unexpected EOF suffixes cannot authorize removal from historical digests alone.</summary>
    [TestMethod]
    [DataRow("backup", "throw")]
    [DataRow("backup", "mismatch")]
    [DataRow("prepared", "short")]
    [DataRow("prepared", "mismatch")]
    [DataRow("recovery", "mismatch")]
    [DataRow("recovery", "suffix")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CleanupAsyncWhenCurrentReadsFailRetainsOriginalContentEvidenceAsync(string roleName, string fault)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteCleanupFixture calls = new();
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), calls.Io, WindowsTransferArtifactQueries.Native);
        calls.Attempt = attempt;
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);
        TransferArtifactRole role = roleName == "backup" ? TransferArtifactRole.OriginalBackup : roleName == "prepared" ? TransferArtifactRole.PreparedContent : TransferArtifactRole.RecoveryRecord;
        ArtifactContentObservation before = WindowsOverwritePreparationFixture.Artifact(attempt, role).Content;
        SafeFileHandle selected = calls.Handle(role);
        calls.Read = async (handle, bytes, offset, token) =>
        {
            int count = await RandomAccess.ReadAsync(handle, bytes, offset, token);
            return ReferenceEquals(handle, selected) ? Inject(bytes, count, fault) : count;
        };

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.CleanupAsync(CancellationToken.None));
        Assert.AreEqual(before, WindowsOverwritePreparationFixture.Artifact(attempt, role).Content);
        WindowsOverwriteCleanupTests.RequirePartial(attempt, 0, ArtifactLinkObservation.Retained);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.CleanupAsync(CancellationToken.None));
    }

    private static int Inject(Memory<byte> bytes, int count, string fault)
    {
        if (fault == "throw")
        {
            throw new IOException("Injected held read failure.");
        }
        if (fault == "short")
        {
            return 0;
        }
        if (fault == "mismatch" && count > 0)
        {
            bytes.Span[0] ^= 1;
        }
        return fault == "suffix" && count == 0 ? 1 : count;
    }
}
