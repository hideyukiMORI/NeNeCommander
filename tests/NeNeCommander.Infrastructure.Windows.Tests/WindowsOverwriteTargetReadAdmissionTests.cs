using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves final content verification refuses changed source eligibility or target length before consuming more bytes.</summary>
[TestClass]
public sealed class WindowsOverwriteTargetReadAdmissionTests
{
    /// <summary>A completed target write does not permit comparison reads after a source-shape or target-length mismatch.</summary>
    [TestMethod]
    [DataRow("sourceHidden")]
    [DataRow("targetShort")]
    [DataRow("targetLong")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task OverwritePreparedAsyncWhenPostWriteAdmissionDiffersRefusesBeforeComparisonAsync(string defect)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        TargetReadProbe probe = new(files, defect);
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), probe.Io, WindowsTransferArtifactQueries.Native);
        WindowsTransferRecoveryMapping mapping = await attempt.PrepareAsync(CancellationToken.None);
        FileAttributes attributes = File.GetAttributes(files.Pair.Source.Path.CanonicalText);
        try
        {
            _ = await Assert.ThrowsAsync<IOException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));

            Assert.IsTrue(probe.Written);
            Assert.AreEqual(0, probe.ReadsAfterWrite);
            _ = Assert.IsInstanceOfType<TargetMayHaveChanged>(attempt.Receipt.Target);
            Assert.HasCount(0, attempt.Receipt.Effects);
            Assert.HasCount(3, attempt.Receipt.Artifacts);
            Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
            Assert.AreEqual("target", files.Read(mapping.Names.OriginalBackup));
            Assert.AreEqual(defect == "targetShort" ? 5L : defect == "targetLong" ? 7L : 6L, RandomAccess.GetLength(files.Pair.Target.Handle));
            _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.AppendCompletionAsync(CancellationToken.None));
        }
        finally
        {
            File.SetAttributes(files.Pair.Source.Path.CanonicalText, attributes);
        }
    }

    private sealed class TargetReadProbe
    {
        internal TargetReadProbe(WindowsOverwritePreparationFixture files, string defect)
        {
            Io = new((handle, bytes, offset, token) =>
            {
                ReadsAfterWrite += Written ? 1 : 0;
                return RandomAccess.ReadAsync(handle, bytes, offset, token);
            }, RandomAccess.WriteAsync, (handle, length) =>
            {
                RandomAccess.SetLength(handle, length);
                if (ReferenceEquals(handle, files.Pair.Target.Handle))
                {
                    Written = true;
                    ChangeObservation(files, handle, length, defect);
                }
            }, RandomAccess.FlushToDisk);
        }

        private static void ChangeObservation(WindowsOverwritePreparationFixture files, SafeFileHandle handle, long length, string defect)
        {
            if (defect == "sourceHidden")
            {
                File.SetAttributes(files.Pair.Source.Path.CanonicalText, FileAttributes.Hidden);
            }
            else
            {
                RandomAccess.SetLength(handle, length + (defect == "targetShort" ? -1 : 1));
            }
        }

        internal WindowsTransferContentIo Io { get; }
        internal int ReadsAfterWrite { get; private set; }
        internal bool Written { get; private set; }
    }
}
