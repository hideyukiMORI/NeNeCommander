using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Scripts one in-memory WSL namespace behind the declared <see cref="IWslFileSystem"/> port, so
/// adapter and cross-transfer tests inject failures at each side-effect boundary without a distribution.
/// </summary>
internal sealed class ScriptedWslFileSystem : IWslFileSystem
{
    private readonly Dictionary<string, WslFileSystemEntry> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _findCounts = new(StringComparer.Ordinal);
    private bool _sourceReplacedWhenTargetChecked;

    internal Exception? Failure { get; set; }

    internal List<WslPath> Created { get; } = [];

    internal List<(WslFileSystemEntry Source, WslPath Target)> Renamed { get; } = [];

    internal List<WslFileSystemEntry> Deleted { get; } = [];

    internal List<(WslFileSystemEntry Source, WslPath Target)> Copied { get; } = [];

    internal List<(FileSystemInfo Source, WslPath Target)> CopiedFromWindowsLocal { get; } = [];

    internal bool MatchResult { get; set; } = true;

    internal bool FailCopyAfterTarget { get; set; }

    internal bool FailCopyBeforeTarget { get; set; }

    internal Exception? CopyFailure { get; set; }

    internal bool ReplaceSourceAfterCopy { get; set; }

    internal bool ContainsNestedReparsePoint { get; set; }

    internal bool TargetContainsReparsePoint { get; set; }

    internal WslFileSystemEntry? ReplaceSourceWhenTargetChecked { get; set; }

    public WslFileSystemEntry? Find(WslPath path)
    {
        ThrowWhenConfigured();
        _findCounts[path.CanonicalText] = FindCount(path) + 1;
        return _entries.GetValueOrDefault(path.CanonicalText);
    }

    public bool TargetExists(WslPath path)
    {
        ThrowWhenConfigured();
        if (!_sourceReplacedWhenTargetChecked &&
            ReplaceSourceWhenTargetChecked is WslFileSystemEntry replacement)
        {
            Set(replacement);
            _sourceReplacedWhenTargetChecked = true;
        }
        return _entries.ContainsKey(path.CanonicalText);
    }

    public bool ContainsReparsePoint(WslFileSystemEntry source)
    {
        ThrowWhenConfigured();
        return ContainsNestedReparsePoint ||
            (source.Attributes & FileAttributes.ReparsePoint) != 0;
    }

    public bool ContainsReparsePoint(WslPath target)
    {
        ThrowWhenConfigured();
        return TargetContainsReparsePoint;
    }

    public void Copy(WslFileSystemEntry source, WslPath target)
    {
        if (FailCopyBeforeTarget)
        {
            throw new IOException("Synthetic copy failure before target creation.");
        }
        Copied.Add((source, target));
        Set(new WslFileSystemEntry(target, source.Name, source.Identity, source.Kind, source.Attributes));
        if (ReplaceSourceAfterCopy)
        {
            Set(Entry(source.Path, "replacement", source.Kind, source.Attributes));
        }
        if (FailCopyAfterTarget)
        {
            throw new IOException("Synthetic copy failure after target creation.");
        }
    }

    public bool Matches(WslFileSystemEntry source, WslPath target)
    {
        ThrowWhenConfigured();
        return MatchResult && _entries.ContainsKey(target.CanonicalText);
    }

    public void CopyFromWindowsLocal(FileSystemInfo source, WslPath target)
    {
        if (FailCopyBeforeTarget)
        {
            throw CopyFailure ?? new IOException("Synthetic copy failure before target creation.");
        }
        CopiedFromWindowsLocal.Add((source, target));
        DirectoryEntryKind kind = source is DirectoryInfo ? DirectoryEntryKind.Directory : DirectoryEntryKind.File;
        Set(Entry(target, "copied", kind));
        if (FailCopyAfterTarget)
        {
            throw CopyFailure ?? new IOException("Synthetic copy failure after target creation.");
        }
    }

    public bool MatchesWindowsLocal(FileSystemInfo source, WslPath target)
    {
        ThrowWhenConfigured();
        return MatchResult && _entries.ContainsKey(target.CanonicalText);
    }

    public void CreateDirectory(WslPath target)
    {
        ThrowWhenConfigured();
        Created.Add(target);
        Set(Entry(target, "created", DirectoryEntryKind.Directory));
    }

    public void Rename(WslFileSystemEntry source, WslPath target)
    {
        ThrowWhenConfigured();
        Renamed.Add((source, target));
        _ = _entries.Remove(source.Path.CanonicalText);
        Set(new WslFileSystemEntry(target, source.Name, source.Identity, source.Kind, source.Attributes));
    }

    public void Delete(WslFileSystemEntry source)
    {
        ThrowWhenConfigured();
        Deleted.Add(source);
        _ = _entries.Remove(source.Path.CanonicalText);
    }

    internal static WslFileSystemEntry Entry(
        WslPath path,
        string identity,
        DirectoryEntryKind kind,
        FileAttributes attributes = FileAttributes.None)
    {
        FileIdentity parsed = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse(identity)).Identity;
        int separator = path.LinuxPath.LastIndexOf('/');
        return new WslFileSystemEntry(path, path.LinuxPath[(separator + 1)..], parsed, kind, attributes);
    }

    internal void Set(WslFileSystemEntry entry)
    {
        _entries[entry.Path.CanonicalText] = entry;
    }

    internal int FindCount(WslPath path)
    {
        return _findCounts.GetValueOrDefault(path.CanonicalText);
    }

    private void ThrowWhenConfigured()
    {
        if (Failure is not null)
        {
            throw Failure;
        }
    }
}
