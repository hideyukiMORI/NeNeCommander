using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the destructive entrypoint repeats admission after preparation and cannot replay a rejected prepared attempt.</summary>
[TestClass]
public sealed class WindowsOverwriteTargetAdmissionTests
{
    /// <summary>Proves changed originals or preparation artifacts refuse before any target mutator, retaining external changes and all owned residue.</summary>
    [TestMethod]
    [DataRow("sourceMetadata")]
    [DataRow("targetMetadata")]
    [DataRow("targetBytes")]
    [DataRow("preparedBytes")]
    [DataRow("recoveryBytes")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task OverwritePreparedAsyncWhenInputsChangeAfterPreparationRefusesBeforeTargetMutationAsync(string change)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteAttempt? observed = null;
        SafeFileHandle? artifact = null;
        int targetCalls = 0;
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync, (handle, bytes, offset, token) =>
        {
            if (WindowsOverwritePreparationFixture.IsArtifact(handle, observed,
                change == "preparedBytes" ? TransferArtifactRole.PreparedContent : TransferArtifactRole.RecoveryRecord))
            {
                artifact = handle;
            }
            targetCalls += ReferenceEquals(handle, fixture.Pair.Target.Handle) ? 1 : 0;
            return RandomAccess.WriteAsync(handle, bytes, offset, token);
        }, (handle, length) =>
        {
            targetCalls += ReferenceEquals(handle, fixture.Pair.Target.Handle) ? 1 : 0;
            RandomAccess.SetLength(handle, length);
        }, RandomAccess.FlushToDisk);
        using WindowsOverwriteAttempt attempt = fixture.Attempt(new DeterministicIdentifierSource(), io, WindowsTransferArtifactQueries.Native);
        observed = attempt;
        WindowsTransferRecoveryMapping mapping = await attempt.PrepareAsync(CancellationToken.None);
        ChangeAfterPreparation(fixture, artifact ?? throw new AssertFailedException("The relevant owned artifact was not observed."), change);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
        Assert.AreEqual(0, targetCalls);
        _ = Assert.IsInstanceOfType<UntouchedTransferTarget>(attempt.Receipt.Target);
        Assert.HasCount(0, attempt.Receipt.Effects);
        Assert.HasCount(3, attempt.Receipt.Artifacts);
        Assert.AreEqual("source", fixture.Read(fixture.Pair.Source.Path));
        Assert.AreEqual(change == "targetBytes" ? "*arget" : "target", fixture.Read(fixture.Pair.Target.Path));
        Assert.AreEqual("target", fixture.Read(mapping.Names.OriginalBackup));
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.OverwritePreparedAsync(CancellationToken.None));
    }

    private static void ChangeAfterPreparation(WindowsOverwritePreparationFixture fixture, SafeFileHandle artifact, string change)
    {
        switch (change)
        {
            case "sourceMetadata":
                fixture.Root.SetLastWriteTimeUtc("source", new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc));
                break;
            case "targetMetadata":
                fixture.Root.SetLastWriteTimeUtc("target", new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc));
                break;
            case "targetBytes":
                RandomAccess.Write(fixture.Pair.Target.Handle, [42], 0);
                break;
            case "preparedBytes":
            case "recoveryBytes":
                RandomAccess.Write(artifact, [42], 0);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(change));
        }
    }
}
