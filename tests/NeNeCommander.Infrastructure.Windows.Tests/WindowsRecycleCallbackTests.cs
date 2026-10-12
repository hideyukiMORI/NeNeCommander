using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Exercises callback ordering, cancellation and unrequested Shell operations.</summary>
[TestClass]
public sealed class WindowsRecycleCallbackTests
{
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
        WindowsRecycleProgress progress = new(WindowsRecycleBinTests.Source(root), CancellationToken.None);
        int result = callback switch
        {
            "PreRename" => progress.PreRenameItem(0, IntPtr.Zero, "unexpected"),
            "PostRename" => progress.PostRenameItem(0, IntPtr.Zero, "unexpected", 0, IntPtr.Zero),
            "PreMove" => progress.PreMoveItem(0, IntPtr.Zero, IntPtr.Zero, "unexpected"),
            "PostMove" => progress.PostMoveItem(0, IntPtr.Zero, IntPtr.Zero, "unexpected", 0, IntPtr.Zero),
            "PreCopy" => progress.PreCopyItem(0, IntPtr.Zero, IntPtr.Zero, "unexpected"),
            "PostCopy" => progress.PostCopyItem(0, IntPtr.Zero, IntPtr.Zero, "unexpected", 0, IntPtr.Zero),
            "PreNew" => progress.PreNewItem(0, IntPtr.Zero, "unexpected"),
            "PostNew" => progress.PostNewItem(0, IntPtr.Zero, "unexpected", "", 0, 0, IntPtr.Zero),
            _ => throw new AssertFailedException("Unknown callback fixture."),
        };
        Assert.AreEqual(WindowsRecycleProgress.Abort, result);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, progress.Complete(0, 0, 0).Failure);
    }

    /// <summary>Harmless progress callbacks observe cancellation without inventing an effect.</summary>
    [TestMethod]
    public void UpdateProgressWhenCancelledVetoesFurtherWork()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        using CancellationTokenSource cancellation = new();
        WindowsRecycleProgress progress = new(WindowsRecycleBinTests.Source(root), cancellation.Token);
        Assert.AreEqual(0, progress.StartOperations());
        Assert.AreEqual(0, progress.ResetTimer());
        Assert.AreEqual(0, progress.PauseTimer());
        Assert.AreEqual(0, progress.ResumeTimer());
        Assert.AreEqual(0, progress.UpdateProgress(1, 0));
        cancellation.Cancel();
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.UpdateProgress(1, 0));
        Assert.IsNull(progress.Complete(0, 0, 0).Effect);
    }

    /// <summary>A repeated or invalid completion cannot replace the first confirmed receipt.</summary>
    [TestMethod]
    public void PostDeleteItemWhenRepeatedPreservesFirstEffectAndReportsFailure()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, CancellationToken.None);
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
        WindowsRecycleProgress progress = new(source, CancellationToken.None);
        Assert.AreEqual(0, ScriptedWindowsShellOperation.CompleteRecycle(progress, new ScriptedWindowsShellEntry(source.Path.CanonicalText)));
        Assert.AreEqual(finished < 0 ? WindowsRecycleProgress.Abort : 0, progress.FinishOperations(finished));
        ProviderStepOutcome result = progress.Complete(performed, queried, 0);
        Assert.AreSame(finished < 0 ? FileOperationFailureKind.AccessDenied : FileOperationFailureKind.ProviderUnavailable, result.Failure);
        Assert.AreSame(ProviderStepEffectKind.Recycled, result.Effect);
    }
}
