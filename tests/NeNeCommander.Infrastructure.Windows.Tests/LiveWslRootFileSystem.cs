using System;
using System.Collections.Generic;
using System.IO;
using NeNeCommander.Application.Directories;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal static class LiveWslRootFileSystem
{
    internal static LiveWslRootEntry CaptureDirectory(
        WslPath path,
        WindowsWslFileSystem fileSystem,
        Func<WslPath, string> resolvePath)
    {
        ArgumentNullException.ThrowIfNull(resolvePath);
        return Directory.Exists(resolvePath(path))
            ? CaptureEntry(path, fileSystem, resolvePath)
            : throw new IOException("The required live directory is unavailable.");
    }

    // ADR-0049: every live identity comes from the production WSL file system on the entry's own
    // path. The harness never calls WindowsFileIdentifier directly and never derives an identity
    // from enumeration data, because enumeration and handle identity disagree at mount points.
    internal static LiveWslRootEntry CaptureEntry(
        WslPath path,
        WindowsWslFileSystem fileSystem,
        Func<WslPath, string> resolvePath)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(resolvePath);
        WslFileSystemEntry entry = fileSystem.Find(path) ??
            throw new IOException("The required live entry is unavailable.");
        return LiveWslRootEntry.Create(path, resolvePath(path), entry.Identity.Value, EntryKind(entry));
    }

    internal static LiveWslRootCheckOutcome VerifyEntries(
        IEnumerable<LiveWslRootEntry> entries,
        WindowsWslFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(fileSystem);
        foreach (LiveWslRootEntry entry in entries)
        {
            WslFileSystemEntry? observed = fileSystem.Find(entry.Path);
            if (observed is null ||
                EntryKind(observed) != entry.Kind ||
                !observed.Identity.Value.Equals(entry.Identity, StringComparison.Ordinal))
            {
                return new LiveWslRootCheckRejected(LiveWslRootFailureKind.IdentityChanged);
            }
        }
        return LiveWslRootCheckOutcome.Accepted;
    }

    /// <summary>
    /// Proves an entry still exists with its captured kind without comparing its identity. Only the
    /// outer ancestors above the configured root use this form: their identity changes whenever an
    /// unrelated process writes into them, while a replacement by a link or a non-directory is the
    /// property this harness must refuse.
    /// </summary>
    /// <param name="entries">Captured entries to re-observe.</param>
    /// <param name="fileSystem">Production WSL file system that owns identity.</param>
    /// <returns>The accepted or rejected outcome.</returns>
    internal static LiveWslRootCheckOutcome VerifyShape(
        IEnumerable<LiveWslRootEntry> entries,
        WindowsWslFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(fileSystem);
        foreach (LiveWslRootEntry entry in entries)
        {
            WslFileSystemEntry? observed = fileSystem.Find(entry.Path);
            if (observed is null || EntryKind(observed) != entry.Kind)
            {
                return new LiveWslRootCheckRejected(LiveWslRootFailureKind.IdentityChanged);
            }
        }
        return LiveWslRootCheckOutcome.Accepted;
    }

    internal static IReadOnlyList<LiveWslRootEntry> EnumerateTree(
        WslPath root,
        WindowsWslFileSystem fileSystem,
        Func<WslPath, string> resolvePath)
    {
        ArgumentNullException.ThrowIfNull(resolvePath);
        LiveWslRootEntry rootEntry = CaptureEntry(root, fileSystem, resolvePath);
        List<LiveWslRootEntry> entries = [rootEntry];
        if (rootEntry.Kind == LiveWslRootEntryKind.Link)
        {
            return entries;
        }
        Stack<WslPath> pending = new();
        pending.Push(root);
        while (pending.Count > 0)
        {
            WslPath directory = pending.Pop();
            foreach (string resolved in Directory.EnumerateFileSystemEntries(resolvePath(directory)))
            {
                string name = System.IO.Path.GetFileName(resolved);
                WslPath child = LiveWslRootPath.Child(directory, name);
                LiveWslRootEntry childEntry = CaptureEntry(child, fileSystem, resolvePath);
                entries.Add(childEntry);
                if (childEntry.Kind == LiveWslRootEntryKind.Directory)
                {
                    pending.Push(child);
                }
            }
        }
        return entries;
    }

    // The kind is read from the same no-follow handle that produced the identity, so a link is
    // never mistaken for its target.
    internal static LiveWslRootEntryKind EntryKind(WslFileSystemEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return (entry.Attributes & FileAttributes.ReparsePoint) != 0
            ? LiveWslRootEntryKind.Link
            : entry.Kind == DirectoryEntryKind.Directory
                ? LiveWslRootEntryKind.Directory
                : LiveWslRootEntryKind.File;
    }
}
