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

/// <summary>Proves distribution-separated transfers through the existing router, gateway and filesystem.</summary>
[TestClass]
public sealed class WslCrossDistributionTransferTests
{
    private static readonly string[] CopyCalls = ["copy", "verify"];
    private static readonly string[] MoveCalls = ["capability", "copy", "verify", "delete"];

    /// <summary>Proves real nested I/O keeps equal Linux paths in two independent owned namespaces separate.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    [DataRow("Ubuntu", "Debian", "Copy")]
    [DataRow("Debian", "Ubuntu", "Copy")]
    [DataRow("Ubuntu", "Debian", "Move")]
    [DataRow("Debian", "Ubuntu", "Move")]
    public async Task ExecuteAsyncWhenDistributionsDifferPreservesExactTreeAndSourcePolicy(
        string sourceDistribution,
        string destinationDistribution,
        string operation)
    {
        using TestOwnedTemporaryRoot sourceRoot = TestOwnedTemporaryRoot.Create();
        using TestOwnedTemporaryRoot destinationRoot = TestOwnedTemporaryRoot.Create();
        _ = Directory.CreateDirectory(sourceRoot.ResolveVerbatim("tree.\\nested."));
        File.WriteAllText(sourceRoot.ResolveVerbatim("tree.\\nested.\\payload."), "owned payload");
        WindowsWslFileSystem fileSystem = new(
            path => OwnedWslNamespace.Resolve(
                path.DistributionName.Equals(sourceDistribution, StringComparison.OrdinalIgnoreCase)
                    ? sourceRoot.ResolveVerbatim(string.Empty)
                    : path.DistributionName.Equals(destinationDistribution, StringComparison.OrdinalIgnoreCase)
                        ? destinationRoot.ResolveVerbatim(string.Empty)
                        : throw new InvalidOperationException("Unowned test distribution."),
                path),
            WindowsFileIdentifier.ReadHandleFacts);
        StepHookPort port = new(Router(fileSystem));
        using FileOperationGateway gateway = new(port);
        WslPath source = Path(sourceDistribution, "tree.");
        WslPath destination = Path(destinationDistribution, string.Empty);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Request(operation, [source], destination), IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.IsNull(outcome.Failure);
        Assert.AreEqual("owned payload", File.ReadAllText(destinationRoot.ResolveVerbatim("tree.\\nested.\\payload.")));
        Assert.AreEqual(operation == "Copy", Directory.Exists(sourceRoot.ResolveVerbatim("tree.")));
        CollectionAssert.AreEqual(
            operation == "Copy" ? CopyCalls : MoveCalls,
            port.Calls);
        Assert.AreEqual(operation == "Copy" ? 2 : 3, outcome.Effects.Count);
        Assert.AreEqual(source, outcome.Effects[0].Source);
    }

    /// <summary>Proves complete-batch reservations use case-sensitive WSL identity before copying.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-018")]
    [TestProperty("ThreatId", "ADV-022")]
    [DataRow("same", "Conflict")]
    [DataRow("Same", "Accepted")]
    public async Task PreflightTransferAsyncWhenTargetsShareNamesUsesWslIdentity(string secondName, string expected)
    {
        ScriptedWslFileSystem fileSystem = new();
        WslFileSystemEntry first = Entry(Path("Ubuntu", "one/same"), "first", DirectoryEntryKind.File);
        WslFileSystemEntry second = Entry(Path("Ubuntu", "two/" + secondName), "second", DirectoryEntryKind.File);
        WslPath destination = Path("Debian", "target");
        fileSystem.Set(first);
        fileSystem.Set(second);
        fileSystem.Set(Entry(destination, "destination", DirectoryEntryKind.Directory));

        TransferPreflightOutcome outcome = await Router(fileSystem).PreflightTransferAsync(
            [Snapshot(first), Snapshot(second)], destination, CancellationToken.None);

        if (expected == "Conflict")
        {
            Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        }
        else
        {
            TransferPreflightSucceeded accepted = Assert.IsInstanceOfType<TransferPreflightSucceeded>(outcome);
            Assert.HasCount(2, accepted.Plan);
            Assert.AreEqual(Path("Debian", "target/same"), accepted.Plan[0].Target);
            Assert.AreEqual(Path("Debian", "target/Same"), accepted.Plan[1].Target);
        }
        Assert.IsEmpty(fileSystem.Copied);
        Assert.IsEmpty(fileSystem.Deleted);
    }

