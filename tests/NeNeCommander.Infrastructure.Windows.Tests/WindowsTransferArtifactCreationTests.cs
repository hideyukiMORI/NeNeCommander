using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves protected artifact creation is exclusive, parent-relative and restricted at initial creation.</summary>
[TestClass]
public sealed class WindowsTransferArtifactCreationTests
{
    /// <summary>Proves the initial DACL has only the captured user and never inherits a parent grant.</summary>
    [TestMethod]
    public void CreateArtifactWhenParentHasInheritableGrantCreatesProtectedUserOnlyFile()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string parentText = root.CreateDirectory("parent");
        DirectoryInfo parent = new(parentText);
        DirectorySecurity parentSecurity = parent.GetAccessControl();
        SecurityIdentifier everyone = new(WellKnownSidType.WorldSid, null);
        parentSecurity.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.Read,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        parent.SetAccessControl(parentSecurity);
        WindowsLocalPath anchor = Parse(root.WriteFile("parent/anchor.txt", "anchor"));
        WindowsLocalPath candidate = Parse(root.Resolve("parent/artifact.tmp"));
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using SafeFileHandle artifact = WindowsTransferEntryOpen.CreateArtifact(lease, candidate, security);
        using FileStream stream = new(artifact, FileAccess.ReadWrite);
        FileSecurity actual = stream.GetAccessControl();
        using WindowsIdentity current = WindowsIdentity.GetCurrent();
        SecurityIdentifier user = Assert.IsInstanceOfType<SecurityIdentifier>(current.User);
        AuthorizationRuleCollection rules = actual.GetAccessRules(true, true, typeof(SecurityIdentifier));

