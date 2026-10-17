using System;
using System.IO;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>
/// Uses the Windows WSL namespace without invoking a process or shell. Every entry is addressed
/// through the extended-length text of <see cref="WslWin32PathText"/> (ADR-0060), so no segment is
/// renamed by Win32 normalization.
/// </summary>
internal sealed class WindowsWslFileSystem : IWslFileSystem
{
    private readonly Func<WslPath, string> _resolvePath;
    private readonly Func<string, WslHandleFacts> _readFacts;
    private readonly WindowsDriveBoundary _driveBoundary;

    internal WindowsWslFileSystem()
        : this(WslWin32PathText.Resolve)
    {
    }

    internal WindowsWslFileSystem(WindowsDriveBoundary driveBoundary)
        : this(WslWin32PathText.Resolve, WindowsFileIdentifier.ReadWslFacts, driveBoundary)
    {
    }

    internal WindowsWslFileSystem(Func<WslPath, string> resolvePath)
        : this(resolvePath, WindowsFileIdentifier.ReadWslFacts)
    {
    }

    internal WindowsWslFileSystem(
        Func<WslPath, string> resolvePath,
        Func<string, WslHandleFacts> readFacts)
        : this(resolvePath, readFacts, new WindowsDriveBoundary())
    {
    }

    internal WindowsWslFileSystem(
        Func<WslPath, string> resolvePath,
        Func<string, WslHandleFacts> readFacts,
        WindowsDriveBoundary driveBoundary)
    {
        ArgumentNullException.ThrowIfNull(resolvePath);
        ArgumentNullException.ThrowIfNull(readFacts);
        ArgumentNullException.ThrowIfNull(driveBoundary);
        _resolvePath = resolvePath;
        _readFacts = readFacts;
        _driveBoundary = driveBoundary;
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

    public string ResolveVerificationPath(WslPath path)
    {
        return _resolvePath(path);
    }

    public VerifiedEntrySnapshot CaptureVerificationEntry(WslPath path)
    {
        WslHandleFacts facts = _readFacts(_resolvePath(path));
        string identity = WindowsFileIdentifier.ComposeWslToken(path.DistributionName, facts);
        FileIdentity full = FileIdentity.Parse(identity) is FileIdentityAccepted accepted
            ? accepted.Identity : throw new IOException("The WSL identity exceeds its boundary.");
        return VerifiedEntrySnapshot.Create(path, full,
            FileObjectIdentity.Create(WindowsFileIdentifier.ComposeWslObjectToken(path.DistributionName, facts)));
    }

    public CopyVerificationOutcome VerifyCopy(FileEntrySnapshot source, FileSystemPath target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return WindowsLocalTreeCopy.Verify(source, target, new VerifiedTreeAccess(this, _driveBoundary));
    }

    public ProviderStepOutcome DeleteVerifiedSource(FileEntrySnapshot source, VerifiedCopyReceipt receipt)
    {
        return VerifiedSourceDeletion.Execute(source, receipt, new VerifiedTreeAccess(this, _driveBoundary));
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

    public ProviderStepOutcome Copy(WslFileSystemEntry source, WslPath target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return WindowsLocalTreeCopy.Copy(ResolveEntry(source), target, TreeAccess(InspectWslSource));
    }

    // WindowsLocalTreeCopy rejects a null source itself; only the target needs a guard before resolution.
    public ProviderStepOutcome CopyFromWindowsLocal(FileSystemInfo source, WslPath target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return WindowsLocalTreeCopy.Copy(source, target, TreeAccess(entry =>
            WindowsLocalTreeCopy.InspectLocalSource(entry, _driveBoundary)));
    }

    public FileOperationFailureKind? PreflightToWindowsLocal(WslFileSystemEntry source, WindowsLocalPath target)
    {
        ArgumentNullException.ThrowIfNull(source);
        return WindowsLocalTreeCopy.Preflight(ResolveEntry(source), target, TreeAccess(InspectWslSource));
    }

    public ProviderStepOutcome CopyToWindowsLocal(WslFileSystemEntry source, WindowsLocalPath target)
    {
        ArgumentNullException.ThrowIfNull(source);
        return WindowsLocalTreeCopy.Copy(ResolveEntry(source), target, TreeAccess(InspectWslSource));
    }

    private string ResolveTarget(FileSystemPath target)
    {
        return target is WslPath wsl ? _resolvePath(wsl) : target.CanonicalText;
    }

    private WindowsTreeCopyAccess TreeAccess(Func<FileSystemInfo, bool> inspectSource)
    {
        return new WindowsTreeCopyAccess(ResolveTarget, inspectSource, _driveBoundary);
    }

    private bool InspectWslSource(FileSystemInfo source)
    {
        _ = _readFacts(source.FullName);
        source.Refresh();
        return true;
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
