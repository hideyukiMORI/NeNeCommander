using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves unrepresentable recovery text refuses initial consent and cannot discard prepared-file receipts.</summary>
[TestClass]
public sealed class WindowsOverwriteRecoveryTextTests
{
    /// <summary>Native paths that cannot be encoded exactly never acquire initial overwrite capability.</summary>
    [TestMethod]
    [DataRow("source")]
    [DataRow("target")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public void InspectWhenNativePathContainsUnpairedSurrogateRefusesWithoutArtifacts(string side)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = CreateFiles(root, side);
        WindowsLocalPath target = Target(root, side);

        TransferOverwriteCapability capability = WindowsOverwriteEligibility.Inspect(source, target, new WindowsDriveBoundary());

        Assert.AreSame(TransferOverwriteUnavailableReason.Unconfirmed,
            Assert.IsInstanceOfType<TransferOverwriteCapability.UnavailableOverwrite>(capability).Reason);
        Assert.HasCount(1, Directory.GetFiles(target.Parent!.CanonicalText));
        Assert.AreEqual("desired", File.ReadAllText(source.Path.CanonicalText));
        Assert.AreEqual("original", File.ReadAllText(target.CanonicalText));
    }

    /// <summary>Defensive provider normalization returns the same created artifacts when recovery encoding fails.</summary>
    [TestMethod]
    [DataRow("source")]
    [DataRow("target")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CopyAsyncWhenRecoveryEncodingFailsRetainsPreparedObjectsAndUntouchedTarget(string side)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = CreateFiles(root, side);
        WindowsLocalPath target = Target(root, side);
        TransferOverwriteEvidence original;
        using (WindowsOverwritePairLease pair = WindowsOverwritePairLease.Open(source, target, new WindowsDriveBoundary()))
        {
            original = pair.Evidence;
        }
        WindowsOverwriteCopy copy = WindowsOverwriteCopyTests.Create(new(), WindowsTransferContentIo.Native, WindowsTransferArtifactRemoval.Native);

        ProviderStepOutcome outcome = await copy.CopyAsync(source, original, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Copy, outcome.Failure);
        _ = Assert.IsInstanceOfType<UntouchedTransferTarget>(outcome.Receipt.Target);
        Assert.HasCount(3, outcome.Receipt.Artifacts);
        Assert.IsTrue(outcome.Receipt.Artifacts.Cast<IdentifiedTransferArtifact>().All(artifact =>
            artifact.Link == ArtifactLinkObservation.Retained && artifact.Artifact.PathObservation == TransferPathObservation.LastKnownPath));
        Assert.HasCount(4, Directory.GetFiles(target.Parent!.CanonicalText));
        Assert.AreEqual("desired", File.ReadAllText(source.Path.CanonicalText));
        Assert.AreEqual("original", File.ReadAllText(target.CanonicalText));
    }

    private static FileEntrySnapshot CreateFiles(TestOwnedTemporaryRoot root, string side)
    {
        _ = root.CreateDirectory("bad-\uD800");
        _ = root.CreateDirectory("destination");
        _ = root.WriteFile(side == "source" ? "bad-\uD800/source" : "source", "desired");
        _ = root.WriteFile(side == "target" ? "bad-\uD800/source" : "destination/source", "original");
        return WindowsLocalOperationTestContext.LocalSnapshot(root.Resolve(side == "source" ? "bad-\uD800/source" : "source"));
    }

    private static WindowsLocalPath Target(TestOwnedTemporaryRoot root, string side)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(WindowsLocalOperationTestContext.ParsePath(
            root.Resolve(side == "target" ? "bad-\uD800/source" : "destination/source")));
    }
}
