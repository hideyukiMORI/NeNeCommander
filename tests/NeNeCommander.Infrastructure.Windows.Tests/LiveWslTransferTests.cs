using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Defines the required but currently unexecuted live same-distribution WSL assertions.</summary>
[TestClass]
[DoNotParallelize]
public sealed class LiveWslTransferTests
{
    private const string RootParameterName = "NENE_COMMANDER_WSL_TEST_ROOT";
    private const string TemporaryRootIdentityParameterName = "NENE_COMMANDER_WSL_TMP_IDENTITY";
    private const string ConfiguredRootIdentityParameterName = "NENE_COMMANDER_WSL_ROOT_IDENTITY";
    private const string HomeFactParameterName = "NENE_COMMANDER_WSL_HOME_FACT";
    private const string MountFactParameterName = "NENE_COMMANDER_WSL_MOUNT_FACT";

    /// <summary>Gets the MSTest context carrying the ephemeral opt-in root parameter.</summary>
    public TestContext? TestContext { get; set; }

    /// <summary>Requires nested copy to preserve exact bytes, declared tree shape, and its source.</summary>
    [TestMethod]
    [TestCategory("LiveWsl")]
    public async Task ExecuteAsyncWhenLiveNestedCopyCompletesPreservesSourceAndTargetAsync()
    {
        LiveWslTestRoot root = await OpenAsync();
        try
        {
            WslPath source = root.CreateDirectory("copy-source");
            _ = root.WriteFile("copy-source/top.bin", [0, 1, 2, 255]);
            _ = root.WriteFile("copy-source/nested/payload.bin", [13, 10, 0, 42]);
            IReadOnlyList<LiveWslDeclaredEntry> declaredSource = root.ReadDeclaredTree("copy-source");
            WslPath destination = root.CreateDirectory("copy-destination");
            RequireEffectBoundary(root);
            using FileOperationGateway gateway = CreateGateway();

            FileOperationOutcome outcome = await gateway.ExecuteAsync(
                Copy(source, destination),
                IgnoredFileOperationProgress.Create(),
                CancellationToken.None);
            RecordOutcome("copy", outcome);

            root.AdoptCopiedTree(
                "copy-destination/copy-source",
                [string.Empty, "nested", "nested/payload.bin", "top.bin"],
                outcome,
                source);
            CollectionAssert.AreEqual(
                declaredSource.ToArray(),
                root.ReadDeclaredTree("copy-destination/copy-source").ToArray());
            CollectionAssert.AreEqual(new byte[] { 0, 1, 2, 255 }, root.ReadFile("copy-source/top.bin"));
            CollectionAssert.AreEqual(new byte[] { 13, 10, 0, 42 }, root.ReadFile("copy-source/nested/payload.bin"));
            CollectionAssert.AreEqual(new byte[] { 0, 1, 2, 255 }, root.ReadFile("copy-destination/copy-source/top.bin"));
            CollectionAssert.AreEqual(
                new byte[] { 13, 10, 0, 42 },
                root.ReadFile("copy-destination/copy-source/nested/payload.bin"));
        }
        finally
        {
            RequireCleanup(root);
        }
    }

    /// <summary>Requires composite move to delete its source only after verified exact-byte copy.</summary>
    [TestMethod]
    [TestCategory("LiveWsl")]
    public async Task ExecuteAsyncWhenLiveCompositeMoveCompletesDeletesSourceAfterVerifiedTargetAsync()
    {
        LiveWslTestRoot root = await OpenAsync();
        try
        {
            WslPath source = root.CreateDirectory("move-source");
            _ = root.WriteFile("move-source/top.bin", [8, 6, 7, 5]);
            _ = root.WriteFile("move-source/nested/payload.bin", [3, 0, 9]);
            IReadOnlyList<LiveWslDeclaredEntry> declaredSource = root.ReadDeclaredTree("move-source");
            WslPath destination = root.CreateDirectory("move-destination");
            RequireEffectBoundary(root);
            using FileOperationGateway gateway = CreateGateway();

            FileOperationOutcome outcome = await gateway.ExecuteAsync(
                Move(source, destination),
                IgnoredFileOperationProgress.Create(),
                CancellationToken.None);
            RecordOutcome("move", outcome);

            root.AdoptMovedTree(
                "move-source",
                "move-destination/move-source",
                [string.Empty, "nested", "nested/payload.bin", "top.bin"],
                outcome,
                source);
            CollectionAssert.AreEqual(
                declaredSource.ToArray(),
                root.ReadDeclaredTree("move-destination/move-source").ToArray());
            Assert.IsFalse(root.Exists("move-source"));
            CollectionAssert.AreEqual(
                new byte[] { 8, 6, 7, 5 },
                root.ReadFile("move-destination/move-source/top.bin"));
            CollectionAssert.AreEqual(
                new byte[] { 3, 0, 9 },
                root.ReadFile("move-destination/move-source/nested/payload.bin"));
        }
        finally
        {
            RequireCleanup(root);
        }
    }

