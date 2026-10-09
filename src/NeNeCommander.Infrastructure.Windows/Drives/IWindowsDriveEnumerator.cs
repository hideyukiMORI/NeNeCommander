using System.Collections.Generic;

namespace NeNeCommander.Infrastructure.Windows.Drives;

/// <summary>
/// Reads the operating system's volume facts for the drive catalog. It is the sole seam over
/// <see cref="System.IO.DriveInfo"/>, so the catalog's classification and failure policy are
/// proved with deterministic fixtures instead of the volumes of the machine running the tests.
/// </summary>
internal interface IWindowsDriveEnumerator
{
    internal IReadOnlyList<WindowsDriveSnapshot> Enumerate();

    internal string ReadVolumeLabel(string rootName);
}
