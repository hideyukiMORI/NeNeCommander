using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves current native stream/shape and foreign-name boundaries at cleanup, including metadata lookup failure.</summary>
[TestClass]
public sealed class WindowsOverwriteCleanupBoundaryTests
{
    /// <summary>Proves new named streams, forbidden attributes and aliases are rejected before any owned link is removed.</summary>
    [TestMethod]
    [DataRow("source", "stream")]
    [DataRow("target", "stream")]
    [DataRow("backup", "stream")]
    [DataRow("prepared", "stream")]
    [DataRow("recovery", "stream")]
    [DataRow("backup", "hidden")]
    [DataRow("target", "hidden")]
    [DataRow("source", "alias")]
    [DataRow("target", "alias")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task CleanupAsyncWhenNativeAdmissionChangesRefusesBeforeEffectsAsync(string owner, string change)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native);
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);
        string name = Name(attempt, owner);
        Change(files, name, change);

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.CleanupAsync(CancellationToken.None));
        WindowsOverwriteCleanupTests.RequirePartial(attempt, 0, ArtifactLinkObservation.Retained);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.CleanupAsync(CancellationToken.None));
    }

    private static string Name(WindowsOverwriteAttempt attempt, string owner)
    {
        if (owner is "source" or "target")
        {
            return owner;
        }
        TransferArtifactRole role = owner == "backup" ? TransferArtifactRole.OriginalBackup : owner == "prepared" ? TransferArtifactRole.PreparedContent : TransferArtifactRole.RecoveryRecord;
        return Path.GetFileName(WindowsOverwritePreparationFixture.Artifact(attempt, role).Artifact.Path.CanonicalText);
    }

    private static void Change(WindowsOverwritePreparationFixture files, string name, string change)
    {
        if (change == "stream")
        {
            using FileStream stream = new(files.Root.Resolve(name + ":Zone.Identifier"), FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            Assert.AreEqual(0L, stream.Length);
        }
        else if (change == "hidden")
        {
            File.SetAttributes(files.Root.Resolve(name), FileAttributes.Hidden);
        }
        else
        {
            _ = files.Root.CreateHardLink("late-alias", name);
        }
    }

    /// <summary>Proves fallible current native lookups cannot be replaced by receipt identity or a candidate token.</summary>
    [TestMethod]
    [DataRow("identity")]
    [DataRow("facts")]
    [DataRow("path")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task CleanupAsyncWhenNativeArtifactQueryFailsRetainsAllLinksAsync(string phase)
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        bool armed = false;
        WindowsTransferArtifactQueries queries = new(
            handle => armed && phase == "identity" ? throw new IOException("Injected identity query failure.") : WindowsTransferArtifactQueries.Native.ReadIdentity(handle),
            handle => armed && phase == "facts" ? throw new IOException("Injected facts query failure.") : WindowsTransferArtifactQueries.Native.ReadFacts(handle),
            (handle, path) =>
            {
                if (armed && phase == "path")
                {
                    throw new IOException("Injected path admission failure.");
                }
                WindowsTransferArtifactQueries.Native.RequirePath(handle, path);
            });
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, queries);
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);
        armed = true;

        _ = await Assert.ThrowsAsync<IOException>(() => attempt.CleanupAsync(CancellationToken.None));
        WindowsOverwriteCleanupTests.RequirePartial(attempt, 0, ArtifactLinkObservation.Retained);
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => attempt.CleanupAsync(CancellationToken.None));
    }

    /// <summary>Proves a same-name foreign occupant created after acknowledged removal is never reopened or removed by later cleanup.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task CleanupAsyncWhenForeignNamesAppearAfterRemovalPreservesThemAsync()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsOverwriteAttempt? observed = null;
        WindowsTransferArtifactRemoval removal = new(WindowsTransferArtifactRemoval.Native.RequestDisposition, handle =>
        {
            IdentifiedTransferArtifact pending = observed?.Receipt.Artifacts.OfType<IdentifiedTransferArtifact>().Single(artifact => artifact.Link == ArtifactLinkObservation.Uncertain)
                ?? throw new AssertFailedException("No pending canonical removal receipt.");
            bool closed = WindowsTransferArtifactRemoval.Native.Close(handle);
            _ = files.Root.WriteFile(Path.GetFileName(pending.Artifact.Path.CanonicalText), "foreign");
            return closed;
        });
        using WindowsOverwriteAttempt attempt = files.Attempt(new DeterministicIdentifierSource(), WindowsTransferContentIo.Native, WindowsTransferArtifactQueries.Native, removal);
        observed = attempt;
        _ = await WindowsOverwriteCleanupFixture.CompleteAsync(attempt);

        await attempt.CleanupAsync(CancellationToken.None);
        WindowsOverwriteCleanupTests.RequirePartial(attempt, 3, ArtifactLinkObservation.Removed);
        foreach (IdentifiedTransferArtifact artifact in attempt.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>())
        {
            Assert.AreEqual("foreign", files.Read(artifact.Artifact.Path));
            using SafeFileHandle foreign = File.OpenHandle(artifact.Artifact.Path.CanonicalText);
            Assert.AreNotEqual(artifact.Artifact.ObjectIdentity, WindowsTransferArtifactQueries.Native.ReadIdentity(foreign));
        }
        Assert.AreEqual("source", files.Read(files.Pair.Source.Path));
    }
}
