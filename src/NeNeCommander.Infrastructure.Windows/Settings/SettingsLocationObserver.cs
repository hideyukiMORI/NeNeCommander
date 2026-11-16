using System;
using System.Collections.Generic;
using System.IO;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>
/// Observes and revalidates the ancestor chain of the settings document. Every observation
/// re-admits the document path through the drive boundary between filesystem probes.
/// </summary>
internal sealed class SettingsLocationObserver
{
    private readonly WindowsLocalPath _documentPath;
    private readonly WindowsDriveBoundary _driveBoundary;

    internal SettingsLocationObserver(WindowsLocalPath documentPath, WindowsDriveBoundary driveBoundary)
    {
        _documentPath = documentPath;
        _driveBoundary = driveBoundary;
    }

    /// <summary>Captures every ancestor as a safe chain, or the failure that blocks writing.</summary>
    internal SettingsLocationSnapshot Capture()
    {
        if (!AdmitsDocument())
        {
            return new BlockedLocation(SettingsWriteFailureKind.UnsafeLocation);
        }
        try
        {
            return CaptureAncestors();
        }
        catch (UnauthorizedAccessException)
        {
            return new BlockedLocation(SettingsWriteFailureKind.Unauthorized);
        }
        catch (IOException)
        {
            return new BlockedLocation(SettingsWriteFailureKind.IoFailure);
        }
    }

    /// <summary>Reports whether every ancestor still matches the expected safe chain.</summary>
    internal bool Matches(SettingsLocationSnapshot expected)
    {
        if (!AdmitsDocument() || expected is not SafeLocation safe)
        {
            return false;
        }
        foreach (DirectoryBaseline baseline in safe.Directories)
        {
            if (!DirectoryMatches(baseline))
            {
                return false;
            }
        }
        return AdmitsDocument();
    }

    /// <summary>Revalidates the approved chain after publish and returns the next location baseline.</summary>
    internal SettingsLocationSnapshot ConfirmPublished(SafeLocation approved)
    {
        try
        {
            return Matches(approved)
                ? approved
                : new BlockedLocation(SettingsWriteFailureKind.UnsafeLocation);
        }
        catch (UnauthorizedAccessException)
        {
            return new BlockedLocation(SettingsWriteFailureKind.Unauthorized);
        }
        catch (IOException)
        {
            return new BlockedLocation(SettingsWriteFailureKind.IoFailure);
        }
    }

    internal static bool RequiresDirectoryCreation(SafeLocation location)
    {
        return location.Directories.Count > 0 && location.Directories[0] is AbsentDirectory;
    }

    internal static bool AfterDirectoryCreationMatches(
        SafeLocation expected,
        SafeLocation created)
    {
        if (expected.Directories.Count != created.Directories.Count)
        {
            return false;
        }
        for (int index = 0; index < expected.Directories.Count; index++)
        {
            DirectoryBaseline before = expected.Directories[index];
            DirectoryBaseline after = created.Directories[index];
            if (!FileSystemPathIdentityComparer.Instance.Equals(before.Path, after.Path) ||
                after is not PresentDirectory presentAfter)
            {
                return false;
            }
            if (before is PresentDirectory presentBefore &&
                !presentBefore.Identifier.Equals(presentAfter.Identifier, StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }

    internal static bool SnapshotsMatch(SafeLocation expected, SafeLocation current)
    {
        if (expected.Directories.Count != current.Directories.Count)
        {
            return false;
        }
        for (int index = 0; index < expected.Directories.Count; index++)
        {
            DirectoryBaseline before = expected.Directories[index];
            DirectoryBaseline after = current.Directories[index];
            if (!FileSystemPathIdentityComparer.Instance.Equals(before.Path, after.Path) ||
                before is not PresentDirectory presentBefore ||
                after is not PresentDirectory presentAfter ||
                !presentBefore.Identifier.Equals(presentAfter.Identifier, StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }

    private SettingsLocationSnapshot CaptureAncestors()
    {
        List<DirectoryBaseline> directories = [];
        FileSystemPath? candidate = _documentPath.Parent;
        while (candidate is WindowsLocalPath local)
        {
            DirectoryBaseline? directory = CaptureAncestor(local);
            if (directory is null)
            {
                return new BlockedLocation(SettingsWriteFailureKind.UnsafeLocation);
            }
            directories.Add(directory);
            candidate = local.Parent;
        }
        return candidate is not null || !AdmitsDocument()
            ? new BlockedLocation(SettingsWriteFailureKind.UnsafeLocation)
            : new SafeLocation(directories.AsReadOnly());
    }

    /// <summary>Observes one ancestor; absence of a baseline means the ancestor cannot be admitted.</summary>
    private DirectoryBaseline? CaptureAncestor(WindowsLocalPath local)
    {
        if (!AdmitsDocument())
        {
            return null;
        }
        FileSystemInfo? entry = WindowsLocalEntryIdentity.Find(local);
        return entry is null
            ? new AbsentDirectory(local)
            : entry is DirectoryInfo directory &&
                (directory.Attributes & FileAttributes.ReparsePoint) == 0 &&
                AdmitsDocument()
                ? new PresentDirectory(local, WindowsFileIdentifier.Describe(directory.FullName))
                : null;
    }

    private bool DirectoryMatches(DirectoryBaseline baseline)
    {
        if (!AdmitsDocument())
        {
            return false;
        }
        FileSystemInfo? entry = WindowsLocalEntryIdentity.Find(baseline.Path);
        return baseline is AbsentDirectory
            ? entry is null
            : baseline is PresentDirectory present &&
                entry is DirectoryInfo directory &&
                (directory.Attributes & FileAttributes.ReparsePoint) == 0 &&
                AdmitsDocument() &&
                WindowsFileIdentifier.Describe(directory.FullName).Equals(
                    present.Identifier,
                    StringComparison.Ordinal);
    }

    private bool AdmitsDocument()
    {
        return _driveBoundary.AdmitsLocal(_documentPath);
    }
}
