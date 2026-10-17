using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves invalid collaborators fail at the boundary before native work can begin.</summary>
[TestClass]
public sealed class WindowsRecycleGuardTests
{
    /// <summary>Missing native and policy owners cannot produce partially valid wrappers.</summary>
    [TestMethod]
    public void ConstructorsWhenCollaboratorIsNullRejectImmediately()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), CancellationToken.None);
        ScriptedWindowsShellEntry entry = new(source.Path.CanonicalText);
        ScriptedWindowsShellOperation operation = new();
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsRecycleBin(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsRecycleProgress(null!, new WindowsDriveBoundary(),
            CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new NativeWindowsRecycleProgress(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsShellEntry(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsShellOperation(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsRecycleSession(null!, entry, progress));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsRecycleSession(operation, null!, progress));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsRecycleSession(operation, entry, null!));
    }

    /// <summary>Null requests fail before capability inspection or a thread is started.</summary>
    [TestMethod]
    public void EntryPointsWhenRequestIsNullRejectBeforeScheduling()
    {
        WindowsRecycleBin bin = new(new ScriptedWindowsRecycleShell());
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => bin.GetCapability(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => bin.Recycle(null!, CancellationToken.None));
        WindowsShellIoScheduler scheduler = new();
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => scheduler.ScheduleAsync<int>(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsLocalIoExecutionBoundary(null!, scheduler));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new WindowsLocalIoExecutionBoundary(scheduler, null!));
        WindowsLocalIoExecutionBoundary boundary = new(scheduler, scheduler);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => boundary.ExecuteAsync<int>(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => boundary.ExecuteShellAsync<int>(null!));
    }

    /// <summary>Capability probes release their temporary entry before returning.</summary>
    [TestMethod]
    public void GetCapabilityWhenProbeCompletesReleasesEntry()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        using TrackingWindowsRecycleShell shell = new();
        _ = new WindowsRecycleBin(shell).GetCapability(WindowsRecycleBinTests.Path(root.WriteFile("source", "owned")));
        Assert.AreEqual(1, shell.EntryReleaseCount);
    }

    /// <summary>A failed operation allocation releases the source acquired just before it.</summary>
    [TestMethod]
    public void OpenSessionWhenOperationCreationFailsReleasesEntry()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        using TrackingWindowsRecycleShell shell = new() { OperationFailure = new InvalidOperationException("owned allocation failure") };
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => new WindowsRecycleBin(shell).OpenSession(source, CancellationToken.None));
        Assert.AreEqual(1, shell.EntryReleaseCount);
    }

    /// <summary>Successful allocation transfers both owners to the session until disposal.</summary>
    [TestMethod]
    public void DisposeWhenSessionOwnsResourcesReleasesOnlyAtSessionEnd()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        using TrackingWindowsRecycleShell shell = new();
        using WindowsRecycleSession session = new WindowsRecycleBin(shell).OpenSession(source, CancellationToken.None);
        Assert.AreEqual(0, shell.EntryReleaseCount);
        Assert.AreEqual(0, shell.OperationReleaseCount);
        session.Dispose();
        Assert.AreEqual(1, shell.EntryReleaseCount);
        Assert.AreEqual(1, shell.OperationReleaseCount);
    }
}
