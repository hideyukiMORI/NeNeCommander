using System;
using System.Globalization;
using System.IO;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Uses the canonical Windows WSL namespace without invoking a process or shell.</summary>
internal sealed class WindowsWslFileSystem : IWslFileSystem
{
    private readonly Func<WslPath, string> _resolvePath;

    internal WindowsWslFileSystem()
        : this(path => path.CanonicalText)
    {
    }

    internal WindowsWslFileSystem(Func<WslPath, string> resolvePath)
    {
        ArgumentNullException.ThrowIfNull(resolvePath);
        _resolvePath = resolvePath;
    }

    public WslFileSystemEntry? Find(WslPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        string resolvedPath = _resolvePath(path);
        FileInfo file = new(resolvedPath);
        if (file.Exists)
        {
            return CreateEntry(path, file, DirectoryEntryKind.File);
        }

        DirectoryInfo directory = new(resolvedPath);
        return directory.Exists ? CreateEntry(path, directory, DirectoryEntryKind.Directory) : null;
    }

    public bool TargetExists(WslPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        string resolvedPath = _resolvePath(path);
        return File.Exists(resolvedPath) || Directory.Exists(resolvedPath);
    }

    public bool ContainsReparsePoint(WslFileSystemEntry source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return WindowsLocalTreeCopy.ContainsReparsePoint(ResolveEntry(source));
    }

    public bool ContainsReparsePoint(WslPath target)
    {
        ArgumentNullException.ThrowIfNull(target);
        string resolvedPath = _resolvePath(target);
        FileAttributes attributes = File.GetAttributes(resolvedPath);
        FileSystemInfo entry = (attributes & FileAttributes.Directory) != 0
            ? new DirectoryInfo(resolvedPath)
            : new FileInfo(resolvedPath);
        return WindowsLocalTreeCopy.ContainsReparsePoint(entry);
    }

    public void Copy(WslFileSystemEntry source, WslPath target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        WindowsLocalTreeCopy.Copy(ResolveEntry(source), _resolvePath(target));
    }

    public bool Matches(WslFileSystemEntry source, WslPath target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return WindowsLocalTreeCopy.Matches(ResolveEntry(source), _resolvePath(target));
    }

    public void CreateDirectory(WslPath target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _ = Directory.CreateDirectory(_resolvePath(target));
    }

    public void Rename(WslFileSystemEntry source, WslPath target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        if (source.Kind == DirectoryEntryKind.Directory)
        {
            Directory.Move(_resolvePath(source.Path), _resolvePath(target));
            return;
        }

        File.Move(_resolvePath(source.Path), _resolvePath(target));
    }

    public void Delete(WslFileSystemEntry source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Kind == DirectoryEntryKind.Directory)
        {
            Directory.Delete(_resolvePath(source.Path), recursive: true);
        }
        else
        {
            File.Delete(_resolvePath(source.Path));
        }
    }

    private static WslFileSystemEntry CreateEntry(
        WslPath path,
        FileSystemInfo entry,
        DirectoryEntryKind kind)
    {
        long length = entry is FileInfo file ? file.Length : 0;
        string value = string.Join(
            '|',
            "wsl-v1",
            WindowsFileIdentifier.Describe(entry.FullName),
            kind == DirectoryEntryKind.Directory ? "directory" : "file",
            length.ToString(CultureInfo.InvariantCulture),
            entry.CreationTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture),
            entry.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture));
        FileIdentity identity = ((FileIdentityAccepted)FileIdentity.Parse(value)).Identity;
        return new WslFileSystemEntry(path, entry.Name, identity, kind, entry.Attributes);
    }

    private FileSystemInfo ResolveEntry(WslFileSystemEntry source)
    {
        string sourceText = _resolvePath(source.Path);
        return source.Kind == DirectoryEntryKind.Directory
            ? new DirectoryInfo(sourceText)
            : new FileInfo(sourceText);
    }

}
