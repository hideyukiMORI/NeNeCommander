using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the real operation token cannot turn its source into an owned preparation candidate.</summary>
[TestClass]
public sealed class WindowsOverwriteSourceCandidateTests
{
    /// <summary>A source matching any actual candidate is refused before even the first artifact is attempted.</summary>
    [TestMethod]
    [DataRow("recovery")]
    [DataRow("backup")]
    [DataRow("prepared")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task PrepareAsyncWhenSourceIsAnActualArtifactCandidateRefusesWithoutArtifactsAsync(string role)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath target = WindowsTransferRecoveryTestFixture.Path(root.WriteFile("target", "target"));
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create(target, Guid.ParseExact(new string('0', 31) + "1", "N"));
        WindowsLocalPath sourcePath = role == "recovery" ? names.Recovery : role == "backup" ? names.OriginalBackup : names.PreparedContent;
        FileEntrySnapshot source = WindowsLocalOperationTestContext.LocalSnapshot(root.WriteFile(Path.GetFileName(sourcePath.CanonicalText), "source"));
        using WindowsOverwritePairLease pair = WindowsOverwritePairLease.Open(source, target, new WindowsDriveBoundary());
        DeterministicIdentifierSource identifiers = new();
        using WindowsOverwriteAttempt attempt = WindowsOverwriteAttempt.Create(pair, pair.Evidence, WindowsOverwriteDependencies.Native(identifiers));

        _ = await Assert.ThrowsExactlyAsync<IOException>(() => attempt.PrepareAsync(CancellationToken.None));

        Assert.AreEqual(1, identifiers.IssuedCount);
        Assert.HasCount(0, attempt.Receipt.Artifacts);
        _ = Assert.IsInstanceOfType<UntouchedTransferTarget>(attempt.Receipt.Target);
        Assert.HasCount(2, Directory.GetFiles(root.Path.CanonicalText));
        Assert.AreEqual("source", Read(sourcePath));
        Assert.AreEqual("target", Read(target));
        pair.Revalidate(pair.Evidence);
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.PrepareAsync(CancellationToken.None));
    }

    private static string Read(WindowsLocalPath path)
    {
        using FileStream stream = new(path.CanonicalText, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