    /// <summary>Requires an owned source link to be rejected with zero effects and remain unchanged.</summary>
    [TestMethod]
    [TestCategory("LiveWsl")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task ExecuteAsyncWhenLiveSourceContainsOwnedLinkRejectsWithoutEffectAsync()
    {
        LiveWslTestRoot root = await OpenAsync();
        try
        {
            WslPath sentinel = root.WriteFile("sentinel.bin", [21, 34, 55]);
            WslPath source = root.CreateDirectory("link-source");
            _ = root.WriteFile("link-source/payload.bin", [1, 1, 2, 3, 5]);
            _ = root.CreateFileSymbolicLink("link-source/sentinel-link.bin", "sentinel.bin");
            WslPath destination = root.CreateDirectory("link-destination");
            RequireEffectBoundary(root);
            using FileOperationGateway gateway = CreateGateway();

            FileOperationOutcome outcome = await gateway.ExecuteAsync(
                Copy(source, destination),
                IgnoredFileOperationProgress.Create(),
                CancellationToken.None);
            RecordOutcome("link-refusal", outcome);

            Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
            Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
            Assert.IsEmpty(outcome.Effects);
            Assert.IsTrue(root.Exists("link-source"));
            Assert.IsTrue(root.Exists("link-source/sentinel-link.bin"));
            Assert.IsFalse(root.Exists("link-destination/link-source"));
            CollectionAssert.AreEqual(new byte[] { 21, 34, 55 }, root.ReadFile("sentinel.bin"));
            Assert.AreEqual(sentinel.DistributionName, source.DistributionName);
        }
        finally
        {
            RequireCleanup(root);
        }
    }

    private async Task<LiveWslTestRoot> OpenAsync()
    {
        LiveWslRootAdmission admission = LiveWslRootAdmission.Create(
            ReadParameter(RootParameterName),
            ReadParameter(TemporaryRootIdentityParameterName),
            ReadParameter(ConfiguredRootIdentityParameterName),
            ReadParameter(HomeFactParameterName),
            ReadParameter(MountFactParameterName));
        LiveWslRootOpenOutcome outcome = await LiveWslTestRoot.OpenAsync(admission, CancellationToken.None);
        if (outcome is LiveWslRootOpenRejected { Failure: var failure })
        {
            if (failure == LiveWslRootFailureKind.Unexecuted)
            {
                Assert.Inconclusive("LiveWsl:Unexecuted:RootParameterAbsent");
            }
            Assert.Fail("LiveWsl:RootRejected:" + failure.GetType().Name);
        }
        LiveWslTestRoot root = Assert.IsInstanceOfType<LiveWslRootOpened>(outcome).Root;
        TestContext!.WriteLine("LiveWsl setup=Opened provider=Wsl root=redacted identity=redacted");
        return root;
    }

    private string? ReadParameter(string name)
    {
        TestContext context = TestContext ?? throw new InvalidOperationException("MSTest did not provide TestContext.");
        return context.Properties.TryGetValue(name, out object? value) ? value as string : null;
    }

    private static FileOperationGateway CreateGateway()
    {
        WindowsLocalIoExecutionBoundary execution = new();
        return new FileOperationGateway(new ProviderFileOperationPort(execution));
    }

    private static CopyRequest Copy(WslPath source, WslPath destination)
    {
        FileOperationRequestCreation creation = CopyRequest.Create([source], destination);
        return Assert.IsInstanceOfType<CopyRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(creation).Request);
    }

    private static MoveRequest Move(WslPath source, WslPath destination)
    {
        FileOperationRequestCreation creation = MoveRequest.Create([source], destination);
        return Assert.IsInstanceOfType<MoveRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(creation).Request);
    }

    private static void RequireEffectBoundary(LiveWslTestRoot root)
    {
        _ = Assert.IsInstanceOfType<LiveWslRootCheckAccepted>(root.VerifyForEffect());
    }

    private void RecordOutcome(string operation, FileOperationOutcome outcome)
    {
        TestContext context = TestContext ?? throw new InvalidOperationException("MSTest did not provide TestContext.");
        string effects = string.Join(',', outcome.Effects.Select(effect => effect.Kind.GetType().Name));
        context.WriteLine(
            "LiveWsl operation=" + operation +
            " provider=Wsl root=redacted identity=redacted completion=" + outcome.Completion.GetType().Name +
            " failure=" + (outcome.Failure?.GetType().Name ?? "None") +
            " effects=" + effects);
    }

    private void RequireCleanup(LiveWslTestRoot root)
    {
        LiveWslRootCleanupOutcome cleanup = root.Cleanup();
        if (cleanup is LiveWslRootCleanupRejected rejected)
        {
            TestContext!.WriteLine(
                "LiveWsl cleanup=Rejected provider=Wsl root=redacted identity=redacted failure=" +
                rejected.Failure.GetType().Name);
            Assert.Fail("LiveWsl:CleanupRejected:" + rejected.Failure.GetType().Name);
        }
        _ = Assert.IsInstanceOfType<LiveWslRootCleanupCompleted>(cleanup);
        TestContext!.WriteLine("LiveWsl cleanup=Completed provider=Wsl root=redacted identity=redacted");
    }
}
