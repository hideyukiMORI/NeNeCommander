using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves identity serialization preserves observations without confusing them with overwrite eligibility.</summary>
[TestClass]
public sealed class WindowsTransferIdentityObservationTests
{
    /// <summary>Directory identity excludes a provider's non-content length and therefore does not turn it into a file-size baseline.</summary>
    [TestMethod]
    public void CaptureHeldWhenDirectoryReportsNonzeroLengthRetainsDirectoryIdentity()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsLocalHandleFacts original = WindowsFileIdentifier.ReadLocalFacts(files.Pair.Source.ParentHandle);
        Assert.AreEqual((byte)1, original.Standard.Directory);
        WindowsLocalHandleFacts changed = new(original.ObjectIdentity, original.Basic,
            new WindowsLocalStandardFacts(123, original.Standard.NumberOfLinks, original.Standard.DeletePending, 1), original.FileSystemName);
        VerifiedEntrySnapshot expected = WindowsLocalEntryIdentity.CaptureHeld(files.Pair.Source.ParentPath, original);

        VerifiedEntrySnapshot observed = WindowsLocalEntryIdentity.CaptureHeld(files.Pair.Source.ParentPath, changed);

        Assert.AreEqual(expected, observed);
    }

    /// <summary>A real Boolean delete-pending fact can be described, while the separate overwrite shape admission still refuses it.</summary>
    [TestMethod]
    public void CaptureHeldWhenFileIsDeletePendingDescribesFactsWithoutAdmittingOverwrite()
    {
        using WindowsOverwritePreparationFixture files = WindowsOverwritePreparationFixture.Create("source", "target");
        WindowsLocalHandleFacts original = WindowsFileIdentifier.ReadLocalFacts(files.Pair.Source.Handle);
        WindowsLocalHandleFacts pending = new(original.ObjectIdentity, original.Basic,
            new WindowsLocalStandardFacts(original.Standard.EndOfFile, original.Standard.NumberOfLinks, 1, 0), original.FileSystemName);
        VerifiedEntrySnapshot expected = WindowsLocalEntryIdentity.CaptureHeld(files.Pair.Source.Path, original);

        VerifiedEntrySnapshot observed = WindowsLocalEntryIdentity.CaptureHeld(files.Pair.Source.Path, pending);

        Assert.AreEqual(expected, observed);
        WindowsOverwriteRefusalException refusal = Assert.ThrowsExactly<WindowsOverwriteRefusalException>(() => WindowsOverwriteFileAdmission.RequireShape(pending));
        Assert.AreSame(TransferOverwriteUnavailableReason.Unconfirmed, refusal.Reason);
    }
}