    /// <summary>Proves cancellation after a completed step retains effects and never deletes the source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    [TestProperty("ThreatId", "ADV-022")]
    [DataRow("Copy", 1)]
    [DataRow("Verify", 2)]
    public async Task ExecuteAsyncWhenCancelledBetweenStepsKeepsSource(string stage, int expectedEffects)
    {
        ScriptedWslFileSystem fileSystem = new();
        WslPath source = Path("Ubuntu", "item");
        WslPath destination = Path("Debian", "target");
        fileSystem.Set(Entry(source, "source", DirectoryEntryKind.File));
        fileSystem.Set(Entry(destination, "destination", DirectoryEntryKind.Directory));
        using CancellationTokenSource cancellation = new();
        StepHookPort port = new(Router(fileSystem))
        {
            AfterCopy = _ => CancelAt("Copy", stage, cancellation),
            AfterVerify = _ => CancelAt("Verify", stage, cancellation),
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Request("Move", [source], destination), IgnoredFileOperationProgress.Create(), cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(expectedEffects, outcome.Effects);
        Assert.IsEmpty(fileSystem.Deleted);
        Assert.IsNotNull(fileSystem.Find(source));
        Assert.DoesNotContain("delete", port.Calls);
    }

    /// <summary>Proves identity is checked again when a verified cross-distribution source is deleted.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [TestProperty("ThreatId", "ADV-007")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task ExecuteAsyncWhenSourceChangesAfterVerificationRetainsBothEntries()
    {
        ScriptedWslFileSystem fileSystem = new();
        WslPath source = Path("Ubuntu", "item");
        WslPath destination = Path("Debian", "target");
        fileSystem.Set(Entry(source, "source", DirectoryEntryKind.File));
        fileSystem.Set(Entry(destination, "destination", DirectoryEntryKind.Directory));
        StepHookPort port = new(Router(fileSystem))
        {
            AfterVerify = _ =>
            {
                fileSystem.Set(Entry(source, "replacement", DirectoryEntryKind.File));
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Request("Move", [source], destination), IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.HasCount(2, outcome.Effects);
        Assert.IsEmpty(fileSystem.Deleted);
        Assert.IsNotNull(fileSystem.Find(source));
        Assert.IsNotNull(fileSystem.Find(Path("Debian", "target/item")));
        CollectionAssert.AreEqual(MoveCalls, port.Calls);
    }

    private static Task CancelAt(string current, string requested, CancellationTokenSource cancellation)
    {
        if (current == requested)
        {
            cancellation.Cancel();
        }
        return Task.CompletedTask;
    }

    private static ProviderFileOperationPort Router(IWslFileSystem fileSystem)
    {
        WindowsLocalIoExecutionBoundary boundary = new();
        return new ProviderFileOperationPort(
            new WindowsLocalFileOperationAdapter(boundary),
            new WslFileOperationAdapter(boundary, fileSystem),
            new WindowsToWslTransfer(boundary, fileSystem),
            new WslToWindowsTransfer(boundary, fileSystem));
    }

    private static FileOperationRequest Request(string operation, FileSystemPath[] sources, WslPath destination)
    {
        FileOperationRequestCreation result = operation == "Copy"
            ? CopyRequest.Create(sources, destination) : MoveRequest.Create(sources, destination);
        return Assert.IsInstanceOfType<FileOperationRequestAccepted>(result).Request;
    }

    private static WslFileSystemEntry Entry(WslPath path, string identity, DirectoryEntryKind kind)
    {
        return ScriptedWslFileSystem.Entry(path, identity, kind);
    }

    private static FileEntrySnapshot Snapshot(WslFileSystemEntry entry)
    {
        return FileEntrySnapshot.Create(entry.Path, entry.Identity, DeletionCapability.PermanentOnly);
    }

    private static WslPath Path(string distribution, string relative)
    {
        return Assert.IsInstanceOfType<WslPath>(Assert.IsInstanceOfType<PathParseSuccess>(
            FileSystemPath.Parse(@"\\wsl.localhost\" + distribution + @"\owned" +
                (relative.Length == 0 ? string.Empty : @"\" + relative.Replace('/', '\\')))).Path);
    }
}
