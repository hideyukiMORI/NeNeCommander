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

/// <summary>Proves verified deletion stays with its source owner and pre-effect copy failures claim no target.</summary>
[TestClass]
public sealed class VerifiedAdapterBoundaryTests
{
    /// <summary>Rejects a receipt-bearing source supplied to the other provider's deletion adapter.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("windows-owner")]
    [DataRow("wsl-owner")]
    public async Task DeleteAsyncWhenVerifiedSourceBelongsToOtherProviderRejectsWithoutEffectAsync(string owner)
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.WriteFile("wsl\\file.txt", "first");
        _ = fixture.Root.WriteFile("windows\\file.txt", "first");
        VerifiedTreeAccess access = new(fixture.FileSystem);
        FileSystemPath sourcePath = owner == "windows-owner" ? WslToWindowsTransferFixture.Source("file.txt") : Path(fixture.Root.Resolve("windows\\file.txt"));
        FileSystemPath target = owner == "windows-owner" ? Path(fixture.Root.Resolve("windows\\file.txt")) : WslToWindowsTransferFixture.Source("file.txt");
        FileEntrySnapshot source = FileEntrySnapshot.Create(sourcePath, access.Capture(sourcePath).Identity, DeletionCapability.PermanentOnly);
        VerifiedCopyReceipt receipt = Assert.IsInstanceOfType<CopyVerified>(VerificationTestEvidence.ForTarget(source, target)).Receipt;
        WindowsLocalIoExecutionBoundary execution = new();
        IFileOperationPort adapter = owner == "windows-owner" ? new WindowsLocalFileOperationAdapter(execution, fixture.FileSystem)
            : new WslFileOperationAdapter(execution, fixture.FileSystem);

        ProviderStepOutcome outcome = await adapter.DeleteAsync(source, DeletionExecutionMode.VerifiedCopy(receipt), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("wsl\\file.txt")));
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("windows\\file.txt")));
    }

    /// <summary>Never substitutes permanent deletion for unsupported WSL recycle mode.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task DeleteAsyncWhenWslRecycleIsRequestedRefusesPermanentFallbackAsync()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.WriteFile("wsl\\file.txt", "first");
        FileEntrySnapshot source = Assert.IsInstanceOfType<FileInspectionSucceeded>(await fixture.Router.InspectAsync(
            WslToWindowsTransferFixture.Source("file.txt"), CancellationToken.None)).Snapshot;
        WslFileOperationAdapter adapter = new(new WindowsLocalIoExecutionBoundary(), fixture.FileSystem);

        ProviderStepOutcome outcome = await adapter.DeleteAsync(source, DeletionExecutionMode.Recycle, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("wsl\\file.txt")));
    }

    /// <summary>Normalizes a failure entering the copy engine before the target is created without claiming an effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("access")]
    [DataRow("io")]
    public async Task CopyAsyncWhenSourceTransportFailsBeforeTargetCreationReportsNoEffectAsync(string kind)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("wsl");
        _ = root.CreateDirectory("windows");
        _ = root.WriteFile("wsl\\file.txt", "first");
        bool copying = false;
        bool injected = false;
        int resolutions = 0;
        Exception failure = kind == "access" ? new UnauthorizedAccessException("Owned source transport refused.") : new IOException("Owned source transport failed.");
        WindowsWslFileSystem fileSystem = new(path =>
        {
            if (copying && ++resolutions == 2)
            {
                injected = true;
                throw failure;
            }
            return OwnedWslNamespace.Resolve(root.ResolveVerbatim("wsl"), path);
        }, OwnedWslNamespace.ReadFacts);
        WindowsLocalIoExecutionBoundary execution = new();
        WslFileOperationAdapter sourceAdapter = new(execution, fileSystem);
        FileEntrySnapshot source = Assert.IsInstanceOfType<FileInspectionSucceeded>(await sourceAdapter.InspectAsync(
            WslToWindowsTransferFixture.Source("file.txt"), CancellationToken.None)).Snapshot;
        WslToWindowsTransfer transfer = new(execution, fileSystem);
        copying = true;

        ProviderStepOutcome outcome = await transfer.CopyAsync(source, Path(root.Resolve("windows")), CancellationToken.None);

        Assert.IsTrue(injected);
        Assert.AreSame(kind == "access" ? FileOperationFailureKind.AccessDenied : FileOperationFailureKind.Copy, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.IsFalse(File.Exists(root.Resolve("windows\\file.txt")));
        Assert.AreEqual("first", File.ReadAllText(root.Resolve("wsl\\file.txt")));
    }

    private static FileSystemPath Path(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
