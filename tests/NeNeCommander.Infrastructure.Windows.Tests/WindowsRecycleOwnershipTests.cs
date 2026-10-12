using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves unique native reference ownership without performing a Shell mutation.</summary>
[TestClass]
public sealed class WindowsRecycleOwnershipTests
{
    /// <summary>Native wrappers become unusable when their owned references are released.</summary>
    [TestMethod]
    public async Task DisposeWhenWrappersOwnNativeObjectsReleasesBothOnStaAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        _ = await new WindowsShellIoScheduler().ScheduleAsync(() =>
        {
            WindowsRecycleShell shell = new();
            WindowsShellEntry entry = (WindowsShellEntry)shell.OpenEntry(WindowsRecycleBinTests.Path(source.Path.CanonicalText));
            Assert.AreEqual(0, entry.GetDisplayName(0x80058000, out string name));
            Assert.AreEqual(source.Path.CanonicalText, name);
            Assert.AreEqual(0, entry.GetAttributes(0x40000020, out uint attributes));
            Assert.AreEqual(0x40000020u, attributes);
            WindowsRecycleShell.Release(entry);
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() => entry.GetAttributes(0, out _));
            WindowsShellOperation operation = (WindowsShellOperation)shell.OpenOperation();
            Assert.AreEqual(0, operation.SetOperationFlags(WindowsRecycleSession.OperationFlags));
            WindowsRecycleShell.Release(operation);
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() => operation.SetOperationFlags(WindowsRecycleSession.OperationFlags));
            return 0;
        });
    }

    /// <summary>Each native source is released after its callback, including a rejected null receipt.</summary>
    [TestMethod]
    public async Task NativeCallbackWhenSourceIsBorrowedReleasesItBeforeReturningAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        _ = await new WindowsShellIoScheduler().ScheduleAsync(() =>
        {
            WindowsRecycleProgress policy = new(source, CancellationToken.None);
            using NativeWindowsRecycleProgress callback = new(policy);
            INativeWindowsShellEntry before = OpenNative(source.Path.CanonicalText);
            Assert.AreEqual(0, callback.PreDeleteItem(0x80, before));
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() => before.GetAttributes(0, out _));
            INativeWindowsShellEntry after = OpenNative(source.Path.CanonicalText);
            Assert.AreEqual(WindowsRecycleProgress.Abort, callback.PostDeleteItem(0x80, after, 0, null));
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() => after.GetAttributes(0, out _));
            Assert.IsNull(policy.RecycledEntry);
            return 0;
        });
    }

    /// <summary>The first receipt lives until disposal; a repeated receipt is released immediately.</summary>
    [TestMethod]
    public async Task NativeCallbackWhenReceiptIsRepeatedRetainsOnlyFirstOwnerAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileEntrySnapshot source = WindowsRecycleBinTests.Source(root);
        string receiptPath = root.WriteFile("receipt", "owned callback fixture");
        _ = await new WindowsShellIoScheduler().ScheduleAsync(() =>
        {
            WindowsRecycleProgress policy = new(source, CancellationToken.None);
            NativeWindowsRecycleProgress callback = new(policy);
            Assert.AreEqual(0, callback.PreDeleteItem(0x80, OpenNative(source.Path.CanonicalText)));
            INativeWindowsShellEntry first = OpenNative(receiptPath);
            Assert.AreEqual(0, callback.PostDeleteItem(0x80, OpenNative(source.Path.CanonicalText), 0, first));
            Assert.AreEqual(0, first.GetAttributes(0, out _));
            INativeWindowsShellEntry extra = OpenNative(receiptPath);
            Assert.AreEqual(WindowsRecycleProgress.Abort, callback.PostDeleteItem(0x80, OpenNative(source.Path.CanonicalText), 0, extra));
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() => extra.GetAttributes(0, out _));
            Assert.AreEqual(0, first.GetAttributes(0, out _));
            callback.Dispose();
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() => first.GetAttributes(0, out _));
            return 0;
        });
    }

    private static INativeWindowsShellEntry OpenNative(string path)
    {
        return ((WindowsShellEntry)new WindowsRecycleShell().OpenEntry(WindowsRecycleBinTests.Path(path))).Native;
    }
}
