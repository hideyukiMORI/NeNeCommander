namespace NeNeCommander.Application.Settings;

/// <summary>
/// Identifies one closed reason why persisted text named no approved color scheme.
/// </summary>
public abstract record ColorSchemeFailureKind
{
    /// <summary>Gets the failure for text that is not present at all.</summary>
    public static ColorSchemeFailureKind Absent { get; } = new AbsentFailure();

    /// <summary>Gets the failure for text that names no approved scheme.</summary>
    public static ColorSchemeFailureKind Unknown { get; } = new UnknownFailure();

    private ColorSchemeFailureKind()
    {
    }

    private sealed record AbsentFailure : ColorSchemeFailureKind;
    private sealed record UnknownFailure : ColorSchemeFailureKind;
}
