namespace NeNeCommander.Application.Panes;

/// <summary>
/// Identifies one closed pane-state construction failure.
/// </summary>
public abstract record PaneStateFailureKind
{
    /// <summary>Gets the failure for a null visible item.</summary>
    public static PaneStateFailureKind NullItem { get; } = new NullItemFailure();

    /// <summary>Gets the failure for a duplicate visible path.</summary>
    public static PaneStateFailureKind DuplicateItem { get; } = new DuplicateItemFailure();

    private PaneStateFailureKind()
    {
    }

    private sealed record NullItemFailure : PaneStateFailureKind;
    private sealed record DuplicateItemFailure : PaneStateFailureKind;
}
