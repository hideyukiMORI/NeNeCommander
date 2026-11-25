using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves actual late aliases, streams and incomplete target contents retain uncertainty instead of reporting verified success.</summary>
[TestClass]
public sealed class WindowsOverwriteTargetRaceTests
{
    /// <summary>Proves unsupported observations created after the first target write refuse verification while leaving all actual effects visible.</summary>
    [TestMethod]
    [DataRow("sourceStream")]
    [DataRow("targetStream")]
    [DataRow("targetAlias")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task OverwritePreparedAsyncWhenShapeChangesAfterWriteRetainsTargetAndSourceAsync(string change)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        bool changed = false;
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, async (handle, bytes, offset, token) =>
        {
            await RandomAccess.WriteAsync(handle, bytes, offset, token);
            if (!changed && ReferenceEquals(handle, fixture.Pair.Target.Handle))
            {
                ChangeShape(fixture, change);
                changed = true;
            }
        }, RandomAccess.SetLength, RandomAccess.FlushToDisk);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        WindowsTransferRecoveryMapping prepared = await attempt.PrepareAsync(CancellationToken.None);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
        Assert.IsTrue(changed);
        _ = Assert.IsInstanceOfType<TargetMayHaveChanged>(attempt.Receipt.Target);
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Target.Path));
        Assert.AreEqual("target", fixture.Read(prepared.Names.OriginalBackup));
        Assert.HasCount(0, attempt.Receipt.Effects);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        if (change == "targetAlias")
        {
            Assert.AreEqual("source", fixture.Read(WindowsTransferRecoveryTestFixture.Path(fixture.Root.Resolve("alias"))));
        }
        attempt.Dispose();
        Assert.IsTrue(File.Exists(prepared.Names.Recovery.CanonicalText));
        Assert.IsTrue(File.Exists(prepared.Names.OriginalBackup.CanonicalText));
        Assert.IsTrue(File.Exists(prepared.Names.PreparedContent.CanonicalText));
    }

    /// <summary>Proves same-length wrong bytes and a missing truncate never become desired-content evidence.</summary>
    [TestMethod]
    [DataRow("wrongBytes")]
    [DataRow("noTruncate")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task OverwritePreparedAsyncWhenNativeCallsLeaveWrongContentsRefusesVerificationAsync(string defect)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target-longer");
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, (handle, bytes, offset, token) =>
        {
            return ReferenceEquals(handle, fixture.Pair.Target.Handle) && defect == "wrongBytes"
                ? RandomAccess.WriteAsync(handle, new byte[bytes.Length], offset, token)
                : RandomAccess.WriteAsync(handle, bytes, offset, token);
        }, (handle, length) =>
        {
            if (defect != "noTruncate" || !ReferenceEquals(handle, fixture.Pair.Target.Handle))
            {
                RandomAccess.SetLength(handle, length);
            }
        }, RandomAccess.FlushToDisk);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        _ = await attempt.PrepareAsync(CancellationToken.None);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
        _ = Assert.IsInstanceOfType<TargetMayHaveChanged>(attempt.Receipt.Target);
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        Assert.HasCount(0, attempt.Receipt.Effects);
    }

    private static void ChangeShape(WindowsOverwritePreparationFixture fixture, string change)
    {
        _ = change switch
        {
            "sourceStream" => fixture.Root.WriteFile("source:Zone.Identifier", string.Empty),
            "targetStream" => fixture.Root.WriteFile("target:Zone.Identifier", string.Empty),
            "targetAlias" => fixture.Root.CreateHardLink("alias", "target"),
            _ => throw new ArgumentOutOfRangeException(nameof(change)),
        };
    }
}
