using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Copies and verifies trees through one typed destination-child admission.</summary>
public static partial class WindowsLocalTreeCopy
{
    /// <summary>Reports whether an entry is a symbolic link, junction, or other reparse point.</summary>
    /// <param name="entry">Existing entry.</param>
    /// <returns>True when the entry is a reparse point.</returns>
    public static bool IsReparsePoint(FileSystemInfo entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return (entry.Attributes & FileAttributes.ReparsePoint) != 0;
    }

    /// <summary>Checks an entire source tree without following links.</summary>
    /// <param name="entry">Existing entry.</param>
    /// <returns>True when any entry is a reparse point.</returns>
    public static bool ContainsReparsePoint(FileSystemInfo entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return IsReparsePoint(entry) ||
            (entry is DirectoryInfo directory && Children(directory).Any(ContainsReparsePoint));
    }

    internal static bool ContainsReparsePoint(FileSystemInfo entry, Func<FileSystemInfo, bool> admit)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(admit);
        return !admit(entry) || IsReparsePoint(entry) ||
            (entry is DirectoryInfo directory && Children(directory).Any(child => ContainsReparsePoint(child, admit)));
    }

    /// <summary>Checks every destination name and reservation before any tree is copied.</summary>
    /// <param name="source">Source entry.</param>
    /// <param name="target">Typed target path.</param>
    /// <param name="access">Owning provider's transport and current admission.</param>
    /// <returns>The rejection, or absence when every descendant is admissible.</returns>
    internal static FileOperationFailureKind? Preflight(
        FileSystemInfo source,
        FileSystemPath target,
        WindowsTreeCopyAccess access)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(access);
        return !access.InspectSource(source) || !access.AdmitsTarget(target) || IsReparsePoint(source)
            ? FileOperationFailureKind.ProviderUnavailable
            : Exists(access.ResolveTarget(target)) ? FileOperationFailureKind.Conflict
            : source is DirectoryInfo directory
            ? PreflightChildren(Children(directory), target, access) : null;
    }

    /// <summary>Admits one complete enumeration, including case-sensitive source names on Windows targets.</summary>
    internal static FileOperationFailureKind? PreflightChildren(
        IEnumerable<FileSystemInfo> children,
        FileSystemPath target,
        WindowsTreeCopyAccess access)
    {
        HashSet<FileSystemPath> reserved = new(FileSystemPathIdentityComparer.Instance);
        foreach (FileSystemInfo child in children)
        {
            if (DeriveChild(target, child.Name) is not FileSystemPath childTarget)
            {
                return FileOperationFailureKind.ProviderUnavailable;
            }
            if (!reserved.Add(childTarget))
            {
                return FileOperationFailureKind.Conflict;
            }
            if (Preflight(child, childTarget, access) is FileOperationFailureKind failure)
            {
                return failure;
            }
        }
        return null;
    }

    /// <summary>Copies to an absent target and refuses invalid children or directory merging.</summary>
    /// <param name="source">Existing source.</param>
    /// <param name="target">Typed target path.</param>
    /// <param name="access">Owning provider's transport and current admission.</param>
    /// <returns>The copy outcome, retaining a created target after a child refusal.</returns>
    internal static ProviderStepOutcome Copy(
        FileSystemInfo source,
        FileSystemPath target,
        WindowsTreeCopyAccess access)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(access);
        return !access.InspectSource(source) || !access.AdmitsTarget(target) || IsReparsePoint(source)
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : TargetParentFailure(target, access) is FileOperationFailureKind failure
            ? ProviderStepOutcome.Failed(failure)
            : Exists(access.ResolveTarget(target))
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict)
            : source is DirectoryInfo directory
            ? CopyDirectory(directory, target, access)
            : CopyFile(source, target, access);
    }

    /// <summary>Derives exactly one provider-valid child, including the Windows component limit.</summary>
    internal static FileSystemPath? DeriveChild(FileSystemPath parent, string name)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(name);
        return parent is WindowsLocalPath && name.Length > 255
            ? null
            : (parent.Child(name) as PathParseSuccess)?.Path;
    }

    /// <summary>Refreshes a local source before its current attributes and content are consumed.</summary>
    internal static bool InspectLocalSource(FileSystemInfo source)
    {
        return InspectLocalSource(source, new WindowsDriveBoundary());
    }

    internal static bool InspectLocalSource(FileSystemInfo source, WindowsDriveBoundary driveBoundary)
    {
        if (FileSystemPath.Parse(source.FullName) is not PathParseSuccess { Path: WindowsLocalPath local } ||
            !driveBoundary.AdmitsLocal(local))
        {
            return false;
        }
        source.Refresh();
        return true;
    }

    private static ProviderStepOutcome CopyFile(FileSystemInfo source, FileSystemPath target, WindowsTreeCopyAccess access)
    {
        if (!access.InspectSource(source) || !access.AdmitsTarget(target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        string targetText = access.ResolveTarget(target);
        try
        {
            File.Copy(source.FullName, targetText, overwrite: false);
            return ProviderStepOutcome.Succeeded();
        }
        catch (IOException exception) when (
            exception.HResult is unchecked((int)0x80070050) or unchecked((int)0x800700B7) || Directory.Exists(targetText))
        {
            // Exclusive file creation cannot have created the competing file or directory.
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
    }

    private static ProviderStepOutcome CopyDirectory(
        DirectoryInfo source,
        FileSystemPath target,
        WindowsTreeCopyAccess access)
    {
        if (!access.InspectSource(source) || !access.AdmitsTarget(target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        string targetText = access.ResolveTarget(target);
        string nativeText = target is WindowsLocalPath ? @"\\?\" + targetText : targetText;
        if (!CreateDirectory(nativeText, IntPtr.Zero))
        {
            int error = Marshal.GetLastWin32Error();
            return ProviderStepOutcome.Failed(error is 80 or 183
                ? FileOperationFailureKind.Conflict
                : WindowsLocalFileOperationAdapter.Normalize(
                    unchecked((int)(0x80070000u | (uint)error)), FileOperationFailureKind.Copy));
        }
        return CopyChildren(Children(source), target, access);
    }

    /// <summary>Copies an enumeration into the newly owned target directory without merging children.</summary>
    internal static ProviderStepOutcome CopyChildren(
        IEnumerable<FileSystemInfo> children,
        FileSystemPath target,
        WindowsTreeCopyAccess access)
    {
        HashSet<FileSystemPath> reserved = new(FileSystemPathIdentityComparer.Instance);
        foreach (FileSystemInfo child in children)
        {
            if (DeriveChild(target, child.Name) is not FileSystemPath childTarget)
            {
                return Partial(FileOperationFailureKind.ProviderUnavailable);
            }
            if (!reserved.Add(childTarget))
            {
                return Partial(FileOperationFailureKind.Conflict);
            }
            if (Copy(child, childTarget, access).Failure is FileOperationFailureKind failure)
            {
                return Partial(failure);
            }
        }
        return ProviderStepOutcome.Succeeded();
    }

    private static FileOperationFailureKind? TargetParentFailure(
        FileSystemPath target,
        WindowsTreeCopyAccess access)
    {
        if (target.Parent is not FileSystemPath parent)
        {
            return FileOperationFailureKind.ProviderUnavailable;
        }
        if (!access.AdmitsTarget(parent))
        {
            return FileOperationFailureKind.ProviderUnavailable;
        }
        DirectoryInfo directory = new(access.ResolveTarget(parent));
        return !directory.Exists ? FileOperationFailureKind.NotFound
            : IsReparsePoint(directory) ? FileOperationFailureKind.ProviderUnavailable : null;
    }

    private static ProviderStepOutcome Partial(FileOperationFailureKind failure)
    {
        return ProviderStepOutcome.FailedAfterEffect(failure, ProviderStepEffectKind.CopyTargetCreated);
    }

    private static bool Exists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    private static IEnumerable<FileSystemInfo> Children(DirectoryInfo directory)
    {
        return directory.EnumerateFileSystemInfos("*", new EnumerationOptions
        {
            AttributesToSkip = FileAttributes.None,
            IgnoreInaccessible = false,
            RecurseSubdirectories = false,
            ReturnSpecialDirectories = false,
        });
    }

    [LibraryImport("kernel32.dll", EntryPoint = "CreateDirectoryW", SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CreateDirectory(string path, IntPtr securityAttributes);

    /// <summary>Compares bounded file streams and captures provider-owned descendant evidence.</summary>
    internal static CopyVerificationOutcome Verify(FileEntrySnapshot source, FileSystemPath target, VerifiedTreeAccess access)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(access);
        if (source.TransferTarget is FileSystemPath expected &&
            !FileSystemPathIdentityComparer.Instance.Equals(expected, target))
        {
            return CopyVerificationOutcome.Failed(FileOperationFailureKind.Verification);
        }
        try
        {
            IReadOnlyList<VerifiedEntrySnapshot> sourceAncestors = access.Ancestors(source.Path);
            IReadOnlyList<VerifiedEntrySnapshot> targetAncestors = access.Ancestors(target);
            VerifiedCopyEntry root = VerifyEntry(source.Path, target, access);
            return source.Identity == root.Source.Identity &&
                access.MatchesAncestors(sourceAncestors) && access.MatchesAncestors(targetAncestors) &&
                VerifiedTreeEvidence.Matches(root, access)
                ? CopyVerificationOutcome.Succeeded(VerifiedCopyReceipt.Create(source, root,
                    VerifiedTreeBoundary.Create(sourceAncestors, targetAncestors)))
                : CopyVerificationOutcome.Failed(FileOperationFailureKind.Verification);
        }
        catch (UnauthorizedAccessException exception)
        {
            return CopyVerificationOutcome.Failed(WindowsLocalFileOperationAdapter.Normalize(
                exception.HResult, FileOperationFailureKind.Verification));
        }
        catch (WindowsDriveBoundaryRejectedException)
        {
            return CopyVerificationOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        catch (IOException exception)
        {
            return CopyVerificationOutcome.Failed(WindowsLocalFileOperationAdapter.Normalize(
                exception.HResult, FileOperationFailureKind.Verification));
        }
    }

    private static VerifiedCopyEntry VerifyEntry(FileSystemPath source, FileSystemPath target, VerifiedTreeAccess access)
    {
        VerifiedEntrySnapshot sourceBefore = access.Capture(source);
        VerifiedEntrySnapshot targetBefore = access.Capture(target);
        FileSystemInfo entry = access.Find(source);
        VerifiedCopyEntry result = entry is DirectoryInfo
            ? VerifyDirectory(sourceBefore, targetBefore, access)
            : access.Find(target) is FileInfo
                ? VerifiedCopyEntry.File(sourceBefore, targetBefore,
                    VerifiedFileContent.Compare(access.Resolve(source), access.Resolve(target)))
                : throw new IOException("The copied entry kind differs.");
        return access.Capture(source) != sourceBefore || access.Capture(target) != targetBefore
            ? throw new IOException("An entry changed during verification.") : result;
    }

    private static VerifiedCopyEntry VerifyDirectory(
        VerifiedEntrySnapshot source,
        VerifiedEntrySnapshot target,
        VerifiedTreeAccess access)
    {
        IReadOnlyList<FileSystemPath> sourceChildren = access.Children(source.Path);
        return VerifiedCopyEntry.Directory(source, target, VerifyChildren(sourceChildren, target.Path, access));
    }

    /// <summary>Applies the canonical target-name and exact membership admission to one complete source enumeration.</summary>
    internal static IReadOnlyList<VerifiedCopyEntry> VerifyChildren(
        IReadOnlyList<FileSystemPath> sourceChildren,
        FileSystemPath target,
        VerifiedTreeAccess access)
    {
        IReadOnlyList<FileSystemPath> targetChildren = access.Children(target);
        HashSet<FileSystemPath> targets = new(targetChildren, FileSystemPathIdentityComparer.Instance);
        List<VerifiedCopyEntry> children = [];
        foreach (FileSystemPath child in sourceChildren)
        {
            FileSystemPath childTarget = DeriveChild(target, access.Find(child).Name) ??
                throw new IOException("A copied child name is not representable.");
            if (!targets.Remove(childTarget))
            {
                throw new IOException("The copied directory entry set differs.");
            }
            children.Add(VerifyEntry(child, childTarget, access));
        }
        return targets.Count != 0 ? throw new IOException("The copied directory contains an unexpected entry.")
            : children.AsReadOnly();
    }
}
