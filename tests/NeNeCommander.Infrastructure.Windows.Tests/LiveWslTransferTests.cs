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

/// <summary>Defines the required live same-distribution WSL transfer assertions.</summary>
[TestClass]
[DoNotParallelize]
public sealed class LiveWslTransferTests
{
    /// <summary>Gets the MSTest context carrying the ephemeral opt-in root parameter.</summary>
    public TestContext? TestContext { get; set; }

    /// <summary>Requires nested copy to preserve exact bytes, declared tree shape, and its source.</summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("LiveWsl")]
    public async Task ExecuteAsyncWhenLiveNestedCopyCompletesPreservesSourceAndTargetAsync()
    {
        TestContext context = RequireContext();
        LiveWslTestRoot root = await LiveWslRunFixture.OpenAsync(context);
        LiveWslRootCleanupOutcome cleanup;
        try
        {
            WslPath source = root.CreateDirectory("copy-source");
            _ = root.WriteFile("copy-source/top.bin", [0, 1, 2, 255]);
            _ = root.WriteFile("copy-source/nested/payload.bin", [13, 10, 0, 42]);
            IReadOnlyList<LiveWslDeclaredEntry> declaredSource = root.ReadDeclaredTree("copy-source");
            WslPath destination = root.CreateDirectory("copy-destination");
            LiveWslRunFixture.RequireEffectBoundary(root);
            using FileOperationGateway gateway = CreateGateway();

            FileOperationOutcome outcome = await gateway.ExecuteAsync(
                Copy(source, destination),
                IgnoredFileOperationProgress.Create(),
                CancellationToken.None);
            RecordOutcome(context, "copy", outcome);

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
            cleanup = LiveWslRunFixture.Close(context, root);
        }
        LiveWslRunFixture.RequireCleanup(cleanup);
    }

    /// <summary>Requires composite move to delete its source only after verified exact-byte copy.</summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("LiveWsl")]
    public async Task ExecuteAsyncWhenLiveCompositeMoveCompletesDeletesSourceAfterVerifiedTargetAsync()
    {
        TestContext context = RequireContext();
        LiveWslTestRoot root = await LiveWslRunFixture.OpenAsync(context);
        LiveWslRootCleanupOutcome cleanup;
        try
        {
            WslPath source = root.CreateDirectory("move-source");
            _ = root.WriteFile("move-source/top.bin", [8, 6, 7, 5]);
            _ = root.WriteFile("move-source/nested/payload.bin", [3, 0, 9]);
            IReadOnlyList<LiveWslDeclaredEntry> declaredSource = root.ReadDeclaredTree("move-source");
            WslPath destination = root.CreateDirectory("move-destination");
            LiveWslRunFixture.RequireEffectBoundary(root);
            using FileOperationGateway gateway = CreateGateway();

            FileOperationOutcome outcome = await gateway.ExecuteAsync(
                Move(source, destination),
                IgnoredFileOperationProgress.Create(),
                CancellationToken.None);
            RecordOutcome(context, "move", outcome);

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
            cleanup = LiveWslRunFixture.Close(context, root);
        }
        LiveWslRunFixture.RequireCleanup(cleanup);
    }

    /// <summary>Requires an owned source link to be rejected with zero effects and remain unchanged.</summary>
    /// <returns>The running assertion.</returns>
    [TestMethod]
    [TestCategory("LiveWsl")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task ExecuteAsyncWhenLiveSourceContainsOwnedLinkRejectsWithoutEffectAsync()
    {
        TestContext context = RequireContext();
        LiveWslTestRoot root = await LiveWslRunFixture.OpenAsync(context);
        LiveWslRootCleanupOutcome cleanup;
        try
        {
            WslPath sentinel = root.WriteFile("sentinel.bin", [21, 34, 55]);
            WslPath source = root.CreateDirectory("link-source");
            _ = root.WriteFile("link-source/payload.bin", [1, 1, 2, 3, 5]);
            _ = root.CreateFileSymbolicLink("link-source/sentinel-link.bin", "sentinel.bin");
            WslPath destination = root.CreateDirectory("link-destination");
            LiveWslRunFixture.RequireEffectBoundary(root);
            using FileOperationGateway gateway = CreateGateway();

            FileOperationOutcome outcome = await gateway.ExecuteAsync(
                Copy(source, destination),
                IgnoredFileOperationProgress.Create(),
                CancellationToken.None);
            RecordOutcome(context, "link-refusal", outcome);

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
            cleanup = LiveWslRunFixture.Close(context, root);
        }
        LiveWslRunFixture.RequireCleanup(cleanup);
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

    private static void RecordOutcome(TestContext context, string operation, FileOperationOutcome outcome)
    {
        string effects = string.Join(',', outcome.Effects.Select(effect => effect.Kind.GetType().Name));
        context.WriteLine(
            "LiveWsl operation=" + operation +
            " provider=Wsl root=redacted identity=redacted completion=" + outcome.Completion.GetType().Name +
            " failure=" + (outcome.Failure?.GetType().Name ?? "None") +
            " effects=" + effects);
    }

    private TestContext RequireContext()
    {
        return TestContext ?? throw new InvalidOperationException("MSTest did not provide TestContext.");
    }
}
