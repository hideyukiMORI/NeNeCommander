using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves LiveWslTestRootTests evidence for LiveWslRootIdentityTests.</summary>
[TestClass]
public sealed class LiveWslRootIdentityTests
{
    /// <summary>
    /// Proves why the deterministic seam presents the 9P directory length. Once an NTFS directory
    /// index stops fitting in its MFT record the handle reports a nonzero length, which the
    /// ADR-0049 token would reject as a provider anomaly, while a 9P directory always reports zero.
    /// </summary>
    [TestMethod]
    public void ReadProviderShapedFactsWhenNtfsDirectoryIndexIsNonResidentReportsTheWslLength()
    {
        using TestOwnedTemporaryRoot host = TestOwnedTemporaryRoot.Create();
        string directory = host.CreateDirectory("spilled-index");
        for (int index = 0; index < 256; index++)
        {
            _ = host.CreateFile(
                "spilled-index\\entry-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".txt");
        }

        WslHandleFacts actual = WindowsFileIdentifier.ReadHandleFacts(directory);
        WslHandleFacts shaped = LiveWslRootTestContext.ReadProviderShapedFacts(directory);

        Assert.AreNotEqual(0, actual.EndOfFile);
        Assert.AreEqual(0, shaped.EndOfFile);
        Assert.AreEqual(actual.Inode, shaped.Inode);
        Assert.AreEqual(actual.NumberOfLinks, shaped.NumberOfLinks);
        Assert.AreEqual(actual.ChangeFileTimeUtc, shaped.ChangeFileTimeUtc);
    }

    /// <summary>Proves an owned fixture keeps one identity across reads and declared-tree evidence.</summary>
    [TestMethod]
    public void ReadIdentityWhenOwnedFixtureIsUnchangedRepeatsTheSameToken()
    {
        using TestOwnedTemporaryRoot host = LiveWslRootTestContext.CreateHost();
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(LiveWslRootTestContext.Open(host)).Root;
        _ = root.WriteFile("payload.bin", [7, 7, 7]);
        _ = root.CreateFileSymbolicLink("payload-link.bin", "payload.bin");

        string first = root.ReadIdentity("payload.bin");
        CollectionAssert.AreEqual(new byte[] { 7, 7, 7 }, root.ReadFile("payload.bin"));
        string second = root.ReadIdentity("payload.bin");
        string link = root.ReadIdentity("payload-link.bin");

        Assert.AreEqual(first, second);
        Assert.AreNotEqual(first, link);
        _ = Assert.IsInstanceOfType<LiveWslRootCleanupCompleted>(root.Cleanup());
    }
}
