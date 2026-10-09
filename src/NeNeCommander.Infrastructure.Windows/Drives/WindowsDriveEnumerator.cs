using System.Collections.Generic;
using System.IO;

namespace NeNeCommander.Infrastructure.Windows.Drives;

/// <summary>
/// Translates <see cref="DriveInfo"/> into frozen volume facts. It decides nothing: classification,
/// readiness policy, root validation, and failure normalization belong to the catalog.
/// </summary>
internal sealed class WindowsDriveEnumerator : IWindowsDriveEnumerator
{
    IReadOnlyList<WindowsDriveSnapshot> IWindowsDriveEnumerator.Enumerate()
    {
        List<WindowsDriveSnapshot> drives = [];
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            drives.Add(new WindowsDriveSnapshot(
                drive.Name,
                drive.DriveType,
                drive.IsReady ? WindowsDriveReadiness.Ready : WindowsDriveReadiness.NotReady));
        }
        return drives.AsReadOnly();
    }

    string IWindowsDriveEnumerator.ReadVolumeLabel(string rootName)
    {
        return new DriveInfo(rootName).VolumeLabel;
    }
}
