namespace NeNeCommander.Application.Drives;

/// <summary>
/// Identifies the closed kind of one listed volume. A volume that is not ready, or whose provider
/// type is not one of the named kinds, is <see cref="Unknown"/>; it is still listed.
/// </summary>
public abstract record DriveKind
{
    /// <summary>Gets the kind of a fixed local disk.</summary>
    public static DriveKind Fixed { get; } = new FixedKind();

    /// <summary>Gets the kind of removable media such as a USB drive or card reader.</summary>
    public static DriveKind Removable { get; } = new RemovableKind();

    /// <summary>Gets the kind of a network share mapped to a drive letter.</summary>
    public static DriveKind Network { get; } = new NetworkKind();

    /// <summary>Gets the kind of an optical drive.</summary>
    public static DriveKind Optical { get; } = new OpticalKind();

    /// <summary>Gets the kind of a volume that is not ready or reports no named kind.</summary>
    public static DriveKind Unknown { get; } = new UnknownKind();

    private DriveKind()
    {
    }

    private sealed record FixedKind : DriveKind;

    private sealed record RemovableKind : DriveKind;

    private sealed record NetworkKind : DriveKind;

    private sealed record OpticalKind : DriveKind;

    private sealed record UnknownKind : DriveKind;
}
