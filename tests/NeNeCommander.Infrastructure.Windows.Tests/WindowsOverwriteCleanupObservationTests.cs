using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves cleanup rejects metadata-only drift and repeats target and artifact admission after its content reads.</summary>
[TestClass]
public sealed class WindowsOverwriteCleanupObservationTests
{
    /// <summary>Unchanged bytes do not substitute for the original complete artifact identity, including a change during the final content read.</summary>
    [TestMethod]
    [DataRow("backup", "before")]
    [DataRow("prepared", "before")]
    [DataRow("recovery", "before")]
    [DataRow("backup", "afterRead")]
    [DataRow("prepared", "afterRead")]
    [DataRow("recovery", "afterRead")]
    [DataRow("target", "afterRead")]
    [DataRow("source", "afterRead")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task CleanupAsyncWhenObservationChangesBeforeRemovalRetainsEveryLinkAsync(string changed, string timing)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteCleanupFixture calls = new();
        int removals = 0;
        WindowsTransferArtifactRemoval removal = new(_ => { removals++; return unchecked((int)0xC0000001); }, _ => false);
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), calls.Io, WindowsTransferArtifactQueries.Native, removal);
        calls.Attempt = attempt;
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);
        bool changedAtRead = false;
        if (timing == "before")
        {
            Change(files, calls, changed);
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
                    Change(files, calls, changed);
                }
                return count;
            };
        }

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.CleanupAsync(CancellationToken.None));

        Assert.AreEqual(timing == "afterRead", changedAtRead);
        Assert.AreEqual(0, removals);
        WindowsOverwriteCleanupTests.RequirePartial(attempt, 0, ArtifactLinkObservation.Retained);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        Assert.AreEqual(changed == "target" ? "Xource" : "source", files.Read(files.Pair.Target.Path));
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.CleanupAsync(CancellationToken.None));
    }

    /// <summary>Extra bytes after an admitted backup or prepared length cannot be hidden by an unchanged prefix digest.</summary>
    [TestMethod]
    [DataRow("backup")]
    [DataRow("prepared")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CleanupAsyncWhenArtifactEofReportsExtraBytesRefusesDeletionAsync(string name)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteCleanupFixture calls = new();
        int removals = 0;
        WindowsTransferArtifactRemoval removal = new(_ => { removals++; return unchecked((int)0xC0000001); }, _ => false);
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), calls.Io, WindowsTransferArtifactQueries.Native, removal);
        calls.Attempt = attempt;
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);
        TransferArtifactRole role = name == "backup" ? TransferArtifactRole.OriginalBackup : TransferArtifactRole.PreparedContent;
        ArtifactContentObservation previous = WindowsOverwritePreparationFixture.Artifact(attempt, role).Content;
        SafeFileHandle selected = calls.Handle(role);
        int extraReads = 0;
        calls.Read = async (handle, bytes, offset, token) =>
        {
            int count = await RandomAccess.ReadAsync(handle, bytes, offset, token);
            if (count == 0 && ReferenceEquals(handle, selected))
            {
                extraReads++;
                bytes.Span[0] = (byte)'x';
                return 1;
            }
            return count;
        };

        _ = await Assert.ThrowsExactlyAsync<IOException>(() => attempt.CleanupAsync(CancellationToken.None));

        Assert.AreEqual(1, extraReads);
        Assert.AreEqual(0, removals);
        Assert.AreEqual(previous, WindowsOverwritePreparationFixture.Artifact(attempt, role).Content);
        WindowsOverwriteCleanupTests.RequirePartial(attempt, 0, ArtifactLinkObservation.Retained);
    }

    private static void Change(WindowsOverwritePreparationFixture files, WindowsOverwriteCleanupFixture calls, string name)
    {
        if (name == "target")
        {
            RandomAccess.Write(files.Pair.Target.Handle, "X"u8, 0);
            return;
        }
        string changed = name;
        if (name != "source")
        {
            TransferArtifactRole role = name == "backup" ? TransferArtifactRole.OriginalBackup
                : name == "prepared" ? TransferArtifactRole.PreparedContent : TransferArtifactRole.RecoveryRecord;
            changed = Path.GetFileName(WindowsOverwritePreparationFixture.Artifact(
                calls.Attempt ?? throw new AssertFailedException("Attempt missing."), role).Artifact.Path.CanonicalText);
        }
        files.Root.SetLastWriteTimeUtc(changed, new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc));
    }
}
