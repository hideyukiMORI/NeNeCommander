using System;
using NeNeCommander.Application.Drives;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Locations;

/// <summary>Represents one listed drive root in the Locations picker.</summary>
public sealed record DriveLocationItem : LocationItem
{
    internal DriveLocationItem(DriveLocation drive)
    {
        ArgumentNullException.ThrowIfNull(drive);
        Drive = drive;
    }

    /// <summary>Gets the listed volume.</summary>
    public DriveLocation Drive { get; }

    /// <inheritdoc />
    public override FileSystemPath Location => Drive.Root;
}
