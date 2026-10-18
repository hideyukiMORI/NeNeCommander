using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Exercises callback ordering, cancellation and unrequested Shell operations.</summary>
[TestClass]
public sealed class WindowsRecycleCallbackTests
{
    /// <summary>A drive reassignment before Shell's effect callback vetoes recycle.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public void PreDeleteItemWhenDriveRemapsToNetworkVetoesShellEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        DriveType observed = DriveType.Fixed;
        WindowsDriveBoundary boundary = new(_ => observed);
        ScriptedWindowsShellOperation operation = new()
        {
            Perform = (sink, entry) =>
            {
                observed = DriveType.Network;
                return sink.PreDeleteItem(WindowsRecycleProgress.RecycleFlag, entry);
            },
        };
        WindowsRecycleBin bin = new(new ScriptedWindowsRecycleShell { Operation = operation }, boundary);

        ProviderStepOutcome outcome = bin.Recycle(source, CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.AreEqual(1, operation.PerformCount);
        Assert.IsTrue(File.Exists(source.Path.CanonicalText));
    }

    /// <summary>Cancellation after admission cannot hide a receipt for work already completed.</summary>
    [TestMethod]
    public void PostDeleteItemWhenCancellationPrecedesReceiptRetainsCompletedEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        using CancellationTokenSource cancellation = new();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), cancellation.Token);
        ScriptedWindowsShellEntry entry = new(source.Path.CanonicalText);
        Assert.AreEqual(0, progress.PreDeleteItem(0x80, entry));
        cancellation.Cancel();
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.UpdateProgress(1, 0));
        Assert.AreEqual(0, progress.PostDeleteItem(0x80, entry, 0, new ScriptedWindowsShellEntry("completed owned receipt")));
        ProviderStepOutcome result = progress.Complete(0, 0, 1);
        Assert.AreSame(FileOperationFailureKind.Delete, result.Failure);
        Assert.AreSame(ProviderStepEffectKind.Recycled, result.Effect);
    }

    /// <summary>A vetoed pre-delete cannot authorize a later receipt.</summary>
    [TestMethod]
    public void PostDeleteItemWhenPreDeleteWasVetoedDoesNotConfirmRecycle()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), new CancellationToken(true));
        ScriptedWindowsShellEntry entry = new(source.Path.CanonicalText);
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PreDeleteItem(0x80, entry));
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PostDeleteItem(0x80, entry, 0, new ScriptedWindowsShellEntry("unadmitted receipt")));
        Assert.IsNull(progress.Complete(0, 0, 0).Effect);
    }

    /// <summary>An earlier rejected mutation prevents later admission in the same operation.</summary>
    [TestMethod]
    public void PreDeleteItemWhenOperationAlreadyFailedDoesNotAdmitSource()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), CancellationToken.None);
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.RejectUnrequestedMutation());
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PreDeleteItem(0x80, new ScriptedWindowsShellEntry(source.Path.CanonicalText)));
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, progress.Failure);
    }

    /// <summary>Unexpected mutation callbacks cannot authorize a second kind of effect.</summary>
    [TestMethod]
    [DataRow("PreRename")]
    [DataRow("PostRename")]
    [DataRow("PreMove")]
    [DataRow("PostMove")]
    [DataRow("PreCopy")]
    [DataRow("PostCopy")]
    [DataRow("PreNew")]
    [DataRow("PostNew")]
    public void CallbackWhenMutationWasNotRequestedVetoesOperation(string callback)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), CancellationToken.None);
        using NativeWindowsRecycleProgress native = new(progress);
        int result = callback switch
        {
            "PreRename" => native.PreRenameItem(0, IntPtr.Zero, "unexpected"),
            "PostRename" => native.PostRenameItem(0, IntPtr.Zero, "unexpected", 0, IntPtr.Zero),
            "PreMove" => native.PreMoveItem(0, IntPtr.Zero, IntPtr.Zero, "unexpected"),
            "PostMove" => native.PostMoveItem(0, IntPtr.Zero, IntPtr.Zero, "unexpected", 0, IntPtr.Zero),
            "PreCopy" => native.PreCopyItem(0, IntPtr.Zero, IntPtr.Zero, "unexpected"),
            "PostCopy" => native.PostCopyItem(0, IntPtr.Zero, IntPtr.Zero, "unexpected", 0, IntPtr.Zero),
            "PreNew" => native.PreNewItem(0, IntPtr.Zero, "unexpected"),
            "PostNew" => native.PostNewItem(0, IntPtr.Zero, "unexpected", "", 0, 0, IntPtr.Zero),
            _ => throw new AssertFailedException("Unknown callback fixture."),
        };
        Assert.AreEqual(WindowsRecycleProgress.Abort, result);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, progress.Complete(0, 0, 0).Failure);
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PreDeleteItem(0x80,
            new ScriptedWindowsShellEntry(source.Path.CanonicalText)));
        Assert.IsNull(progress.Complete(0, 0, 0).Effect);
    }

    /// <summary>Harmless progress callbacks observe cancellation without inventing an effect.</summary>
    [TestMethod]
    public void UpdateProgressWhenCancelledVetoesFurtherWork()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        using CancellationTokenSource cancellation = new();
        WindowsRecycleProgress progress = new(WindowsRecycleBinTests.Source(root), new WindowsDriveBoundary(), cancellation.Token);
        using NativeWindowsRecycleProgress native = new(progress);
        Assert.AreEqual(0, native.StartOperations());
        Assert.AreEqual(0, native.ResetTimer());
        Assert.AreEqual(0, native.PauseTimer());
        Assert.AreEqual(0, native.ResumeTimer());
        Assert.AreEqual(0, native.UpdateProgress(1, 0));
        cancellation.Cancel();
        Assert.AreEqual(WindowsRecycleProgress.Abort, native.UpdateProgress(1, 0));
        Assert.IsNull(progress.Complete(0, 0, 0).Effect);
    }

    /// <summary>A repeated or invalid completion cannot replace the first confirmed receipt.</summary>
    [TestMethod]
    public void PostDeleteItemWhenRepeatedPreservesFirstEffectAndReportsFailure()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), CancellationToken.None);
        ScriptedWindowsShellEntry entry = new(source.Path.CanonicalText);
        ScriptedWindowsShellEntry receipt = new("first receipt");
        Assert.AreEqual(0, progress.PreDeleteItem(0x80, entry));
        Assert.AreEqual(0, progress.PostDeleteItem(0x80, entry, 0, receipt));
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PostDeleteItem(0x80, entry, 0, new ScriptedWindowsShellEntry("unexpected receipt")));
        Assert.AreSame(receipt, progress.RecycledEntry);
        Assert.AreSame(receipt, progress.ObservedEntry);
        Assert.AreSame(ProviderStepEffectKind.Recycled, progress.Complete(0, 0, 0).Effect);
    }

    /// <summary>An abort, unknown API status or finish failure does not erase a completed recycle.</summary>
    [TestMethod]
    [DataRow(1, 0, 0)]
    [DataRow(0, 1, 0)]
    [DataRow(0, 0, unchecked((int)0x80070005))]
    public void CompleteWhenLaterApiFailsRetainsConfirmedReceipt(int performed, int queried, int finished)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), CancellationToken.None);
        Assert.AreEqual(0, ScriptedWindowsShellOperation.CompleteRecycle(progress, new ScriptedWindowsShellEntry(source.Path.CanonicalText)));
        using NativeWindowsRecycleProgress native = new(progress);
        Assert.AreEqual(finished < 0 ? WindowsRecycleProgress.Abort : 0, native.FinishOperations(finished));
        ProviderStepOutcome result = progress.Complete(performed, queried, 0);
        Assert.AreSame(finished < 0 ? FileOperationFailureKind.AccessDenied : FileOperationFailureKind.ProviderUnavailable, result.Failure);
        Assert.AreSame(ProviderStepEffectKind.Recycled, result.Effect);
    }
}
