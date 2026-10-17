using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves verification refuses stale snapshots and changes observed only between its captures.</summary>
[TestClass]
public sealed class VerifiedVerificationBoundaryTests
{
    /// <summary>A remapped local verification endpoint yields a typed denial before any effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public void VerifyWhenLocalDriveIsRejectedReportsProviderUnavailable()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourceText = root.WriteFile("source.txt", "kept");
        string targetText = root.WriteFile("target.txt", "kept");
        FileSystemPath sourcePath = Path(sourceText);
        FileSystemPath targetPath = Path(targetText);
        VerifiedTreeAccess admitted = new(new ScriptedWslFileSystem(), new WindowsDriveBoundary(_ => DriveType.Fixed));
        FileEntrySnapshot source = FileEntrySnapshot.Create(sourcePath, admitted.Capture(sourcePath).Identity,
            DeletionCapability.PermanentOnly);
        bool driveQueried = false;
        VerifiedTreeAccess denied = new(new ScriptedWslFileSystem(), new WindowsDriveBoundary(_ =>
        {
            driveQueried = true;
            return DriveType.Network;
        }));

        CopyVerificationOutcome outcome = WindowsLocalTreeCopy.Verify(source, targetPath, denied);

        Assert.IsTrue(driveQueried);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.AreEqual("kept", File.ReadAllText(sourceText));
        Assert.AreEqual("kept", File.ReadAllText(targetText));
    }

    /// <summary>Never issues a successful receipt for a frozen source identity different from the observed tree.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("matching")]
    [DataRow("different")]
    public void VerifyWhenFrozenSourceIdentityDiffersRejectsWithoutMutation(string identity)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile("source", "first");
        _ = root.WriteFile("target", "first");
        VerifiedTreeAccess access = new(new ScriptedWslFileSystem());
        FileSystemPath sourcePath = Path(root.Resolve("source"));
        FileIdentity sourceIdentity = identity == "matching" ? access.Capture(sourcePath).Identity :
            Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("stale-source")).Identity;
        FileEntrySnapshot source = FileEntrySnapshot.Create(sourcePath, sourceIdentity, DeletionCapability.PermanentOnly);

        CopyVerificationOutcome outcome = WindowsLocalTreeCopy.Verify(source, Path(root.Resolve("target")), access);

        Assert.AreSame(identity == "matching" ? null : FileOperationFailureKind.Verification, outcome.Failure);
        Assert.AreEqual("first", File.ReadAllText(root.Resolve("source")));
        Assert.AreEqual("first", File.ReadAllText(root.Resolve("target")));
    }

    /// <summary>Rejects a transient source or target change observed after comparing the file bytes.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("source")]
    [DataRow("target")]
    public void VerifyWhenOneEndpointChangesOnlyDuringFinalEntryCaptureRejects(string side)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("wsl");
        _ = root.WriteFile("wsl\\file.txt", "first");
        _ = root.WriteFile("local.txt", "first");
        bool active = false;
        bool duringEntry = false;
        bool observedChange = false;
        WindowsWslFileSystem fileSystem = new(path => OwnedWslNamespace.Resolve(root.ResolveVerbatim("wsl"), path), path =>
        {
            WslHandleFacts facts = OwnedWslNamespace.ReadFacts(path);
            if (!active)
            {
                return facts;
            }
            if (!path.EndsWith("\\file.txt", StringComparison.Ordinal))
            {
                if (duringEntry)
                {
                    active = false;
                }
                duringEntry = false;
                return facts;
            }
            if (duringEntry)
            {
                observedChange = true;
                return ChangedTime(facts);
            }
            duringEntry = true;
            return facts;
        });
        VerifiedTreeAccess access = new(fileSystem);
        FileSystemPath sourcePath = side == "source" ? WslToWindowsTransferFixture.Source("file.txt") : Path(root.Resolve("local.txt"));
        FileSystemPath targetPath = side == "target" ? WslToWindowsTransferFixture.Source("file.txt") : Path(root.Resolve("local.txt"));
        FileEntrySnapshot source = FileEntrySnapshot.Create(sourcePath, access.Capture(sourcePath).Identity, DeletionCapability.PermanentOnly);
        active = true;

        CopyVerificationOutcome outcome = WindowsLocalTreeCopy.Verify(source, targetPath, access);

        Assert.IsTrue(observedChange);
        Assert.AreSame(FileOperationFailureKind.Verification, outcome.Failure);
        Assert.AreEqual("first", File.ReadAllText(root.Resolve("wsl\\file.txt")));
        Assert.AreEqual("first", File.ReadAllText(root.Resolve("local.txt")));
    }

    /// <summary>Requires the evidence recheck's final identity capture on each side, after content verification.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("source")]
    [DataRow("target")]
    public void MatchesWhenOneEndpointChangesOnlyDuringFinalEvidenceCaptureRejects(string side)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("wsl");
        _ = root.WriteFile("wsl\\file.txt", "first");
        _ = root.WriteFile("local.txt", "first");
        bool active = false;
        int observations = 0;
        WindowsWslFileSystem fileSystem = new(path => OwnedWslNamespace.Resolve(root.ResolveVerbatim("wsl"), path), path =>
        {
            WslHandleFacts facts = OwnedWslNamespace.ReadFacts(path);
            return active && path.EndsWith("\\file.txt", StringComparison.Ordinal) && ++observations == 2
                ? ChangedTime(facts) : facts;
        });
        VerifiedTreeAccess access = new(fileSystem);
        FileSystemPath source = side == "source" ? WslToWindowsTransferFixture.Source("file.txt") : Path(root.Resolve("local.txt"));
        FileSystemPath target = side == "target" ? WslToWindowsTransferFixture.Source("file.txt") : Path(root.Resolve("local.txt"));
        VerifiedCopyEntry evidence = VerifiedCopyEntry.File(access.Capture(source), access.Capture(target),
            VerifiedFileContent.Compare(access.Resolve(source), access.Resolve(target)));
        active = true;

        bool matches = VerifiedTreeEvidence.Matches(evidence, access);

        Assert.IsTrue(observations >= 2);
        Assert.IsFalse(matches);
        Assert.AreEqual("first", File.ReadAllText(root.Resolve("wsl\\file.txt")));
    }

    /// <summary>Rejects a surplus copied child at the child-verification boundary without a later guard masking it.</summary>
    [TestMethod]
    [DataRow("matching")]
    [DataRow("surplus")]
    public void VerifyChildrenWhenTargetHasUnrecordedChildRejects(string membership)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("source");
        _ = root.CreateDirectory("target");
        if (membership == "surplus")
        {
            _ = root.WriteFile("target\\late", "unrecorded");
        }
        VerifiedTreeAccess access = new(new ScriptedWslFileSystem());

        if (membership == "matching")
        {
            Assert.HasCount(0, WindowsLocalTreeCopy.VerifyChildren([], Path(root.Resolve("target")), access));
        }
        else
        {
            _ = Assert.ThrowsExactly<IOException>(() => WindowsLocalTreeCopy.VerifyChildren([], Path(root.Resolve("target")), access));
            Assert.AreEqual("unrecorded", File.ReadAllText(root.Resolve("target\\late")));
        }
    }

    /// <summary>Normalizes a denied verification observation without mutating either side.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public void VerifyWhenIdentityObservationIsDeniedReturnsTypedAccessDenied()
    {
        bool denied = false;
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create(path =>
            denied ? throw new UnauthorizedAccessException("Owned verification observation denied.") : OwnedWslNamespace.ReadFacts(path));
        _ = fixture.Root.WriteFile("wsl\\file.txt", "first");
        _ = fixture.Root.WriteFile("windows\\file.txt", "first");
        WslPath sourcePath = WslToWindowsTransferFixture.Source("file.txt");
        FileEntrySnapshot source = FileEntrySnapshot.Create(sourcePath, fixture.FileSystem.CaptureVerificationEntry(sourcePath).Identity,
            DeletionCapability.PermanentOnly);
        denied = true;

        CopyVerificationOutcome outcome = fixture.FileSystem.VerifyCopy(source, Path(fixture.Root.Resolve("windows\\file.txt")));

        Assert.AreSame(FileOperationFailureKind.AccessDenied, outcome.Failure);
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("wsl\\file.txt")));
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("windows\\file.txt")));
    }

    private static WslHandleFacts ChangedTime(WslHandleFacts facts)
    {
        return new WslHandleFacts(facts.Inode, facts.Attributes, facts.ReparseTag, facts.EndOfFile,
            facts.NumberOfLinks, facts.LastWriteFileTimeUtc, facts.ChangeFileTimeUtc + 1);
    }

    private static FileSystemPath Path(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
