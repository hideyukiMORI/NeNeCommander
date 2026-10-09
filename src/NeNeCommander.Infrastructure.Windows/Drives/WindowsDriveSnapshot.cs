using System;
using System.IO;

namespace NeNeCommander.Infrastructure.Windows.Drives;

/// <summary>Freezes the provider facts of one reported volume needed to list it.</summary>
internal sealed record WindowsDriveSnapshot
{
    internal WindowsDriveSnapshot(string name, DriveType driveType, WindowsDriveReadiness readiness)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(readiness);
        Name = name;
        DriveType = driveType;
        Readiness = readiness;
    }

    internal string Name { get; }

    internal DriveType DriveType { get; }

    internal WindowsDriveReadiness Readiness { get; }
}
