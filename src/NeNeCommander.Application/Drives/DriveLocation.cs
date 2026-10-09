using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Drives;

/// <summary>
/// Represents one listed volume: its validated drive root, its closed kind, and the label the
/// volume reported. The root is always a provider root, so navigating to it never leaves the volume.
/// </summary>
public sealed record DriveLocation
{
    private DriveLocation(WindowsLocalPath root, DriveKind kind, string? volumeLabel)
    {
        Root = root;
        Kind = kind;
        VolumeLabel = volumeLabel;
    }

    /// <summary>Gets the validated drive root.</summary>
    public WindowsLocalPath Root { get; }

    /// <summary>Gets the closed volume kind.</summary>
    public DriveKind Kind { get; }

    /// <summary>Gets the reported volume label, or absence when the volume reported none.</summary>
    public string? VolumeLabel { get; }

    /// <summary>Creates one listed volume from validated components.</summary>
    /// <param name="root">Validated drive root; a path below a root is rejected.</param>
    /// <param name="kind">Closed volume kind.</param>
    /// <param name="volumeLabel">Reported label; empty text means the volume reported none.</param>
    /// <returns>The listed volume.</returns>
    public static DriveLocation Create(WindowsLocalPath root, DriveKind kind, string? volumeLabel)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(kind);
        return root.Parent is not null
            ? throw new ArgumentException("A drive location must be a drive root.", nameof(root))
            : new DriveLocation(root, kind, string.IsNullOrEmpty(volumeLabel) ? null : volumeLabel);
    }
}
