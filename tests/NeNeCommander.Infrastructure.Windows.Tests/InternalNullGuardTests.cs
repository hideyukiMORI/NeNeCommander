using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Directories;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;
using NeNeCommander.Infrastructure.Windows.Settings;
using NeNeCommander.Infrastructure.Windows.Wsl;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves internal Infrastructure seams reject each absent required argument themselves, before any
/// later dereference or delegated guard could report a different failure.
/// </summary>
[TestClass]
public sealed class InternalNullGuardTests
{
    /// <summary>Proves each internal constructor rejects its absent required collaborator.</summary>
    /// <param name="argument">Owner and parameter that is passed as null.</param>
    [TestMethod]
    [DataRow("WindowsDriveBoundary.readType")]
    [DataRow("WindowsDirectoryReadOperation.observeDriveKind")]
    [DataRow("WindowsLocalDirectoryReader.driveBoundary")]
    [DataRow("VerifiedTreeAccess.wsl")]
    [DataRow("VerifiedTreeAccess.driveBoundary")]
    [DataRow("WindowsLocalFileOperationAdapter.recycleBin")]
    [DataRow("WindowsLocalOperationSteps.wslFileSystem")]
    [DataRow("WindowsLocalOperationSteps.driveBoundary")]
    [DataRow("WindowsRecycleBin.driveBoundary")]
    [DataRow("WindowsRecycleProgress.driveBoundary")]
    [DataRow("WindowsToWslTransfer.driveBoundary")]
    [DataRow("WslToWindowsTransfer.driveBoundary")]
    [DataRow("WindowsTreeCopyAccess.resolveTarget")]
    [DataRow("WindowsTreeCopyAccess.inspectSource")]
    [DataRow("WindowsTreeCopyAccess.driveBoundary")]
    [DataRow("WindowsLocalSettingsStore.dependencies")]
    [DataRow("WindowsLocalSettingsStoreDependencies.driveBoundary")]
    public void ConstructWhenRequiredInternalArgumentIsNullThrowsArgumentNullException(string argument)
    {
        Action construct = Construction(argument);

        _ = Assert.ThrowsExactly<ArgumentNullException>(construct);
    }

    /// <summary>Proves each internal operation rejects its absent required argument before using it.</summary>
    /// <param name="argument">Operation and parameter that is passed as null.</param>
    [TestMethod]
    [DataRow("WindowsDriveBoundary.ReadType.path")]
    [DataRow("WindowsLocalIoExecutionBoundary.ExecuteAsync.operation")]
    [DataRow("WindowsLocalIoExecutionBoundary.ExecuteShellAsync.operation")]
    [DataRow("WindowsLocalTreeCopy.ContainsReparsePoint.entry")]
    [DataRow("WindowsLocalTreeCopy.ContainsReparsePoint.admit")]
    public void InvokeWhenRequiredInternalArgumentIsNullThrowsArgumentNullException(string argument)
    {
        Action invoke = Invocation(argument);

        _ = Assert.ThrowsExactly<ArgumentNullException>(invoke);
    }

    /// <summary>Proves each WSL transport operation rejects its absent argument before resolving any path.</summary>
    /// <param name="argument">Operation and parameter that is passed as null.</param>
    [TestMethod]
    [DataRow("Find.path")]
    [DataRow("TargetExists.path")]
    [DataRow("ContainsReparsePoint.target")]
    [DataRow("Copy.target")]
    [DataRow("PreflightToWindowsLocal.source")]
    [DataRow("CopyToWindowsLocal.source")]
    [DataRow("CreateDirectory.target")]
    [DataRow("CreateEmptyFile.target")]
    [DataRow("Rename.target")]
    public void InvokeWhenWslFileSystemArgumentIsNullThrowsArgumentNullException(string argument)
    {
        Action invoke = WslInvocation(argument);

        _ = Assert.ThrowsExactly<ArgumentNullException>(invoke);
    }

