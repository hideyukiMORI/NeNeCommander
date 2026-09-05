using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the closed Win32 file-identifier query boundary.</summary>
[TestClass]
public sealed class WindowsLocalFileIdentifierTests
{
    /// <summary>Proves the native query boundary rejects a missing path argument.</summary>
    [TestMethod]
    public void DescribeWhenPathIsNullThrowsArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowsLocalFileIdentifier.Describe(null!));
    }

    /// <summary>Proves one entry has a stable fixed-width volume and 128-bit identifier token.</summary>
    [TestMethod]
    public void DescribeWhenEntryExistsReturnsStableFixedWidthToken()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string path = root.WriteFile("entry.txt", "entry");

        string first = WindowsLocalFileIdentifier.Describe(path);
        string again = WindowsLocalFileIdentifier.Describe(path);

        Assert.AreEqual(first, again);
        Assert.HasCount(48, first);
        Assert.IsTrue(first.All(char.IsAsciiHexDigit));
    }

    /// <summary>Proves an entry that cannot be opened fails closed at the native query boundary.</summary>
    [TestMethod]
    public void DescribeWhenEntryIsMissingThrowsIOException()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();

        IOException exception = Assert.ThrowsExactly<IOException>(
            () => WindowsLocalFileIdentifier.Describe(root.Resolve("missing.txt")));

        Assert.AreEqual(unchecked((int)0x80070002), exception.HResult);
    }

    /// <summary>Proves a junction is identified as its own directory entry, not as its target.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void DescribeWhenEntryIsJunctionDoesNotFollowItsTarget()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string target = root.CreateDirectory("target");
        string junction = root.CreateJunction("junction", "target");

        string targetIdentity = WindowsLocalFileIdentifier.Describe(target);
        string junctionIdentity = WindowsLocalFileIdentifier.Describe(junction);

        Assert.AreNotEqual(targetIdentity, junctionIdentity);
    }
}
