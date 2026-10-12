using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the reverse transfer on real Linux names and a separately owned Windows root.</summary>
[TestClass]
[DoNotParallelize]
public sealed class LiveWslReverseTransferTests
{
    /// <summary>Gets the opt-in live root parameters supplied by the sole launcher.</summary>
    public TestContext? TestContext { get; set; }

    /// <summary>Requires exact visible-name copying, including the observed Linux colon substitute.</summary>
    [TestMethod]
    [TestCategory("LiveWsl")]
    public async Task ExecuteAsyncWhenLiveWslTreeCopiesToWindowsPreservesVisibleNamesAsync()
    {
        TestContext context = RequireContext();
        LiveWslTestRoot root = await LiveWslRunFixture.OpenAsync(context);
        LiveWslRootCleanupOutcome cleanup;
        try
        {
            using TestOwnedTemporaryRoot windows = TestOwnedTemporaryRoot.Create();
            WslPath source = root.CreateDirectory("reverse-copy");
            _ = root.WriteFile("reverse-copy/a\uF03Ab", [0, 1, 255, 7]);
            _ = root.WriteFile("reverse-copy/nested/payload.bin", [4, 2]);
            string size = LiveWslDistributionCommand.Run(source.DistributionName,
                ["stat", "--printf=%s", "--", source.LinuxPath + "/a:b"]);
            Assert.AreEqual("4", size);
            LiveWslRunFixture.RequireEffectBoundary(root);
            using FileOperationGateway gateway = CreateGateway();
            FileOperationOutcome outcome = await gateway.ExecuteAsync(
                Copy(source, windows.Path), IgnoredFileOperationProgress.Create(), CancellationToken.None);
            RequireOutcome(context, outcome, FileOperationCompletionKind.Succeeded);
            WslToWindowsTransferFixture.AssertEffects(outcome, FileOperationEffectKind.Copied, FileOperationEffectKind.Verified);
            CollectionAssert.AreEqual(new byte[] { 0, 1, 255, 7 }, File.ReadAllBytes(windows.Resolve("reverse-copy\\a\uF03Ab")));
            CollectionAssert.AreEqual(new byte[] { 4, 2 }, File.ReadAllBytes(windows.Resolve("reverse-copy\\nested\\payload.bin")));
            CollectionAssert.AreEqual(new byte[] { 0, 1, 255, 7 }, root.ReadFile("reverse-copy/a\uF03Ab"));
        }
        finally
        {
            cleanup = LiveWslRunFixture.Close(context, root);
        }
        LiveWslRunFixture.RequireCleanup(cleanup);
    }

    /// <summary>Requires a verified reverse move to remove its owned WSL source through the gateway.</summary>
    [TestMethod]
    [TestCategory("LiveWsl")]
    public async Task ExecuteAsyncWhenLiveWslTreeMovesToWindowsDeletesVerifiedSourceAsync()
    {
        TestContext context = RequireContext();
        LiveWslTestRoot root = await LiveWslRunFixture.OpenAsync(context);
        LiveWslRootCleanupOutcome cleanup;
        try
        {
            using TestOwnedTemporaryRoot windows = TestOwnedTemporaryRoot.Create();
            WslPath source = root.CreateDirectory("reverse-move");
            _ = root.WriteFile("reverse-move/nested/payload.bin", [0, 7, 255]);
            LiveWslRunFixture.RequireEffectBoundary(root);
            using FileOperationGateway gateway = CreateGateway();
            MoveRequest request = Assert.IsInstanceOfType<MoveRequest>(
                Assert.IsInstanceOfType<FileOperationRequestAccepted>(MoveRequest.Create([source], windows.Path)).Request);
            FileOperationOutcome outcome = await gateway.ExecuteAsync(
                request, IgnoredFileOperationProgress.Create(), CancellationToken.None);
            RequireOutcome(context, outcome, FileOperationCompletionKind.Succeeded);
            root.AdoptMovedOutTree("reverse-move", outcome, source);
            CollectionAssert.AreEqual(new byte[] { 0, 7, 255 }, File.ReadAllBytes(windows.Resolve("reverse-move\\nested\\payload.bin")));
            Assert.IsFalse(root.Exists("reverse-move"));
        }
        finally
        {
            cleanup = LiveWslRunFixture.Close(context, root);
        }
        LiveWslRunFixture.RequireCleanup(cleanup);
    }

