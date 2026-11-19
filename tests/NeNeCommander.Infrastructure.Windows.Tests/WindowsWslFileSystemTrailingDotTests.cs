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

/// <summary>
/// Proves the Windows-side WSL filesystem mechanism keeps names that end with a dot or a space
/// exact against a test-owned root (ADR-0060).
/// </summary>
[TestClass]
public sealed class WindowsWslFileSystemTrailingDotTests
{
    /// <summary>
    /// Proves entries whose names end with a dot are found, checked, and inspected under their exact
    /// names, while the dot-less sibling the Win32 normalizer would have produced stays absent.
    /// </summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public async Task InspectAsyncWhenNamesEndWithDotKeepsExactIdentityAsync()
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
            await adapter.InspectAsync(Wsl("/owned/foo."), FileInspectionPurpose.Deletion, CancellationToken.None)).Snapshot;
        Assert.AreEqual(Wsl("/owned/foo."), snapshot.Path);
        Assert.AreEqual(file.Identity, snapshot.Identity);
    }

    /// <summary>Proves trailing-dot names survive mutation and exact-name tree verification (ADR-0060).</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public void MutationsWhenNamesEndWithDotKeepEveryExactName()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteVerbatimFile("foo.", "dot");
        _ = root.CreateVerbatimDirectory("sub.");
        _ = root.WriteVerbatimFile("sub.\\inner", "inner");
        WindowsWslFileSystem fileSystem = FileSystem(root);
        WslFileSystemEntry file = RequireEntry(fileSystem.Find(Wsl("/owned/foo.")));
        WslFileSystemEntry directory = RequireEntry(fileSystem.Find(Wsl("/owned/sub.")));

        fileSystem.CreateDirectory(Wsl("/owned/made."));
        Assert.IsNull(fileSystem.Copy(directory, Wsl("/owned/copy.")).Failure);
        Assert.IsNull(fileSystem.Copy(file, Wsl("/owned/copy./foo.")).Failure);
        Assert.IsTrue(Directory.Exists(root.ResolveVerbatim("made.")));
        Assert.IsTrue(Directory.Exists(root.ResolveVerbatim("copy.")));
        Assert.AreEqual("inner", File.ReadAllText(root.ResolveVerbatim("copy.\\inner")));
        Assert.AreEqual("dot", File.ReadAllText(root.ResolveVerbatim("copy.\\foo.")));
        Assert.IsTrue(fileSystem.VerifyCopy(Snapshot(file), Wsl("/owned/copy./foo.")).Failure is null);
        Assert.IsFalse(fileSystem.VerifyCopy(Snapshot(directory), Wsl("/owned/copy.")).Failure is null);
        File.Delete(root.ResolveVerbatim("copy.\\foo."));
        Assert.IsTrue(fileSystem.VerifyCopy(Snapshot(directory), Wsl("/owned/copy.")).Failure is null);

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
    /// because the entry's full name keeps the prefix and its trailing dot or space.
    /// </summary>
    /// <param name="ending">A trailing character Win32 normalizes away from ordinary path text.</param>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    [DataRow(".")]
    [DataRow(" ")]
    public void FindWhenNameEndsWithDotOrSpacePassesTheResolvedTextToTheIdentityQuery(string ending)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string fileName = "foo" + ending;
        string directoryName = "sub" + ending;
        _ = root.WriteVerbatimFile(fileName, "dot");
        _ = root.CreateVerbatimDirectory(directoryName);
        List<string> queried = [];
        WindowsWslFileSystem fileSystem = new(
            path => Resolve(root, path),
            text =>
            {
                queried.Add(text);
                return OwnedWslNamespace.ReadFacts(text);
            });

        Assert.AreEqual(fileName, RequireEntry(fileSystem.Find(Wsl("/owned/" + fileName))).Name);
        Assert.AreEqual(directoryName, RequireEntry(fileSystem.Find(Wsl("/owned/" + directoryName))).Name);

        CollectionAssert.AreEqual(
            new[] { root.ResolveVerbatim(fileName), root.ResolveVerbatim(directoryName) },
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
            Resolve(root, Assert.IsInstanceOfType<WslPath>(WindowsLocalTreeCopy.DeriveChild(Wsl("/owned/sub."), "foo."))));
        Assert.AreEqual(
            root.ResolveVerbatim("sub.\\bar "),
            Resolve(root, Assert.IsInstanceOfType<WslPath>(WindowsLocalTreeCopy.DeriveChild(Wsl("/owned/sub."), "bar "))));
    }

    private static FileEntrySnapshot Snapshot(WslFileSystemEntry entry)
    {
        return FileEntrySnapshot.Create(entry.Path, entry.Identity, DeletionCapability.PermanentOnly);
    }

    private static WindowsWslFileSystem FileSystem(TestOwnedTemporaryRoot root)
    {
        // The unguarded reader is the only way to obtain real handle facts from an NTFS test root;
        // production composes the same file system with the 9P-guarded reader by default.
        return new WindowsWslFileSystem(
            path => Resolve(root, path),
            OwnedWslNamespace.ReadFacts);
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
        string text = "\\\\wsl.localhost\\Ubuntu" + linuxPath.Replace('/', '\\');
        return Assert.IsInstanceOfType<WslPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path);
    }
}
