using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the Windows local file-operation adapter against a test-owned temporary root.</summary>
[TestClass]
public sealed class WindowsLocalFileOperationAdapterTests
{
    /// <summary>Proves inspection reports a stable identity and permanent-only deletion for a file.</summary>
    [TestMethod]
    public async Task InspectAsyncWhenEntryIsFileReportsStableIdentityAndPermanentOnly()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath file = ParsePath(root.WriteFile("a.txt", "abc"));
        WindowsLocalFileOperationAdapter adapter = new();

        FileEntrySnapshot first = await InspectAsync(adapter, file);
        FileEntrySnapshot again = await InspectAsync(adapter, file);

        Assert.AreEqual(first.Identity, again.Identity);
        Assert.AreSame(DeletionCapability.PermanentOnly, first.DeletionCapability);
        Assert.AreSame(file, first.Path);
        Assert.StartsWith("windows-v2|", first.Identity.Value);
        Assert.Contains("|file|3|", first.Identity.Value);
    }

    /// <summary>Proves inspection describes a directory with its own kind.</summary>
    [TestMethod]
    public async Task InspectAsyncWhenEntryIsDirectoryReportsDirectoryIdentity()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath directory = ParsePath(root.CreateDirectory("docs"));
        WindowsLocalFileOperationAdapter adapter = new();

        FileEntrySnapshot snapshot = await InspectAsync(adapter, directory);

        Assert.Contains("|directory|0|", snapshot.Identity.Value);
    }

    /// <summary>Proves a rewritten file changes its identity.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task InspectAsyncWhenFileIsRewrittenIdentityChanges()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string path = root.WriteFile("a.txt", "abc");
        WindowsLocalFileOperationAdapter adapter = new();
        FileEntrySnapshot before = await InspectAsync(adapter, ParsePath(path));

        _ = root.WriteFile("a.txt", "abcdef");
        FileEntrySnapshot after = await InspectAsync(adapter, ParsePath(path));

        Assert.AreNotEqual(before.Identity, after.Identity);
    }

    /// <summary>Proves a different entry cannot impersonate a snapshot by preserving its metadata tuple.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task InspectAndPreflightWhenFileIsReplacedWithMatchingMetadataRejectReplacement()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string path = root.WriteFile("a.txt", "first");
        FileSystemPath destination = ParsePath(root.CreateDirectory("dest"));
        WindowsLocalFileOperationAdapter adapter = new();
        FileEntrySnapshot before = await InspectAsync(adapter, ParsePath(path));

        root.ReplaceFilePreservingMetadata("a.txt", "other");
        FileEntrySnapshot after = await InspectAsync(adapter, ParsePath(path));
        TransferPreflightOutcome outcome = await adapter.PreflightTransferAsync(
            [before],
            destination,
            CancellationToken.None);

        Assert.AreNotEqual(before.Identity, after.Identity);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
    }

    /// <summary>Proves missing entries and foreign providers are closed failures.</summary>
    [TestMethod]
    public async Task InspectAsyncWhenEntryIsMissingOrNotWindowsLocalFailsClosed()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();

        FileInspectionOutcome missing = await adapter.InspectAsync(ParsePath(root.Resolve("missing")), CancellationToken.None);
        FileInspectionOutcome wsl = await adapter.InspectAsync(ParsePath("\\\\wsl.localhost\\Ubuntu\\home"), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, Assert.IsInstanceOfType<FileInspectionFailed>(missing).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, Assert.IsInstanceOfType<FileInspectionFailed>(wsl).Failure);
    }

    /// <summary>Proves preflight validates the destination before any source.</summary>
    [TestMethod]
    public async Task PreflightTransferAsyncWhenDestinationIsMissingOrForeignFailsClosed()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileEntrySnapshot source = await InspectAsync(adapter, ParsePath(root.WriteFile("a.txt", "abc")));

        TransferPreflightOutcome missing = await adapter.PreflightTransferAsync([source], ParsePath(root.Resolve("missing")), CancellationToken.None);
        TransferPreflightOutcome unc = await adapter.PreflightTransferAsync([source], ParsePath("\\\\server\\share"), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, missing.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, unc.Failure);
    }

    /// <summary>Proves collisions and self-containment are conflicts, and a clean batch succeeds.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task PreflightTransferAsyncWhenTargetCollidesOrDestinationIsInsideSourceReturnsConflict()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileSystemPath destination = ParsePath(root.CreateDirectory("dest"));
        FileEntrySnapshot file = await InspectAsync(adapter, ParsePath(root.WriteFile("a.txt", "abc")));
        _ = root.CreateDirectory("tree");
        _ = root.CreateDirectory("tree\\inner");
        _ = root.WriteFile("dest\\A.TXT", "x");
        FileEntrySnapshot folder = await InspectAsync(adapter, ParsePath(root.Resolve("tree")));

        TransferPreflightOutcome collision = await adapter.PreflightTransferAsync([file], destination, CancellationToken.None);
        TransferPreflightOutcome intoItself = await adapter.PreflightTransferAsync(
            [folder],
            ParsePath(root.Resolve("tree\\inner")),
            CancellationToken.None);
        File.Delete(root.Resolve("dest\\A.TXT"));
        TransferPreflightOutcome clean = await adapter.PreflightTransferAsync([file, folder], destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Conflict, collision.Failure);
        Assert.AreSame(FileOperationFailureKind.Conflict, intoItself.Failure);
        Assert.IsNull(clean.Failure);
    }

    /// <summary>Proves a source changed after inspection stops preflight.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task PreflightTransferAsyncWhenSourceChangedAfterInspectionReturnsIdentityChanged()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileSystemPath destination = ParsePath(root.CreateDirectory("dest"));
        FileEntrySnapshot source = await InspectAsync(adapter, ParsePath(root.WriteFile("a.txt", "abc")));
        _ = root.WriteFile("a.txt", "abcdef");

        TransferPreflightOutcome outcome = await adapter.PreflightTransferAsync([source], destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
    }

    /// <summary>Proves a directory is created beneath a revalidated directory location and a second attempt is a conflict.</summary>
    [TestMethod]
    public async Task CreateDirectoryAsyncWhenLocationIsDirectoryCreatesOnceThenConflicts()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileEntrySnapshot location = await InspectAsync(adapter, ParsePath(root.CreateDirectory("parent")));
        FileSystemPath target = ParsePath(root.Resolve("parent\\child"));

        ProviderStepOutcome first = await adapter.CreateDirectoryAsync(location, target, CancellationToken.None);
        FileEntrySnapshot relocated = await InspectAsync(adapter, location.Path);
        ProviderStepOutcome second = await adapter.CreateDirectoryAsync(relocated, target, CancellationToken.None);

        Assert.IsNull(first.Failure);
        Assert.IsTrue(Directory.Exists(root.Resolve("parent\\child")));
        Assert.AreSame(FileOperationFailureKind.Conflict, second.Failure);
    }

    /// <summary>Proves a file location, a junction location, a foreign target, and a target outside the location each create nothing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public async Task CreateDirectoryAsyncWhenLocationOrTargetIsUnsafeFailsClosedWithoutWriting()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileEntrySnapshot file = await InspectAsync(adapter, ParsePath(root.WriteFile("file.txt", "x")));
        _ = root.CreateDirectory("real");
        FileEntrySnapshot junction = await InspectAsync(adapter, ParsePath(root.CreateJunction("link", "real")));
        FileEntrySnapshot parent = await InspectAsync(adapter, ParsePath(root.CreateDirectory("parent")));

        ProviderStepOutcome underFile = await adapter.CreateDirectoryAsync(file, ParsePath(root.Resolve("file.txt\\child")), CancellationToken.None);
        ProviderStepOutcome underJunction = await adapter.CreateDirectoryAsync(junction, ParsePath(root.Resolve("link\\child")), CancellationToken.None);
        ProviderStepOutcome foreign = await adapter.CreateDirectoryAsync(parent, ParsePath("\\\\server\\share\\child"), CancellationToken.None);
        ProviderStepOutcome outside = await adapter.CreateDirectoryAsync(parent, ParsePath(root.Resolve("outside")), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, underFile.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, underJunction.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, foreign.Failure);
        Assert.AreSame(FileOperationFailureKind.Inspection, outside.Failure);
        Assert.IsFalse(Directory.Exists(root.Resolve("real\\child")));
        Assert.IsFalse(Directory.Exists(root.Resolve("outside")));
    }

    /// <summary>Proves a file and a directory are each renamed inside their own parent.</summary>
    [TestMethod]
    public async Task RenameAsyncWhenEntryIsFileOrDirectoryRenamesInsideTheParent()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileEntrySnapshot file = await InspectAsync(adapter, ParsePath(root.WriteFile("before.txt", "abc")));
        FileEntrySnapshot directory = await InspectAsync(adapter, ParsePath(root.CreateDirectory("docs")));

        ProviderStepOutcome renamedFile = await adapter.RenameAsync(file, ParsePath(root.Resolve("after.txt")), CancellationToken.None);
        ProviderStepOutcome renamedDirectory = await adapter.RenameAsync(directory, ParsePath(root.Resolve("papers")), CancellationToken.None);

        Assert.IsNull(renamedFile.Failure);
        Assert.IsNull(renamedDirectory.Failure);
        Assert.IsFalse(File.Exists(root.Resolve("before.txt")));
        Assert.AreEqual("abc", File.ReadAllText(root.Resolve("after.txt")));
        Assert.IsFalse(Directory.Exists(root.Resolve("docs")));
        Assert.IsTrue(Directory.Exists(root.Resolve("papers")));
    }

    /// <summary>Proves a rename that only changes letter case reaches the filesystem instead of being read as a conflict.</summary>
    [TestMethod]
    public async Task RenameAsyncWhenOnlyCaseChangesRenamesToTheNewCase()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileEntrySnapshot file = await InspectAsync(adapter, ParsePath(root.WriteFile("notes.txt", "abc")));

        ProviderStepOutcome outcome = await adapter.RenameAsync(file, ParsePath(root.Resolve("Notes.TXT")), CancellationToken.None);

        Assert.IsNull(outcome.Failure);
        Assert.AreEqual("Notes.TXT", new DirectoryInfo(root.Path.CanonicalText).GetFiles()[0].Name);
    }

    /// <summary>Proves an existing sibling, a changed identity, and a vanished entry each rename nothing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task RenameAsyncWhenTargetIsTakenOrSourceChangedFailsClosed()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileEntrySnapshot taken = await InspectAsync(adapter, ParsePath(root.WriteFile("a.txt", "abc")));
        _ = root.WriteFile("b.txt", "keep");
        FileEntrySnapshot rewritten = await InspectAsync(adapter, ParsePath(root.WriteFile("c.txt", "abc")));
        FileEntrySnapshot vanished = await InspectAsync(adapter, ParsePath(root.WriteFile("d.txt", "abc")));
        _ = root.WriteFile("c.txt", "abcdef");
        File.Delete(root.Resolve("d.txt"));

        ProviderStepOutcome conflict = await adapter.RenameAsync(taken, ParsePath(root.Resolve("b.txt")), CancellationToken.None);
        ProviderStepOutcome changed = await adapter.RenameAsync(rewritten, ParsePath(root.Resolve("e.txt")), CancellationToken.None);
        ProviderStepOutcome missing = await adapter.RenameAsync(vanished, ParsePath(root.Resolve("f.txt")), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Conflict, conflict.Failure);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, changed.Failure);
        Assert.AreSame(FileOperationFailureKind.NotFound, missing.Failure);
        Assert.AreEqual("keep", File.ReadAllText(root.Resolve("b.txt")));
        Assert.IsTrue(File.Exists(root.Resolve("a.txt")));
        Assert.IsFalse(File.Exists(root.Resolve("e.txt")));
    }

    /// <summary>Proves a foreign target, a target in another parent, and a rootless target each rename nothing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public async Task RenameAsyncWhenTargetLeavesTheParentFailsClosedWithoutWriting()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        _ = root.CreateDirectory("parent");
        _ = root.CreateDirectory("other");
        FileEntrySnapshot child = await InspectAsync(adapter, ParsePath(root.WriteFile("parent\\a.txt", "abc")));

        ProviderStepOutcome foreign = await adapter.RenameAsync(child, ParsePath("\\\\server\\share\\a.txt"), CancellationToken.None);
        ProviderStepOutcome elsewhere = await adapter.RenameAsync(child, ParsePath(root.Resolve("other\\a.txt")), CancellationToken.None);
        ProviderStepOutcome rootless = await adapter.RenameAsync(child, ParsePath("C:\\"), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, foreign.Failure);
        Assert.AreSame(FileOperationFailureKind.Inspection, elsewhere.Failure);
        Assert.AreSame(FileOperationFailureKind.Inspection, rootless.Failure);
        Assert.IsTrue(File.Exists(root.Resolve("parent\\a.txt")));
        Assert.IsFalse(File.Exists(root.Resolve("other\\a.txt")));
    }

    /// <summary>Proves file and directory trees are copied and verified beneath the destination.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenSourceIsFileOrTreeCopiesAndVerifies()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileSystemPath destination = ParsePath(root.CreateDirectory("dest"));
        _ = root.CreateDirectory("tree");
        _ = root.CreateDirectory("tree\\inner");
        _ = root.WriteFile("tree\\inner\\deep.txt", "deep");
        _ = root.WriteFile("tree\\top.txt", "top");
        FileEntrySnapshot file = await InspectAsync(adapter, ParsePath(root.WriteFile("a.txt", "abc")));
        FileEntrySnapshot tree = await InspectAsync(adapter, ParsePath(root.Resolve("tree")));

        ProviderStepOutcome fileCopy = await adapter.CopyAsync(file, destination, CancellationToken.None);
        ProviderStepOutcome treeCopy = await adapter.CopyAsync(tree, destination, CancellationToken.None);
        ProviderStepOutcome fileVerify = await adapter.VerifyCopyAsync(file, destination, CancellationToken.None);
        ProviderStepOutcome treeVerify = await adapter.VerifyCopyAsync(tree, destination, CancellationToken.None);

        Assert.IsNull(fileCopy.Failure);
        Assert.IsNull(treeCopy.Failure);
        Assert.IsNull(fileVerify.Failure);
        Assert.IsNull(treeVerify.Failure);
        Assert.AreEqual("abc", File.ReadAllText(root.Resolve("dest\\a.txt")));
        Assert.AreEqual("deep", File.ReadAllText(root.Resolve("dest\\tree\\inner\\deep.txt")));
        Assert.AreEqual("top", File.ReadAllText(root.Resolve("dest\\tree\\top.txt")));
        Assert.IsTrue(File.Exists(root.Resolve("a.txt")));
    }

    /// <summary>Proves a tree that contains a junction is refused before anything is written.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public async Task CopyAsyncWhenTreeContainsJunctionFailsClosedWithoutWriting()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileSystemPath destination = ParsePath(root.CreateDirectory("dest"));
        _ = root.CreateDirectory("outside");
        _ = root.WriteFile("outside\\secret.txt", "secret");
        _ = root.CreateDirectory("tree");
        _ = root.WriteFile("tree\\top.txt", "top");
        _ = root.CreateJunction("tree\\link", "outside");
        FileEntrySnapshot tree = await InspectAsync(adapter, ParsePath(root.Resolve("tree")));
        FileEntrySnapshot link = await InspectAsync(adapter, ParsePath(root.Resolve("tree\\link")));

        ProviderStepOutcome treeCopy = await adapter.CopyAsync(tree, destination, CancellationToken.None);
        ProviderStepOutcome linkCopy = await adapter.CopyAsync(link, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, treeCopy.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, linkCopy.Failure);
        Assert.IsFalse(Directory.Exists(root.Resolve("dest\\tree")));
        Assert.IsFalse(Directory.Exists(root.Resolve("dest\\link")));
        Assert.IsTrue(File.Exists(root.Resolve("outside\\secret.txt")));
    }

    /// <summary>Proves copy refuses an existing target, a changed source, and a foreign destination.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenTargetExistsOrSourceChangedOrDestinationIsForeignFailsClosed()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileSystemPath destination = ParsePath(root.CreateDirectory("dest"));
        FileEntrySnapshot file = await InspectAsync(adapter, ParsePath(root.WriteFile("a.txt", "abc")));
        _ = root.WriteFile("dest\\a.txt", "existing");
        FileEntrySnapshot changed = await InspectAsync(adapter, ParsePath(root.WriteFile("b.txt", "abc")));
        _ = root.WriteFile("b.txt", "abcdef");

        ProviderStepOutcome collision = await adapter.CopyAsync(file, destination, CancellationToken.None);
        ProviderStepOutcome identity = await adapter.CopyAsync(changed, destination, CancellationToken.None);
        ProviderStepOutcome foreign = await adapter.CopyAsync(file, ParsePath("\\\\server\\share"), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Conflict, collision.Failure);
        Assert.AreEqual("existing", File.ReadAllText(root.Resolve("dest\\a.txt")));
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, identity.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, foreign.Failure);
    }

    /// <summary>Proves verification detects a damaged or missing copy and a changed source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task VerifyCopyAsyncWhenCopyIsDamagedOrSourceChangedFailsClosed()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileSystemPath destination = ParsePath(root.CreateDirectory("dest"));
        FileEntrySnapshot file = await InspectAsync(adapter, ParsePath(root.WriteFile("a.txt", "abc")));
        FileEntrySnapshot never = await InspectAsync(adapter, ParsePath(root.WriteFile("never.txt", "abc")));
        _ = root.CreateDirectory("tree");
        _ = root.WriteFile("tree\\top.txt", "top");
        FileEntrySnapshot tree = await InspectAsync(adapter, ParsePath(root.Resolve("tree")));
        _ = await adapter.CopyAsync(file, destination, CancellationToken.None);
        _ = await adapter.CopyAsync(tree, destination, CancellationToken.None);
        _ = root.WriteFile("dest\\a.txt", "ab");
        File.Delete(root.Resolve("dest\\tree\\top.txt"));

        ProviderStepOutcome truncated = await adapter.VerifyCopyAsync(file, destination, CancellationToken.None);
        ProviderStepOutcome missingTarget = await adapter.VerifyCopyAsync(never, destination, CancellationToken.None);
        ProviderStepOutcome missingChild = await adapter.VerifyCopyAsync(tree, destination, CancellationToken.None);
        _ = root.WriteFile("a.txt", "changed source");
        ProviderStepOutcome changed = await adapter.VerifyCopyAsync(file, destination, CancellationToken.None);
        ProviderStepOutcome foreign = await adapter.VerifyCopyAsync(file, ParsePath("\\\\server\\share"), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Verification, truncated.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, missingTarget.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, missingChild.Failure);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, changed.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, foreign.Failure);
    }

    /// <summary>Proves verification reports a target of the wrong kind or with extra entries.</summary>
    [TestMethod]
    public async Task VerifyCopyAsyncWhenTargetKindOrEntrySetDiffersReturnsVerification()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileSystemPath destination = ParsePath(root.CreateDirectory("dest"));
        _ = root.CreateDirectory("tree");
        FileEntrySnapshot tree = await InspectAsync(adapter, ParsePath(root.Resolve("tree")));
        FileEntrySnapshot file = await InspectAsync(adapter, ParsePath(root.WriteFile("same", "abc")));
        _ = root.CreateDirectory("dest\\same");
        _ = root.WriteFile("dest\\tree", "not a directory");

        ProviderStepOutcome fileAgainstDirectory = await adapter.VerifyCopyAsync(file, destination, CancellationToken.None);
        ProviderStepOutcome directoryAgainstFile = await adapter.VerifyCopyAsync(tree, destination, CancellationToken.None);
        File.Delete(root.Resolve("dest\\tree"));
        _ = root.CreateDirectory("dest\\tree");
        _ = root.WriteFile("dest\\tree\\extra.txt", "extra");
        ProviderStepOutcome extraEntry = await adapter.VerifyCopyAsync(tree, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Verification, fileAgainstDirectory.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, directoryAgainstFile.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, extraEntry.Failure);
    }

    /// <summary>Proves permanent deletion removes files and trees and recycle is refused.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task DeleteAsyncWhenModeIsPermanentDeletesAndRecycleIsRefused()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileEntrySnapshot file = await InspectAsync(adapter, ParsePath(root.WriteFile("a.txt", "abc")));
        _ = root.CreateDirectory("tree");
        _ = root.WriteFile("tree\\top.txt", "top");
        FileEntrySnapshot tree = await InspectAsync(adapter, ParsePath(root.Resolve("tree")));

        ProviderStepOutcome recycle = await adapter.DeleteAsync(file, DeletionExecutionMode.Recycle, CancellationToken.None);
        ProviderStepOutcome fileDelete = await adapter.DeleteAsync(file, DeletionExecutionMode.Permanent, CancellationToken.None);
        ProviderStepOutcome treeDelete = await adapter.DeleteAsync(tree, DeletionExecutionMode.Permanent, CancellationToken.None);
        ProviderStepOutcome again = await adapter.DeleteAsync(file, DeletionExecutionMode.Permanent, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, recycle.Failure);
        Assert.IsNull(fileDelete.Failure);
        Assert.IsNull(treeDelete.Failure);
        Assert.IsFalse(File.Exists(root.Resolve("a.txt")));
        Assert.IsFalse(Directory.Exists(root.Resolve("tree")));
        Assert.AreSame(FileOperationFailureKind.NotFound, again.Failure);
    }

    /// <summary>Proves deletion never acts on an entry that changed after preflight.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task DeleteAsyncWhenSourceChangedAfterInspectionKeepsEntry()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileEntrySnapshot file = await InspectAsync(adapter, ParsePath(root.WriteFile("a.txt", "abc")));
        _ = root.WriteFile("a.txt", "rewritten");

        ProviderStepOutcome outcome = await adapter.DeleteAsync(file, DeletionExecutionMode.Permanent, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.AreEqual("rewritten", File.ReadAllText(root.Resolve("a.txt")));
    }

    /// <summary>Proves a denied source tree is a normalized access failure that writes nothing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-017")]
    public async Task CopyAsyncWhenSourceListingIsDeniedReturnsAccessDenied()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileSystemPath destination = ParsePath(root.CreateDirectory("dest"));
        FileEntrySnapshot denied = await InspectAsync(adapter, ParsePath(root.DenyDirectoryListing("denied")));

        ProviderStepOutcome outcome = await adapter.CopyAsync(denied, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.AccessDenied, outcome.Failure);
        Assert.IsFalse(Directory.Exists(root.Resolve("dest\\denied")));
    }

    /// <summary>Proves a tree copy failure after target creation reports the filesystem change.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenTreeCopyFailsAfterTargetCreationReportsPartialEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath destination = ParsePath(root.CreateDirectory("dest"));
        FileSystemPath source = ParsePath(root.CreateDirectory("tree"));
        string lockedPath = root.WriteFile("tree\\locked.txt", "content");
        using FileStream locked = new(lockedPath, FileMode.Open, FileAccess.Read, FileShare.None);
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter());
        CopyRequest request = Assert.IsInstanceOfType<CopyRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(CopyRequest.Create([source], destination)).Request);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            request,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Copy, outcome.Failure);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.CopyTargetCreated, outcome.Effects[0].Kind);
        Assert.AreSame(source, outcome.Effects[0].Source);
        Assert.IsTrue(Directory.Exists(root.Resolve("dest\\tree")));
        Assert.IsFalse(File.Exists(root.Resolve("dest\\tree\\locked.txt")));
    }

    /// <summary>Proves a copy failure before target creation reports no completed effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task CopyAsyncWhenLockedFileFailsBeforeTargetCreationReportsNoEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileSystemPath destination = ParsePath(root.CreateDirectory("dest"));
        string sourcePath = root.WriteFile("locked.txt", "content");
        FileEntrySnapshot source = await InspectAsync(adapter, ParsePath(sourcePath));
        using FileStream locked = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.None);

        ProviderStepOutcome outcome = await adapter.CopyAsync(source, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Copy, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.IsFalse(File.Exists(root.Resolve("dest\\locked.txt")));
    }

    /// <summary>Proves a same-volume gateway move is one atomic provider effect rather than a composite copy.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenGatewayMovesFileThroughAdapterSourceIsGoneAndTargetIsVerified()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath destination = ParsePath(root.CreateDirectory("dest"));
        FileSystemPath source = ParsePath(root.WriteFile("a.txt", "abc"));
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter());
        MoveRequest request = Assert.IsInstanceOfType<MoveRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(MoveRequest.Create([source], destination)).Request);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.AtomicallyMoved, outcome.Effects[0].Kind);
        Assert.IsFalse(File.Exists(root.Resolve("a.txt")));
        Assert.AreEqual("abc", File.ReadAllText(root.Resolve("dest\\a.txt")));
    }

    /// <summary>Proves same-volume files and directories report atomic capability and move without overwrite.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task MoveAsyncWhenSourceAndDestinationShareVolumeMovesEntryAtomically()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileSystemPath destination = ParsePath(root.CreateDirectory("dest"));
        FileSystemPath filePath = ParsePath(root.WriteFile("a.txt", "abc"));
        FileSystemPath directoryPath = ParsePath(root.CreateDirectory("tree"));
        _ = root.WriteFile("tree\\child.txt", "child");
        FileEntrySnapshot file = await InspectAsync(adapter, filePath);
        FileEntrySnapshot directory = await InspectAsync(adapter, directoryPath);

        AtomicMoveCapabilityOutcome fileCapability = await adapter.GetAtomicMoveCapabilityAsync(
            file,
            destination,
            CancellationToken.None);
        AtomicMoveCapabilityOutcome directoryCapability = await adapter.GetAtomicMoveCapabilityAsync(
            directory,
            destination,
            CancellationToken.None);
        ProviderStepOutcome fileMove = await adapter.MoveAsync(file, destination, CancellationToken.None);
        ProviderStepOutcome directoryMove = await adapter.MoveAsync(directory, destination, CancellationToken.None);

        Assert.AreSame(AtomicMoveCapabilityOutcome.Supported, fileCapability);
        Assert.AreSame(AtomicMoveCapabilityOutcome.Supported, directoryCapability);
        Assert.IsNull(fileMove.Failure);
        Assert.IsNull(directoryMove.Failure);
        Assert.AreEqual("abc", File.ReadAllText(root.Resolve("dest\\a.txt")));
        Assert.AreEqual("child", File.ReadAllText(root.Resolve("dest\\tree\\child.txt")));
    }

    /// <summary>Proves the atomic step revalidates source identity and a late target collision before moving.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task MoveAsyncWhenIdentityOrTargetChangesMovesNothing()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileSystemPath destination = ParsePath(root.CreateDirectory("dest"));
        FileSystemPath changedPath = ParsePath(root.WriteFile("changed.txt", "before"));
        FileEntrySnapshot changed = await InspectAsync(adapter, changedPath);
        _ = root.WriteFile("changed.txt", "after");
        FileSystemPath collisionPath = ParsePath(root.WriteFile("collision.txt", "source"));
        FileEntrySnapshot collision = await InspectAsync(adapter, collisionPath);
        _ = root.WriteFile("dest\\collision.txt", "target");

        ProviderStepOutcome changedMove = await adapter.MoveAsync(changed, destination, CancellationToken.None);
        ProviderStepOutcome collisionMove = await adapter.MoveAsync(collision, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, changedMove.Failure);
        Assert.AreSame(FileOperationFailureKind.Conflict, collisionMove.Failure);
        Assert.AreEqual("after", File.ReadAllText(root.Resolve("changed.txt")));
        Assert.AreEqual("source", File.ReadAllText(root.Resolve("collision.txt")));
        Assert.AreEqual("target", File.ReadAllText(root.Resolve("dest\\collision.txt")));
    }

    /// <summary>Proves a reparse source and destination never advertise atomic-move capability.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public async Task GetAtomicMoveCapabilityAsyncWhenPathIsReparsePointReportsUnsupported()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileSystemPath sourceTarget = ParsePath(root.CreateDirectory("source-target"));
        FileSystemPath sourceLink = ParsePath(root.CreateJunction("source-link", "source-target"));
        FileSystemPath destinationTarget = ParsePath(root.CreateDirectory("destination-target"));
        FileSystemPath destinationLink = ParsePath(root.CreateJunction("destination-link", "destination-target"));
        FileEntrySnapshot linkedSource = await InspectAsync(adapter, sourceLink);
        FileEntrySnapshot plainSource = await InspectAsync(adapter, sourceTarget);

        AtomicMoveCapabilityOutcome sourceCapability = await adapter.GetAtomicMoveCapabilityAsync(
            linkedSource,
            destinationTarget,
            CancellationToken.None);
        AtomicMoveCapabilityOutcome destinationCapability = await adapter.GetAtomicMoveCapabilityAsync(
            plainSource,
            destinationLink,
            CancellationToken.None);

        Assert.AreSame(AtomicMoveCapabilityOutcome.Unsupported, sourceCapability);
        Assert.AreSame(AtomicMoveCapabilityOutcome.Unsupported, destinationCapability);
    }

    /// <summary>Proves missing destinations fail capability discovery before a move can start.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task GetAtomicMoveCapabilityAsyncWhenDestinationIsMissingReportsNotFound()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileEntrySnapshot source = await InspectAsync(adapter, ParsePath(root.WriteFile("source.txt", "source")));
        FileSystemPath missingDestination = ParsePath(root.Resolve("missing"));

        AtomicMoveCapabilityOutcome capability = await adapter.GetAtomicMoveCapabilityAsync(
            source,
            missingDestination,
            CancellationToken.None);

        AtomicMoveCapabilityFailed failed = Assert.IsInstanceOfType<AtomicMoveCapabilityFailed>(capability);
        Assert.AreSame(FileOperationFailureKind.NotFound, failed.Failure);
        Assert.AreEqual("source", File.ReadAllText(root.Resolve("source.txt")));
    }

    /// <summary>Proves the atomic effect itself rejects reparse sources and destinations without mutation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public async Task MoveAsyncWhenPathIsReparsePointReportsProviderUnavailable()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        _ = root.CreateDirectory("source-target");
        FileSystemPath sourceLink = ParsePath(root.CreateJunction("source-link", "source-target"));
        FileSystemPath destinationTarget = ParsePath(root.CreateDirectory("destination-target"));
        FileSystemPath destinationLink = ParsePath(root.CreateJunction("destination-link", "destination-target"));
        FileEntrySnapshot linkedSource = await InspectAsync(adapter, sourceLink);
        FileSystemPath plainPath = ParsePath(root.WriteFile("plain.txt", "plain"));
        FileEntrySnapshot plainSource = await InspectAsync(adapter, plainPath);

        ProviderStepOutcome sourceOutcome = await adapter.MoveAsync(
            linkedSource,
            destinationTarget,
            CancellationToken.None);
        ProviderStepOutcome destinationOutcome = await adapter.MoveAsync(
            plainSource,
            destinationLink,
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, sourceOutcome.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, destinationOutcome.Failure);
        Assert.IsTrue(Directory.Exists(root.Resolve("source-link")));
        Assert.AreEqual("plain", File.ReadAllText(root.Resolve("plain.txt")));
    }

    /// <summary>Proves the gateway refuses unconfirmed permanent deletion through the real adapter.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task ExecuteAsyncWhenGatewayDeletesThroughAdapterRequiresConfirmation()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath source = ParsePath(root.WriteFile("a.txt", "abc"));
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter());
        DeleteRequest unconfirmed = Assert.IsInstanceOfType<DeleteRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(DeleteRequest.Create([source], null)).Request);
        DeleteRequest confirmed = Assert.IsInstanceOfType<DeleteRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                DeleteRequest.Create([source], PermanentDeletionConfirmation.CreateFor(unconfirmed))).Request);

        FileOperationOutcome refused = await gateway.ExecuteAsync(unconfirmed, IgnoredFileOperationProgress.Create(), CancellationToken.None);
        FileOperationOutcome deleted = await gateway.ExecuteAsync(confirmed, IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ConfirmationRequired, refused.Failure);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, deleted.Completion);
        Assert.AreSame(FileOperationEffectKind.PermanentlyDeleted, deleted.Effects[0].Kind);
        Assert.IsFalse(File.Exists(root.Resolve("a.txt")));
    }

    /// <summary>Proves a destination that is a file is a normalized not-found failure that copies nothing.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenDestinationIsFileReturnsNotFound()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new();
        FileEntrySnapshot file = await InspectAsync(adapter, ParsePath(root.WriteFile("a.txt", "abc")));
        FileSystemPath destinationFile = ParsePath(root.WriteFile("destfile", "x"));

        ProviderStepOutcome outcome = await adapter.CopyAsync(file, destinationFile, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, outcome.Failure);
        Assert.AreEqual("x", File.ReadAllText(root.Resolve("destfile")));
    }

    /// <summary>Proves an unknown platform failure falls back to the step's own failure kind.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-017")]
    public void NormalizeWhenHResultIsUnknownFallsBackToStepKind()
    {
        Assert.AreSame(
            FileOperationFailureKind.Copy,
            WindowsLocalFileOperationAdapter.Normalize(unchecked((int)0x81234567), FileOperationFailureKind.Copy));
        Assert.AreSame(
            FileOperationFailureKind.AccessDenied,
            WindowsLocalFileOperationAdapter.Normalize(unchecked((int)0x80070005), FileOperationFailureKind.Copy));
    }

    /// <summary>Proves the tree copier never overwrites an existing target file.</summary>
    [TestMethod]
    public void CopyWhenTargetFileExistsThrowsIOException()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileInfo source = new(root.WriteFile("a.txt", "abc"));
        string target = root.WriteFile("b.txt", "keep");

        _ = Assert.ThrowsExactly<IOException>(() => WindowsLocalTreeCopy.Copy(source, target));

        Assert.AreEqual("keep", File.ReadAllText(target));
    }

    /// <summary>Proves the shared target resolver keeps a direct name beneath its exact parent.</summary>
    [TestMethod]
    public void ResolveDirectChildWhenNameIsOneSegmentReturnsContainedPath()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();

        string resolved = WindowsLocalTreeCopy.ResolveDirectChild(root.Path.CanonicalText, "child.txt");

        Assert.AreEqual(root.Resolve("child.txt"), resolved, ignoreCase: true);
    }

    /// <summary>Proves rooted and multi-segment names cannot replace or escape the target parent.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-009")]
    [DataRow("")]
    [DataRow(".")]
    [DataRow("..")]
    [DataRow("..\\escape.txt")]
    [DataRow("nested\\escape.txt")]
    [DataRow("C:\\escape.txt")]
    public void ResolveDirectChildWhenNameIsNotOneSegmentThrowsArgumentException(string childName)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();

        ArgumentException failure = Assert.ThrowsExactly<ArgumentException>(
            () => WindowsLocalTreeCopy.ResolveDirectChild(root.Path.CanonicalText, childName));
        Assert.AreEqual("childName", failure.ParamName);
        Assert.StartsWith("A direct child name must contain exactly one path segment.", failure.Message);
    }

    /// <summary>Proves each required direct-child resolver argument is rejected at its own boundary.</summary>
    [TestMethod]
    public void ResolveDirectChildWhenArgumentIsNullThrowsNamedArgumentNullException()
    {
        MethodInfo method = typeof(WindowsLocalTreeCopy).GetMethod(
            "ResolveDirectChild",
            BindingFlags.Static | BindingFlags.NonPublic) ??
            throw new AssertFailedException("The direct-child resolver was not found.");

        AssertResolverNullGuard(method, [null, "child.txt"], "parentText");
        AssertResolverNullGuard(method, ["C:\\parent", null], "childName");
    }

    /// <summary>Proves every child must match, rather than only one child, when trees are compared.</summary>
    [TestMethod]
    public void MatchesWhenOneOfTwoChildrenDiffersReturnsFalse()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourceText = root.CreateDirectory("source");
        _ = root.WriteFile("source\\a.txt", "same");
        _ = root.WriteFile("source\\b.txt", "source");
        string targetText = root.CreateDirectory("target");
        _ = root.WriteFile("target\\a.txt", "same");
        _ = root.WriteFile("target\\b.txt", "different");

        Assert.IsFalse(WindowsLocalTreeCopy.Matches(new DirectoryInfo(sourceText), targetText));
    }

    /// <summary>Proves every port method rejects an absent argument before touching the filesystem.</summary>
    [TestMethod]
    public void PortMethodsWhenRequiredArgumentIsNullThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsLocalFileOperationAdapter(null!));
        WindowsLocalFileOperationAdapter adapter = new();
        FileSystemPath path = ParsePath("C:\\source");
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("file|0|0|0")).Identity;
        FileEntrySnapshot snapshot = FileEntrySnapshot.Create(path, identity, DeletionCapability.PermanentOnly);

        AssertNullGuard(adapter, nameof(IFileOperationPort.InspectAsync), [null, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.PreflightTransferAsync), [null, path, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.PreflightTransferAsync), [new[] { snapshot }, null, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.CopyAsync), [null, path, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.CopyAsync), [snapshot, null, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.GetAtomicMoveCapabilityAsync), [null, path, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.GetAtomicMoveCapabilityAsync), [snapshot, null, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.MoveAsync), [null, path, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.MoveAsync), [snapshot, null, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.VerifyCopyAsync), [null, path, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.VerifyCopyAsync), [snapshot, null, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.DeleteAsync), [null, DeletionExecutionMode.Permanent, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.DeleteAsync), [snapshot, null, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.CreateDirectoryAsync), [null, path, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.CreateDirectoryAsync), [snapshot, null, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.RenameAsync), [null, path, CancellationToken.None]);
        AssertNullGuard(adapter, nameof(IFileOperationPort.RenameAsync), [snapshot, null, CancellationToken.None]);
    }

    private static void AssertNullGuard(object instance, string methodName, object?[] arguments)
    {
        MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance) ??
            throw new AssertFailedException("The port method was not found.");
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(instance, arguments));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static void AssertResolverNullGuard(MethodInfo method, object?[] arguments, string parameterName)
    {
        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, arguments));
        ArgumentNullException inner = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
        Assert.AreEqual(parameterName, inner.ParamName);
    }

    private static async Task<FileEntrySnapshot> InspectAsync(WindowsLocalFileOperationAdapter adapter, FileSystemPath path)
    {
        FileInspectionOutcome outcome = await adapter.InspectAsync(path, CancellationToken.None);
        return Assert.IsInstanceOfType<FileInspectionSucceeded>(outcome).Snapshot;
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