    /// <summary>Requires real case-sensitive file and directory collisions to stop before Windows effects.</summary>
    [TestMethod]
    [TestCategory("LiveWsl")]
    public async Task ExecuteAsyncWhenLiveLinuxNamesCollideOnWindowsRejectsBeforeEffectsAsync()
    {
        TestContext context = RequireContext();
        LiveWslTestRoot root = await LiveWslRunFixture.OpenAsync(context);
        LiveWslRootCleanupOutcome cleanup;
        try
        {
            using TestOwnedTemporaryRoot windows = TestOwnedTemporaryRoot.Create();
            WslPath files = root.CreateDirectory("case-files");
            _ = root.WriteFile("case-files/Name", [1]);
            _ = root.WriteFile("case-files/name", [2]);
            WslPath directories = root.CreateDirectory("case-directories");
            _ = root.CreateDirectory("case-directories/Name");
            _ = root.CreateDirectory("case-directories/name");
            WslPath mixed = root.CreateDirectory("case-mixed");
            _ = root.WriteFile("case-mixed/Name", [3]);
            _ = root.CreateDirectory("case-mixed/name");
            foreach (WslPath source in new[] { files, directories, mixed })
            {
                await RequireRejectedCopyAsync(root, windows, source, FileOperationFailureKind.Conflict);
            }
        }
        finally
        {
            cleanup = LiveWslRunFixture.Close(context, root);
        }
        LiveWslRunFixture.RequireCleanup(cleanup);
    }

    /// <summary>Requires Linux trailing-dot and Windows-reserved names to remain untouched after rejection.</summary>
    [TestMethod]
    [TestCategory("LiveWsl")]
    public async Task ExecuteAsyncWhenLiveLinuxNamesAreInvalidOnWindowsRejectsBeforeEffectsAsync()
    {
        TestContext context = RequireContext();
        LiveWslTestRoot root = await LiveWslRunFixture.OpenAsync(context);
        LiveWslRootCleanupOutcome cleanup;
        try
        {
            using TestOwnedTemporaryRoot windows = TestOwnedTemporaryRoot.Create();
            WslPath dot = root.CreateDirectory("invalid-dot");
            _ = root.WriteFile("invalid-dot/nested/ends.", [1]);
            WslPath reserved = root.CreateDirectory("invalid-device");
            _ = root.WriteFile("invalid-device/nested/CON", [2]);
            WslPath space = root.CreateDirectory("invalid-space");
            _ = root.WriteFile("invalid-space/nested/ends ", [3]);
            foreach (WslPath source in new[] { dot, reserved, space })
            {
                await RequireRejectedCopyAsync(root, windows, source, FileOperationFailureKind.ProviderUnavailable);
            }
        }
        finally
        {
            cleanup = LiveWslRunFixture.Close(context, root);
        }
        LiveWslRunFixture.RequireCleanup(cleanup);
    }

    private static async Task RequireRejectedCopyAsync(
        LiveWslTestRoot root, TestOwnedTemporaryRoot windows, WslPath source, FileOperationFailureKind failure)
    {
        LiveWslRunFixture.RequireEffectBoundary(root);
        using FileOperationGateway gateway = CreateGateway();
        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Copy(source, windows.Path), IgnoredFileOperationProgress.Create(), CancellationToken.None);
        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(failure, outcome.Failure);
        Assert.HasCount(0, outcome.Effects);
        Assert.HasCount(0, Directory.GetFileSystemEntries(windows.Path.CanonicalText));
        LiveWslRunFixture.RequireEffectBoundary(root);
    }

    private static CopyRequest Copy(WslPath source, WindowsLocalPath destination)
    {
        return Assert.IsInstanceOfType<CopyRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(CopyRequest.Create([source], destination)).Request);
    }

    private static FileOperationGateway CreateGateway()
    {
        return new FileOperationGateway(new ProviderFileOperationPort(new WindowsLocalIoExecutionBoundary()));
    }

    private static void RequireOutcome(TestContext context, FileOperationOutcome outcome, FileOperationCompletionKind expected)
    {
        context.WriteLine("LiveWsl reverse completion=" + outcome.Completion.GetType().Name +
            " failure=" + outcome.Failure?.GetType().Name);
        Assert.AreSame(expected, outcome.Completion);
    }

    private TestContext RequireContext()
    {
        return TestContext ?? throw new InvalidOperationException("The live test context is unavailable.");
    }
}
