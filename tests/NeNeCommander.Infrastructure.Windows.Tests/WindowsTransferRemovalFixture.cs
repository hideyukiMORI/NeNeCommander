using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed class WindowsTransferRemovalFixture : IDisposable
{
    private WindowsTransferRemovalFixture(TestOwnedTemporaryRoot root, WindowsTransferPathLease parent, SafeFileHandle handle, WindowsLocalPath path)
    {
        Root = root;
        Parent = parent;
        Handle = handle;
        Path = path;
    }

    internal TestOwnedTemporaryRoot Root { get; }
    internal WindowsTransferPathLease Parent { get; }
    internal SafeFileHandle Handle { get; }
    internal WindowsLocalPath Path { get; }

    internal static WindowsTransferRemovalFixture Create()
    {
        TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsTransferPathLease? parent = null;
        SafeFileHandle? handle = null;
        bool completed = false;
        try
        {
            parent = WindowsTransferPathLease.Open(Parse(root.WriteFile("anchor", "original")), FileAccess.Read, new WindowsDriveBoundary());
            WindowsLocalPath path = Parse(root.Resolve("artifact"));
            using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
            handle = WindowsTransferEntryOpen.CreateArtifact(parent, path, security);
            RandomAccess.Write(handle, [1, 2, 3], 0);
            WindowsTransferRemovalFixture fixture = new(root, parent, handle, path);
            completed = true;
            return fixture;
        }
        finally
        {
            if (!completed)
            {
                handle?.Dispose();
                parent?.Dispose();
                root.Dispose();
            }
        }
    }

    internal void RequireOriginalContent()
    {
        byte[] content = new byte[3];
        Assert.AreEqual(3, RandomAccess.Read(Handle, content, 0));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, content);
        Assert.IsFalse(Handle.IsClosed);
        Parent.Revalidate();
    }

    private static WindowsLocalPath Parse(string path)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path);
    }

    public void Dispose()
    {
        Handle.Dispose();
        Parent.Dispose();
        Root.Dispose();
    }
}
