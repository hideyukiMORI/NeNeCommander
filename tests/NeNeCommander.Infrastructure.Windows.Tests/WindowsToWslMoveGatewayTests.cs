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
/// Proves the Windows local to WSL move from the gateway down to real I/O (ADR-0059). One child of
/// a test-owned root is the Windows local side and another is the WSL <c>/owned</c> tree, mapped
/// through the production <see cref="WindowsWslFileSystem"/> seam.
/// </summary>
[TestClass]
public sealed class WindowsToWslMoveGatewayTests
{
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
        Assert.IsTrue(port.DeletionModes.TrueForAll(mode => mode is DeletionExecutionMode.VerifiedCopyMode));
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
        using (new FileStream(lockedPath, FileMode.Open, FileAccess.Read, FileShare.None))
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
    /// Proves a move from Windows or WSL into UNC still stops
    /// before any step with <c>ProviderUnavailable</c>, leaving every source and destination as it was.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task ExecuteAsyncWhenMovingIntoUncRejectsBeforeAnyEffect()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        _ = root.WriteFile("wsl\\item.txt", "linux");
        FileSystemPath windowsSource = ParsePath(root.WriteFile("windows\\item.txt", "windows"));
        _ = root.CreateDirectory("windows\\dest");
        _ = root.CreateDirectory("wsl\\dest");
        StepHookPort port = new(CreateRouter(root));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome wslToUnc = await gateway.ExecuteAsync(
            Move([Wsl("/owned/item.txt")], ParsePath(@"\\server\share\dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);
        FileOperationOutcome toUnc = await gateway.ExecuteAsync(
            Move([windowsSource], ParsePath("\\\\server\\share\\dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        foreach (FileOperationOutcome outcome in new[] { wslToUnc, toUnc })
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

    private static TestOwnedTemporaryRoot CreateRoot()
    {
        TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("windows");
        _ = root.CreateDirectory("wsl");
        return root;
    }

    private static ProviderFileOperationPort CreateRouter(TestOwnedTemporaryRoot root)
    {
        // The unguarded reader is the only way to obtain real handle facts from an NTFS test root;
        // production composes the same file system with the 9P-guarded reader by default.
        WindowsWslFileSystem fileSystem = new(path => Resolve(root, path), OwnedWslNamespace.ReadFacts);
        WindowsLocalIoExecutionBoundary execution = new();
        return new ProviderFileOperationPort(
            new WindowsLocalFileOperationAdapter(execution, fileSystem),
            new WslFileOperationAdapter(execution, fileSystem),
            new WindowsToWslTransfer(execution, fileSystem),
            new WslToWindowsTransfer(execution, fileSystem));
    }

    private static string Resolve(TestOwnedTemporaryRoot root, WslPath path)
    {
        return OwnedWslNamespace.Resolve(root.ResolveVerbatim("wsl"), path);
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
        string text = "\\\\wsl.localhost\\Ubuntu" + linuxPath.Replace('/', '\\');
        return Assert.IsInstanceOfType<WslPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path);
    }
}
