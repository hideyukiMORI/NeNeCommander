using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves same-distribution WSL copy and composite move from the gateway down to real I/O keep
/// trailing-dot names exactly (ADR-0060). The WSL tree <c>/owned</c> is a test-owned NTFS root
/// reached through the production resolver's extended-length text, where NTFS keeps a trailing dot
/// exactly as the 9P redirector does.
/// </summary>
[TestClass]
public sealed class WslTrailingDotGatewayTests
{
    private static readonly string[] TreeName = ["tree."];
    private static readonly string[] TreeChildren = ["foo.", "sub."];
    private static readonly string[] InnerChild = ["inner"];
    private static readonly string[] DottedName = ["foo."];
    private static readonly string[] MoveCalls = ["capability", "copy", "verify", "delete"];
    private static readonly string[] FailedMoveCalls = ["capability", "copy", "verify"];

    /// <summary>Proves a tree with trailing-dot names copies and verifies under its exact names.</summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public async Task ExecuteAsyncWhenCopyingTrailingDotTreeVerifiesExactNamesAndKeepsSource()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        using FileOperationGateway gateway = new(Adapter(root));

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Copy([Wsl("/owned/src/tree.")], Wsl("/owned/dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.IsNull(outcome.Failure);
        AssertEffectKinds(outcome, FileOperationEffectKind.Copied, FileOperationEffectKind.Verified);
        AssertExactTree(root, "dest\\tree.");
        AssertExactTree(root, "src\\tree.");
        CollectionAssert.AreEqual(TreeName, Names(root, "dest"));
    }

    /// <summary>Proves a composite move deletes the source only after the exact-name target verified.</summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task ExecuteAsyncWhenMovingTrailingDotTreeDeletesSourceAfterExactVerification()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        StepHookPort port = new(Adapter(root));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Move([Wsl("/owned/src/tree.")], Wsl("/owned/dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        AssertEffectKinds(
            outcome,
            FileOperationEffectKind.Copied,
            FileOperationEffectKind.Verified,
            FileOperationEffectKind.SourceDeleted);
        CollectionAssert.AreEqual(MoveCalls, port.Calls);
        AssertExactTree(root, "dest\\tree.");
        Assert.IsFalse(Directory.Exists(root.ResolveVerbatim("src\\tree.")));
        CollectionAssert.AreEqual(TreeName, Names(root, "dest"));
    }

    /// <summary>
    /// Proves a moved trailing-dot target that changes before verification fails verification on the
    /// exact name, so the source is never deleted.
    /// </summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task ExecuteAsyncWhenMovedTrailingDotTargetFailsVerificationKeepsSource()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        StepHookPort port = new(Adapter(root))
        {
            AfterCopy = _ =>
            {
                File.AppendAllText(root.ResolveVerbatim("dest\\tree.\\sub.\\inner"), "-damaged");
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Move([Wsl("/owned/src/tree.")], Wsl("/owned/dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Verification, outcome.Failure);
        AssertEffectKinds(outcome, FileOperationEffectKind.Copied);
        CollectionAssert.AreEqual(FailedMoveCalls, port.Calls);
        AssertExactTree(root, "src\\tree.");
        Assert.AreEqual("inner-damaged", File.ReadAllText(root.ResolveVerbatim("dest\\tree.\\sub.\\inner")));
    }

    /// <summary>
    /// Proves an existing <c>foo.</c> is a conflict for a source named <c>foo.</c>, rejected before any
    /// effect, and that the dot-less name the Win32 normalizer would have produced is never written.
    /// </summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task ExecuteAsyncWhenTrailingDotTargetExistsRejectsAsConflictWithoutEffect()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        _ = root.WriteVerbatimFile("src\\foo.", "new");
        _ = root.WriteVerbatimFile("dest\\foo.", "existing");
        using FileOperationGateway gateway = new(Adapter(root));

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Copy([Wsl("/owned/src/foo.")], Wsl("/owned/dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.HasCount(0, outcome.Effects);
        Assert.AreEqual("existing", File.ReadAllText(root.ResolveVerbatim("dest\\foo.")));
        Assert.AreEqual("new", File.ReadAllText(root.ResolveVerbatim("src\\foo.")));
        CollectionAssert.AreEqual(DottedName, Names(root, "dest"));
    }

    /// <summary>
    /// Proves <c>foo.</c> and <c>foo</c> are distinct WSL names: an existing <c>foo</c> neither blocks
    /// nor receives a copy of <c>foo.</c>.
    /// </summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    public async Task ExecuteAsyncWhenDotlessSiblingExistsCopiesTrailingDotNameBesideIt()
    {
        using TestOwnedTemporaryRoot root = CreateRoot();
        _ = root.WriteVerbatimFile("src\\foo.", "dotted");
        _ = root.WriteVerbatimFile("dest\\foo", "plain");
        using FileOperationGateway gateway = new(Adapter(root));

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Copy([Wsl("/owned/src/foo.")], Wsl("/owned/dest")),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        AssertEffectKinds(outcome, FileOperationEffectKind.Copied, FileOperationEffectKind.Verified);
        Assert.AreEqual("dotted", File.ReadAllText(root.ResolveVerbatim("dest\\foo.")));
        Assert.AreEqual("plain", File.ReadAllText(root.ResolveVerbatim("dest\\foo")));
    }

    private static TestOwnedTemporaryRoot CreateRoot()
    {
        TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateVerbatimDirectory("src");
        _ = root.CreateVerbatimDirectory("src\\tree.");
        _ = root.WriteVerbatimFile("src\\tree.\\foo.", "dot");
        _ = root.CreateVerbatimDirectory("src\\tree.\\sub.");
        _ = root.WriteVerbatimFile("src\\tree.\\sub.\\inner", "inner");
        _ = root.CreateVerbatimDirectory("dest");
        return root;
    }

    private static WslFileOperationAdapter Adapter(TestOwnedTemporaryRoot root)
    {
        // The unguarded reader is the only way to obtain real handle facts from an NTFS test root;
        // production composes the same file system with the 9P-guarded reader by default.
        string verbatimRoot = root.ResolveVerbatim(string.Empty);
        WindowsWslFileSystem fileSystem = new(
            path => OwnedWslNamespace.Resolve(verbatimRoot, path),
            WindowsFileIdentifier.ReadHandleFacts);
        return new WslFileOperationAdapter(new WindowsLocalIoExecutionBoundary(), fileSystem);
    }

    private static void AssertExactTree(TestOwnedTemporaryRoot root, string relativePath)
    {
        CollectionAssert.AreEqual(TreeChildren, Names(root, relativePath));
        CollectionAssert.AreEqual(InnerChild, Names(root, relativePath + "\\sub."));
        Assert.AreEqual("dot", File.ReadAllText(root.ResolveVerbatim(relativePath + "\\foo.")));
        Assert.AreEqual("inner", File.ReadAllText(root.ResolveVerbatim(relativePath + "\\sub.\\inner")));
    }

    private static string[] Names(TestOwnedTemporaryRoot root, string relativePath)
    {
        return [.. Directory.GetFileSystemEntries(root.ResolveVerbatim(relativePath))
            .Select(System.IO.Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)];
    }

    private static void AssertEffectKinds(FileOperationOutcome outcome, params FileOperationEffectKind[] expected)
    {
        CollectionAssert.AreEqual(expected, outcome.Effects.Select(effect => effect.Kind).ToArray());
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

    private static WslPath Wsl(string linuxPath)
    {
        string text = "\\\\wsl.localhost\\Ubuntu" + linuxPath.Replace('/', '\\');
        return Assert.IsInstanceOfType<WslPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path);
    }
}
