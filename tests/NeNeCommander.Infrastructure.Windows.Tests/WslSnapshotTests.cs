using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves grouped provider observations retain their exact immutable projections.</summary>
[TestClass]
public sealed class WslSnapshotTests
{
    /// <summary>Every handle fact survives grouping, independent construction and record copying.</summary>
    [TestMethod]
    public void WslHandleFactsWhenGroupedPreservesEveryFieldAndValueCopy()
    {
        WslRewriteFacts rewrite = new(7, 2, 101, 103);
        WslHandleFacts facts = new(41, 0x80, 0, rewrite);
        Assert.AreEqual(41L, facts.Inode);
        Assert.AreEqual(0x80u, facts.Attributes);
        Assert.AreEqual(0u, facts.ReparseTag);
        Assert.AreEqual(7L, facts.EndOfFile);
        Assert.AreEqual(2u, facts.NumberOfLinks);
        Assert.AreEqual(101L, facts.LastWriteFileTimeUtc);
        Assert.AreEqual(103L, facts.ChangeFileTimeUtc);
        WslHandleFacts equivalent = new(41, 0x80, 0, new WslRewriteFacts(7, 2, 101, 103));
        Assert.AreEqual(facts, equivalent);
        Assert.AreEqual(facts.GetHashCode(), equivalent.GetHashCode());
        Assert.AreEqual(facts, facts with { });
        Assert.AreEqual(rewrite, rewrite with { });
        Assert.AreEqual("wsl-v2|Ubuntu|41|file|00000000|2|7|101|103", WindowsFileIdentifier.ComposeWslToken("Ubuntu", facts));
    }

    /// <summary>Provider names remain observed values and metadata remains immutable value content.</summary>
    [TestMethod]
    public void WslFileSystemEntryWhenGroupedPreservesProjectionAndValueCopy()
    {
        WslPath path = Assert.IsInstanceOfType<WslPath>(Assert.IsInstanceOfType<PathParseSuccess>(
            FileSystemPath.Parse(@"\\wsl.localhost\Ubuntu\item")).Path);
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("observed")).Identity;
        WslEntryMetadata metadata = new(identity, DirectoryEntryKind.File, FileAttributes.ReadOnly);
        WslFileSystemEntry entry = new(path, "observed-item", metadata);
        Assert.AreSame(path, entry.Path);
        Assert.AreEqual("observed-item", entry.Name);
        Assert.AreSame(identity, entry.Identity);
        Assert.AreSame(DirectoryEntryKind.File, entry.Kind);
        Assert.AreEqual(FileAttributes.ReadOnly, entry.Attributes);
        WslFileSystemEntry equivalent = new(path, "observed-item", new WslEntryMetadata(identity, DirectoryEntryKind.File, FileAttributes.ReadOnly));
        Assert.AreEqual(entry, equivalent);
        Assert.AreEqual(entry.GetHashCode(), equivalent.GetHashCode());
        Assert.AreEqual(entry, entry with { });
        Assert.AreEqual(metadata, metadata with { });
    }

    /// <summary>The newly required grouped values fail at their named boundary when absent.</summary>
    [TestMethod]
    public void GroupedSnapshotsWhenRequiredObservationIsNullRejectDefect()
    {
        Assert.AreEqual("rewrite", Assert.ThrowsExactly<ArgumentNullException>(() => new WslHandleFacts(1, 0, 0, null!)).ParamName);
        WslPath path = Assert.IsInstanceOfType<WslPath>(Assert.IsInstanceOfType<PathParseSuccess>(
            FileSystemPath.Parse(@"\\wsl.localhost\Ubuntu\item")).Path);
        Assert.AreEqual("metadata", Assert.ThrowsExactly<ArgumentNullException>(() => new WslFileSystemEntry(path, "item", null!)).ParamName);
    }
}
