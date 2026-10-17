using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves copy admission refreshes cached source facts and refuses linked source or target boundaries.</summary>
[TestClass]
public sealed class VerifiedCopyAdmissionBoundaryTests
{
    /// <summary>A remap after root creation stops the next child while preserving the truthful partial target.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public void CopyWhenDriveRemapsBeforeRecursiveChildRetainsOnlyCreatedTarget()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("source");
        _ = root.WriteFile("source\\child.txt", "kept");
        string targetText = root.Resolve("target");
        WindowsDriveBoundary drives = new(_ => Directory.Exists(targetText)
            ? DriveType.Network : DriveType.Fixed);
        WindowsTreeCopyAccess access = new(path => path.CanonicalText,
            entry => WindowsLocalTreeCopy.InspectLocalSource(entry, drives), drives);

        ProviderStepOutcome outcome = WindowsLocalTreeCopy.Copy(new DirectoryInfo(root.Resolve("source")),
            Path(targetText), access);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.AreSame(ProviderStepEffectKind.CopyTargetCreated, outcome.Effect);
        Assert.IsTrue(Directory.Exists(targetText));
        Assert.HasCount(0, Directory.GetFileSystemEntries(targetText));
        Assert.AreEqual("kept", File.ReadAllText(root.Resolve("source\\child.txt")));
    }

    /// <summary>Rechecks a cached local directory after replacement by a junction before creating any target.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public void CopyWhenCachedLocalDirectoryBecomesJunctionRejectsBeforeTargetCreation()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("source");
        _ = root.CreateDirectory("linked");
        _ = root.WriteFile("linked\\kept", "unfollowed");
        DirectoryInfo source = new(root.Resolve("source"));
        Assert.IsFalse((source.Attributes & FileAttributes.ReparsePoint) != 0);
        Directory.Delete(root.Resolve("source"));
        _ = root.CreateJunction("source", "linked");

        ProviderStepOutcome outcome = WindowsLocalTreeCopy.Copy(source, Path(root.Resolve("target")),
            new WindowsTreeCopyAccess(path => path.CanonicalText,
                WindowsLocalTreeCopy.InspectLocalSource, new WindowsDriveBoundary()));

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.IsFalse(Directory.Exists(root.Resolve("target")));
        Assert.AreEqual("unfollowed", File.ReadAllText(root.Resolve("linked\\kept")));
    }

    /// <summary>Requires an existing unlinked destination parent before creating a target.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("junction")]
    [DataRow("absent")]
    public void CopyWhenTargetParentIsUnavailableRejectsBeforeWritingTarget(string parent)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile("source", "kept");
        _ = root.CreateDirectory("linked");
        if (parent == "junction")
        {
            _ = root.CreateJunction("parent", "linked");
        }

        ProviderStepOutcome outcome = WindowsLocalTreeCopy.Copy(new FileInfo(root.Resolve("source")),
            Path(root.Resolve("parent\\target")), new WindowsTreeCopyAccess(path => path.CanonicalText,
                WindowsLocalTreeCopy.InspectLocalSource, new WindowsDriveBoundary()));

        Assert.AreSame(parent == "junction" ? FileOperationFailureKind.ProviderUnavailable : FileOperationFailureKind.NotFound, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.IsFalse(File.Exists(root.Resolve("linked\\target")));
        Assert.IsFalse(File.Exists(root.Resolve("parent\\target")));
        Assert.AreEqual("kept", File.ReadAllText(root.Resolve("source")));
    }

    /// <summary>Refreshes an enumerated WSL child after its facts callback observes replacement by a junction.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public void CopyToWindowsLocalWhenEnumeratedChildBecomesJunctionRetainsOnlyCreatedRootEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("wsl\\source\\child");
        _ = root.CreateDirectory("linked");
        _ = root.WriteFile("linked\\kept", "unfollowed");
        bool copying = false;
        bool replaced = false;
        WindowsWslFileSystem fileSystem = new(path => OwnedWslNamespace.Resolve(root.ResolveVerbatim("wsl"), path), path =>
        {
            WslHandleFacts facts = OwnedWslNamespace.ReadFacts(path);
            if (copying && !replaced && path.EndsWith("\\child", StringComparison.Ordinal))
            {
                Directory.Delete(root.Resolve("wsl\\source\\child"));
                _ = root.CreateJunction("wsl\\source\\child", "linked");
                replaced = true;
            }
            return facts;
        });
        WslFileSystemEntry source = fileSystem.Find(WslToWindowsTransferFixture.Source("source")) ??
            throw new AssertFailedException("The owned WSL source is absent.");
        copying = true;

        ProviderStepOutcome outcome = fileSystem.CopyToWindowsLocal(source,
            Assert.IsInstanceOfType<WindowsLocalPath>(Path(root.Resolve("target"))));

        Assert.IsTrue(replaced);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.AreSame(ProviderStepEffectKind.CopyTargetCreated, outcome.Effect);
        Assert.IsTrue(Directory.Exists(root.Resolve("target")));
        Assert.HasCount(0, Directory.GetFileSystemEntries(root.Resolve("target")));
        Assert.AreEqual("unfollowed", File.ReadAllText(root.Resolve("linked\\kept")));
    }

    private static FileSystemPath Path(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
