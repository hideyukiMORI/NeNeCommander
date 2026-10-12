using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves recycle admission and the Shell completion contract without touching a recycle bin.</summary>
[TestClass]
public sealed class WindowsRecycleBinTests
{
    /// <summary>Requires both entry attributes and an available recycle namespace.</summary>
    [TestMethod]
    [DataRow(0x40000020u, 0, 0, "Recycle")]
    [DataRow(0x40000000u, 0, 0, "Permanent")]
    [DataRow(0x20u, 0, 0, "Permanent")]
    [DataRow(0x40000020u, -1, 0, "Permanent")]
    [DataRow(0x40000020u, 0, -1, "Permanent")]
    public void GetCapabilityWhenShellFactsDifferReportsOnlyQueriedCapability(uint attributes, int attributeResult, int queryResult, string expected)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalPath path = Path(root.WriteFile("source", "owned"));
        WindowsRecycleBin bin = new(new ScriptedWindowsRecycleShell
        {
            Attributes = attributes,
            AttributesResult = attributeResult,
            BinQueryResult = queryResult,
        });
        Assert.AreSame(expected == "Recycle" ? DeletionCapability.Recycle : DeletionCapability.PermanentOnly, bin.GetCapability(path));
    }

    /// <summary>A successful queue is insufficient; an admitted callback receipt is required.</summary>
    [TestMethod]
    public void RecycleWhenShellCompletesOneReceiptSucceedsWithStrictFlags()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = Source(root);
        ScriptedWindowsRecycleShell shell = new();
        ProviderStepOutcome result = new WindowsRecycleBin(shell).Recycle(source, CancellationToken.None);
        Assert.IsNull(result.Failure);
        Assert.AreEqual(1, shell.Operation.PerformCount);
        Assert.AreEqual(0x2098A414u, shell.Operation.Flags);
    }

    /// <summary>Queued, skipped, aborted and failed operations do not manufacture a recycle.</summary>
    [TestMethod]
    public void RecycleWhenShellReportsNoCallbackRejectsSuccess()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        ScriptedWindowsShellOperation operation = new() { Perform = (_, _) => 0 };
        ProviderStepOutcome result = Execute(root, operation);
        Assert.AreSame(FileOperationFailureKind.Delete, result.Failure);
        Assert.IsNull(result.Effect);
        Assert.IsTrue(File.Exists(root.Resolve("source")));
    }

    /// <summary>Failures before perform cannot cause an effect.</summary>
    [TestMethod]
    public void RecycleWhenQueueFailsNeverPerforms()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        ScriptedWindowsShellOperation operation = new() { QueueResult = unchecked((int)0x80070005) };
        ProviderStepOutcome result = Execute(root, operation);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, result.Failure);
        Assert.AreEqual(0, operation.PerformCount);
        Assert.IsNull(result.Effect);
    }

    /// <summary>Failure to set mandatory flags stops before queuing any effect.</summary>
    [TestMethod]
    public void RecycleWhenFlagsAreRejectedNeverPerforms()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        ScriptedWindowsShellOperation operation = new() { FlagsResult = -1 };
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, Execute(root, operation).Failure);
        Assert.AreEqual(0, operation.PerformCount);
    }

    /// <summary>A later abort retains the already completed recycle.</summary>
    [TestMethod]
    public void RecycleWhenAbortFollowsConfirmedReceiptReportsPartialEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        ProviderStepOutcome result = Execute(root, new ScriptedWindowsShellOperation { Aborted = 1 });
        Assert.AreSame(FileOperationFailureKind.Delete, result.Failure);
        Assert.AreSame(ProviderStepEffectKind.Recycled, result.Effect);
    }

    /// <summary>Query failure never masks a completed recycle or claims whole-step success.</summary>
    [TestMethod]
    public void RecycleWhenAbortQueryFailsRetainsReceipt()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        ProviderStepOutcome result = Execute(root, new ScriptedWindowsShellOperation { AbortQueryResult = unchecked((int)0x80070005) });
        Assert.AreSame(FileOperationFailureKind.AccessDenied, result.Failure);
        Assert.AreSame(ProviderStepEffectKind.Recycled, result.Effect);
    }

    /// <summary>Unavailable COM capability cannot imply that ordinary deletion is recoverable.</summary>
    [TestMethod]
    public void GetCapabilityWhenShellCannotOpenReturnsPermanentOnly()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsRecycleBin bin = new(new ScriptedWindowsRecycleShell { OpenFailure = Marshal.GetExceptionForHR(unchecked((int)0x80004005)) });
        Assert.AreSame(DeletionCapability.PermanentOnly, bin.GetCapability(Path(root.WriteFile("source", "owned"))));
    }

    internal static FileEntrySnapshot Source(TestOwnedTemporaryRoot root)
    {
        WindowsLocalPath path = Path(root.WriteFile("source", "owned"));
        FileSystemInfo entry = WindowsLocalEntryIdentity.Find(path) ?? throw new InvalidOperationException("Missing owned fixture.");
        return FileEntrySnapshot.Create(path, WindowsLocalEntryIdentity.Describe(entry), DeletionCapability.Recycle);
    }

    private static ProviderStepOutcome Execute(TestOwnedTemporaryRoot root, ScriptedWindowsShellOperation operation)
    {
        return new WindowsRecycleBin(new ScriptedWindowsRecycleShell { Operation = operation }).Recycle(Source(root), CancellationToken.None);
    }

    internal static WindowsLocalPath Path(string text)
    {
        return (WindowsLocalPath)Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