        Assert.IsTrue(actual.AreAccessRulesProtected);
        Assert.AreEqual(user, actual.GetOwner(typeof(SecurityIdentifier)));
        Assert.HasCount(1, rules);
        FileSystemAccessRule rule = Assert.IsInstanceOfType<FileSystemAccessRule>(rules[0]);
        Assert.AreEqual(user, rule.IdentityReference);
        Assert.AreEqual(AccessControlType.Allow, rule.AccessControlType);
        Assert.AreEqual(FileSystemRights.FullControl, rule.FileSystemRights);
        Assert.IsFalse(rule.IsInherited);
        Assert.AreEqual(InheritanceFlags.None, rule.InheritanceFlags);
        Assert.AreEqual(0L, stream.Length);
        Assert.IsTrue((FileAttributes)WindowsFileIdentifier.ReadLocalFacts(artifact).Basic.Attributes is FileAttributes.Normal or FileAttributes.Archive);
        WindowsTransferPathObservation.Require(artifact, candidate);
        lease.Revalidate();
    }

    /// <summary>Proves an existing entry is never opened, truncated, followed or removed by create-new.</summary>
    [TestMethod]
    [DataRow("file")]
    [DataRow("caseFile")]
    [DataRow("directory")]
    [DataRow("junction")]
    [DataRow("hardlink")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CreateArtifactWhenCandidateAlreadyExistsRefusesWithoutChangingIt(string kind)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = Parse(root.WriteFile("anchor.txt", "anchor"));
        string foreign = root.WriteFile("foreign.txt", "foreign");
        _ = root.CreateDirectory("foreign-directory");
        string candidateText = kind switch
        {
            "file" => root.WriteFile("artifact", "original"),
            "caseFile" => root.WriteFile("ARTIFACT", "original"),
            "directory" => root.CreateDirectory("artifact"),
            "junction" => root.CreateJunction("artifact", "foreign-directory"),
            "hardlink" => root.CreateHardLink("artifact", "foreign.txt"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        WindowsLocalPath candidate = Parse(kind == "caseFile" ? root.Resolve("artifact") : candidateText);
        string originalIdentity = WindowsFileIdentifier.Describe(candidateText);
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();

        _ = Assert.Throws<IOException>(() =>
        {
            using SafeFileHandle refused = WindowsTransferEntryOpen.CreateArtifact(lease, candidate, security);
        });
        Assert.AreEqual(originalIdentity, WindowsFileIdentifier.Describe(candidateText));
        Assert.AreEqual("foreign", File.ReadAllText(foreign));
        if (kind is "file" or "caseFile")
        {
            Assert.AreEqual("original", File.ReadAllText(candidateText));
        }
        lease.Revalidate();
    }

    /// <summary>Proves an artifact handle excludes ordinary writers and name replacement while admitting a compatible reader.</summary>
    [TestMethod]
    public void CreateArtifactWhenOwnedHandleLivesExcludesWritersAndRename()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = Parse(root.WriteFile("anchor.txt", "anchor"));
        WindowsLocalPath candidate = Parse(root.Resolve("artifact.tmp"));
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        using SafeFileHandle artifact = WindowsTransferEntryOpen.CreateArtifact(lease, candidate, security);
        RandomAccess.Write(artifact, [1, 2, 3], 0);

        _ = Assert.Throws<IOException>(() =>
        {
            using SafeFileHandle writer = File.OpenHandle(candidate.CanonicalText, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        });
        _ = Assert.Throws<IOException>(() => File.Move(candidate.CanonicalText, root.Resolve("moved.tmp")));
        using SafeFileHandle reader = File.OpenHandle(candidate.CanonicalText, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        byte[] content = new byte[3];
        Assert.AreEqual(3, RandomAccess.Read(reader, content, 0));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, content);
    }

    /// <summary>Proves the declared pathname must be one exact direct child of the held parent.</summary>
    [TestMethod]
    public void CreateArtifactWhenChildBelongsToAnotherParentRefusesBeforeCreation()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = Parse(root.WriteFile("anchor.txt", "anchor"));
        _ = root.CreateDirectory("nested");
        WindowsLocalPath candidate = Parse(root.Resolve("nested/artifact.tmp"));
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            using SafeFileHandle refused = WindowsTransferEntryOpen.CreateArtifact(lease, candidate, security);
        });
        Assert.IsFalse(File.Exists(candidate.CanonicalText));
    }

    /// <summary>Proves disposed native ownership refuses rather than creating with a default descriptor or current directory.</summary>
    [TestMethod]
    [DataRow("parent")]
    [DataRow("security")]
    public void CreateArtifactWhenAnOwnerIsDisposedRefusesWithoutCreating(string owner)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath anchor = Parse(root.WriteFile("anchor.txt", "anchor"));
        WindowsLocalPath candidate = Parse(root.Resolve("artifact.tmp"));
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        if (owner == "parent")
        {
            lease.Dispose();
        }
        else
        {
            security.Dispose();
        }

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            using SafeFileHandle refused = WindowsTransferEntryOpen.CreateArtifact(lease, candidate, security);
        });
        Assert.IsFalse(File.Exists(candidate.CanonicalText));
    }

    /// <summary>Proves a foreign parent label cannot redirect creation under the held parent object.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public void CreateArtifactWhenDeclaredParentDiffersFromHeldObjectRefusesBeforeEffects()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("actual");
        _ = root.CreateDirectory("declared");
        WindowsLocalPath anchor = Parse(root.WriteFile("actual/anchor.txt", "anchor"));
        WindowsLocalPath candidate = Parse(root.Resolve("declared/artifact.tmp"));
        using WindowsTransferPathLease lease = WindowsTransferPathLease.Open(anchor, FileAccess.Read, new WindowsDriveBoundary());
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            using SafeFileHandle refused = WindowsTransferEntryOpen.CreateArtifact(lease, candidate, security);
        });
        Assert.IsFalse(File.Exists(root.Resolve("actual/artifact.tmp")));
        Assert.IsFalse(File.Exists(candidate.CanonicalText));
    }

    /// <summary>Proves nonzero or unknown directory flags cannot authorize case-insensitive artifact naming.</summary>
    [TestMethod]
    [DataRow(1u)]
    [DataRow(2u)]
    [DataRow(uint.MaxValue)]
    public void RequireCaseInsensitiveFlagsWhenObservationIsNotExplicitlyInsensitiveRefusesIt(uint flags)
    {
        WindowsFileIdentifier.RequireCaseInsensitiveFlags(0);
        _ = Assert.ThrowsExactly<IOException>(() => WindowsFileIdentifier.RequireCaseInsensitiveFlags(flags));
    }

    /// <summary>Proves a failed query cannot be replaced by the usual case-insensitive assumption.</summary>
    [TestMethod]
    public void RequireCaseInsensitiveDirectoryWhenHandleIsInvalidRefusesIt()
    {
        using SafeFileHandle invalid = new(IntPtr.Zero, true);
        _ = Assert.ThrowsExactly<IOException>(() => WindowsFileIdentifier.RequireCaseInsensitiveDirectory(invalid));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowsFileIdentifier.RequireCaseInsensitiveDirectory(null!));
    }

    private static WindowsLocalPath Parse(string path)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path);
    }
}
