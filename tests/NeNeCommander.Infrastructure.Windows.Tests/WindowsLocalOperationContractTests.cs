using System;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves the local adapter proof for OperationContract.</summary>
[TestClass]
public sealed class WindowsLocalOperationContractTests
{
    /// <summary>Proves an unknown platform failure falls back to the step's own failure kind.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-017")]
    public void NormalizeWhenHResultIsUnknownFallsBackToStepKind()
    {
        Assert.AreSame(
            FileOperationFailureKind.Copy,
            WindowsLocalFileOperationAdapter.Normalize(unchecked((int)0x81234567), FileOperationFailureKind.Copy));
        Assert.AreSame(
            FileOperationFailureKind.AccessDenied,
            WindowsLocalFileOperationAdapter.Normalize(unchecked((int)0x80070005), FileOperationFailureKind.Copy));
    }

    /// <summary>Proves the shared target resolver keeps a direct name beneath its exact parent.</summary>
    [TestMethod]
    public void DeriveChildWhenNameIsOneSegmentReturnsContainedPath()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();

        FileSystemPath resolved = Assert.IsInstanceOfType<WindowsLocalPath>(WindowsLocalTreeCopy.DeriveChild(root.Path, "child.txt"));

        Assert.AreEqual(root.Resolve("child.txt"), resolved.CanonicalText, ignoreCase: true);
    }

    /// <summary>Proves rooted and multi-segment names cannot replace or escape the target parent.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-009")]
    [DataRow("")]
    [DataRow(".")]
    [DataRow("..")]
    [DataRow("..\\escape.txt")]
    [DataRow("nested\\escape.txt")]
    [DataRow("C:\\escape.txt")]
    public void DeriveChildWhenNameIsNotOneSegmentRejectsName(string childName)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();

        Assert.IsNull(WindowsLocalTreeCopy.DeriveChild(root.Path, childName));
    }

    /// <summary>Proves each required direct-child resolver argument is rejected at its own boundary.</summary>
    [TestMethod]
    public void DeriveChildWhenArgumentIsNullThrowsNamedArgumentNullException()
    {
        MethodInfo method = typeof(WindowsLocalTreeCopy).GetMethod(
            "DeriveChild",
            BindingFlags.Static | BindingFlags.NonPublic) ??
            throw new AssertFailedException("The direct-child resolver was not found.");

        WindowsLocalOperationTestContext.AssertResolverNullGuard(method, [null, "child.txt"], "parent");
        WindowsLocalOperationTestContext.AssertResolverNullGuard(method, [WindowsLocalOperationTestContext.ParsePath("C:\\parent"), null], "name");
    }

    /// <summary>Proves every port method rejects an absent argument before touching the filesystem.</summary>
    [TestMethod]
    public void PortMethodsWhenRequiredArgumentIsNullThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => new WindowsLocalFileOperationAdapter(null!));
        WindowsLocalFileOperationAdapter adapter = new(new(), new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileSystemPath path = WindowsLocalOperationTestContext.ParsePath("C:\\source");
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("file|0|0|0")).Identity;
        FileEntrySnapshot snapshot = FileEntrySnapshot.Create(path, identity, DeletionCapability.PermanentOnly);

        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.InspectAsync), [null, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.PreflightTransferAsync), [null, path, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.PreflightTransferAsync), [new[] { snapshot }, null, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.CopyAsync), [null, path, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.CopyAsync), [snapshot, null, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.GetAtomicMoveCapabilityAsync), [null, path, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.GetAtomicMoveCapabilityAsync), [snapshot, null, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.MoveAsync), [null, path, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.MoveAsync), [snapshot, null, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.VerifyCopyAsync), [null, path, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.VerifyCopyAsync), [snapshot, null, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.DeleteAsync), [null, DeletionExecutionMode.Permanent, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.DeleteAsync), [snapshot, null, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.CreateDirectoryAsync), [null, path, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.CreateDirectoryAsync), [snapshot, null, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.RenameAsync), [null, path, CancellationToken.None]);
        WindowsLocalOperationTestContext.AssertNullGuard(adapter, nameof(IFileOperationPort.RenameAsync), [snapshot, null, CancellationToken.None]);
    }
}
