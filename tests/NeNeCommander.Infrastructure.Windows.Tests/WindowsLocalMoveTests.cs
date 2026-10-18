using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves the local adapter proof for Move.</summary>
[TestClass]
public sealed class WindowsLocalMoveTests
{
    /// <summary>Proves a same-volume gateway move is one atomic provider effect rather than a composite copy.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenGatewayMovesFileThroughAdapterSourceIsGoneAndTargetIsVerified()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("dest"));
        FileSystemPath source = WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc"));
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 })));
        MoveRequest request = Assert.IsInstanceOfType<MoveRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(MoveRequest.Create([source], destination)).Request);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.AtomicallyMoved, outcome.Effects[0].Kind);
        Assert.IsFalse(File.Exists(root.Resolve("a.txt")));
        Assert.AreEqual("abc", File.ReadAllText(root.Resolve("dest\\a.txt")));
    }

    /// <summary>Proves same-volume files and directories report atomic capability and move without overwrite.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task MoveAsyncWhenSourceAndDestinationShareVolumeMovesEntryAtomically()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("dest"));
        FileSystemPath filePath = WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc"));
        FileSystemPath directoryPath = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("tree"));
        _ = root.WriteFile("tree\\child.txt", "child");
        FileEntrySnapshot file = await WindowsLocalOperationTestContext.InspectAsync(adapter, filePath);
        FileEntrySnapshot directory = await WindowsLocalOperationTestContext.InspectAsync(adapter, directoryPath);

        AtomicMoveCapabilityOutcome fileCapability = await adapter.GetAtomicMoveCapabilityAsync(
            file,
            destination,
            CancellationToken.None);
        AtomicMoveCapabilityOutcome directoryCapability = await adapter.GetAtomicMoveCapabilityAsync(
            directory,
            destination,
            CancellationToken.None);
        ProviderStepOutcome fileMove = await adapter.MoveAsync(file, destination, CancellationToken.None);
        ProviderStepOutcome directoryMove = await adapter.MoveAsync(directory, destination, CancellationToken.None);

        Assert.AreSame(AtomicMoveCapabilityOutcome.Supported, fileCapability);
        Assert.AreSame(AtomicMoveCapabilityOutcome.Supported, directoryCapability);
        Assert.IsNull(fileMove.Failure);
        Assert.IsNull(directoryMove.Failure);
        Assert.AreEqual("abc", File.ReadAllText(root.Resolve("dest\\a.txt")));
        Assert.AreEqual("child", File.ReadAllText(root.Resolve("dest\\tree\\child.txt")));
    }

    /// <summary>Proves the atomic step revalidates source identity and a late target collision before moving.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task MoveAsyncWhenIdentityOrTargetChangesMovesNothing()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("dest"));
        FileSystemPath changedPath = WindowsLocalOperationTestContext.ParsePath(root.WriteFile("changed.txt", "before"));
        FileEntrySnapshot changed = await WindowsLocalOperationTestContext.InspectAsync(adapter, changedPath);
        _ = root.WriteFile("changed.txt", "after");
        FileSystemPath collisionPath = WindowsLocalOperationTestContext.ParsePath(root.WriteFile("collision.txt", "source"));
        FileEntrySnapshot collision = await WindowsLocalOperationTestContext.InspectAsync(adapter, collisionPath);
        _ = root.WriteFile("dest\\collision.txt", "target");

        ProviderStepOutcome changedMove = await adapter.MoveAsync(changed, destination, CancellationToken.None);
        ProviderStepOutcome collisionMove = await adapter.MoveAsync(collision, destination, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, changedMove.Failure);
        Assert.AreSame(FileOperationFailureKind.Conflict, collisionMove.Failure);
        Assert.AreEqual("after", File.ReadAllText(root.Resolve("changed.txt")));
        Assert.AreEqual("source", File.ReadAllText(root.Resolve("collision.txt")));
        Assert.AreEqual("target", File.ReadAllText(root.Resolve("dest\\collision.txt")));
    }

    /// <summary>Proves a reparse source and destination never advertise atomic-move capability.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public async Task GetAtomicMoveCapabilityAsyncWhenPathIsReparsePointReportsUnsupported()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileSystemPath sourceTarget = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("source-target"));
        FileSystemPath sourceLink = WindowsLocalOperationTestContext.ParsePath(root.CreateJunction("source-link", "source-target"));
        FileSystemPath destinationTarget = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("destination-target"));
        FileSystemPath destinationLink = WindowsLocalOperationTestContext.ParsePath(root.CreateJunction("destination-link", "destination-target"));
        FileEntrySnapshot linkedSource = await WindowsLocalOperationTestContext.InspectAsync(adapter, sourceLink);
        FileEntrySnapshot plainSource = await WindowsLocalOperationTestContext.InspectAsync(adapter, sourceTarget);

        AtomicMoveCapabilityOutcome sourceCapability = await adapter.GetAtomicMoveCapabilityAsync(
            linkedSource,
            destinationTarget,
            CancellationToken.None);
        AtomicMoveCapabilityOutcome destinationCapability = await adapter.GetAtomicMoveCapabilityAsync(
            plainSource,
            destinationLink,
            CancellationToken.None);

        Assert.AreSame(AtomicMoveCapabilityOutcome.Unsupported, sourceCapability);
        Assert.AreSame(AtomicMoveCapabilityOutcome.Unsupported, destinationCapability);
    }

    /// <summary>Proves missing destinations fail capability discovery before a move can start.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task GetAtomicMoveCapabilityAsyncWhenDestinationIsMissingReportsNotFound()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileEntrySnapshot source = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(root.WriteFile("source.txt", "source")));
        FileSystemPath missingDestination = WindowsLocalOperationTestContext.ParsePath(root.Resolve("missing"));

        AtomicMoveCapabilityOutcome capability = await adapter.GetAtomicMoveCapabilityAsync(
            source,
            missingDestination,
            CancellationToken.None);

        AtomicMoveCapabilityFailed failed = Assert.IsInstanceOfType<AtomicMoveCapabilityFailed>(capability);
        Assert.AreSame(FileOperationFailureKind.NotFound, failed.Failure);
        Assert.AreEqual("source", File.ReadAllText(root.Resolve("source.txt")));
    }

    /// <summary>Proves the atomic effect itself rejects reparse sources and destinations without mutation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public async Task MoveAsyncWhenPathIsReparsePointReportsProviderUnavailable()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        _ = root.CreateDirectory("source-target");
        FileSystemPath sourceLink = WindowsLocalOperationTestContext.ParsePath(root.CreateJunction("source-link", "source-target"));
        FileSystemPath destinationTarget = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("destination-target"));
        FileSystemPath destinationLink = WindowsLocalOperationTestContext.ParsePath(root.CreateJunction("destination-link", "destination-target"));
        FileEntrySnapshot linkedSource = await WindowsLocalOperationTestContext.InspectAsync(adapter, sourceLink);
        FileSystemPath plainPath = WindowsLocalOperationTestContext.ParsePath(root.WriteFile("plain.txt", "plain"));
        FileEntrySnapshot plainSource = await WindowsLocalOperationTestContext.InspectAsync(adapter, plainPath);

        ProviderStepOutcome sourceOutcome = await adapter.MoveAsync(
            linkedSource,
            destinationTarget,
            CancellationToken.None);
        ProviderStepOutcome destinationOutcome = await adapter.MoveAsync(
            plainSource,
            destinationLink,
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, sourceOutcome.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, destinationOutcome.Failure);
        Assert.IsTrue(Directory.Exists(root.Resolve("source-link")));
        Assert.AreEqual("plain", File.ReadAllText(root.Resolve("plain.txt")));
    }
}
