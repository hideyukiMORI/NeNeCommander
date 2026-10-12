using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the reverse transfer's destination and execution boundaries fail closed.</summary>
[TestClass]
public sealed class WslToWindowsBoundaryTests
{
    /// <summary>Proves every step refuses a missing destination directory.</summary>
    [TestMethod]
    public async Task TransferWhenDestinationIsMissingRejectsEveryStep()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.WriteFile("wsl\\item.txt", "keep");
        await AssertDestinationRejectedAsync(fixture, Parse(fixture.Root.Resolve("missing")), FileOperationFailureKind.NotFound);
    }

    /// <summary>Proves every step refuses a linked destination ancestor.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task TransferWhenDestinationAncestorIsLinkedRejectsEveryStep()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.WriteFile("wsl\\item.txt", "keep");
        _ = fixture.Root.CreateDirectory("other\\nested");
        _ = fixture.Root.CreateJunction("link", "other");
        await AssertDestinationRejectedAsync(fixture, Parse(fixture.Root.Resolve("link\\nested")), FileOperationFailureKind.ProviderUnavailable);
        Assert.HasCount(0, Directory.GetFileSystemEntries(fixture.Root.Resolve("other\\nested")));
    }

    /// <summary>Proves a changed source is refused before a reverse plan is accepted.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task PreflightWhenSourceChangesRejectsWithoutEffects()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.WriteFile("wsl\\item.txt", "before");
        FileEntrySnapshot source = await SnapshotAsync(fixture);
        _ = fixture.Root.WriteFile("wsl\\item.txt", "changed length");
        await AssertPreflightRejectedAsync(fixture, source, FileOperationFailureKind.IdentityChanged);
    }

    /// <summary>Proves a missing source is refused before a reverse plan is accepted.</summary>
    [TestMethod]
    public async Task PreflightWhenSourceIsRemovedRejectsWithoutEffects()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.WriteFile("wsl\\item.txt", "before");
        FileEntrySnapshot source = await SnapshotAsync(fixture);
        File.Delete(fixture.Root.Resolve("wsl\\item.txt"));
        await AssertPreflightRejectedAsync(fixture, source, FileOperationFailureKind.NotFound);
    }

    /// <summary>Proves source deletion belongs exclusively to the existing source adapter.</summary>
    [TestMethod]
    public async Task NonTransferMembersWhenCalledRejectWithoutEffects()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.WriteFile("wsl\\item.txt", "keep");
        FileEntrySnapshot source = await SnapshotAsync(fixture);
        WslToWindowsTransfer transfer = fixture.Transfer;
        FileSystemPath target = fixture.Destination;

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<FileInspectionFailed>(await transfer.InspectAsync(source.Path, CancellationToken.None)).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, (await transfer.MoveAsync(source, target, CancellationToken.None)).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, (await transfer.DeleteAsync(source, DeletionExecutionMode.Permanent, CancellationToken.None)).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, (await transfer.CreateDirectoryAsync(source, target, CancellationToken.None)).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, (await transfer.RenameAsync(source, target, CancellationToken.None)).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable,
            (await transfer.PreflightTransferAsync([source], source.Path, CancellationToken.None)).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<AtomicMoveCapabilityFailed>(await transfer.GetAtomicMoveCapabilityAsync(source, source.Path, CancellationToken.None)).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable,
            (await transfer.CopyAsync(source, source.Path, CancellationToken.None)).Failure);
        Assert.AreEqual("keep", File.ReadAllText(fixture.Root.Resolve("wsl\\item.txt")));
        Assert.HasCount(0, Directory.GetFileSystemEntries(target.CanonicalText));
    }

    /// <summary>Proves every required reverse-transfer reference rejects defects synchronously.</summary>
    [TestMethod]
    public void BoundariesWhenArgumentIsNullRejectDefect()
    {
        ScriptedWslFileSystem fileSystem = new();
        WindowsLocalIoExecutionBoundary execution = new();
        WslToWindowsTransfer transfer = new(execution, fileSystem);
        FileSystemPath path = WslToWindowsTransferFixture.Source("item");
        FileEntrySnapshot source = FileEntrySnapshot.Create(path,
            Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("source")).Identity,
            DeletionCapability.PermanentOnly);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WslToWindowsTransfer(null!, fileSystem));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WslToWindowsTransfer(execution, null!));
        foreach (MethodInfo method in typeof(IFileOperationPort).GetMethods())
        {
            ParameterInfo[] parameters = method.GetParameters();
            object?[] arguments = new object?[parameters.Length];
            for (int index = 0; index < arguments.Length; index++)
            {
                Type type = parameters[index].ParameterType;
                arguments[index] = type == typeof(CancellationToken) ? CancellationToken.None
                    : type == typeof(FileEntrySnapshot) ? source
                    : type == typeof(FileSystemPath) ? path
                    : type == typeof(DeletionExecutionMode) ? DeletionExecutionMode.Permanent
                    : new[] { source };
            }
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                object?[] supplied = (object?[])arguments.Clone();
                supplied[index] = null;
                TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(() => method.Invoke(transfer, supplied));
                _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
            }
        }
    }

    private static async Task AssertDestinationRejectedAsync(
        WslToWindowsTransferFixture fixture, FileSystemPath destination, FileOperationFailureKind expected)
    {
        FileEntrySnapshot source = await SnapshotAsync(fixture);
        Assert.AreSame(expected, (await fixture.Transfer.PreflightTransferAsync([source], destination, CancellationToken.None)).Failure);
        Assert.AreSame(expected, (await fixture.Transfer.CopyAsync(source, destination, CancellationToken.None)).Failure);
        Assert.AreSame(expected, (await fixture.Transfer.VerifyCopyAsync(source, destination, CancellationToken.None)).Failure);
        Assert.AreEqual("keep", File.ReadAllText(fixture.Root.Resolve("wsl\\item.txt")));
    }

    private static async Task AssertPreflightRejectedAsync(
        WslToWindowsTransferFixture fixture, FileEntrySnapshot source, FileOperationFailureKind failure)
    {
        TransferPreflightOutcome result = await fixture.Transfer.PreflightTransferAsync(
            [source], fixture.Destination, CancellationToken.None);
        Assert.AreSame(failure, result.Failure);
        Assert.HasCount(0, Directory.GetFileSystemEntries(fixture.Destination.CanonicalText));
    }

    private static async Task<FileEntrySnapshot> SnapshotAsync(WslToWindowsTransferFixture fixture)
    {
        return Assert.IsInstanceOfType<FileInspectionSucceeded>(await fixture.Router.InspectAsync(
            WslToWindowsTransferFixture.Source("item.txt"), CancellationToken.None)).Snapshot;
    }

    private static FileSystemPath Parse(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
