using System;
using System.Collections.Generic;
using System.IO;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal static class LiveWslRootFileSystem
{
    internal static LiveWslRootEntry CaptureDirectory(WslPath path, Func<WslPath, string> resolvePath)
    {
        string resolved = resolvePath(path);
        return Directory.Exists(resolved)
            ? CaptureEntry(path, resolvePath, EntryKind(resolved))
            : throw new IOException("The required live directory is unavailable.");
    }

    internal static LiveWslRootEntry CaptureEntry(
        WslPath path,
        Func<WslPath, string> resolvePath,
        LiveWslRootEntryKind kind)
    {
        string resolved = resolvePath(path);
        return LiveWslRootEntry.Create(path, resolved, WindowsFileIdentifier.Describe(resolved), kind);
    }

    internal static LiveWslRootCheckOutcome VerifyEntries(IEnumerable<LiveWslRootEntry> entries)
    {
        foreach (LiveWslRootEntry entry in entries)
        {
            if ((!File.Exists(entry.ResolvedPath) && !Directory.Exists(entry.ResolvedPath)) ||
                EntryKind(entry.ResolvedPath) != entry.Kind)
            {
                return new LiveWslRootCheckRejected(LiveWslRootFailureKind.IdentityChanged);
            }
            if (!WindowsFileIdentifier.Describe(entry.ResolvedPath).Equals(entry.Identity, StringComparison.Ordinal))
            {
                return new LiveWslRootCheckRejected(LiveWslRootFailureKind.IdentityChanged);
            }
        }
        return LiveWslRootCheckOutcome.Accepted;
    }

    internal static IReadOnlyList<LiveWslRootEntry> EnumerateTree(
        WslPath root,
        Func<WslPath, string> resolvePath)
    {
        LiveWslRootEntry rootEntry = CaptureEntry(root, resolvePath, EntryKind(resolvePath(root)));
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
                LiveWslRootEntryKind kind = EntryKind(resolved);
                entries.Add(CaptureEntry(child, resolvePath, kind));
                if (kind == LiveWslRootEntryKind.Regular && Directory.Exists(resolved))
                {
                    pending.Push(child);
                }
            }
        }
        return entries;
    }

    internal static LiveWslRootEntryKind EntryKind(string resolvedPath)
    {
        return (File.GetAttributes(resolvedPath) & FileAttributes.ReparsePoint) != 0
            ? LiveWslRootEntryKind.Link
            : LiveWslRootEntryKind.Regular;
    }
}
