using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves independent identity, membership, content and ancestor admission at the verifier boundary.</summary>
[TestClass]
public sealed class VerifiedTreeBoundaryTests
{
    /// <summary>Requires every ancestor to match even when another captured ancestor remains unchanged.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("first")]
    [DataRow("second")]
    public void MatchesAncestorsWhenOneOfTwoIdentitiesChangesRejects(string changed)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("first");
        _ = root.CreateDirectory("second");
        VerifiedTreeAccess access = new(new ScriptedWslFileSystem());
        VerifiedEntrySnapshot first = access.Capture(Path(root.Resolve("first")));
        VerifiedEntrySnapshot second = access.Capture(Path(root.Resolve("second")));
        Assert.IsTrue(access.MatchesAncestors([first, second]));
        root.SetLastWriteTimeUtc(changed, Directory.GetLastWriteTimeUtc(root.Resolve(changed)).AddTicks(10000000));

        bool matches = access.MatchesAncestors([first, second]);

        Assert.IsFalse(matches);
        Assert.IsTrue(Directory.Exists(root.Resolve("first")));
        Assert.IsTrue(Directory.Exists(root.Resolve("second")));
    }

    /// <summary>Proves exact bytes are required on each side independently of length and restored timestamps.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("source")]
    [DataRow("target")]
    public void MatchesFileWhenOneSideBytesChangeWithStableIdentityRejects(string changed)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile("source", "first");
        _ = root.WriteFile("target", "first");
        VerifiedTreeAccess access = new(new ScriptedWslFileSystem());
        VerifiedCopyEntry.VerifiedFile evidence = FileEvidence(root, access);
        DateTime modified = File.GetLastWriteTimeUtc(root.Resolve(changed));
        _ = root.WriteFile(changed, "other");
        File.SetLastWriteTimeUtc(root.Resolve(changed), modified);
        Assert.AreEqual(changed == "source" ? evidence.Source : evidence.Target, access.Capture(Path(root.Resolve(changed))));

        bool matches = VerifiedTreeEvidence.MatchesFile(evidence, access);

        Assert.IsFalse(matches);
        Assert.AreEqual("other", File.ReadAllText(root.Resolve(changed)));
    }

    /// <summary>Requires each evidence endpoint to remain a file.</summary>
    [TestMethod]
    [DataRow("source")]
    [DataRow("target")]
    public void MatchesFileWhenOneSideBecomesDirectoryRejects(string changed)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile("source", "first");
        _ = root.WriteFile("target", "first");
        VerifiedTreeAccess access = new(new ScriptedWslFileSystem());
        VerifiedCopyEntry.VerifiedFile evidence = FileEvidence(root, access);
        File.Delete(root.Resolve(changed));
        _ = root.CreateDirectory(changed);

        bool matches = VerifiedTreeEvidence.MatchesFile(evidence, access);

        Assert.IsFalse(matches);
        Assert.IsTrue(Directory.Exists(root.Resolve(changed)));
    }

    /// <summary>Rejects one changed full identity even when both file streams still match.</summary>
    [TestMethod]
    [DataRow("source")]
    [DataRow("target")]
    public void MatchesWhenOnlyOneFileIdentityChangesRejects(string changed)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile("source", "first");
        _ = root.WriteFile("target", "first");
        VerifiedTreeAccess access = new(new ScriptedWslFileSystem());
        VerifiedCopyEntry.VerifiedFile evidence = FileEvidence(root, access);
        root.SetLastWriteTimeUtc(changed, File.GetLastWriteTimeUtc(root.Resolve(changed)).AddTicks(10000000));
        Assert.IsTrue(VerifiedTreeEvidence.MatchesFile(evidence, access));

        bool matches = VerifiedTreeEvidence.Matches(evidence, access);

        Assert.IsFalse(matches);
        Assert.AreEqual("first", File.ReadAllText(root.Resolve(changed)));
    }

    /// <summary>Requires both directory memberships independently of their unchanged full identities.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("source")]
    [DataRow("target")]
    public void MatchesWhenOneDirectoryHasUnrecordedChildRejects(string changed)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("source");
        _ = root.CreateDirectory("target");
        VerifiedTreeAccess access = new(new ScriptedWslFileSystem());
        VerifiedCopyEntry.VerifiedDirectory evidence = DirectoryEvidence(root, access);
        DateTime modified = Directory.GetLastWriteTimeUtc(root.Resolve(changed));
        _ = root.WriteFile(changed + "\\late", "unrecorded");
        Directory.SetLastWriteTimeUtc(root.Resolve(changed), modified);
        Assert.AreEqual(changed == "source" ? evidence.Source : evidence.Target, access.Capture(Path(root.Resolve(changed))));

        bool matches = VerifiedTreeEvidence.Matches(evidence, access);

        Assert.IsFalse(matches);
        Assert.AreEqual("unrecorded", File.ReadAllText(root.Resolve(changed + "\\late")));
    }

    /// <summary>Requires target identity and membership rather than accepting either one alone.</summary>
    [TestMethod]
    [DataRow("identity")]
    [DataRow("membership")]
    public void TargetMatchesWhenOnlyOneTargetInvariantChangesRejects(string change)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("source");
        _ = root.CreateDirectory("target");
        VerifiedTreeAccess access = new(new ScriptedWslFileSystem());
        VerifiedCopyEntry.VerifiedDirectory evidence = DirectoryEvidence(root, access);
        DateTime modified = Directory.GetLastWriteTimeUtc(root.Resolve("target"));
        if (change == "membership")
        {
            _ = root.WriteFile("target\\late", "unrecorded");
        }
        Directory.SetLastWriteTimeUtc(root.Resolve("target"), change == "identity" ? modified.AddTicks(10000000) : modified);

        bool matches = VerifiedTreeEvidence.TargetMatches(evidence, access);

        Assert.IsFalse(matches);
        Assert.IsTrue(Directory.Exists(root.Resolve("source")));
    }

    /// <summary>Includes hidden and system children and returns ordinal order independently of creation order.</summary>
    [TestMethod]
    public void ChildrenWhenAttributesDifferKeepsCompleteOrdinalMembership()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("directory");
        _ = root.CreateHiddenFile("directory\\a");
        _ = root.CreateSystemFile("directory\\B");
        VerifiedTreeAccess access = new(new ScriptedWslFileSystem());

        string[] children = [.. access.Children(Path(root.Resolve("directory"))).Select(child => child.CanonicalText)];

        CollectionAssert.AreEqual(new[] { root.Resolve("directory\\B"), root.Resolve("directory\\a") }, children);
    }

    /// <summary>Rejects a link at the tree-access owner instead of relying on a later identity query.</summary>
    [TestMethod]
    public void FindWhenEntryIsJunctionRejectsWithoutFollowingIt()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("target");
        _ = root.CreateJunction("link", "target");
        VerifiedTreeAccess access = new(new ScriptedWslFileSystem());

        _ = Assert.ThrowsExactly<IOException>(() => access.Find(Path(root.Resolve("link"))));

        Assert.IsTrue(Directory.Exists(root.Resolve("target")));
    }

    /// <summary>Refuses incomplete enumeration instead of treating a denied directory as empty.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public void ChildrenWhenDirectoryListingIsDeniedRejectsIncompleteEvidence()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("directory");
        _ = root.WriteFile("directory\\kept", "unrecorded");
        _ = root.DenyDirectoryListing("directory");
        VerifiedTreeAccess access = new(new ScriptedWslFileSystem());

        _ = Assert.ThrowsExactly<UnauthorizedAccessException>(() => access.Children(Path(root.Resolve("directory"))));

        Assert.AreEqual("unrecorded", File.ReadAllText(root.Resolve("directory\\kept")));
    }

    /// <summary>Rejects a file in the ancestor chain rather than accepting its identity as a directory boundary.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public void AncestorsWhenParentIsFileRejectsWithoutMutation()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile("file", "kept");
        VerifiedTreeAccess access = new(new ScriptedWslFileSystem());

        _ = Assert.ThrowsExactly<IOException>(() => access.Ancestors(Path(root.Resolve("file\\child"))));

        Assert.AreEqual("kept", File.ReadAllText(root.Resolve("file")));
    }

    /// <summary>Reads matching evidence while a cooperating reader already holds a shared file handle.</summary>
    [TestMethod]
    public void FileContentWhenCooperatingReaderIsOpenComparesAndReadsMatchingBytes()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile("source", "first");
        _ = root.WriteFile("target", "first");
        using FileStream reader = new(root.Resolve("source"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        FileContentHash compared = VerifiedFileContent.Compare(root.Resolve("source"), root.Resolve("target"));

        Assert.AreEqual(compared, VerifiedFileContent.Read(root.Resolve("source")));
        Assert.AreEqual("first", File.ReadAllText(root.Resolve("source")));
    }

    private static VerifiedCopyEntry.VerifiedFile FileEvidence(TestOwnedTemporaryRoot root, VerifiedTreeAccess access)
    {
        return Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedFile>(VerifiedCopyEntry.File(
            access.Capture(Path(root.Resolve("source"))), access.Capture(Path(root.Resolve("target"))),
            VerifiedFileContent.Compare(root.Resolve("source"), root.Resolve("target"))));
    }

    private static VerifiedCopyEntry.VerifiedDirectory DirectoryEvidence(TestOwnedTemporaryRoot root, VerifiedTreeAccess access)
    {
        return Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedDirectory>(VerifiedCopyEntry.Directory(
            access.Capture(Path(root.Resolve("source"))), access.Capture(Path(root.Resolve("target"))), []));
    }

    private static FileSystemPath Path(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
