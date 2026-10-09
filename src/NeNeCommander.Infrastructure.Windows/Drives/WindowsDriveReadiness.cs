namespace NeNeCommander.Infrastructure.Windows.Drives;

/// <summary>
/// Identifies whether the operating system reported a volume ready to be read. It is translated
/// from the framework flag once, at the enumerator, so the catalog decides on a closed value.
/// </summary>
internal abstract record WindowsDriveReadiness
{
    /// <summary>Gets the readiness of a volume whose media can be read now.</summary>
    internal static WindowsDriveReadiness Ready { get; } = new ReadyVolume();

    /// <summary>Gets the readiness of a volume without readable media, such as an empty card reader.</summary>
    internal static WindowsDriveReadiness NotReady { get; } = new NotReadyVolume();

    private WindowsDriveReadiness()
    {
    }

    private sealed record ReadyVolume : WindowsDriveReadiness;

    private sealed record NotReadyVolume : WindowsDriveReadiness;
}
