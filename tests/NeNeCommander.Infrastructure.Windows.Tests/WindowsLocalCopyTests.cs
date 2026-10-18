using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves the local adapter proof for Copy.</summary>
[TestClass]
public sealed class WindowsLocalCopyTests
{
    /// <summary>Proves file and directory trees are copied and verified beneath the destination.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenSourceIsFileOrTreeCopiesAndVerifies()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("dest"));
        _ = root.CreateDirectory("tree");
        _ = root.CreateDirectory("tree\\inner");
        _ = root.WriteFile("tree\\inner\\deep.txt", "deep");
        _ = root.WriteFile("tree\\top.txt", "top");
        FileEntrySnapshot file = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc")));
        FileEntrySnapshot tree = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.Resolve("tree")));

        ProviderStepOutcome fileCopy = await adapter.CopyAsync(file, destination, CancellationToken.None);
        ProviderStepOutcome treeCopy = await adapter.CopyAsync(tree, destination, CancellationToken.None);
        CopyVerificationOutcome fileVerify = await adapter.VerifyCopyAsync(file, destination, CancellationToken.None);
        CopyVerificationOutcome treeVerify = await adapter.VerifyCopyAsync(tree, destination, CancellationToken.None);

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
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("dest"));
        _ = root.CreateDirectory("outside");
        _ = root.WriteFile("outside\\secret.txt", "secret");
        _ = root.CreateDirectory("tree");
        _ = root.WriteFile("tree\\top.txt", "top");
        _ = root.CreateJunction("tree\\link", "outside");
        FileEntrySnapshot tree = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.Resolve("tree")));
        FileEntrySnapshot link = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.Resolve("tree\\link")));

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
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("dest"));
        FileEntrySnapshot file = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc")));
        _ = root.WriteFile("dest\\a.txt", "existing");
        FileEntrySnapshot changed = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("b.txt", "abc")));
        _ = root.WriteFile("b.txt", "abcdef");

        ProviderStepOutcome collision = await adapter.CopyAsync(file, destination, CancellationToken.None);
        ProviderStepOutcome identity = await adapter.CopyAsync(changed, destination, CancellationToken.None);
        ProviderStepOutcome foreign = await adapter.CopyAsync(file, WindowsLocalOperationTestContext.ParsePath("\\\\server\\share"), CancellationToken.None);

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
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("dest"));
        FileEntrySnapshot file = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc")));
        FileEntrySnapshot never = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("never.txt", "abc")));
        _ = root.CreateDirectory("tree");
        _ = root.WriteFile("tree\\top.txt", "top");
        FileEntrySnapshot tree = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.Resolve("tree")));
        _ = await adapter.CopyAsync(file, destination, CancellationToken.None);
        _ = await adapter.CopyAsync(tree, destination, CancellationToken.None);
        _ = root.WriteFile("dest\\a.txt", "ab");
        File.Delete(root.Resolve("dest\\tree\\top.txt"));

        CopyVerificationOutcome truncated = await adapter.VerifyCopyAsync(file, destination, CancellationToken.None);
        CopyVerificationOutcome missingTarget = await adapter.VerifyCopyAsync(never, destination, CancellationToken.None);
        CopyVerificationOutcome missingChild = await adapter.VerifyCopyAsync(tree, destination, CancellationToken.None);
        _ = root.WriteFile("a.txt", "changed source");
        CopyVerificationOutcome changed = await adapter.VerifyCopyAsync(file, destination, CancellationToken.None);
        CopyVerificationOutcome foreign = await adapter.VerifyCopyAsync(file, WindowsLocalOperationTestContext.ParsePath("\\\\server\\share"), CancellationToken.None);

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
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("dest"));
        _ = root.CreateDirectory("tree");
        FileEntrySnapshot tree = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.Resolve("tree")));
        FileEntrySnapshot file = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("same", "abc")));
        _ = root.CreateDirectory("dest\\same");
        _ = root.WriteFile("dest\\tree", "not a directory");

        CopyVerificationOutcome fileAgainstDirectory = await adapter.VerifyCopyAsync(file, destination, CancellationToken.None);
        CopyVerificationOutcome directoryAgainstFile = await adapter.VerifyCopyAsync(tree, destination, CancellationToken.None);
        File.Delete(root.Resolve("dest\\tree"));
        _ = root.CreateDirectory("dest\\tree");
        _ = root.WriteFile("dest\\tree\\extra.txt", "extra");
        CopyVerificationOutcome extraEntry = await adapter.VerifyCopyAsync(tree, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Verification, fileAgainstDirectory.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, directoryAgainstFile.Failure);
        Assert.AreSame(FileOperationFailureKind.Verification, extraEntry.Failure);
    }

    /// <summary>Proves a denied source tree is a normalized access failure that writes nothing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-017")]
    public async Task CopyAsyncWhenSourceListingIsDeniedReturnsAccessDenied()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("dest"));
        FileEntrySnapshot denied = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.DenyDirectoryListing("denied")));

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
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("dest"));
        FileSystemPath source = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("tree"));
        string lockedPath = root.WriteFile("tree\\locked.txt", "content");
        using FileStream locked = new(lockedPath, FileMode.Open, FileAccess.Read, FileShare.None);
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 })));
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
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("dest"));
        string sourcePath = root.WriteFile("locked.txt", "content");
        FileEntrySnapshot source = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(sourcePath));
        using FileStream locked = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.None);

        ProviderStepOutcome outcome = await adapter.CopyAsync(source, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Copy, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.IsFalse(File.Exists(root.Resolve("dest\\locked.txt")));
    }

    /// <summary>Proves a destination that is a file is a normalized not-found failure that copies nothing.</summary>
    [TestMethod]
    public async Task CopyAsyncWhenDestinationIsFileReturnsNotFound()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileEntrySnapshot file = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc")));
        FileSystemPath destinationFile = WindowsLocalOperationTestContext.ParsePath(root.WriteFile("destfile", "x"));

        ProviderStepOutcome outcome = await adapter.CopyAsync(file, destinationFile, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, outcome.Failure);
        Assert.AreEqual("x", File.ReadAllText(root.Resolve("destfile")));
    }

    /// <summary>Proves the tree copier never overwrites an existing target file.</summary>
    [TestMethod]
    public void CopyWhenTargetFileExistsReturnsConflict()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileInfo source = new(root.WriteFile("a.txt", "abc"));
        string target = root.WriteFile("b.txt", "keep");

        ProviderStepOutcome outcome = WindowsLocalTreeCopy.Copy(
            source, WindowsLocalOperationTestContext.ParsePath(target), new WindowsTreeCopyAccess(path => path.CanonicalText,
                WindowsLocalTreeCopy.InspectLocalSource, new WindowsDriveBoundary()));
        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.IsNull(outcome.Effect);

        Assert.AreEqual("keep", File.ReadAllText(target));
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

        Assert.IsNotNull(new WindowsWslFileSystem().VerifyCopy(
            FileEntrySnapshot.Create(WindowsLocalOperationTestContext.ParsePath(sourceText), WindowsLocalEntryIdentity.Describe(new DirectoryInfo(sourceText)),
                DeletionCapability.PermanentOnly), WindowsLocalOperationTestContext.ParsePath(targetText)).Failure);
    }
}
