using System;
using System.IO;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Uses the canonical Windows WSL namespace without invoking a process or shell.</summary>
internal sealed class WindowsWslFileSystem : IWslFileSystem
{
    private readonly Func<WslPath, string> _resolvePath;
    private readonly Func<string, WslHandleFacts> _readFacts;

    internal WindowsWslFileSystem()
        : this(path => path.CanonicalText)
    {
    }

    internal WindowsWslFileSystem(Func<WslPath, string> resolvePath)
        : this(resolvePath, WindowsFileIdentifier.ReadWslFacts)
    {
    }

    internal WindowsWslFileSystem(
        Func<WslPath, string> resolvePath,
        Func<string, WslHandleFacts> readFacts)
    {
        ArgumentNullException.ThrowIfNull(resolvePath);
        ArgumentNullException.ThrowIfNull(readFacts);
        _resolvePath = resolvePath;
        _readFacts = readFacts;
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

    private WslFileSystemEntry CreateEntry(
        WslPath path,
        FileSystemInfo entry,
        DirectoryEntryKind kind)
    {
        // ADR-0049: identity comes from the entry's own handle, never from enumeration data.
        WslHandleFacts facts = _readFacts(entry.FullName);
        string value = WindowsFileIdentifier.ComposeWslToken(path.DistributionName, facts);
        FileIdentity identity = FileIdentity.Parse(value) is FileIdentityAccepted accepted
            ? accepted.Identity
            : throw new IOException("The WSL identity token exceeds the identity boundary.");
        return new WslFileSystemEntry(path, entry.Name, identity, kind, (FileAttributes)facts.Attributes);
    }

    private FileSystemInfo ResolveEntry(WslFileSystemEntry source)
    {
        string sourceText = _resolvePath(source.Path);
        return source.Kind == DirectoryEntryKind.Directory
            ? new DirectoryInfo(sourceText)
            : new FileInfo(sourceText);
    }

}
