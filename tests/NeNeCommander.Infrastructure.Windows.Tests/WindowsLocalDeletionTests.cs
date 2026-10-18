using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves the local adapter proof for Deletion.</summary>
[TestClass]
public sealed class WindowsLocalDeletionTests
{
    /// <summary>Proves permanent deletion removes files and trees and recycle is refused.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task DeleteAsyncWhenModeIsPermanentDeletesAndRecycleIsRefused()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileEntrySnapshot file = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc")));
        _ = root.CreateDirectory("tree");
        _ = root.WriteFile("tree\\top.txt", "top");
        FileEntrySnapshot tree = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.Resolve("tree")));

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
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileEntrySnapshot file = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc")));
        _ = root.WriteFile("a.txt", "rewritten");

        ProviderStepOutcome outcome = await adapter.DeleteAsync(file, DeletionExecutionMode.Permanent, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.AreEqual("rewritten", File.ReadAllText(root.Resolve("a.txt")));
    }

    /// <summary>Proves the gateway refuses unconfirmed permanent deletion through the real adapter.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task ExecuteAsyncWhenGatewayDeletesThroughAdapterRequiresConfirmation()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath source = WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc"));
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 })));
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
}
