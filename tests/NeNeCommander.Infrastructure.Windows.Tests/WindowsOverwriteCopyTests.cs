using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Exercises complete native attempts and retained receipts through the provider copy orchestration.</summary>
[TestClass]
public sealed class WindowsOverwriteCopyTests
{
    /// <summary>Success follows complete cleanup and leaves the source available for separate gateway verification.</summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("desired")]
    public async Task CopyAsyncWhenCleanupCompletesRetainsSourceAndRemovedArtifactFacts(string desired)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create(desired, "original-longer");
        FileEntrySnapshot source = Snapshot(fixture);
        TransferOverwriteEvidence original = fixture.Pair.Evidence;
        fixture.Pair.Dispose();
        DeterministicIdentifierSource identifiers = new();
        WindowsOverwriteCopy copy = Create(identifiers, WindowsTransferContentIo.Native, WindowsTransferArtifactRemoval.Native);

        ProviderStepOutcome outcome = await copy.CopyAsync(source, original, CancellationToken.None);

        Assert.AreEqual(ProviderStepCompletion.Succeeded, outcome.Completion);
        DesiredTargetContentVerified target = Assert.IsInstanceOfType<DesiredTargetContentVerified>(outcome.Receipt.Target);
        Assert.AreEqual(original.Target.Observation.Entry.ObjectIdentity, target.Target.Entry.ObjectIdentity);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, target.Target.PathObservation);
        Assert.HasCount(0, outcome.Receipt.Effects);
        Assert.HasCount(3, outcome.Receipt.Artifacts);
        Assert.IsTrue(outcome.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>().All(artifact => artifact.Link == ArtifactLinkObservation.Removed));
        Assert.AreEqual(desired, fixture.Read(source.Path));
        Assert.AreEqual(desired, fixture.Read(original.Target.Observation.Entry.Path));
        Assert.AreEqual(1, identifiers.IssuedCount);
    }

    /// <summary>A changed target cannot replace the original consent or allocate preparations.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task CopyAsyncWhenOriginalTargetChangedRefusesBeforeAllocatingArtifacts()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("desired", "original");
        FileEntrySnapshot source = Snapshot(fixture);
        TransferOverwriteEvidence original = fixture.Pair.Evidence;
        fixture.Pair.Dispose();
        File.WriteAllText(original.Target.Observation.Entry.Path.CanonicalText, "newer-content");
        DeterministicIdentifierSource identifiers = new();
        WindowsOverwriteCopy copy = Create(identifiers, WindowsTransferContentIo.Native, WindowsTransferArtifactRemoval.Native);

        ProviderStepOutcome outcome = await copy.CopyAsync(source, original, CancellationToken.None);

        Assert.IsNotNull(outcome.Completion.Failure);
        _ = Assert.IsInstanceOfType<UntouchedTransferTarget>(outcome.Receipt.Target);
        Assert.HasCount(0, outcome.Receipt.Artifacts);
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.AreEqual("newer-content", fixture.Read(original.Target.Observation.Entry.Path));
        Assert.AreEqual("desired", fixture.Read(source.Path));
    }

    /// <summary>Cancellation before admission does not open an attempt or generate candidate names.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenAlreadyCancelledReturnsUntouchedWithoutArtifacts()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("desired", "original");
        FileEntrySnapshot source = Snapshot(fixture);
        TransferOverwriteEvidence original = fixture.Pair.Evidence;
        fixture.Pair.Dispose();
        DeterministicIdentifierSource identifiers = new();
        WindowsOverwriteCopy copy = Create(identifiers, WindowsTransferContentIo.Native, WindowsTransferArtifactRemoval.Native);

        ProviderStepOutcome outcome = await copy.CopyAsync(source, original, new CancellationToken(true));

        Assert.AreEqual(ProviderStepCompletion.Cancelled, outcome.Completion);
        _ = Assert.IsInstanceOfType<UntouchedTransferTarget>(outcome.Receipt.Target);
        Assert.HasCount(0, outcome.Receipt.Artifacts);
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.AreEqual("original", fixture.Read(original.Target.Observation.Entry.Path));
    }

    /// <summary>A failed prepared write keeps the original target and every created artifact in the returned receipt.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CopyAsyncWhenPreparationWriteFailsRetainsArtifactsAndOriginalTarget()
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("desired", "original");
        FileEntrySnapshot source = Snapshot(fixture);
        TransferOverwriteEvidence original = fixture.Pair.Evidence;
        fixture.Pair.Dispose();
        WindowsTransferContentIo io = new(RandomAccess.ReadAsync,
            (_, _, _, _) => ValueTask.FromException(new IOException("Injected preparation write failure.")), RandomAccess.SetLength, RandomAccess.FlushToDisk);
        WindowsOverwriteCopy copy = Create(new(), io, WindowsTransferArtifactRemoval.Native);

        ProviderStepOutcome outcome = await copy.CopyAsync(source, original, CancellationToken.None);

        Assert.IsNotNull(outcome.Completion.Failure);
        _ = Assert.IsInstanceOfType<UntouchedTransferTarget>(outcome.Receipt.Target);
        AssertRetained(outcome, 0);
        Assert.AreEqual("original", fixture.Read(original.Target.Observation.Entry.Path));
        Assert.AreEqual("desired", fixture.Read(source.Path));
    }

    /// <summary>Cleanup failure after earlier removals cannot be reported as copy success or discard those removals.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CopyAsyncWhenCleanupFailsRetainsDesiredTargetAndPartialCleanup(int failedRemoval)
    {
        using WindowsOverwritePreparationFixture fixture = WindowsOverwritePreparationFixture.Create("desired", "original");
        FileEntrySnapshot source = Snapshot(fixture);
        TransferOverwriteEvidence original = fixture.Pair.Evidence;
        fixture.Pair.Dispose();
        int removed = 0;
        WindowsTransferArtifactRemoval removal = new(handle => ++removed == failedRemoval
            ? unchecked((int)0xC0000022) : WindowsTransferArtifactRemoval.Native.RequestDisposition(handle), WindowsTransferArtifactRemoval.Native.Close);
        WindowsOverwriteCopy copy = Create(new(), WindowsTransferContentIo.Native, removal);

        ProviderStepOutcome outcome = await copy.CopyAsync(source, original, CancellationToken.None);

        Assert.IsNotNull(outcome.Completion.Failure);
        DesiredTargetContentVerified target = Assert.IsInstanceOfType<DesiredTargetContentVerified>(outcome.Receipt.Target);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, target.Target.PathObservation);
        AssertRetained(outcome, failedRemoval - 1);
        Assert.HasCount(1, outcome.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>().Where(artifact => artifact.Link == ArtifactLinkObservation.Uncertain).ToArray());
        Assert.AreEqual("desired", fixture.Read(original.Target.Observation.Entry.Path));
        Assert.AreEqual("desired", fixture.Read(source.Path));
    }

    private static void AssertRetained(ProviderStepOutcome outcome, int removed)
    {
        Assert.HasCount(0, outcome.Receipt.Effects);
        Assert.HasCount(3, outcome.Receipt.Artifacts);
        Assert.HasCount(removed, outcome.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>().Where(artifact => artifact.Link == ArtifactLinkObservation.Removed).ToArray());
        Assert.IsTrue(outcome.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>().All(artifact => artifact.Artifact.PathObservation == TransferPathObservation.LastKnownPath));
    }

    internal static FileEntrySnapshot Snapshot(WindowsOverwritePreparationFixture fixture)
    {
        return FileEntrySnapshot.Create(fixture.Pair.Source.Path, fixture.Pair.Evidence.Source.Observation.Entry.Identity, DeletionCapability.NotRequested);
    }

    internal static WindowsOverwriteCopy Create(DeterministicIdentifierSource identifiers, WindowsTransferContentIo io, WindowsTransferArtifactRemoval removal)
    {
        return new WindowsOverwriteCopy(new WindowsOverwriteDependencies(identifiers, io, WindowsTransferArtifactQueries.Native, removal), new WindowsDriveBoundary());
    }
}
