using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the Windows-side WSL filesystem mechanism against a test-owned root.</summary>
[TestClass]
public sealed class WindowsWslFileSystemTests
{
    /// <summary>Proves file and directory mutations use the injected namespace mapping exactly.</summary>
    [TestMethod]
    public void MutationsWhenEntriesAreValidCreateRenameAndDeleteTheirExactTargets()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile("file.txt", "content");
        WindowsWslFileSystem fileSystem = FileSystem(root);
        WslPath file = Wsl("/owned/file.txt");
        WslPath renamedFile = Wsl("/owned/renamed.txt");
        WslPath copiedFile = Wsl("/owned/copied.txt");
        WslPath created = Wsl("/owned/created");
        WslPath renamedDirectory = Wsl("/owned/renamed-directory");

        WslFileSystemEntry fileEntry = RequireEntry(fileSystem.Find(file));
        Assert.AreSame(DirectoryEntryKind.File, fileEntry.Kind);
        StringAssert.StartsWith(fileEntry.Identity.Value, "wsl-v1|");
        StringAssert.Contains(fileEntry.Identity.Value, "|file|7|");
        Assert.IsTrue(fileSystem.TargetExists(file));
        Assert.IsFalse(fileSystem.ContainsReparsePoint(fileEntry));
        fileSystem.Copy(fileEntry, copiedFile);
        Assert.IsFalse(fileSystem.ContainsReparsePoint(copiedFile));
        Assert.IsTrue(fileSystem.Matches(fileEntry, copiedFile));
        File.AppendAllText(root.Resolve("copied.txt"), "changed");
        Assert.IsFalse(fileSystem.Matches(fileEntry, copiedFile));
        fileSystem.Delete(RequireEntry(fileSystem.Find(copiedFile)));
        fileSystem.Rename(fileEntry, renamedFile);
        Assert.IsFalse(File.Exists(root.Resolve("file.txt")));
        Assert.IsTrue(File.Exists(root.Resolve("renamed.txt")));
        WslFileSystemEntry renamedFileEntry = RequireEntry(fileSystem.Find(renamedFile));
        fileSystem.Delete(renamedFileEntry);
        Assert.IsFalse(File.Exists(root.Resolve("renamed.txt")));

        fileSystem.CreateDirectory(created);
        WslFileSystemEntry directoryEntry = RequireEntry(fileSystem.Find(created));
        Assert.IsFalse(fileSystem.ContainsReparsePoint(created));
        _ = root.CreateDirectory("link-target");
        _ = root.CreateJunction("link", "link-target");
        Assert.IsTrue(fileSystem.ContainsReparsePoint(Wsl("/owned/link")));
        Assert.AreSame(DirectoryEntryKind.Directory, directoryEntry.Kind);
        StringAssert.Contains(directoryEntry.Identity.Value, "|directory|0|");
        _ = root.WriteFile("created\\child.txt", "child");
        WslPath copiedDirectory = Wsl("/owned/copied-directory");
        fileSystem.Copy(directoryEntry, copiedDirectory);
        Assert.IsTrue(fileSystem.Matches(directoryEntry, copiedDirectory));
        fileSystem.Delete(RequireEntry(fileSystem.Find(copiedDirectory)));
        fileSystem.Rename(directoryEntry, renamedDirectory);
        Assert.IsFalse(Directory.Exists(root.Resolve("created")));
        Assert.IsTrue(Directory.Exists(root.Resolve("renamed-directory")));
        fileSystem.Delete(RequireEntry(fileSystem.Find(renamedDirectory)));
        Assert.IsFalse(Directory.Exists(root.Resolve("renamed-directory")));
    }

    /// <summary>Proves identities are stable for one entry and change after replacement.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void FindWhenEntryIsReplacedChangesIdentityAndMissingRemainsAbsent()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile("item.txt", "first");
        WindowsWslFileSystem fileSystem = FileSystem(root);
        WslPath item = Wsl("/owned/item.txt");

        WslFileSystemEntry first = RequireEntry(fileSystem.Find(item));
        WslFileSystemEntry again = RequireEntry(fileSystem.Find(item));
        root.ReplaceFilePreservingMetadata("item.txt", "other");
        WslFileSystemEntry replacement = RequireEntry(fileSystem.Find(item));

        Assert.AreEqual(first.Identity, again.Identity);
        Assert.AreNotEqual(first.Identity, replacement.Identity);
        Assert.IsNull(fileSystem.Find(Wsl("/owned/missing")));
        Assert.IsFalse(fileSystem.TargetExists(Wsl("/owned/missing")));
    }

    /// <summary>Proves required filesystem arguments reject defects.</summary>
    [TestMethod]
    public void BoundariesWhenArgumentIsNullRejectDefect()
    {
        WindowsWslFileSystem fileSystem = new();
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateFile("item");
        WindowsWslFileSystem mapped = FileSystem(root);
        WslFileSystemEntry entry = RequireEntry(mapped.Find(Wsl("/owned/item")));

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsWslFileSystem(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Find(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.TargetExists(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => fileSystem.ContainsReparsePoint((WslFileSystemEntry)null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.ContainsReparsePoint((WslPath)null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Copy(null!, Wsl("/owned/new")));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Copy(entry, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Matches(null!, Wsl("/owned/new")));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Matches(entry, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.CreateDirectory(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Rename(null!, Wsl("/owned/new")));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Rename(entry, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Delete(null!));
    }

    private static WindowsWslFileSystem FileSystem(TestOwnedTemporaryRoot root)
    {
        return new WindowsWslFileSystem(path => Resolve(root, path));
    }

    private static WslFileSystemEntry RequireEntry(WslFileSystemEntry? entry)
    {
        Assert.IsNotNull(entry);
        return entry;
    }

    private static string Resolve(TestOwnedTemporaryRoot root, WslPath path)
    {
        const string operationRoot = "/owned";
        if (path.LinuxPath.Equals(operationRoot, StringComparison.Ordinal))
        {
            return root.Path.CanonicalText;
        }
        string boundary = operationRoot + "/";
        return path.LinuxPath.StartsWith(boundary, StringComparison.Ordinal)
            ? root.Resolve(path.LinuxPath[boundary.Length..].Replace('/', '\\'))
            : throw new InvalidOperationException("The mapped WSL path is outside the test-owned root.");
    }

    private static WslPath Wsl(string linuxPath)
    {
        string text = "\\\\wsl.localhost\\Ubuntu" + linuxPath.Replace('/', '\\');
        return Assert.IsInstanceOfType<WslPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path);
    }
}
