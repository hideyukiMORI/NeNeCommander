using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Owns an existing transfer file and its entire non-link directory chain until the attempt ends.</summary>
internal sealed class WindowsTransferPathLease : IDisposable
{
    private readonly List<WindowsTransferHeldEntry> _entries = [];
    private readonly WindowsDriveBoundary _driveBoundary;
    private bool _disposed;

    private WindowsTransferPathLease(WindowsDriveBoundary driveBoundary)
    {
        _driveBoundary = driveBoundary;
    }

    internal SafeFileHandle Handle => _entries[^1].Handle;
    internal WindowsLocalPath Path => _entries[^1].Path;
    internal VerifiedEntrySnapshot Baseline => _entries[^1].Baseline;
    internal SafeFileHandle ParentHandle => _entries[^2].Handle;
    internal WindowsLocalPath ParentPath => _entries[^2].Path;

    internal static WindowsTransferPathLease Open(WindowsLocalPath path, FileAccess access, WindowsDriveBoundary driveBoundary)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(driveBoundary);
        if (path.Parent is null || access is not (FileAccess.Read or FileAccess.ReadWrite))
        {
            throw new ArgumentException("A transfer lease requires a file path and read or read-write access.", nameof(path));
        }
        if (!driveBoundary.AdmitsLocal(path))
        {
            throw new WindowsDriveBoundaryRejectedException();
        }
        WindowsTransferPathLease lease = new(driveBoundary);
        try
        {
            lease.OpenEntries(path, access);
            lease.Revalidate();
            return lease;
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    internal void Revalidate()
    {
        RevalidateChain(_entries[^1].RequireOriginalFile);
    }

    /// <summary>Revalidates the SAME root, ancestors, path and stable object after content effects; it grants no original-content or shape admission.</summary>
    internal void RevalidateObject()
    {
        RevalidateChain(_entries[^1].RequireFileObject);
    }

    private void RevalidateChain(Action requireFile)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_driveBoundary.AdmitsLocal(_entries[^1].Path))
        {
            throw new WindowsDriveBoundaryRejectedException();
        }
        for (int index = 0; index < _entries.Count - 1; index++)
        {
            _entries[index].RequireDirectory();
        }
        requireFile();
        using SafeFileHandle root = WindowsTransferEntryOpen.OpenRoot(_entries[0].Path);
        if (WindowsFileIdentifier.ReadLocalFacts(root).ObjectIdentity != _entries[0].Baseline.ObjectIdentity)
        {
            throw new IOException("The transfer drive root mapping changed.");
        }
        WindowsTransferPathObservation.Require(root, _entries[0].Path);
    }

    private void OpenEntries(WindowsLocalPath path, FileAccess access)
    {
        Stack<WindowsLocalPath> ancestors = new();
        for (WindowsLocalPath? parent = path.Parent as WindowsLocalPath; parent is not null; parent = parent.Parent as WindowsLocalPath)
        {
            ancestors.Push(parent);
        }
        WindowsLocalPath root = ancestors.Pop();
        AddEntry(root, () => WindowsTransferEntryOpen.OpenRoot(root));
        while (ancestors.TryPop(out WindowsLocalPath? next))
        {
            WindowsTransferHeldEntry parent = _entries[^1];
            parent.RequireDirectory();
            AddEntry(next, () => WindowsTransferEntryOpen.OpenDirectory(parent.Handle, parent.Path, next));
        }
        WindowsTransferHeldEntry directory = _entries[^1];
        directory.RequireDirectory();
        AddEntry(path, () => WindowsTransferEntryOpen.OpenFile(directory.Handle, directory.Path, path, access));
    }

    private void AddEntry(WindowsLocalPath path, Func<SafeFileHandle> open)
    {
        WindowsTransferHeldEntry entry = WindowsTransferHeldEntry.Take(path, open());
        try
        {
            _entries.Add(entry);
        }
        catch
        {
            entry.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        for (int index = _entries.Count - 1; index >= 0; index--)
        {
            _entries[index].Dispose();
        }
        _disposed = true;
    }
}
