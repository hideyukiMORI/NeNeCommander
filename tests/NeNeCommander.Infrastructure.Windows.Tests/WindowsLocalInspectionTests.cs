using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves the local adapter proof for Inspection.</summary>
[TestClass]
public sealed class WindowsLocalInspectionTests
{
    /// <summary>A mapped or unidentified drive cannot produce a local file identity.</summary>
    [TestMethod]
    [DataRow(DriveType.Network)]
    [DataRow(DriveType.Unknown)]
    [DataRow(DriveType.NoRootDirectory)]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public async Task InspectAsyncWhenDriveIsNotLocalRejectsBeforeIdentity(DriveType observed)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath path = (WindowsLocalPath)WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc"));
        WindowsDriveBoundary drives = new(_ => observed);
        WindowsLocalFileOperationAdapter adapter = new(new(),
            new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }),
            new WindowsWslFileSystem(), drives);

        FileInspectionOutcome outcome = await adapter.InspectAsync(path, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable,
            Assert.IsInstanceOfType<FileInspectionFailed>(outcome).Failure);
    }

    /// <summary>Proves inspection reports a stable identity and permanent-only deletion for a file.</summary>
    [TestMethod]
    public async Task InspectAsyncWhenEntryIsFileReportsStableIdentityAndPermanentOnly()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath file = WindowsLocalOperationTestContext.ParsePath(root.WriteFile("a.txt", "abc"));
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));

        FileEntrySnapshot first = await WindowsLocalOperationTestContext.InspectAsync(adapter, file);
        FileEntrySnapshot again = await WindowsLocalOperationTestContext.InspectAsync(adapter, file);

        Assert.AreEqual(first.Identity, again.Identity);
        Assert.AreSame(DeletionCapability.PermanentOnly, first.DeletionCapability);
        Assert.AreSame(file, first.Path);
        Assert.StartsWith("windows-v2|", first.Identity.Value);
        Assert.Contains("|file|3|", first.Identity.Value);
    }

    /// <summary>Proves inspection describes a directory with its own kind.</summary>
    [TestMethod]
    public async Task InspectAsyncWhenEntryIsDirectoryReportsDirectoryIdentity()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath directory = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("docs"));
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));

        FileEntrySnapshot snapshot = await WindowsLocalOperationTestContext.InspectAsync(adapter, directory);

        Assert.Contains("|directory|0|", snapshot.Identity.Value);
    }

    /// <summary>Proves a rewritten file changes its identity.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task InspectAsyncWhenFileIsRewrittenIdentityChanges()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string path = root.WriteFile("a.txt", "abc");
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileEntrySnapshot before = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(path));

        _ = root.WriteFile("a.txt", "abcdef");
        FileEntrySnapshot after = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(path));

        Assert.AreNotEqual(before.Identity, after.Identity);
    }

    /// <summary>Proves a different entry cannot impersonate a snapshot by preserving its metadata tuple.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task InspectAndPreflightWhenFileIsReplacedWithMatchingMetadataRejectReplacement()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string path = root.WriteFile("a.txt", "first");
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.CreateDirectory("dest"));
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileEntrySnapshot before = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(path));

        root.ReplaceFilePreservingMetadata("a.txt", "other");
        FileEntrySnapshot after = await WindowsLocalOperationTestContext.InspectAsync(adapter, WindowsLocalOperationTestContext.ParsePath(path));
        TransferPreflightOutcome outcome = await adapter.PreflightTransferAsync(
            [before],
            destination,
            CancellationToken.None);

        Assert.AreNotEqual(before.Identity, after.Identity);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
    }

    /// <summary>Proves missing entries and foreign providers are closed failures.</summary>
    [TestMethod]
    public async Task InspectAsyncWhenEntryIsMissingOrNotWindowsLocalFailsClosed()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));

        FileInspectionOutcome missing = await adapter.InspectAsync(WindowsLocalOperationTestContext.ParsePath(root.Resolve("missing")), CancellationToken.None);
        FileInspectionOutcome wsl = await adapter.InspectAsync(WindowsLocalOperationTestContext.ParsePath("\\\\wsl.localhost\\Ubuntu\\home"), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, Assert.IsInstanceOfType<FileInspectionFailed>(missing).Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, Assert.IsInstanceOfType<FileInspectionFailed>(wsl).Failure);
    }
}
