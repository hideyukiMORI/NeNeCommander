using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves exclusive zero-byte creation in the existing opt-in WSL owner.</summary>
[TestClass]
[DoNotParallelize]
public sealed class LiveWslEmptyFileTests
{
    /// <summary>Gets the context carrying the validated opt-in root parameters.</summary>
    public TestContext? TestContext { get; set; }

    /// <summary>Proves exact case, reserved Windows names, trailing dots and long paths remain WSL names.</summary>
    [TestMethod]
    [TestCategory("LiveWsl")]
    public async Task ExecuteAsyncWhenLiveEmptyFileNamesAreCreatedPreservesExactZeroByteEntriesAsync()
    {
        TestContext context = RequireContext();
        LiveWslTestRoot root = await LiveWslRunFixture.OpenAsync(context);
        LiveWslRootCleanupOutcome cleanup;
        try
        {
            string relativeParent = "create/" + new string('a', 120) + "/" + new string('b', 120);
            WslPath location = root.CreateDirectory(relativeParent);
            using FileOperationGateway gateway = CreateGateway();
            foreach (string name in new[] { "notes.txt", "NOTES.txt", "foo.", "COM1", "記録.txt" })
            {
                LiveWslRunFixture.RequireEffectBoundary(root);
                FileOperationOutcome outcome = await gateway.ExecuteAsync(
                    Request(location, name), IgnoredFileOperationProgress.Create(), CancellationToken.None);
                root.AdoptCreatedFile(relativeParent + "/" + name, outcome);
                Assert.IsEmpty(root.ReadFile(relativeParent + "/" + name));
            }
            context.WriteLine("LiveWsl empty-file=exact-names-and-zero-bytes provider=Wsl root=redacted");
        }
        finally
        {
            cleanup = LiveWslRunFixture.Close(context, root);
        }
        LiveWslRunFixture.RequireCleanup(cleanup);
    }

    /// <summary>Proves file, directory and link conflicts preserve the declared owned entries.</summary>
    [TestMethod]
    [TestCategory("LiveWsl")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task ExecuteAsyncWhenLiveEmptyFileTargetsExistPreservesAllEntriesAsync()
    {
        TestContext context = RequireContext();
        LiveWslTestRoot root = await LiveWslRunFixture.OpenAsync(context);
        LiveWslRootCleanupOutcome cleanup;
        try
        {
            WslPath location = root.CreateDirectory("conflicts");
            _ = root.WriteFile("conflicts/file.txt", [1, 3, 5, 7]);
            _ = root.CreateDirectory("conflicts/directory.txt");
            _ = root.CreateFileSymbolicLink("conflicts/link.txt", "conflicts/file.txt");
            string before = root.ReadIdentity("conflicts/file.txt");
            using FileOperationGateway gateway = CreateGateway();
            foreach (string name in new[] { "file.txt", "directory.txt", "link.txt" })
            {
                LiveWslRunFixture.RequireEffectBoundary(root);
                FileOperationOutcome outcome = await gateway.ExecuteAsync(
                    Request(location, name), IgnoredFileOperationProgress.Create(), CancellationToken.None);
                Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
                Assert.IsEmpty(outcome.Effects);
            }
            Assert.AreEqual(before, root.ReadIdentity("conflicts/file.txt"));
            CollectionAssert.AreEqual(new byte[] { 1, 3, 5, 7 }, root.ReadFile("conflicts/file.txt"));
        }
        finally
        {
            cleanup = LiveWslRunFixture.Close(context, root);
        }
        LiveWslRunFixture.RequireCleanup(cleanup);
    }

    /// <summary>Proves the native exclusive step itself refuses an owned link and preserves its target.</summary>
    [TestMethod]
    [TestCategory("LiveWsl")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public async Task CreateEmptyFileWhenLiveFinalLinkExistsRefusesNativeStepAsync()
    {
        TestContext context = RequireContext();
        LiveWslTestRoot root = await LiveWslRunFixture.OpenAsync(context);
        LiveWslRootCleanupOutcome cleanup;
        try
        {
            _ = root.WriteFile("native/target.txt", [4, 2]);
            WslPath link = root.CreateFileSymbolicLink("native/link.txt", "native/target.txt");
            string targetBefore = root.ReadIdentity("native/target.txt");
            string linkBefore = root.ReadIdentity("native/link.txt");
            LiveWslRunFixture.RequireEffectBoundary(root);

            ProviderStepOutcome outcome = new WindowsWslFileSystem().CreateEmptyFile(link);

            Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
            Assert.AreEqual(targetBefore, root.ReadIdentity("native/target.txt"));
            Assert.AreEqual(linkBefore, root.ReadIdentity("native/link.txt"));
            CollectionAssert.AreEqual(new byte[] { 4, 2 }, root.ReadFile("native/target.txt"));
        }
        finally
        {
            cleanup = LiveWslRunFixture.Close(context, root);
        }
        LiveWslRunFixture.RequireCleanup(cleanup);
    }

    /// <summary>Proves native exclusive creation refuses a dangling link without creating its absent target.</summary>
    [TestMethod]
    [TestCategory("LiveWsl")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public async Task CreateEmptyFileWhenLiveFinalLinkIsDanglingPreservesAbsentTargetAsync()
    {
        TestContext context = RequireContext();
        LiveWslTestRoot root = await LiveWslRunFixture.OpenAsync(context);
        LiveWslRootCleanupOutcome cleanup;
        try
        {
            WslPath link = root.CreateDanglingFileSymbolicLink("native/link.txt", "native/absent.txt");
            string linkBefore = root.ReadIdentity("native/link.txt");
            LiveWslRunFixture.RequireEffectBoundary(root);

            ProviderStepOutcome outcome = new WindowsWslFileSystem().CreateEmptyFile(link);

            Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
            Assert.IsEmpty(outcome.Receipt.Effects);
            Assert.IsFalse(root.Exists("native/absent.txt"));
            Assert.AreEqual(linkBefore, root.ReadIdentity("native/link.txt"));
            context.WriteLine("LiveWsl empty-file=dangling-link-conflict target=absent link=unchanged");
        }
        finally
        {
            cleanup = LiveWslRunFixture.Close(context, root);
        }
        LiveWslRunFixture.RequireCleanup(cleanup);
    }

    private static FileOperationGateway CreateGateway()
    {
        return new FileOperationGateway(new ProviderFileOperationPort(new WindowsLocalIoExecutionBoundary()));
    }

    private static FileOperationRequest Request(WslPath location, string name)
    {
        return Assert.IsInstanceOfType<FileOperationRequestAccepted>(CreateTextFileRequest.Create(location, name)).Request;
    }

    private TestContext RequireContext()
    {
        return TestContext ?? throw new InvalidOperationException("MSTest did not provide TestContext.");
    }
}
