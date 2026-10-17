using System;
using System.IO;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Holds the owning provider's path resolution and fresh admission for one tree copy.</summary>
internal sealed class WindowsTreeCopyAccess
{
    private readonly Func<FileSystemPath, string> _resolveTarget;
    private readonly Func<FileSystemInfo, bool> _inspectSource;
    private readonly WindowsDriveBoundary _driveBoundary;

    internal WindowsTreeCopyAccess(
        Func<FileSystemPath, string> resolveTarget,
        Func<FileSystemInfo, bool> inspectSource,
        WindowsDriveBoundary driveBoundary)
    {
        ArgumentNullException.ThrowIfNull(resolveTarget);
        ArgumentNullException.ThrowIfNull(inspectSource);
        ArgumentNullException.ThrowIfNull(driveBoundary);
        _resolveTarget = resolveTarget;
        _inspectSource = inspectSource;
        _driveBoundary = driveBoundary;
    }

    internal bool InspectSource(FileSystemInfo source)
    {
        return _inspectSource(source);
    }

    internal bool AdmitsTarget(FileSystemPath target)
    {
        return target is not WindowsLocalPath local || _driveBoundary.AdmitsLocal(local);
    }

    internal string ResolveTarget(FileSystemPath target)
    {
        return _resolveTarget(target);
    }
}
