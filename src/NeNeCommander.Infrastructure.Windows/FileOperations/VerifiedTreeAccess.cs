using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Uses the owning providers' transport and identity mechanisms for verified tree access.</summary>
internal sealed class VerifiedTreeAccess
{
    private readonly IWslFileSystem _wsl;
    private readonly WindowsDriveBoundary _driveBoundary;

    internal VerifiedTreeAccess(IWslFileSystem wsl)
        : this(wsl, new WindowsDriveBoundary())
    {
    }

    internal VerifiedTreeAccess(IWslFileSystem wsl, WindowsDriveBoundary driveBoundary)
    {
        ArgumentNullException.ThrowIfNull(wsl);
        ArgumentNullException.ThrowIfNull(driveBoundary);
        _wsl = wsl;
        _driveBoundary = driveBoundary;
    }

    internal string Resolve(FileSystemPath path)
    {
        return path switch
        {
            WindowsLocalPath local => _driveBoundary.AdmitsLocal(local)
                ? path.CanonicalText : throw new WindowsDriveBoundaryRejectedException(),
            WslPath wsl => _wsl.ResolveVerificationPath(wsl),
            _ => throw new IOException("The verification provider is unavailable."),
        };
    }

    internal FileSystemInfo Find(FileSystemPath path)
    {
        string text = Resolve(path);
        FileAttributes attributes = System.IO.File.GetAttributes(text);
        return (attributes & FileAttributes.ReparsePoint) != 0
            ? throw new IOException("A verified tree entry became a link.")
            : (attributes & FileAttributes.Directory) != 0 ? new DirectoryInfo(text) : new FileInfo(text);
    }

    internal VerifiedEntrySnapshot Capture(FileSystemPath path)
    {
        FileSystemInfo entry = Find(path);
        return path switch
        {
            WindowsLocalPath local => WindowsLocalEntryIdentity.Capture(local, entry),
            WslPath wsl => _wsl.CaptureVerificationEntry(wsl),
            _ => throw new IOException("The identity provider is unavailable."),
        };
    }

    internal IReadOnlyList<FileSystemPath> Children(FileSystemPath path)
    {
        DirectoryInfo directory = Find(path) as DirectoryInfo ?? throw new IOException("The entry is not a directory.");
        HashSet<FileSystemPath> reserved = new(FileSystemPathIdentityComparer.Instance);
        List<FileSystemPath> children = [];
        foreach (FileSystemInfo child in directory.EnumerateFileSystemInfos("*", new EnumerationOptions
        {
            AttributesToSkip = FileAttributes.None,
            IgnoreInaccessible = false,
        }))
        {
            FileSystemPath derived = WindowsLocalTreeCopy.DeriveChild(path, child.Name) ??
                throw new IOException("A verified child name is not representable.");
            if (!reserved.Add(derived))
            {
                throw new IOException("Verified child identities collide.");
            }
            children.Add(derived);
        }
        children.Sort((left, right) => StringComparer.Ordinal.Compare(left.CanonicalText, right.CanonicalText));
        return children.AsReadOnly();
    }

    internal IReadOnlyList<VerifiedEntrySnapshot> Ancestors(FileSystemPath path)
    {
        List<VerifiedEntrySnapshot> ancestors = [];
        FileSystemPath? parent = path.Parent;
        while (parent is not null)
        {
            if (Find(parent) is not DirectoryInfo)
            {
                throw new IOException("A verified ancestor is not a directory.");
            }
            ancestors.Add(Capture(parent));
            parent = parent.Parent;
        }
        return ancestors.AsReadOnly();
    }

    internal bool MatchesAncestors(IReadOnlyList<VerifiedEntrySnapshot> entries)
    {
        return entries.All(entry => Find(entry.Path) is DirectoryInfo && Capture(entry.Path) == entry);
    }

    internal void Delete(FileSystemPath path)
    {
        FileSystemInfo entry = Find(path);
        _ = Resolve(path);
        if (entry is DirectoryInfo directory)
        {
            directory.Delete(recursive: false);
        }
        else
        {
            entry.Delete();
        }
    }
}
