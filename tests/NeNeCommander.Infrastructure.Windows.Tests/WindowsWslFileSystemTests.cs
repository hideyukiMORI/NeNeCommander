using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
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
        StringAssert.StartsWith(fileEntry.Identity.Value, "wsl-v2|Ubuntu|");
        StringAssert.Contains(fileEntry.Identity.Value, "|file|00000000|1|7|");
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
        StringAssert.Contains(directoryEntry.Identity.Value, "|directory|00000000|1|0|");
        _ = root.WriteFile("created\\child.txt", "child");
        WslPath copiedDirectory = Wsl("/owned/copied-directory");
        fileSystem.Copy(directoryEntry, copiedDirectory);
        Assert.IsTrue(fileSystem.Matches(directoryEntry, copiedDirectory));
        _ = root.CreateDirectory("linked-tree");
        _ = root.CreateJunction("linked-tree\\nested-link", "link-target");
        Assert.IsTrue(fileSystem.ContainsReparsePoint(Wsl("/owned/linked-tree")));
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
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsWslFileSystem(path => Resolve(root, path), null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Find(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.TargetExists(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => fileSystem.ContainsReparsePoint((WslFileSystemEntry)null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.ContainsReparsePoint((WslPath)null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Copy(null!, Wsl("/owned/new")));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Copy(entry, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Matches(null!, Wsl("/owned/new")));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Matches(entry, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => fileSystem.CopyFromWindowsLocal(null!, Wsl("/owned/new")));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => fileSystem.CopyFromWindowsLocal(new FileInfo(root.Resolve("item")), null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => fileSystem.MatchesWindowsLocal(null!, Wsl("/owned/new")));
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => fileSystem.MatchesWindowsLocal(new FileInfo(root.Resolve("item")), null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.CreateDirectory(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Rename(null!, Wsl("/owned/new")));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Rename(entry, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => fileSystem.Delete(null!));
    }

    /// <summary>
    /// Proves a rewritten entry and a same-length replacement that restores creation and last-write
    /// time are both rejected, while read access alone leaves the identity untouched.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-020")]
    public void FindWhenEntryIsRewrittenOrReplacedChangesIdentityAndSurvivesReadAccess()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string filePath = root.WriteFile("entry.txt", "content");
        WindowsWslFileSystem fileSystem = FileSystem(root);
        WslPath entry = Wsl("/owned/entry.txt");

        FileIdentity captured = RequireEntry(fileSystem.Find(entry)).Identity;
        Assert.AreEqual("content", File.ReadAllText(filePath));
        FileIdentity afterRead = RequireEntry(fileSystem.Find(entry)).Identity;
        File.WriteAllText(filePath, "content-rewritten");
        FileIdentity afterRewrite = RequireEntry(fileSystem.Find(entry)).Identity;
        root.ReplaceFilePreservingMetadata("entry.txt", "content-different");
        FileIdentity afterReplacement = RequireEntry(fileSystem.Find(entry)).Identity;

        Assert.AreEqual(captured, afterRead);
        Assert.AreNotEqual(captured, afterRewrite);
        Assert.AreNotEqual(afterRewrite, afterReplacement);
        StringAssert.Contains(captured.Value, "|file|00000000|1|7|");
        StringAssert.Contains(afterReplacement.Value, "|file|00000000|1|17|");
    }

    /// <summary>Proves a second name for one entry changes its identity through the link count.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-020")]
    public void FindWhenHardLinkIsAddedChangesIdentityThroughTheLinkCount()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile("entry.txt", "content");
        WindowsWslFileSystem fileSystem = FileSystem(root);
        WslPath entry = Wsl("/owned/entry.txt");

        FileIdentity captured = RequireEntry(fileSystem.Find(entry)).Identity;
        _ = root.CreateHardLink("second.txt", "entry.txt");
        FileIdentity afterLink = RequireEntry(fileSystem.Find(entry)).Identity;

        Assert.AreNotEqual(captured, afterLink);
        StringAssert.Contains(captured.Value, "|file|00000000|1|7|");
        StringAssert.Contains(afterLink.Value, "|file|00000000|2|7|");
    }

    /// <summary>Proves a link entry never shares the identity of the entry it points at.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-020")]
    public void FindWhenEntryIsLinkNeverSharesTheIdentityOfItsTarget()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("target");
        _ = root.CreateJunction("link", "target");
        _ = root.WriteFile("entry.txt", "content");
        _ = root.CreateFileSymbolicLink("entry-link.txt", "entry.txt");
        WindowsWslFileSystem fileSystem = FileSystem(root);

        FileIdentity junction = RequireEntry(fileSystem.Find(Wsl("/owned/link"))).Identity;
        FileIdentity directory = RequireEntry(fileSystem.Find(Wsl("/owned/target"))).Identity;
        FileIdentity symbolicLink = RequireEntry(fileSystem.Find(Wsl("/owned/entry-link.txt"))).Identity;
        FileIdentity file = RequireEntry(fileSystem.Find(Wsl("/owned/entry.txt"))).Identity;

        Assert.AreNotEqual(junction, directory);
        Assert.AreNotEqual(symbolicLink, file);
        StringAssert.Contains(junction.Value, "|link|A0000003|");
        StringAssert.Contains(directory.Value, "|directory|00000000|1|0|");
        StringAssert.Contains(symbolicLink.Value, "|link|A000000C|");
        StringAssert.Contains(file.Value, "|file|00000000|1|7|");
    }

    /// <summary>
    /// Proves the WSL adapter revalidates the real handle identity immediately before its side
    /// effect, so an entry replaced after inspection is rejected with zero effects on disk.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-020")]
    public async Task RenameAsyncWhenEntryIsReplacedAfterInspectionRejectsWithoutEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile("entry.txt", "content");
        WslFileOperationAdapter adapter = new(new WindowsLocalIoExecutionBoundary(), FileSystem(root));

        FileEntrySnapshot snapshot = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            await adapter.InspectAsync(Wsl("/owned/entry.txt"), CancellationToken.None)).Snapshot;
        root.ReplaceFilePreservingMetadata("entry.txt", "replace");
        ProviderStepOutcome outcome = await adapter.RenameAsync(
            snapshot,
            Wsl("/owned/renamed.txt"),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.IsTrue(File.Exists(root.Resolve("entry.txt")));
        Assert.IsFalse(File.Exists(root.Resolve("renamed.txt")));
    }

    /// <summary>Proves an identity token that cannot be bounded closes the entry instead of truncating.</summary>
    [TestMethod]
    public void FindWhenTokenExceedsTheIdentityBoundaryFailsClosed()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile("entry.txt", "content");
        WindowsWslFileSystem fileSystem = FileSystem(root);

        IOException exception = Assert.ThrowsExactly<IOException>(
            () => fileSystem.Find(Wsl(new string('d', 500), "/owned/entry.txt")));

        Assert.AreEqual("The WSL identity token exceeds the identity boundary.", exception.Message);
        Assert.IsNotNull(fileSystem.Find(Wsl(new string('d', 400), "/owned/entry.txt")));
    }

    /// <summary>
    /// Proves entries whose names end with a dot are found, checked, inspected, created, copied,
    /// verified, renamed, and deleted under their exact names, and that the dot-less sibling the
    /// Win32 normalizer would have produced is never touched (ADR-0060).
    /// </summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public async Task OperationsWhenNamesEndWithDotKeepEveryExactName()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteVerbatimFile("foo.", "dot");
        _ = root.CreateVerbatimDirectory("sub.");
        _ = root.WriteVerbatimFile("sub.\\inner", "inner");
        WindowsWslFileSystem fileSystem = FileSystem(root);

        WslFileSystemEntry file = RequireEntry(fileSystem.Find(Wsl("/owned/foo.")));
        WslFileSystemEntry directory = RequireEntry(fileSystem.Find(Wsl("/owned/sub.")));
        WslFileSystemEntry inner = RequireEntry(fileSystem.Find(Wsl("/owned/sub./inner")));
        Assert.AreEqual("foo.", file.Name);
        Assert.AreSame(DirectoryEntryKind.File, file.Kind);
        StringAssert.Contains(file.Identity.Value, "|file|00000000|1|3|");
        Assert.AreEqual("sub.", directory.Name);
        Assert.AreSame(DirectoryEntryKind.Directory, directory.Kind);
        StringAssert.Contains(inner.Identity.Value, "|file|00000000|1|5|");
        Assert.IsTrue(fileSystem.TargetExists(Wsl("/owned/foo.")));
        Assert.IsTrue(fileSystem.TargetExists(Wsl("/owned/sub.")));
        Assert.IsFalse(fileSystem.TargetExists(Wsl("/owned/foo")));
        Assert.IsFalse(fileSystem.TargetExists(Wsl("/owned/sub")));
        Assert.IsNull(fileSystem.Find(Wsl("/owned/foo")));
        Assert.IsFalse(fileSystem.ContainsReparsePoint(directory));
        Assert.IsFalse(fileSystem.ContainsReparsePoint(Wsl("/owned/sub.")));

        WslFileOperationAdapter adapter = new(new WindowsLocalIoExecutionBoundary(), fileSystem);
        FileEntrySnapshot snapshot = Assert.IsInstanceOfType<FileInspectionSucceeded>(
            await adapter.InspectAsync(Wsl("/owned/foo."), CancellationToken.None)).Snapshot;
        Assert.AreEqual(Wsl("/owned/foo."), snapshot.Path);
        Assert.AreEqual(file.Identity, snapshot.Identity);

        fileSystem.CreateDirectory(Wsl("/owned/made."));
        fileSystem.Copy(directory, Wsl("/owned/copy."));
        fileSystem.Copy(file, Wsl("/owned/copy./foo."));
        Assert.IsTrue(Directory.Exists(root.ResolveVerbatim("made.")));
        Assert.IsTrue(Directory.Exists(root.ResolveVerbatim("copy.")));
        Assert.AreEqual("inner", File.ReadAllText(root.ResolveVerbatim("copy.\\inner")));
        Assert.AreEqual("dot", File.ReadAllText(root.ResolveVerbatim("copy.\\foo.")));
        Assert.IsTrue(fileSystem.Matches(file, Wsl("/owned/copy./foo.")));
        Assert.IsFalse(fileSystem.Matches(directory, Wsl("/owned/copy.")));
        File.Delete(root.ResolveVerbatim("copy.\\foo."));
        Assert.IsTrue(fileSystem.Matches(directory, Wsl("/owned/copy.")));

        fileSystem.Rename(file, Wsl("/owned/renamed."));
        Assert.AreEqual("dot", File.ReadAllText(root.ResolveVerbatim("renamed.")));
        Assert.IsFalse(File.Exists(root.ResolveVerbatim("foo.")));
        fileSystem.Delete(RequireEntry(fileSystem.Find(Wsl("/owned/copy."))));
        Assert.IsFalse(Directory.Exists(root.ResolveVerbatim("copy.")));

        string[] names = [.. Directory.GetFileSystemEntries(root.ResolveVerbatim(string.Empty))
            .Select(System.IO.Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)];
        string[] expectedNames = ["made.", "renamed.", "sub."];
        CollectionAssert.AreEqual(expectedNames, names);
    }

    /// <summary>
    /// Proves the identity query receives the exact extended-length text the resolver produced,
    /// because the entry's full name keeps the prefix and the trailing dot.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public void FindWhenNameEndsWithDotPassesTheResolvedTextToTheIdentityQuery()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteVerbatimFile("foo.", "dot");
        _ = root.CreateVerbatimDirectory("sub.");
        List<string> queried = [];
        WindowsWslFileSystem fileSystem = new(
            path => Resolve(root, path),
            text =>
            {
                queried.Add(text);
                return WindowsFileIdentifier.ReadHandleFacts(text);
            });

        _ = RequireEntry(fileSystem.Find(Wsl("/owned/foo.")));
        _ = RequireEntry(fileSystem.Find(Wsl("/owned/sub.")));

        CollectionAssert.AreEqual(
            new[] { root.ResolveVerbatim("foo."), root.ResolveVerbatim("sub.") },
            queried);
        StringAssert.StartsWith(queried[0], "\\\\?\\");
    }

    /// <summary>
    /// Proves the tree mechanism keeps the extended-length form beneath a prefixed parent: a
    /// prefixed root enumerates, every enumerated full name keeps the prefix and its trailing dot,
    /// and a direct child resolves without normalization.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public void TreeResolutionWhenParentIsExtendedLengthKeepsPrefixAndTrailingDots()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string parent = root.CreateVerbatimDirectory("sub.");
        _ = root.WriteVerbatimFile("sub.\\foo.", "dot");
        _ = root.CreateVerbatimDirectory("sub.\\nested.");

        string[] fullNames = [.. new DirectoryInfo(parent)
            .EnumerateFileSystemInfos()
            .Select(entry => entry.FullName)
            .Order(StringComparer.Ordinal)];

        CollectionAssert.AreEqual(
            new[] { root.ResolveVerbatim("sub.\\foo."), root.ResolveVerbatim("sub.\\nested.") },
            fullNames);
        Assert.AreEqual(
            root.ResolveVerbatim("sub.\\foo."),
            WindowsLocalTreeCopy.ResolveDirectChild(parent, "foo."));
        Assert.AreEqual(
            root.ResolveVerbatim("sub.\\bar "),
            WindowsLocalTreeCopy.ResolveDirectChild(parent + "\\", "bar "));
    }

    private static WindowsWslFileSystem FileSystem(TestOwnedTemporaryRoot root)
    {
        // The unguarded reader is the only way to obtain real handle facts from an NTFS test root;
        // production composes the same file system with the 9P-guarded reader by default.
        return new WindowsWslFileSystem(
            path => Resolve(root, path),
            WindowsFileIdentifier.ReadHandleFacts);
    }

    private static WslFileSystemEntry RequireEntry(WslFileSystemEntry? entry)
    {
        Assert.IsNotNull(entry);
        return entry;
    }

    private static string Resolve(TestOwnedTemporaryRoot root, WslPath path)
    {
        return OwnedWslNamespace.Resolve(root.ResolveVerbatim(string.Empty), path);
    }

    private static WslPath Wsl(string linuxPath)
    {
        return Wsl("Ubuntu", linuxPath);
    }

    private static WslPath Wsl(string distribution, string linuxPath)
    {
        string text = "\\\\wsl.localhost\\" + distribution + linuxPath.Replace('/', '\\');
        return Assert.IsInstanceOfType<WslPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path);
    }
}
