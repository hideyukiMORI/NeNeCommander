namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Identifies the closed status a pane shows for its most recent listing attempt.
/// Each status names a localization resource; no user-facing text is assembled in code.
/// </summary>
public sealed record PaneStatus
{
    /// <summary>Gets the status for a listing that contains every representable entry.</summary>
    public static PaneStatus Complete { get; } = new("PaneStatusComplete");

    /// <summary>Gets the status for a listing that stopped at its entry boundary.</summary>
    public static PaneStatus Bounded { get; } = new("PaneStatusBounded");

    /// <summary>Gets the status for a complete listing that omitted unrepresentable entries.</summary>
    public static PaneStatus EntriesOmitted { get; } = new("PaneStatusEntriesOmitted");

    /// <summary>Gets the status for a provider that denied access to the location.</summary>
    public static PaneStatus AccessDenied { get; } = new("PaneStatusAccessDenied");

    /// <summary>Gets the status for a location that does not exist as a directory.</summary>
    public static PaneStatus NotFound { get; } = new("PaneStatusNotFound");

    /// <summary>Gets the status for every other normalized provider failure.</summary>
    public static PaneStatus ProviderUnavailable { get; } = new("PaneStatusProviderUnavailable");

    /// <summary>Gets the status for a read stopped by cancellation.</summary>
    public static PaneStatus Cancelled { get; } = new("PaneStatusCancelled");

    private PaneStatus(string resourceKey)
    {
        ResourceKey = resourceKey;
    }

    /// <summary>Gets the localization resource key that names this status.</summary>
    public string ResourceKey { get; }
}
