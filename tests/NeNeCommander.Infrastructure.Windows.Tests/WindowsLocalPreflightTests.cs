using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves the local adapter proof for Preflight.</summary>
[TestClass]
public sealed class WindowsLocalPreflightTests
{
    /// <summary>Proves preflight validates the destination before any source.</summary>
    [TestMethod]
    public async Task PreflightTransferAsyncWhenDestinationIsMissingOrForeignFailsClosed()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileEntrySnapshot source = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc")));

        TransferPreflightOutcome missing = await adapter.PreflightTransferAsync([source], WindowsLocalOperationTestContext.ParsePath(root.Resolve("missing")), CancellationToken.None);
        TransferPreflightOutcome unc = await adapter.PreflightTransferAsync([source], WindowsLocalOperationTestContext.ParsePath("\\\\server\\share"), CancellationToken.None);

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
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("dest"));
        FileEntrySnapshot file = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc")));
        _ = root.CreateDirectory("tree");
        _ = root.CreateDirectory("tree\\inner");
        _ = root.WriteFile("dest\\A.TXT", "x");
        FileEntrySnapshot folder = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.Resolve("tree")));

        TransferPreflightOutcome collision = await adapter.PreflightTransferAsync([file], destination, CancellationToken.None);
        TransferPreflightOutcome intoItself = await adapter.PreflightTransferAsync(
            [folder],
            WindowsLocalOperationTestContext.ParsePath(root.Resolve("tree\\inner")),
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
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("dest"));
        FileEntrySnapshot source = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc")));
        _ = root.WriteFile("a.txt", "abcdef");

        TransferPreflightOutcome outcome = await adapter.PreflightTransferAsync([source], destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
    }
}
