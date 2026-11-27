using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves purpose propagation preserves WSL identity and verified deletion authority.</summary>
[TestClass]
public sealed class InspectionPurposeBoundaryTests
{
    /// <summary>WSL observes deletion capability only when requested while keeping the same identity.</summary>
    [TestMethod]
    [DataRow("Identity")]
    [DataRow("Deletion")]
    public async Task WslInspectionWhenPurposeDiffersPreservesProviderIdentity(string choice)
    {
        ScriptedWslFileSystem fileSystem = new();
        WslPath path = WslToWindowsTransferFixture.Source("owned.txt");
        WslFileSystemEntry entry = WslOperationTestContext.Entry(path, "owned-identity", DirectoryEntryKind.File);
        fileSystem.Set(entry);
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);
        FileInspectionPurpose purpose = choice == "Deletion" ? FileInspectionPurpose.Deletion : FileInspectionPurpose.IdentityOnly;

        FileEntrySnapshot snapshot = Assert.IsInstanceOfType<FileInspectionSucceeded>(await adapter.InspectAsync(path, purpose, CancellationToken.None)).Snapshot;

        Assert.AreSame(entry.Identity, snapshot.Identity);
        Assert.AreSame(path, snapshot.Path);
        Assert.AreSame(choice == "Deletion" ? DeletionCapability.PermanentOnly : DeletionCapability.NotRequested, snapshot.DeletionCapability);
        Assert.IsEmpty(fileSystem.Deleted);
    }

    /// <summary>Uninspected WSL sources cannot enter either ordinary deletion mode.</summary>
    [TestMethod]
    [DataRow("Permanent")]
    [DataRow("Recycle")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task WslDeletionWhenCapabilityWasNotRequestedDoesNotReachFileSystem(string choice)
    {
        ScriptedWslFileSystem fileSystem = new() { Failure = new InvalidOperationException("Unrequested provider access.") };
        WslPath path = WslToWindowsTransferFixture.Source("owned.txt");
        WslFileSystemEntry entry = WslOperationTestContext.Entry(path, "owned-identity", DirectoryEntryKind.File);
        FileEntrySnapshot source = FileEntrySnapshot.Create(path, entry.Identity, DeletionCapability.NotRequested);
        WslFileOperationAdapter adapter = WslOperationTestContext.Adapter(fileSystem);
        DeletionExecutionMode mode = choice == "Recycle" ? DeletionExecutionMode.Recycle : DeletionExecutionMode.Permanent;

        ProviderStepOutcome outcome = await adapter.DeleteAsync(source, mode, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Inspection, outcome.Failure);
        Assert.IsEmpty(outcome.Receipt.Effects);
        Assert.IsEmpty(fileSystem.Deleted);
    }

    /// <summary>The new purpose is required at every concrete public port boundary before scheduling.</summary>
    [TestMethod]
    public void InspectionWhenPurposeIsNullRejectsAtEveryProviderBoundary()
    {
        WindowsLocalIoExecutionBoundary execution = new();
        ScriptedWslFileSystem fileSystem = new();
        WindowsLocalFileOperationAdapter local = new(execution, fileSystem, new DeterministicIdentifierSource());
        WslFileOperationAdapter wsl = new(execution, fileSystem);
        WindowsToWslTransfer outward = new(execution, fileSystem);
        WslToWindowsTransfer inward = new(execution, fileSystem);
        ProviderFileOperationPort router = new(local, wsl, outward, inward);
        IFileOperationPort[] ports = [local, wsl, outward, inward, router];
        FileSystemPath path = WslOperationTestContext.Local("C:\\owned.txt");

        foreach (IFileOperationPort port in ports)
        {
            Assert.AreEqual("purpose", Assert.ThrowsExactly<ArgumentNullException>(
                () => port.InspectAsync(path, null!, CancellationToken.None)).ParamName);
        }
    }

    /// <summary>Real verified receipts remain sufficient for both source providers without ordinary-delete capability.</summary>
    [TestMethod]
    [DataRow("Windows")]
    [DataRow("Wsl")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task VerifiedDeletionWhenCapabilityWasNotRequestedStillRequiresAndConsumesReceipt(string owner)
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.WriteFile(owner == "Windows" ? "windows\\file.txt" : "wsl\\file.txt", "same-owned-content");
        FileSystemPath local = WslOperationTestContext.Local(fixture.Root.Resolve("windows\\file.txt"));
        FileSystemPath remote = WslToWindowsTransferFixture.Source("file.txt");
        FileSystemPath sourcePath = owner == "Windows" ? local : remote;
        FileSystemPath target = owner == "Windows" ? remote : local;
        FileSystemPath destination = target.Parent ?? throw new InvalidOperationException("Missing fixture parent.");
        MoveRequest request = Assert.IsInstanceOfType<MoveRequest>(Assert.IsInstanceOfType<FileOperationRequestAccepted>(
            MoveRequest.Create([sourcePath], destination)).Request);
        StepHookPort port = new(fixture.Router);
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.IsNull(outcome.Failure);
        Assert.HasCount(1, port.DeletionModes);
        VerifiedCopyReceipt receipt = Assert.IsInstanceOfType<DeletionExecutionMode.VerifiedCopyMode>(port.DeletionModes[0]).Receipt;
        Assert.AreSame(DeletionCapability.NotRequested, receipt.Source.DeletionCapability);
        Assert.AreEqual(target, receipt.Source.TransferTarget);
        Assert.AreEqual("capability,copy,verify,delete", string.Join(',', port.Calls));
        Assert.IsFalse(File.Exists(fixture.Root.Resolve(owner == "Windows" ? "windows\\file.txt" : "wsl\\file.txt")));
        Assert.AreEqual("same-owned-content", File.ReadAllText(fixture.Root.Resolve(owner == "Windows" ? "wsl\\file.txt" : "windows\\file.txt")));
    }
}
