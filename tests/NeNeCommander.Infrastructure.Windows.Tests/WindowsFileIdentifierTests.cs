using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the closed Win32 file-identifier query boundary.</summary>
[TestClass]
public sealed class WindowsFileIdentifierTests
{
    private const uint FileAttribute = 0x00000080;
    private const uint DirectoryAttribute = 0x00000010;
    private const uint ReparsePointAttribute = 0x00000400;
    private const int NotSupported = unchecked((int)0x80070032);

    /// <summary>Proves the native query boundary rejects a missing path argument.</summary>
    [TestMethod]
    public void DescribeWhenPathIsNullThrowsArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowsFileIdentifier.Describe(null!));
    }

    /// <summary>Proves the owned-handle identity boundary rejects a missing handle.</summary>
    [TestMethod]
    public void DescribeHandleWhenHandleIsNullThrowsArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowsFileIdentifier.DescribeHandle(null!));
    }

    /// <summary>Proves one entry has a stable fixed-width volume and 128-bit identifier token.</summary>
    [TestMethod]
    public void DescribeWhenEntryExistsReturnsStableFixedWidthToken()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string path = root.WriteFile("entry.txt", "entry");

        string first = WindowsFileIdentifier.Describe(path);
        string again = WindowsFileIdentifier.Describe(path);

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
            () => WindowsFileIdentifier.Describe(root.Resolve("missing.txt")));

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

        string targetIdentity = WindowsFileIdentifier.Describe(target);
        string junctionIdentity = WindowsFileIdentifier.Describe(junction);

        Assert.AreNotEqual(targetIdentity, junctionIdentity);
    }

    /// <summary>Proves the ADR-0049 facts and token boundaries reject missing arguments.</summary>
    [TestMethod]
    public void WslIdentityBoundariesWhenArgumentIsNullRejectDefect()
    {
        WslHandleFacts facts = Facts(1, FileAttribute, 0, 0, 1);

        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowsFileIdentifier.ReadHandleFacts(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowsFileIdentifier.ReadWslFacts(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowsFileIdentifier.RequireWslFileSystem(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowsFileIdentifier.ComposeWslToken(null!, facts));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => WindowsFileIdentifier.ComposeWslToken("Ubuntu", null!));
    }

    /// <summary>Proves the unguarded facts reader reports the real facts of that one handle.</summary>
    [TestMethod]
    public void ReadHandleFactsWhenEntryExistsReportsTheFactsOfThatHandle()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string file = root.WriteFile("entry.txt", "content");
        string directory = root.CreateDirectory("entry");

        WslHandleFacts fileFacts = WindowsFileIdentifier.ReadHandleFacts(file);
        WslHandleFacts directoryFacts = WindowsFileIdentifier.ReadHandleFacts(directory);

        Assert.AreEqual(7, fileFacts.EndOfFile);
        Assert.AreEqual(1u, fileFacts.NumberOfLinks);
        Assert.AreEqual(0u, fileFacts.ReparseTag);
        Assert.AreEqual(0u, fileFacts.Attributes & DirectoryAttribute);
        Assert.AreNotEqual(0, fileFacts.Inode);
        Assert.AreNotEqual(0, fileFacts.LastWriteFileTimeUtc);
        Assert.AreNotEqual(0, fileFacts.ChangeFileTimeUtc);
        Assert.AreEqual(0, directoryFacts.EndOfFile);
        Assert.AreEqual(DirectoryAttribute, directoryFacts.Attributes & DirectoryAttribute);
        Assert.AreNotEqual(fileFacts.Inode, directoryFacts.Inode);
    }

    /// <summary>Proves an entry that cannot be opened fails closed before any facts are read.</summary>
    [TestMethod]
    public void ReadHandleFactsWhenEntryIsMissingThrowsIOException()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();

        IOException exception = Assert.ThrowsExactly<IOException>(
            () => WindowsFileIdentifier.ReadHandleFacts(root.Resolve("missing.txt")));

        Assert.AreEqual(unchecked((int)0x80070002), exception.HResult);
    }

    /// <summary>Proves the guarded reader refuses every file system that is not exactly 9P.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-020")]
    public void ReadWslFactsWhenFileSystemIsNotNinePFailsClosed()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string file = root.WriteFile("entry.txt", "content");

        IOException exception = Assert.ThrowsExactly<IOException>(
            () => WindowsFileIdentifier.ReadWslFacts(file));

        Assert.AreEqual(NotSupported, exception.HResult);
        WindowsFileIdentifier.RequireWslFileSystem("9P");
        foreach (string rejected in new[] { "NTFS", "9p", "9P ", " 9P", "9", string.Empty })
        {
            Assert.AreEqual(
                NotSupported,
                Assert.ThrowsExactly<IOException>(
                    () => WindowsFileIdentifier.RequireWslFileSystem(rejected)).HResult);
        }
    }

    /// <summary>Proves the wsl-v2 token has the exact ADR-0049 field order and separators.</summary>
    [TestMethod]
    public void ComposeWslTokenWhenFactsAreCompleteReturnsTheExactOrderedToken()
    {
        WslHandleFacts facts = new(12345, FileAttribute, 0, 7, 2, 130000000000000000, 130000000000000001);

        string token = WindowsFileIdentifier.ComposeWslToken("Ubuntu-22.04", facts);

        Assert.AreEqual(
            "wsl-v2|Ubuntu-22.04|12345|file|00000000|2|7|130000000000000000|130000000000000001",
            token);
        _ = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse(token));
    }

    /// <summary>Proves kind comes from the entry's own attributes and reparse tag together.</summary>
    [TestMethod]
    public void ComposeWslTokenWhenEntryIsLinkedDistinguishesKindFromAttributesAndTag()
    {
        WslHandleFacts link = Facts(1, FileAttribute | ReparsePointAttribute, 0xA000001D, 0, 1);
        WslHandleFacts directoryLink = Facts(1, DirectoryAttribute | ReparsePointAttribute, 0xA0000003, 0, 1);
        WslHandleFacts directory = Facts(1, DirectoryAttribute, 0, 0, 1);
        WslHandleFacts untaggedReparse = Facts(1, DirectoryAttribute | ReparsePointAttribute, 0, 0, 1);
        WslHandleFacts taggedFile = Facts(1, FileAttribute, 0xA000001D, 0, 1);

        Assert.AreEqual("wsl-v2|Ubuntu|1|link|A000001D|1|0|11|12", Token(link));
        Assert.AreEqual("wsl-v2|Ubuntu|1|link|A0000003|1|0|11|12", Token(directoryLink));
        Assert.AreEqual("wsl-v2|Ubuntu|1|directory|00000000|1|0|11|12", Token(directory));
        Assert.AreEqual("wsl-v2|Ubuntu|1|directory|00000000|1|0|11|12", Token(untaggedReparse));
        Assert.AreEqual("wsl-v2|Ubuntu|1|file|A000001D|1|0|11|12", Token(taggedFile));
    }

    /// <summary>Proves every identity field separates two otherwise identical entries.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-020")]
    public void ComposeWslTokenWhenOneFieldDiffersProducesADifferentToken()
    {
        WslHandleFacts facts = new(1, FileAttribute, 0, 7, 1, 11, 12);
        string token = Token(facts);

        Assert.AreNotEqual(token, Token(new WslHandleFacts(2, FileAttribute, 0, 7, 1, 11, 12)));
        Assert.AreNotEqual(token, Token(new WslHandleFacts(1, FileAttribute, 0, 8, 1, 11, 12)));
        Assert.AreNotEqual(token, Token(new WslHandleFacts(1, FileAttribute, 0, 7, 2, 11, 12)));
        Assert.AreNotEqual(token, Token(new WslHandleFacts(1, FileAttribute, 0, 7, 1, 13, 12)));
        Assert.AreNotEqual(token, Token(new WslHandleFacts(1, FileAttribute, 0, 7, 1, 11, 13)));
        Assert.AreNotEqual(token, WindowsFileIdentifier.ComposeWslToken("Debian", facts));
        Assert.AreEqual(token, Token(new WslHandleFacts(1, FileAttribute, 0, 7, 1, 11, 12)));
    }

    /// <summary>Proves a directory that reports a byte length is a provider anomaly and closes.</summary>
    [TestMethod]
    public void ComposeWslTokenWhenDirectoryReportsLengthFailsClosed()
    {
        WslHandleFacts directory = Facts(1, DirectoryAttribute, 0, 1, 1);
        WslHandleFacts directoryLink = Facts(1, DirectoryAttribute | ReparsePointAttribute, 0xA0000003, 1, 1);

        Assert.AreEqual(
            NotSupported,
            Assert.ThrowsExactly<IOException>(() => Token(directory)).HResult);
        Assert.AreEqual("wsl-v2|Ubuntu|1|link|A0000003|1|1|11|12", Token(directoryLink));
        Assert.AreEqual(
            "wsl-v2|Ubuntu|1|directory|00000000|1|0|11|12",
            Token(Facts(1, DirectoryAttribute, 0, 0, 1)));
    }

    private static WslHandleFacts Facts(
        long inode,
        uint attributes,
        uint reparseTag,
        long endOfFile,
        uint numberOfLinks)
    {
        return new WslHandleFacts(inode, attributes, reparseTag, endOfFile, numberOfLinks, 11, 12);
    }

    private static string Token(WslHandleFacts facts)
    {
        return WindowsFileIdentifier.ComposeWslToken("Ubuntu", facts);
    }
}
