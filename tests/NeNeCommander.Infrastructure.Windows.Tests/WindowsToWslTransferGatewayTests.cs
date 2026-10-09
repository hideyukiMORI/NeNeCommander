using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves the Windows local to WSL copy from the gateway down to real I/O (ADR-0059). One child of
/// a test-owned root is the Windows local side and another is the WSL <c>/owned</c> tree, mapped
/// through the production <see cref="WindowsWslFileSystem"/> seam.
/// </summary>
[TestClass]
public sealed class WindowsToWslTransferGatewayTests
{
    /// <summary>Proves a file and a nested tree copy with exact bytes, both verified, and the sources stay intact.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCopyingNestedTreeAndFileToWslVerifiesAndKeepsSources()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        FileSystemPath tree = ParsePath(root.CreateDirectory("windows\\tree"));
        _ = root.WriteFile("windows\\tree\\top.txt", "abcd");
        _ = root.CreateDirectory("windows\\tree\\nested");
        _ = root.WriteFile("windows\\tree\\nested\\payload.txt", "payload");
        FileSystemPath file = ParsePath(root.WriteFile("windows\\single.txt", "one"));
        _ = root.CreateDirectory("wsl\\dest");
        using FileOperationGateway gateway = CreateGateway(root);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Copy([tree, file], Wsl("/owned/dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.IsNull(outcome.Failure);
        AssertEffects(
            outcome,
            (tree, FileOperationEffectKind.Copied),
            (tree, FileOperationEffectKind.Verified),
            (file, FileOperationEffectKind.Copied),
            (file, FileOperationEffectKind.Verified));
        Assert.AreEqual("abcd", File.ReadAllText(root.Resolve("wsl\\dest\\tree\\top.txt")));
        Assert.AreEqual("payload", File.ReadAllText(root.Resolve("wsl\\dest\\tree\\nested\\payload.txt")));
        Assert.AreEqual("one", File.ReadAllText(root.Resolve("wsl\\dest\\single.txt")));
        Assert.AreEqual("abcd", File.ReadAllText(root.Resolve("windows\\tree\\top.txt")));
        Assert.AreEqual("payload", File.ReadAllText(root.Resolve("windows\\tree\\nested\\payload.txt")));
        Assert.AreEqual("one", File.ReadAllText(root.Resolve("windows\\single.txt")));
    }

    /// <summary>Proves an existing destination entry rejects the whole batch as a conflict with no choice and no write.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task ExecuteAsyncWhenWslTargetExistsRejectsBatchAsConflictWithoutEffect()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        FileSystemPath first = ParsePath(root.WriteFile("windows\\first.txt", "first"));
        FileSystemPath taken = ParsePath(root.WriteFile("windows\\taken.txt", "new"));
        _ = root.CreateDirectory("wsl\\dest");
        _ = root.WriteFile("wsl\\dest\\taken.txt", "existing");
        using FileOperationGateway gateway = CreateGateway(root);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Copy([first, taken], Wsl("/owned/dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.HasCount(0, outcome.Effects);
        Assert.IsFalse(File.Exists(root.Resolve("wsl\\dest\\first.txt")));
        Assert.AreEqual("existing", File.ReadAllText(root.Resolve("wsl\\dest\\taken.txt")));
        Assert.AreEqual("new", File.ReadAllText(root.Resolve("windows\\taken.txt")));
    }

    /// <summary>Proves two sources that derive one WSL target collide inside the batch before any write.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task ExecuteAsyncWhenTwoSourcesShareOneWslTargetRejectsAsConflictWithoutEffect()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        _ = root.CreateDirectory("windows\\a");
        _ = root.CreateDirectory("windows\\b");
        FileSystemPath first = ParsePath(root.WriteFile("windows\\a\\same.txt", "a"));
        FileSystemPath second = ParsePath(root.WriteFile("windows\\b\\same.txt", "b"));
        _ = root.CreateDirectory("wsl\\dest");
        using FileOperationGateway gateway = CreateGateway(root);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Copy([first, second], Wsl("/owned/dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.HasCount(0, outcome.Effects);
        Assert.IsFalse(File.Exists(root.Resolve("wsl\\dest\\same.txt")));
    }

    /// <summary>Proves a source tree holding a junction is refused before anything is written on either side.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task ExecuteAsyncWhenSourceTreeContainsJunctionRejectsWithZeroEffects()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        _ = root.CreateDirectory("windows\\outside");
        _ = root.WriteFile("windows\\outside\\secret.txt", "secret");
        FileSystemPath tree = ParsePath(root.CreateDirectory("windows\\tree"));
        _ = root.WriteFile("windows\\tree\\plain.txt", "plain");
        _ = root.CreateJunction("windows\\tree\\link", "windows\\outside");
        _ = root.CreateDirectory("wsl\\dest");
        using FileOperationGateway gateway = CreateGateway(root);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Copy([tree], Wsl("/owned/dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.HasCount(0, outcome.Effects);
        Assert.IsFalse(Directory.Exists(root.Resolve("wsl\\dest\\tree")));
        Assert.AreEqual("plain", File.ReadAllText(root.Resolve("windows\\tree\\plain.txt")));
        Assert.AreEqual("secret", File.ReadAllText(root.Resolve("windows\\outside\\secret.txt")));
    }

    /// <summary>Proves a tree copy that fails after creating its WSL target reports that partial effect and keeps the source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task ExecuteAsyncWhenTreeCopyFailsAfterWslTargetCreationReportsPartialEffect()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        FileSystemPath tree = ParsePath(root.CreateDirectory("windows\\tree"));
        string lockedPath = root.WriteFile("windows\\tree\\locked.txt", "content");
        _ = root.CreateDirectory("wsl\\dest");
        using FileOperationGateway gateway = CreateGateway(root);
        FileOperationOutcome outcome;
        using (FileStream locked = new(lockedPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            outcome = await gateway.ExecuteAsync(
                Copy([tree], Wsl("/owned/dest")),
                IgnoredFileOperationProgress.Create(),
                CancellationToken.None);
        }

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Copy, outcome.Failure);
        AssertEffects(outcome, (tree, FileOperationEffectKind.CopyTargetCreated));
        Assert.IsTrue(Directory.Exists(root.Resolve("wsl\\dest\\tree")));
        Assert.IsFalse(File.Exists(root.Resolve("wsl\\dest\\tree\\locked.txt")));
        Assert.AreEqual("content", File.ReadAllText(lockedPath));
    }

    /// <summary>Proves cancellation observed after a copy step stops before verification and keeps the copied effect exact.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenCancelledBetweenCopyAndVerifyReportsCopiedOnly()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        FileSystemPath first = ParsePath(root.WriteFile("windows\\first.txt", "first"));
        FileSystemPath second = ParsePath(root.WriteFile("windows\\second.txt", "second"));
        _ = root.CreateDirectory("wsl\\dest");
        using CancellationTokenSource cancellation = new();
        StepHookPort port = new(CreateRouter(root))
        {
            AfterCopy = _ => cancellation.CancelAsync(),
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Copy([first, second], Wsl("/owned/dest")),
            IgnoredFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        AssertEffects(outcome, (first, FileOperationEffectKind.Copied));
        Assert.AreEqual("first", File.ReadAllText(root.Resolve("wsl\\dest\\first.txt")));
        Assert.IsFalse(File.Exists(root.Resolve("wsl\\dest\\second.txt")));
        AssertCalls(port, "copy");
        Assert.AreEqual("first", File.ReadAllText(root.Resolve("windows\\first.txt")));
        Assert.AreEqual("second", File.ReadAllText(root.Resolve("windows\\second.txt")));
    }

    /// <summary>
    /// Proves a move across the pair is the gateway's composite: each item is copied with exact
    /// bytes, verified, and only then permanently deleted from the Windows local side, with no
    /// confirmation requested.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenMovingNestedTreeAndFileToWslDeletesSourcesAfterVerifiedTargets()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        FileSystemPath tree = ParsePath(root.CreateDirectory("windows\\tree"));
        _ = root.WriteFile("windows\\tree\\top.txt", "abcd");
        _ = root.CreateDirectory("windows\\tree\\nested");
        _ = root.WriteFile("windows\\tree\\nested\\payload.txt", "payload");
        FileSystemPath file = ParsePath(root.WriteFile("windows\\single.txt", "one"));
        _ = root.CreateDirectory("wsl\\dest");
        StepHookPort port = new(CreateRouter(root));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Move([tree, file], Wsl("/owned/dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.IsNull(outcome.Failure);
        AssertEffects(
            outcome,
            (tree, FileOperationEffectKind.Copied),
            (tree, FileOperationEffectKind.Verified),
            (tree, FileOperationEffectKind.SourceDeleted),
            (file, FileOperationEffectKind.Copied),
            (file, FileOperationEffectKind.Verified),
            (file, FileOperationEffectKind.SourceDeleted));
        AssertCalls(port, "capability", "capability", "copy", "verify", "delete", "copy", "verify", "delete");
        Assert.HasCount(2, port.DeletionModes);
        Assert.IsTrue(port.DeletionModes.TrueForAll(mode => mode == DeletionExecutionMode.Permanent));
        Assert.AreEqual("abcd", File.ReadAllText(root.Resolve("wsl\\dest\\tree\\top.txt")));
        Assert.AreEqual("payload", File.ReadAllText(root.Resolve("wsl\\dest\\tree\\nested\\payload.txt")));
        Assert.AreEqual("one", File.ReadAllText(root.Resolve("wsl\\dest\\single.txt")));
        Assert.IsFalse(Directory.Exists(root.Resolve("windows\\tree")));
        Assert.IsFalse(File.Exists(root.Resolve("windows\\single.txt")));
    }

    /// <summary>
    /// Proves a target changed between copy and verification fails that item's verification on the
    /// real bytes, so its source is never deleted, while the item already moved stays reported.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task ExecuteAsyncWhenMovedTargetFailsVerificationKeepsThatSource()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        FileSystemPath first = ParsePath(root.WriteFile("windows\\first.txt", "first"));
        FileSystemPath second = ParsePath(root.WriteFile("windows\\second.txt", "second"));
        _ = root.CreateDirectory("wsl\\dest");
        StepHookPort port = new(CreateRouter(root))
        {
            AfterCopy = snapshot =>
            {
                if (FileSystemPathIdentityComparer.Instance.Equals(snapshot.Path, second))
                {
                    File.AppendAllText(root.Resolve("wsl\\dest\\second.txt"), "-damaged");
                }
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Move([first, second], Wsl("/owned/dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Verification, outcome.Failure);
        AssertEffects(
            outcome,
            (first, FileOperationEffectKind.Copied),
            (first, FileOperationEffectKind.Verified),
            (first, FileOperationEffectKind.SourceDeleted),
            (second, FileOperationEffectKind.Copied));
        AssertCalls(port, "capability", "capability", "copy", "verify", "delete", "copy", "verify");
        Assert.IsFalse(File.Exists(root.Resolve("windows\\first.txt")));
        Assert.AreEqual("first", File.ReadAllText(root.Resolve("wsl\\dest\\first.txt")));
        Assert.AreEqual("second", File.ReadAllText(root.Resolve("windows\\second.txt")));
        Assert.AreEqual("second-damaged", File.ReadAllText(root.Resolve("wsl\\dest\\second.txt")));
    }

    /// <summary>Proves a move whose tree copy fails after creating its WSL target reports that partial effect and keeps the source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    [TestProperty("ThreatId", "ADV-007")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task ExecuteAsyncWhenMovedTreeCopyFailsAfterWslTargetCreationKeepsSource()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        FileSystemPath tree = ParsePath(root.CreateDirectory("windows\\tree"));
        _ = root.WriteFile("windows\\tree\\plain.txt", "plain");
        string lockedPath = root.WriteFile("windows\\tree\\locked.txt", "content");
        _ = root.CreateDirectory("wsl\\dest");
        StepHookPort port = new(CreateRouter(root));
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome outcome;
        using (FileStream locked = new(lockedPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            outcome = await gateway.ExecuteAsync(
                Move([tree], Wsl("/owned/dest")),
                IgnoredFileOperationProgress.Create(),
                CancellationToken.None);
        }

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Copy, outcome.Failure);
        AssertEffects(outcome, (tree, FileOperationEffectKind.CopyTargetCreated));
        AssertCalls(port, "capability", "copy");
        Assert.IsTrue(Directory.Exists(root.Resolve("wsl\\dest\\tree")));
        Assert.AreEqual("plain", File.ReadAllText(root.Resolve("windows\\tree\\plain.txt")));
        Assert.AreEqual("content", File.ReadAllText(lockedPath));
    }

    /// <summary>
    /// Proves cancellation observed after verification and before deletion stops the move at that
    /// step boundary, so the verified target and its source both remain.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task ExecuteAsyncWhenMoveIsCancelledBetweenVerifyAndDeleteKeepsSource()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        FileSystemPath first = ParsePath(root.WriteFile("windows\\first.txt", "first"));
        FileSystemPath second = ParsePath(root.WriteFile("windows\\second.txt", "second"));
        _ = root.CreateDirectory("wsl\\dest");
        using CancellationTokenSource cancellation = new();
        StepHookPort port = new(CreateRouter(root))
        {
            AfterVerify = _ => cancellation.CancelAsync(),
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Move([first, second], Wsl("/owned/dest")),
            IgnoredFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        AssertEffects(
            outcome,
            (first, FileOperationEffectKind.Copied),
            (first, FileOperationEffectKind.Verified));
        AssertCalls(port, "capability", "capability", "copy", "verify");
        Assert.AreEqual("first", File.ReadAllText(root.Resolve("wsl\\dest\\first.txt")));
        Assert.IsFalse(File.Exists(root.Resolve("wsl\\dest\\second.txt")));
        Assert.AreEqual("first", File.ReadAllText(root.Resolve("windows\\first.txt")));
        Assert.AreEqual("second", File.ReadAllText(root.Resolve("windows\\second.txt")));
    }

    /// <summary>
    /// Proves a move from WSL to Windows local, between distributions, or into UNC still stops
    /// before any step with <c>ProviderUnavailable</c>, leaving every source and destination as it was.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task ExecuteAsyncWhenMovingAcrossAnyOtherPairRejectsBeforeAnyEffect()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        _ = root.WriteFile("wsl\\item.txt", "linux");
        FileSystemPath windowsSource = ParsePath(root.WriteFile("windows\\item.txt", "windows"));
        FileSystemPath windowsDestination = ParsePath(root.CreateDirectory("windows\\dest"));
        _ = root.CreateDirectory("wsl\\dest");
        StepHookPort port = new(CreateRouter(root));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome toWindows = await gateway.ExecuteAsync(
            Move([Wsl("/owned/item.txt")], windowsDestination),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);
        FileOperationOutcome toDistribution = await gateway.ExecuteAsync(
            Move([Wsl("/owned/item.txt")], Wsl("Debian", "/owned/dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);
        FileOperationOutcome toUnc = await gateway.ExecuteAsync(
            Move([windowsSource], ParsePath("\\\\server\\share\\dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        foreach (FileOperationOutcome outcome in new[] { toWindows, toDistribution, toUnc })
        {
            Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
            Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
            Assert.HasCount(0, outcome.Effects);
        }
        Assert.HasCount(0, port.Calls);
        Assert.AreEqual("linux", File.ReadAllText(root.Resolve("wsl\\item.txt")));
        Assert.AreEqual("windows", File.ReadAllText(root.Resolve("windows\\item.txt")));
        Assert.IsEmpty(Directory.GetFileSystemEntries(root.Resolve("windows\\dest")));
        Assert.IsEmpty(Directory.GetFileSystemEntries(root.Resolve("wsl\\dest")));
    }

    /// <summary>
    /// Proves the drvfs alias of a Windows volume fails closed. ADR-0049 observed that a
    /// <c>/mnt/&lt;drive&gt;</c> entry cannot be opened through the share and answers
    /// <c>ERROR_ACCESS_DENIED</c>; the alias here names the source tree itself, and the refusal comes
    /// from the WSL handle query, never from a path text rule, before any self-overwrite or recursion.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task ExecuteAsyncWhenDestinationIsDrvfsAliasOfSourceFailsClosedWithAccessDenied()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath tree = ParsePath(root.CreateDirectory("tree"));
        _ = root.WriteFile("tree\\top.txt", "top");
        WslPath alias = Wsl("/mnt/c/alias/tree");
        WindowsWslFileSystem drvfs = new(
            path => FileSystemPathIdentityComparer.Instance.Equals(path, alias) ? root.Resolve("tree") : throw new InvalidOperationException("Unmapped WSL path."),
            DenyHandle);
        WindowsLocalIoExecutionBoundary execution = new();
        WindowsToWslTransfer transfer = new(execution, drvfs);
        using FileOperationGateway gateway = new(new ProviderFileOperationPort(
            new WindowsLocalFileOperationAdapter(execution),
            new WslFileOperationAdapter(execution, drvfs),
            transfer));
        FileEntrySnapshot snapshot = await InspectAsync(tree);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Copy([tree], alias),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);
        ProviderStepOutcome copy = await transfer.CopyAsync(snapshot, alias, CancellationToken.None);
        ProviderStepOutcome verify = await transfer.VerifyCopyAsync(snapshot, alias, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, outcome.Failure);
        Assert.HasCount(0, outcome.Effects);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, copy.Failure);
        Assert.IsNull(copy.Effect);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, verify.Failure);
        CollectionAssert.AreEquivalent(
            new[] { root.Resolve("tree\\top.txt") },
            Directory.GetFileSystemEntries(root.Resolve("tree")));
        Assert.AreEqual("top", File.ReadAllText(root.Resolve("tree\\top.txt")));
    }

    private static WslHandleFacts DenyHandle(string resolvedPath)
    {
        throw new UnauthorizedAccessException("Synthetic drvfs handle refusal: " + resolvedPath.Length);
    }

    private static TestOwnedTemporaryRoot CreateRoot()
    {
        TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("windows");
        _ = root.CreateDirectory("wsl");
        return root;
    }

    private static FileOperationGateway CreateGateway(TestOwnedTemporaryRoot root)
    {
        return new FileOperationGateway(CreateRouter(root));
    }

    private static ProviderFileOperationPort CreateRouter(TestOwnedTemporaryRoot root)
    {
        // The unguarded reader is the only way to obtain real handle facts from an NTFS test root;
        // production composes the same file system with the 9P-guarded reader by default.
        WindowsWslFileSystem fileSystem = new(path => Resolve(root, path), WindowsFileIdentifier.ReadHandleFacts);
        WindowsLocalIoExecutionBoundary execution = new();
        return new ProviderFileOperationPort(
            new WindowsLocalFileOperationAdapter(execution),
            new WslFileOperationAdapter(execution, fileSystem),
            new WindowsToWslTransfer(execution, fileSystem));
    }

    private static string Resolve(TestOwnedTemporaryRoot root, WslPath path)
    {
        const string operationRoot = "/owned";
        if (path.LinuxPath.Equals(operationRoot, StringComparison.Ordinal))
        {
            return root.Resolve("wsl");
        }
        string boundary = operationRoot + "/";
        return path.LinuxPath.StartsWith(boundary, StringComparison.Ordinal)
            ? root.Resolve("wsl\\" + path.LinuxPath[boundary.Length..].Replace('/', '\\'))
            : throw new InvalidOperationException("The mapped WSL path is outside the test-owned root.");
    }

    private static async Task<FileEntrySnapshot> InspectAsync(FileSystemPath path)
    {
        FileInspectionOutcome outcome = await new WindowsLocalFileOperationAdapter().InspectAsync(
            path,
            CancellationToken.None);
        return Assert.IsInstanceOfType<FileInspectionSucceeded>(outcome).Snapshot;
    }

    private static CopyRequest Copy(IReadOnlyList<FileSystemPath> sources, FileSystemPath destination)
    {
        return Assert.IsInstanceOfType<CopyRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                CopyRequest.Create(sources, destination)).Request);
    }

    private static MoveRequest Move(IReadOnlyList<FileSystemPath> sources, FileSystemPath destination)
    {
        return Assert.IsInstanceOfType<MoveRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(
                MoveRequest.Create(sources, destination)).Request);
    }

    private static void AssertEffects(
        FileOperationOutcome outcome,
        params (FileSystemPath Source, FileOperationEffectKind Kind)[] expected)
    {
        Assert.HasCount(expected.Length, outcome.Effects);
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.IsTrue(FileSystemPathIdentityComparer.Instance.Equals(expected[index].Source, outcome.Effects[index].Source));
            Assert.AreSame(expected[index].Kind, outcome.Effects[index].Kind);
        }
    }

    private static void AssertCalls(StepHookPort port, params string[] expected)
    {
        CollectionAssert.AreEqual(expected, port.Calls);
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
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

    /// <summary>
    /// Records the transfer steps the gateway sends through the router and runs an optional hook
    /// right after a copy or verification step returns, at the declared port boundary.
    /// </summary>
    private sealed class StepHookPort : IFileOperationPort
    {
        private readonly IFileOperationPort _inner;

        internal StepHookPort(IFileOperationPort inner)
        {
            _inner = inner;
        }

        internal Func<FileEntrySnapshot, Task> AfterCopy { get; init; } = _ => Task.CompletedTask;

        internal Func<FileEntrySnapshot, Task> AfterVerify { get; init; } = _ => Task.CompletedTask;

        internal List<string> Calls { get; } = [];

        internal List<DeletionExecutionMode> DeletionModes { get; } = [];

        public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken)
        {
            return _inner.InspectAsync(path, cancellationToken);
        }

        public Task<TransferPreflightOutcome> PreflightTransferAsync(
            IReadOnlyList<FileEntrySnapshot> sources,
            FileSystemPath destination,
            CancellationToken cancellationToken)
        {
            return _inner.PreflightTransferAsync(sources, destination, cancellationToken);
        }

        public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
            FileEntrySnapshot source,
            FileSystemPath destination,
            CancellationToken cancellationToken)
        {
            Calls.Add("capability");
            return _inner.GetAtomicMoveCapabilityAsync(source, destination, cancellationToken);
        }

        public Task<ProviderStepOutcome> MoveAsync(
            FileEntrySnapshot source,
            FileSystemPath destination,
            CancellationToken cancellationToken)
        {
            Calls.Add("move");
            return _inner.MoveAsync(source, destination, cancellationToken);
        }

        public async Task<ProviderStepOutcome> CopyAsync(
            FileEntrySnapshot source,
            FileSystemPath destination,
            CancellationToken cancellationToken)
        {
            Calls.Add("copy");
            ProviderStepOutcome outcome = await _inner.CopyAsync(source, destination, cancellationToken);
            await AfterCopy(source);
            return outcome;
        }

        public async Task<ProviderStepOutcome> VerifyCopyAsync(
            FileEntrySnapshot source,
            FileSystemPath destination,
            CancellationToken cancellationToken)
        {
            Calls.Add("verify");
            ProviderStepOutcome outcome = await _inner.VerifyCopyAsync(source, destination, cancellationToken);
            await AfterVerify(source);
            return outcome;
        }

        public Task<ProviderStepOutcome> DeleteAsync(
            FileEntrySnapshot source,
            DeletionExecutionMode mode,
            CancellationToken cancellationToken)
        {
            Calls.Add("delete");
            DeletionModes.Add(mode);
            return _inner.DeleteAsync(source, mode, cancellationToken);
        }

        public Task<ProviderStepOutcome> CreateDirectoryAsync(
            FileEntrySnapshot location,
            FileSystemPath target,
            CancellationToken cancellationToken)
        {
            return _inner.CreateDirectoryAsync(location, target, cancellationToken);
        }

        public Task<ProviderStepOutcome> RenameAsync(
            FileEntrySnapshot source,
            FileSystemPath target,
            CancellationToken cancellationToken)
        {
            return _inner.RenameAsync(source, target, cancellationToken);
        }
    }
}