    /// <summary>Proves the bounded process reader rejects an absent reader before reading.</summary>
    [TestMethod]
    public async Task ReadBoundedAsyncWhenReaderIsNullThrowsArgumentNullException()
    {
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => WslDistributionProcess.ReadBoundedAsync(null!, () => { }, CancellationToken.None));
    }

    /// <summary>Proves the bounded process reader rejects an absent termination even when output is small.</summary>
    [TestMethod]
    public async Task ReadBoundedAsyncWhenTerminateIsNullThrowsArgumentNullException()
    {
        using StringReader reader = new("Ubuntu");

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => WslDistributionProcess.ReadBoundedAsync(reader, null!, CancellationToken.None));
    }

    private static Action Construction(string argument)
    {
        WindowsDriveBoundary boundary = new(_ => DriveType.Fixed);
        ScriptedWslFileSystem wsl = new();
        WindowsLocalIoExecutionBoundary execution = new();
        return argument switch
        {
            "WindowsDriveBoundary.readType" => () => _ = new WindowsDriveBoundary(null!),
            "WindowsDirectoryReadOperation.observeDriveKind" => () => _ = new WindowsDirectoryReadOperation(
                () => [], _ => throw new InvalidOperationException("Not classified."), null!),
            "WindowsLocalDirectoryReader.driveBoundary" => () => _ = new WindowsLocalDirectoryReader(
                execution, new WindowsDirectoryEnumerator(), null!),
            "VerifiedTreeAccess.wsl" => () => _ = new VerifiedTreeAccess(null!, boundary),
            "VerifiedTreeAccess.driveBoundary" => () => _ = new VerifiedTreeAccess(wsl, null!),
            "WindowsLocalFileOperationAdapter.recycleBin" => () => _ = new WindowsLocalFileOperationAdapter(
                execution, new WindowsLocalAdapterDependencies(null!, wsl, boundary, WindowsOverwriteDependencies.Native(new DeterministicIdentifierSource()))),
            "WindowsLocalOperationSteps.wslFileSystem" => () => _ = new WindowsLocalOperationSteps(null!, boundary, WindowsOverwriteDependencies.Native(new DeterministicIdentifierSource())),
            "WindowsLocalOperationSteps.driveBoundary" => () => _ = new WindowsLocalOperationSteps(wsl, null!, WindowsOverwriteDependencies.Native(new DeterministicIdentifierSource())),
            "WindowsRecycleBin.driveBoundary" => () => _ = new WindowsRecycleBin(new ScriptedWindowsRecycleShell(), null!),
            "WindowsRecycleProgress.driveBoundary" => () => _ = new WindowsRecycleProgress(
                Snapshot(), null!, CancellationToken.None),
            "WindowsToWslTransfer.driveBoundary" => () => _ = new WindowsToWslTransfer(execution, wsl, null!),
            "WslToWindowsTransfer.driveBoundary" => () => _ = new WslToWindowsTransfer(execution, wsl, null!),
            _ => SettingsOrTreeAccessConstruction(argument, boundary, execution),
        };
    }

    private static Action SettingsOrTreeAccessConstruction(
        string argument,
        WindowsDriveBoundary boundary,
        WindowsLocalIoExecutionBoundary execution)
    {
        WindowsLocalPath document = Assert.IsInstanceOfType<WindowsLocalPath>(
            WslOperationTestContext.Path("C:\\settings\\settings.json"));
        return argument switch
        {
            "WindowsTreeCopyAccess.resolveTarget" => () => _ = new WindowsTreeCopyAccess(null!, _ => true, boundary),
            "WindowsTreeCopyAccess.inspectSource" => () => _ = new WindowsTreeCopyAccess(
                path => path.CanonicalText, null!, boundary),
            "WindowsTreeCopyAccess.driveBoundary" => () => _ = new WindowsTreeCopyAccess(
                path => path.CanonicalText, _ => true, null!),
            "WindowsLocalSettingsStore.dependencies" => () => _ = new WindowsLocalSettingsStore(
                document, execution, (WindowsLocalSettingsStoreDependencies)null!),
            "WindowsLocalSettingsStoreDependencies.driveBoundary" => () => _ = new WindowsLocalSettingsStoreDependencies(
                new DeterministicIdentifierSource(), new ScriptedSettingsWriteTestHook(), null!),
            _ => throw new AssertFailedException("The guarded argument is not declared."),
        };
    }

    private static Action Invocation(string argument)
    {
        WindowsLocalIoExecutionBoundary execution = new(new CompletingScheduler(), new CompletingScheduler());
        return argument switch
        {
            "WindowsDriveBoundary.ReadType.path" => () => _ = new WindowsDriveBoundary(_ => DriveType.Fixed).ReadType(null!),
            "WindowsLocalIoExecutionBoundary.ExecuteAsync.operation" => () => _ = execution.ExecuteAsync<int>(null!),
            "WindowsLocalIoExecutionBoundary.ExecuteShellAsync.operation" => () => _ = execution.ExecuteShellAsync<int>(null!),
            "WindowsLocalTreeCopy.ContainsReparsePoint.entry" => () => _ = WindowsLocalTreeCopy.ContainsReparsePoint(
                null!, _ => false),
            "WindowsLocalTreeCopy.ContainsReparsePoint.admit" => () => _ = WindowsLocalTreeCopy.ContainsReparsePoint(
                new FileInfo("C:\\unused-entry"), null!),
            _ => throw new AssertFailedException("The guarded argument is not declared."),
        };
    }

    private static Action WslInvocation(string argument)
    {
        WindowsWslFileSystem fileSystem = new(
            _ => throw new InvalidOperationException("A null argument reached path resolution."),
            _ => throw new InvalidOperationException("A null argument reached identity reading."),
            new WindowsDriveBoundary(_ => DriveType.Fixed));
        WslFileSystemEntry source = WslOperationTestContext.Entry(
            WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\source.txt"), "wsl-source", DirectoryEntryKind.File);
        WindowsLocalPath local = Assert.IsInstanceOfType<WindowsLocalPath>(WslOperationTestContext.Path("C:\\target.txt"));
        return argument switch
        {
            "Find.path" => () => _ = fileSystem.Find(null!),
            "TargetExists.path" => () => _ = fileSystem.TargetExists(null!),
            "ContainsReparsePoint.target" => () => _ = fileSystem.ContainsReparsePoint((WslPath)null!),
            "Copy.target" => () => _ = fileSystem.Copy(source, null!),
            "PreflightToWindowsLocal.source" => () => _ = fileSystem.PreflightToWindowsLocal(null!, local),
            "CopyToWindowsLocal.source" => () => _ = fileSystem.CopyToWindowsLocal(null!, local),
            "CreateDirectory.target" => () => fileSystem.CreateDirectory(null!),
            "CreateEmptyFile.target" => () => fileSystem.CreateEmptyFile(null!),
            "Rename.target" => () => fileSystem.Rename(source, null!),
            _ => throw new AssertFailedException("The guarded argument is not declared."),
        };
    }

    private static FileEntrySnapshot Snapshot()
    {
        return FileEntrySnapshot.Create(
            WslOperationTestContext.Path("C:\\source.txt"),
            WslOperationTestContext.Entry(
                WslOperationTestContext.Wsl("\\\\wsl.localhost\\Ubuntu\\source.txt"), "local-source",
                DirectoryEntryKind.File).Identity,
            DeletionCapability.Recycle);
    }

    private sealed class CompletingScheduler : IWindowsLocalIoScheduler
    {
        public Task<TResult> ScheduleAsync<TResult>(Func<TResult> operation)
        {
            return Task.FromResult(default(TResult)!);
        }
    }
}
