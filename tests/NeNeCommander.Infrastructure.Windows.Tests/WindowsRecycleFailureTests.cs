using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves failures preserve sources or report the already completed recycle.</summary>
[TestClass]
public sealed class WindowsRecycleFailureTests
{
    /// <summary>A source disappearing between capability and native activation remains a typed failure.</summary>
    [TestMethod]
    public void RecycleWhenSourceDisappearsAtShellOpenReturnsNotFound()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        ScriptedWindowsRecycleShell shell = new() { OperationFailure = new FileNotFoundException("owned missing source") };
        ProviderStepOutcome result = new WindowsRecycleBin(shell).Recycle(source, CancellationToken.None);
        Assert.AreSame(FileOperationFailureKind.NotFound, result.Failure);
        Assert.IsNull(result.Effect);
        Assert.AreEqual(0, shell.Operation.PerformCount);
    }

    /// <summary>Expected filesystem exceptions cannot hide a confirmed callback effect.</summary>
    [TestMethod]
    public void RecycleWhenFilesystemFailsAfterReceiptPreservesCompletedEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        ScriptedWindowsRecycleShell shell = new()
        {
            Operation = new ScriptedWindowsShellOperation
            {
                Perform = (sink, entry) =>
                {
                    Assert.AreEqual(0, ScriptedWindowsShellOperation.CompleteRecycle(sink, entry));
                    throw new FileNotFoundException("owned missing source");
                },
            },
        };
        ProviderStepOutcome result = new WindowsRecycleBin(shell).Recycle(WindowsRecycleBinTests.Source(root), CancellationToken.None);
        Assert.AreSame(FileOperationFailureKind.NotFound, result.Failure);
        Assert.AreSame(ProviderStepEffectKind.Recycled, result.Effect);
    }

    /// <summary>Copy-engine permission, missing-path and recycle-policy failures stay distinct.</summary>
    [TestMethod]
    [DataRow(0x80270021u, "Denied")]
    [DataRow(0x80270022u, "Denied")]
    [DataRow(0x8027003Fu, "Denied")]
    [DataRow(0x80270023u, "Missing")]
    [DataRow(0x80270024u, "Missing")]
    [DataRow(0x80270036u, "Unavailable")]
    [DataRow(0x80270037u, "Unavailable")]
    [DataRow(0x80270038u, "Unavailable")]
    [DataRow(0x8027003Au, "Unavailable")]
    public void RecycleWhenShellRejectsItemNormalizesFailureWithoutFallback(uint code, string expected)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        ScriptedWindowsRecycleShell shell = new()
        {
            Operation = new ScriptedWindowsShellOperation { Perform = (_, _) => unchecked((int)code) },
        };
        ProviderStepOutcome result = new WindowsRecycleBin(shell).Recycle(source, CancellationToken.None);
        FileOperationFailureKind failure = expected == "Denied" ? FileOperationFailureKind.AccessDenied :
            expected == "Missing" ? FileOperationFailureKind.NotFound : FileOperationFailureKind.ProviderUnavailable;
        Assert.AreSame(failure, result.Failure);
        Assert.IsNull(result.Effect);
        Assert.AreEqual("owned", File.ReadAllText(source.Path.CanonicalText));
    }

    /// <summary>An exception after the callback cannot erase an already completed effect.</summary>
    [TestMethod]
    [DataRow("ComOrIo")]
    [DataRow("AccessDenied")]
    public void RecycleWhenPerformThrowsAfterReceiptPreservesCompletedEffect(string failureKind)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        ScriptedWindowsRecycleShell shell = new()
        {
            Operation = new ScriptedWindowsShellOperation
            {
                Perform = (sink, entry) =>
                {
                    Assert.AreEqual(0, ScriptedWindowsShellOperation.CompleteRecycle(sink, entry));
                    throw failureKind == "AccessDenied" ? new UnauthorizedAccessException("owned failure") : Assert.IsInstanceOfType<COMException>(Marshal.GetExceptionForHR(unchecked((int)0x80004005)));
                },
            },
        };
        ProviderStepOutcome result = new WindowsRecycleBin(shell).Recycle(WindowsRecycleBinTests.Source(root), CancellationToken.None);
        Assert.AreSame(failureKind == "AccessDenied" ? FileOperationFailureKind.AccessDenied : FileOperationFailureKind.ProviderUnavailable, result.Failure);
        Assert.AreSame(ProviderStepEffectKind.Recycled, result.Effect);
    }

    /// <summary>A disappeared recycle capability never selects a permanent-delete implementation.</summary>
    [TestMethod]
    public void RecycleWhenCapabilityWasLostLeavesSourceIntact()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        ScriptedWindowsRecycleShell shell = new() { BinQueryResult = -1 };
        ProviderStepOutcome result = new WindowsRecycleBin(shell).Recycle(source, CancellationToken.None);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, result.Failure);
        Assert.AreEqual(0, shell.Operation.PerformCount);
        Assert.AreEqual("owned", File.ReadAllText(source.Path.CanonicalText));
    }

    /// <summary>STA activation errors remain typed failures before the source can change.</summary>
    [TestMethod]
    [DataRow("ComOrIo")]
    [DataRow("AccessDenied")]
    public void RecycleWhenOperationCannotOpenReportsFailure(string failureKind)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        ScriptedWindowsRecycleShell shell = new()
        {
            OperationFailure = failureKind == "AccessDenied" ? new UnauthorizedAccessException("owned failure") : Marshal.GetExceptionForHR(unchecked((int)0x80004005)),
        };
        ProviderStepOutcome result = new WindowsRecycleBin(shell).Recycle(WindowsRecycleBinTests.Source(root), CancellationToken.None);
        Assert.AreSame(failureKind == "AccessDenied" ? FileOperationFailureKind.AccessDenied : FileOperationFailureKind.ProviderUnavailable, result.Failure);
        Assert.IsNull(result.Effect);
    }

    /// <summary>A cancellation before queuing never calls the destructive Shell operation.</summary>
    [TestMethod]
    public void RecycleWhenAlreadyCancelledDoesNotPerform()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        ScriptedWindowsRecycleShell shell = new();
        ProviderStepOutcome result = new WindowsRecycleBin(shell).Recycle(WindowsRecycleBinTests.Source(root), new CancellationToken(true));
        Assert.AreSame(FileOperationFailureKind.Delete, result.Failure);
        Assert.AreEqual(0, shell.Operation.PerformCount);
        Assert.AreEqual(0u, shell.Operation.Flags);
    }

    /// <summary>Filesystem errors during a capability query do not claim recoverability.</summary>
    [TestMethod]
    [DataRow("ComOrIo")]
    [DataRow("AccessDenied")]
    public void GetCapabilityWhenEntryCannotBeQueriedReturnsPermanentOnly(string failureKind)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        ScriptedWindowsRecycleShell shell = new()
        {
            OpenFailure = failureKind == "AccessDenied" ? new UnauthorizedAccessException("owned failure") : new IOException("owned failure"),
        };
        Assert.AreSame(DeletionCapability.PermanentOnly,
            new WindowsRecycleBin(shell).GetCapability(WindowsRecycleBinTests.Path(root.WriteFile("source", "owned"))));
    }
}
