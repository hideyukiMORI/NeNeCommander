using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the last callback boundary vetoes permanence, replacement and cancellation.</summary>
[TestClass]
public sealed class WindowsRecycleProgressTests
{
    /// <summary>A remap after recycle admission is vetoed by the final Shell callback.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-024")]
    public void PreDeleteItemWhenDriveRemapsToNetworkVetoesEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        DriveType observed = DriveType.Fixed;
        WindowsDriveBoundary drives = new(_ => observed);
        WindowsRecycleProgress progress = new(source, drives, CancellationToken.None);
        observed = DriveType.Network;

        Assert.AreEqual(WindowsRecycleProgress.Abort,
            progress.PreDeleteItem(0x80, new ScriptedWindowsShellEntry(source.Path.CanonicalText)));
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, progress.Failure);
        Assert.IsTrue(File.Exists(root.Resolve("source")));
    }

    /// <summary>Accepts the completed whole-entry result that the Windows copy engine returns for recycling.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(0x00270008)]
    public void PostDeleteItemWhenCompletedEntryHasReceiptConfirmsRecycle(int result)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), CancellationToken.None);
        ScriptedWindowsShellEntry entry = new(source.Path.CanonicalText);
        ScriptedWindowsShellEntry receipt = new("owned receipt");
        Assert.AreEqual(0, progress.PreDeleteItem(0x80, entry));
        Assert.AreEqual(0, progress.PostDeleteItem(0x80, entry, result, receipt));
        Assert.AreSame(receipt, progress.RecycledEntry);
        Assert.IsNull(progress.Complete(0, 0, 0).Failure);
    }

    /// <summary>Pending, skipped and generic nonzero statuses remain unconfirmed even with an object.</summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(0x00270005)]
    [DataRow(0x0027000b)]
    [DataRow(0x00270010)]
    public void PostDeleteItemWhenStatusDoesNotConfirmCompletionRejectsReceipt(int result)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), CancellationToken.None);
        ScriptedWindowsShellEntry entry = new(source.Path.CanonicalText);
        Assert.AreEqual(0, progress.PreDeleteItem(0x80, entry));
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PostDeleteItem(0x80, entry, result, new ScriptedWindowsShellEntry("unconfirmed receipt")));
        Assert.IsNull(progress.RecycledEntry);
        Assert.IsNull(progress.Complete(0, 0, 0).Effect);
    }

    /// <summary>A permanent delete proposal is rejected before Shell may affect the source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-023")]
    public void PreDeleteItemWhenRecycleFlagIsMissingVetoesEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), CancellationToken.None);
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PreDeleteItem(0, new ScriptedWindowsShellEntry(source.Path.CanonicalText)));
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, progress.Failure);
        Assert.IsTrue(File.Exists(root.Resolve("source")));
    }

    /// <summary>The callback must still refer to the exact requested entry.</summary>
    [TestMethod]
    [DataRow("D:\\foreign", 0)]
    [DataRow("relative", 0)]
    [DataRow("D:\\foreign", -1)]
    public void PreDeleteItemWhenShellEntryIsUnexpectedRejects(string path, int result)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsRecycleProgress progress = new(WindowsRecycleBinTests.Source(root), new WindowsDriveBoundary(), CancellationToken.None);
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PreDeleteItem(0x80, new ScriptedWindowsShellEntry(path) { NameResult = result }));
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, progress.Failure);
    }

    /// <summary>The last source check detects replacement after the initial capability query.</summary>
    [TestMethod]
    public void PreDeleteItemWhenSourceChangesRejectsIdentity()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), CancellationToken.None);
        _ = root.WriteFile("source", "changed bytes");
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PreDeleteItem(0x80, new ScriptedWindowsShellEntry(source.Path.CanonicalText)));
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, progress.Failure);
    }

    /// <summary>Cancellation is observed before granting Shell permission to delete.</summary>
    [TestMethod]
    public void PreDeleteItemWhenCancelledVetoesEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        using CancellationTokenSource cancellation = new();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), cancellation.Token);
        cancellation.Cancel();
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PreDeleteItem(0x80, new ScriptedWindowsShellEntry(source.Path.CanonicalText)));
        Assert.IsNull(progress.RecycledEntry);
    }

    /// <summary>A missing receipt never becomes a Recycled effect.</summary>
    [TestMethod]
    [DataRow(0, "Delete")]
    [DataRow(1, "Delete")]
    [DataRow(unchecked((int)0x80070005), "AccessDenied")]
    public void PostDeleteItemWhenNoRecycleReceiptRejectsSuccess(int result, string expected)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), CancellationToken.None);
        ScriptedWindowsShellEntry entry = new(source.Path.CanonicalText);
        Assert.AreEqual(0, progress.PreDeleteItem(0x80, entry));
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PostDeleteItem(0x80, entry, result, null));
        Assert.IsNull(progress.Complete(0, 0, 0).Effect);
        Assert.AreSame(expected == "Delete" ? FileOperationFailureKind.Delete : FileOperationFailureKind.AccessDenied,
            progress.Complete(0, 0, 0).Failure);
    }

    /// <summary>A completion without the recycle flag is not evidence of recoverability.</summary>
    [TestMethod]
    public void PostDeleteItemWhenRecycleFlagIsMissingRejectsReceipt()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), CancellationToken.None);
        ScriptedWindowsShellEntry entry = new(source.Path.CanonicalText);
        ScriptedWindowsShellEntry receipt = new("unconfirmed receipt");
        Assert.AreEqual(0, progress.PreDeleteItem(0x80, entry));
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PostDeleteItem(0, entry, 0, receipt));
        Assert.IsNull(progress.RecycledEntry);
        Assert.AreSame(FileOperationFailureKind.Delete, progress.Complete(0, 0, 0).Failure);
    }

    /// <summary>The first causal refusal remains visible after a later generic callback failure.</summary>
    [TestMethod]
    public void FinishOperationsWhenAlreadyRejectedRetainsFirstFailure()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), CancellationToken.None);
        ScriptedWindowsShellEntry entry = new(source.Path.CanonicalText);
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PreDeleteItem(0, entry));
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.FinishOperations(unchecked((int)0x80070005)));
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, progress.Failure);
    }

    /// <summary>Neither an unadmitted completion nor an additional callback is authorized.</summary>
    [TestMethod]
    public void PostDeleteItemWhenNoPreDeleteOccurredRejectsReceipt()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), CancellationToken.None);
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PostDeleteItem(0x80,
            new ScriptedWindowsShellEntry(source.Path.CanonicalText), 0, new ScriptedWindowsShellEntry("unadmitted receipt")));
        Assert.IsNull(progress.RecycledEntry);
    }

    /// <summary>A second source must never be admitted through one queued request.</summary>
    [TestMethod]
    public void PreDeleteItemWhenRepeatedVetoesAdditionalEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        WindowsRecycleProgress progress = new(source, new WindowsDriveBoundary(), CancellationToken.None);
        ScriptedWindowsShellEntry entry = new(source.Path.CanonicalText);
        Assert.AreEqual(0, progress.PreDeleteItem(0x80, entry));
        Assert.AreEqual(WindowsRecycleProgress.Abort, progress.PreDeleteItem(0x80, entry));
    }
}
