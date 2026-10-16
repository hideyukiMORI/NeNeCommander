using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves bounded content comparison across buffer boundaries and empty streams.</summary>
[TestClass]
public sealed class VerifiedFileContentTests
{
    /// <summary>Proves a difference after two complete buffers is observed even when lengths agree.</summary>
    [TestMethod]
    public void CompareWhenEqualLengthTailChangesRejectsAfterMultipleBuffers()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string content = new string('x', 131072) + "tail";
        string source = root.WriteFile("source", content);
        string target = root.WriteFile("target", content);
        Assert.AreEqual(VerifiedFileContent.Read(source), VerifiedFileContent.Compare(source, target));
        _ = root.WriteFile("target", content[..^1] + "X");

        _ = Assert.ThrowsExactly<IOException>(() => VerifiedFileContent.Compare(source, target));
    }

    /// <summary>Proves zero bytes have the known SHA-256 evidence and unequal stream lengths are rejected.</summary>
    [TestMethod]
    public void CompareWhenStreamsAreEmptyRetainsExactEmptyDigestAndRejectsAddedByte()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string source = root.CreateFile("source");
        string target = root.CreateFile("target");

        Assert.AreEqual("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855",
            VerifiedFileContent.Compare(source, target).Value);
        _ = root.WriteFile("target", "x");
        _ = Assert.ThrowsExactly<IOException>(() => VerifiedFileContent.Compare(source, target));
    }
}
