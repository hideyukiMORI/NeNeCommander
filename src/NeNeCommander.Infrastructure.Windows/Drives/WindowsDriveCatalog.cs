using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Drives;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.Drives;

/// <summary>
/// Lists the Windows volumes that have a drive-letter root through the shared Windows local I/O
/// execution boundary. A volume that is not ready stays listed as <see cref="DriveKind.Unknown"/>
/// without a label; a reported root that does not parse as a drive root is counted, not shown.
/// </summary>
public sealed class WindowsDriveCatalog : IDriveCatalog
{
    private readonly WindowsLocalIoExecutionBoundary _executionBoundary;
    private readonly IWindowsDriveEnumerator _enumerator;

    /// <summary>Initializes the catalog with the composed Windows local I/O execution boundary.</summary>
    /// <param name="executionBoundary">Shared boundary for synchronous Windows filesystem work.</param>
    public WindowsDriveCatalog(WindowsLocalIoExecutionBoundary executionBoundary)
        : this(executionBoundary, new WindowsDriveEnumerator())
    {
    }

    internal WindowsDriveCatalog(
        WindowsLocalIoExecutionBoundary executionBoundary,
        IWindowsDriveEnumerator enumerator)
    {
        ArgumentNullException.ThrowIfNull(executionBoundary);
        ArgumentNullException.ThrowIfNull(enumerator);
        _executionBoundary = executionBoundary;
        _enumerator = enumerator;
    }

    /// <inheritdoc />
    public Task<DriveCatalogOutcome> ListAsync(CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested
            ? Task.FromResult(DriveCatalogOutcome.Cancelled())
            : _executionBoundary.ExecuteAsync(List);
    }

    private DriveCatalogOutcome List()
    {
        IReadOnlyList<WindowsDriveSnapshot> snapshots;
        try
        {
            snapshots = _enumerator.Enumerate();
        }
        catch (IOException)
        {
            return DriveCatalogOutcome.Failed(DriveCatalogFailureKind.ProviderUnavailable);
        }
        catch (UnauthorizedAccessException)
        {
            return DriveCatalogOutcome.Failed(DriveCatalogFailureKind.AccessDenied);
        }
        List<DriveLocation> drives = [];
        HashSet<FileSystemPath> identities = new(FileSystemPathIdentityComparer.Instance);
        int unrepresentable = 0;
        foreach (WindowsDriveSnapshot snapshot in snapshots)
        {
            if (FileSystemPath.Parse(snapshot.Name) is not PathParseSuccess { Path: WindowsLocalPath { Parent: null } root })
            {
                unrepresentable++;
            }
            else if (identities.Add(root))
            {
                drives.Add(Describe(root, snapshot));
            }
        }
        return DriveCatalogOutcome.Succeeded(drives, unrepresentable);
    }

    private DriveLocation Describe(WindowsLocalPath root, WindowsDriveSnapshot snapshot)
    {
        return snapshot.Readiness == WindowsDriveReadiness.Ready
            ? DriveLocation.Create(root, Classify(snapshot.DriveType), ReadVolumeLabel(snapshot.Name))
            : DriveLocation.Create(root, DriveKind.Unknown, null);
    }

    /// <summary>
    /// Reads the label of a ready volume. A volume can be removed or locked between the readiness
    /// check and this read; the volume is still listed, only without a label.
    /// </summary>
    private string? ReadVolumeLabel(string rootName)
    {
        try
        {
            return _enumerator.ReadVolumeLabel(rootName);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    internal static DriveKind Classify(DriveType driveType)
    {
        return driveType switch
        {
            DriveType.Fixed => DriveKind.Fixed,
            DriveType.Removable => DriveKind.Removable,
            DriveType.Network => DriveKind.Network,
            DriveType.CDRom => DriveKind.Optical,
            DriveType.Ram or DriveType.NoRootDirectory or DriveType.Unknown => DriveKind.Unknown,
            _ => DriveKind.Unknown,
        };
    }
}
